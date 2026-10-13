#!/bin/bash
#
# Loads synthetic patients into Spark and runs a set of Patient searches, for one store.
#
# Usage: Tests/Performance/run-patients.sh mongo|postgres
#
# Environment:
#   COUNT     number of patients to load (default 100000)
#   CLINICAL  also load an encounter and three vital sign observations per patient (default true)
#   VUS       concurrent clients while loading (default 50)
#   RESULTS   directory for the results (default Tests/Performance/results/<store>)
#
# Requires Docker and the .NET SDK. The database runs in Docker with 4 CPUs and 4 GB of memory, Spark runs on the
# host from a Release build, and k6 runs in Docker. Run it from the root of the repository.

set -euo pipefail

STORE=${1:?Usage: $0 mongo|postgres}
COUNT=${COUNT:-100000}
VUS=${VUS:-50}
ROOT=$(pwd)
SCRIPTS=$ROOT/Tests/Performance/k6
RESULTS=${RESULTS:-$ROOT/Tests/Performance/results/$STORE}
PORT=5590
BASE_URL=http://host.docker.internal:$PORT/fhir

mkdir -p "$RESULTS"

stop_spark() {
  local pid
  pid=$(lsof -ti tcp:$PORT || true)
  [ -n "$pid" ] && kill $pid || true
}

stop_spark
docker rm -f perf-pg perf-mongo >/dev/null 2>&1 || true

if [ "$STORE" = mongo ]; then
  # Work around https://jira.mongodb.org/browse/SERVER-121912 on affected Linux kernels.
  docker run -d --name perf-mongo --cpus=4 --memory=4g -p 57017:27017 -e GLIBC_TUNABLES=glibc.pthread.rseq=1 mongo:8 >/dev/null
  until docker exec perf-mongo mongosh --quiet --eval 'db.runCommand({ping:1}).ok' 2>/dev/null | grep -q 1; do sleep 2; done
  # The indexes Spark ships with (see MongoStoreAdministration.EnsureIndicesAsync), plus two that the write path
  # needs: without them every update scans the resources collection.
  docker exec perf-mongo mongosh --quiet spark --eval '
    db.resources.createIndex({"@state":1,"@method":1,"@typename":1});
    db.resources.createIndex({"_id":1,"@state":1});
    db.resources.createIndex({"@when":-1,"@typename":1});
    db.resources.createIndex({"@REFERENCE":1,"@state":1});
    db.resources.createIndex({"@typename":1,"id":1,"@state":1});
    db.searchindex.createIndex({"internal_id":1},{unique:true,sparse:true});
    // Spark ships no indexes on the search fields, so every search scans the whole searchindex collection.
    // These cover the searches this test runs, the way a tuned deployment would index for its own queries.
    // Token and quantity values are arrays searched with $elemMatch, so code and system, or unit and value, of the
    // same value share one index. Two such fields cannot share an index, as MongoDB does not index parallel arrays.
    // The search field comes first: every search filters on internal_resource, so an index that starts with it is a
    // candidate for every search, and the planner then often picks an index on a field the search does not use.
    // Dates index both start and end, so that the overlap test on a period is checked in the index.
    db.searchindex.createIndex({"identifier.code":1,"identifier.system":1,"internal_resource":1});
    db.searchindex.createIndex({"fhir_id.code":1,"internal_resource":1});
    db.searchindex.createIndex({"family":1,"internal_resource":1});
    db.searchindex.createIndex({"given":1,"internal_resource":1});
    db.searchindex.createIndex({"birthdate.start":1,"birthdate.end":1,"internal_resource":1});
    db.searchindex.createIndex({"gender.code":1,"internal_resource":1});
    db.searchindex.createIndex({"code.code":1,"code.system":1,"internal_resource":1});
    db.searchindex.createIndex({"subject":1,"internal_resource":1});
    db.searchindex.createIndex({"value-quantity.unit":1,"value-quantity.value":1,"internal_resource":1});
    db.searchindex.createIndex({"date.start":1,"date.end":1,"internal_resource":1});' >/dev/null
  PROVIDER=MongoDB
  CONNECTION='mongodb://localhost:57017/spark'
elif [ "$STORE" = postgres ]; then
  docker run -d --name perf-pg --cpus=4 --memory=4g --shm-size=1g -e POSTGRES_PASSWORD=secret -e POSTGRES_DB=spark -p 55433:5432 \
    postgres:18-alpine -c shared_buffers=1GB -c effective_cache_size=3GB -c max_connections=200 >/dev/null
  until docker exec perf-pg pg_isready -U postgres -d spark >/dev/null 2>&1; do sleep 1; done
  sleep 2
  PROVIDER=PostgreSQL
  CONNECTION='Host=localhost;Port=55433;Database=spark;Username=postgres;Password=secret'
else
  echo "Unknown store '$STORE', expected mongo or postgres." >&2
  exit 1
fi

dotnet build Applications/Spark.Web.R4 -c Release -v q
StoreSettings__Provider=$PROVIDER StoreSettings__ConnectionString="$CONNECTION" \
  SparkSettings__Endpoint=$BASE_URL ASPNETCORE_URLS=http://0.0.0.0:$PORT Logging__LogLevel__Default=Warning \
  nohup dotnet run -c Release --no-build --project Applications/Spark.Web.R4 --no-launch-profile > "$RESULTS/spark.log" 2>&1 &
trap stop_spark EXIT
until curl -sf -o /dev/null http://localhost:$PORT/fhir/metadata; do sleep 2; done

# Usage: k6 <script> [extra docker arguments, such as -e QUERY=identifier]
k6() {
  local script=$1
  shift
  docker run --rm --add-host=host.docker.internal:host-gateway -v "$SCRIPTS":/scripts:ro \
    -e BASE_URL=$BASE_URL -e COUNT=$COUNT -e VUS=$VUS "$@" \
    grafana/k6 run --quiet --summary-trend-stats "avg,med,p(95),p(99),max" "/scripts/$script"
}

start=$(date +%s)
k6 load-patients.js > "$RESULTS/load.txt" 2>&1 || true
end=$(date +%s)

{
  echo "== load $COUNT patients in $((end - start)) seconds"
  grep -E "checks_succeeded|http_req_duration|http_reqs" "$RESULTS/load.txt" || true
  if [ "$STORE" = mongo ]; then
    docker exec perf-mongo mongosh --quiet spark --eval \
      'const s=db.stats(1024*1024); print("database size MB", (s.storageSize+s.indexSize).toFixed(0), "resources", db.resources.countDocuments())'
  else
    docker exec perf-pg psql -U postgres -d spark -At -c \
      "select 'database size MB ' || (pg_database_size('spark')/1024/1024) || ' resources ' || (select count(*) from resources)"
  fi
} > "$RESULTS/summary.txt"

for query in identifier birthdate family given_birthyear gender_count \
             observation_code observation_value observation_patient observation_chain encounter_date; do
  k6 search-patients.js -e QUERY=$query > "$RESULTS/search-$query.txt" 2>&1 || true
  {
    echo "== $query"
    grep -E "checks_succeeded|http_req_duration|http_reqs" "$RESULTS/search-$query.txt" || true
  } >> "$RESULTS/summary.txt"
done

cat "$RESULTS/summary.txt"
