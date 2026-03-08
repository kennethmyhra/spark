# Spark FHIR Server - Copilot Instructions

## Overview

Spark is a C# FHIR (Fast Healthcare Interoperability Resources) server supporting multiple FHIR versions (DSTU2, STU3, R4) through separate branches. The project uses MongoDB for storage and ASP.NET Core for the web layer.

## Build, Test, and Lint

### Building

```bash
# Build the entire solution
dotnet build

# Build a specific project
dotnet build src/Spark.Engine/Spark.Engine.csproj
dotnet build src/Spark.Web/Spark.Web.csproj
```

### Running Tests

```bash
# Run all unit tests
dotnet test src/Spark.Engine.Test/Spark.Engine.Test.csproj
dotnet test src/Spark.Mongo.Tests/Spark.Mongo.Tests.csproj
dotnet test src/Spark.Web.Tests/Spark.Web.Tests.csproj

# Run a single test
dotnet test src/Spark.Engine.Test/Spark.Engine.Test.csproj --filter "TestName"
```

### Integration Tests

Integration tests run in Docker containers using the FHIR test plan executor:

```bash
cd tests/integration-tests

# Start Spark and MongoDB containers
docker compose up -d spark

# Run all integration tests
docker compose run --rm --no-deps plan_executor ./execute_all.sh 'http://spark:8080/fhir' r4 'html|json|stdout'

# Capture backend logs
docker compose logs spark > logs/backend.log

# Combine test results into annotations format
./combine-test-results.sh json_results annotations.json

# Display combined results
cat annotations.json

# Cleanup
docker compose down
```

#### Apple Silicon (ARM64) Support

The `plan_executor` image only supports AMD64. On Apple Silicon Macs, Docker will automatically use Rosetta emulation after pulling the AMD64 image:

```bash
cd tests/integration-tests

# Pull the AMD64 image explicitly (one-time setup)
docker pull --platform linux/amd64 incendi/plan_executor:latest

# Then run tests normally - Docker will use emulation
docker compose up -d spark
docker compose run --rm --no-deps plan_executor ./execute_all.sh 'http://spark:8080/fhir' r4 'html|json|stdout'
docker compose logs spark > logs/backend.log
./combine-test-results.sh json_results annotations.json
cat annotations.json
docker compose down
```

#### Test Results

After running tests, results are available in multiple formats:

- **JSON summary**: `json_results/_summary_*.json` - Contains pass/fail/skip counts
- **HTML reports**: `html_summaries/*.html` - Detailed test reports for each test suite
- **Raw JSON**: `json_results/*.json` - Machine-readable test data per suite
- **Annotations**: `annotations.json` - Combined results for CI/CD integration
- **Backend logs**: `logs/backend.log` - Spark server logs during test execution

Expected results (R4 version):
- **3,200+ tests pass** (exact number varies by version)
- **400+ tests skipped** (known limitations/TODOs)
- **0 failures/errors** in a healthy build

### Frontend Build

The Spark.Web project includes a Node.js-based frontend build system:

```bash
cd src/Spark.Web/ClientApp
npm install
npm run build:dev  # Development build with source maps
npm run build      # Production build (minified)
```

The MSBuild process automatically runs `npm run build` during `dotnet build`.

## Project Structure

### Core Projects

- **Spark.Engine**: Core FHIR engine handling REST operations, search, indexing, and service layer
- **Spark.Engine.R4**: R4 version-specific engine (used in r4/master branch)
- **Spark.Mongo**: MongoDB storage implementation including index management and search
- **Spark.Mongo.R4**: R4 version-specific MongoDB store
- **Spark.Web**: ASP.NET Core web application with controllers, SignalR hubs, and admin interface

### Test Projects

- **Spark.Engine.Test**: Unit tests for core engine
- **Spark.Mongo.Tests**: Unit tests for MongoDB implementation
- **Spark.Web.Tests**: Unit tests for web layer
- **tests/integration-tests**: Docker-based integration tests

### Version Strategy

- **master**: DSTU2 (no longer maintained)
- **stu3/master**: STU3 version
- **r4/master**: R4 version (current development focus)

Branch from the appropriate version branch for features/fixes specific to that FHIR version.

## Architecture

### Service Layer Pattern

The engine uses a service extension pattern where `IFhirService` is extended with specialized services:

- `ISearchService`: Query and search operations
- `IResourceStorageService`: CRUD operations on resources
- `IHistoryService`: Version history management
- `ITransactionService`: Bundle transactions and batch operations
- `IPatchService`: FHIR Patch operations
- `IIndexService` / `IIndexRebuildService`: Search index management

Extensions are registered via `FhirServiceDictionary` and resolved at runtime.

### Storage Abstraction

Storage is abstracted through interfaces:

- `IFhirStore`: Core resource storage
- `IHistoryStore`: Version history
- `ISnapshotStore`: Search result snapshots for pagination
- `IIndexStore`: Search indices
- `IFhirStoreAdministration`: Administrative operations (clear, rebuild)

MongoDB implementation is in `Spark.Mongo` and can be replaced with other stores.

### Request Processing Flow

1. Request enters via `FhirController` (REST API) or `ResourcesController` (MVC)
2. Controller calls `IFhirService` methods
3. Service delegates to appropriate extension (SearchService, ResourceStorageService, etc.)
4. Extension uses `IFhirStore` and `IIndexStore` for persistence
5. Response is formatted via `FhirResponseFactory` and returned

### Search Implementation

Search is implemented in MongoDB using BSON documents:

- `ElementIndexer`: Extracts searchable values from FHIR resources
- `MongoIndexMapper`: Maps search parameters to BSON structure
- `MongoSearcher`: Builds and executes MongoDB queries from search parameters
- `MongoIndexStore`: Persists and retrieves search indices

## Key Conventions

### Namespace Organization

- All code uses `namespace Spark.*` (file-scoped namespaces)
- Engine code: `Spark.Engine.*`
- MongoDB code: `Spark.Mongo.*`
- Web code: `Spark.Web.*`

### Configuration

Configuration is loaded from `appsettings.json` sections:

- `SparkSettings`: Base URL endpoint, FHIR version
- `StoreSettings`: MongoDB connection string (`mongodb://host:port/database`)
- `ExamplesSettings`: Configuration for example data import

Example MongoDB connection string:
```
mongodb://localhost:27017/spark
mongodb://root:password@mongodb:27017/spark?authSource=admin
```

### Dependency Injection Setup

Standard pattern for setting up a Spark server:

```csharp
// In ConfigureServices
services.AddFhir(new SparkSettings { Endpoint = new Uri("...") });
services.AddMongoFhirStore(new StoreSettings { ConnectionString = "mongodb://..." });

// In Configure
app.UseFhir(r => r.MapRoute(name: "default", template: "{controller}/{id?}"));
```

### Target Frameworks

Projects target multiple .NET versions: `net8.0`, `net9.0`, `net10.0`

### Code Style

- 4 spaces for indentation (no tabs)
- LF line endings
- `TreatWarningsAsError` is enabled - warnings must be fixed
- File header template with copyright notice (BSD-3-Clause license)

### Error Handling

- Custom exceptions inherit from `SparkException`
- `OperationOutcome` is used for FHIR-compliant error responses
- Extensions like `OperationOutcomeExtensions.AddError()` build structured errors

### FHIR Resource Keys

The `Key` class represents FHIR resource identifiers with components:

- `Base`: Server base URL
- `TypeName`: Resource type (Patient, Observation, etc.)
- `ResourceId`: Resource ID
- `VersionId`: Optional version ID

Keys are validated and normalized throughout the codebase.

### Testing Patterns

- Use xUnit for unit tests
- Test classes typically end with `Tests` (e.g., `ElementIndexerTests`)
- Integration tests use Docker Compose with MongoDB and the Spark server
- Test fixtures often use `TestHelper` or similar utility classes

## Common Tasks

### Adding a New Search Parameter

1. Define the search parameter in the FHIR spec conformance statement
2. Update `ElementIndexer` to extract values for the new parameter
3. Add mapping in `MongoIndexMapper` for BSON representation
4. Rebuild the search index after deployment

### Adding a New FHIR Operation

1. Create an extension service implementing the operation logic
2. Register the extension in the DI container
3. Add controller endpoint in `FhirController` or create a custom controller
4. Add operation to the capability statement via `CapabilityStatementBuilder`

### Debugging SignalR Issues

The admin maintenance page uses SignalR for real-time progress updates:

1. Check browser console for "SignalR Connected" message
2. Verify `/assets/js/signalr.js` bundle is loaded (should be ~150-200 KB)
3. Check `MaintenanceHub` server-side logs for connection errors
4. Ensure `app.UseSignalR()` is configured in `Startup.cs`

### Backporting Changes to Other Branches

When a PR is merged into one branch and needs to be applied to other branches (e.g., `r4/master` → `stu3/master`, `v2-r4/master`, `v2-stu3/master`), use cherry-pick — **never merge**.

**Process:**

1. Find the commits to backport (get their SHAs from the merged PR commits, ordered oldest → newest):
   ```bash
   # List commits in a branch to find the relevant SHAs
   git log origin/<target-branch>..origin/r4/master --oneline
   ```

2. Fetch the latest target branches:
   ```bash
   git fetch origin <branch1> <branch2> ...
   ```

3. For each target branch, create a feature branch, cherry-pick, and push to `dev`:
   ```bash
   git checkout -b backport/pr-<PR_NUMBER>-to-<target-branch-name> origin/<target-branch>
   git cherry-pick <sha1> <sha2> <sha3> ...
   git push dev backport/pr-<PR_NUMBER>-to-<target-branch-name>
   ```

**Branch naming convention:** `backport/pr-<PR_NUMBER>-to-<target-branch>` where slashes in branch names are replaced with dashes (e.g., `stu3/master` → `stu3-master`).

**Remotes:**
- `origin` — `git@github.com:FirelyTeam/spark.git` (upstream)
- `dev` — `git@github.com:kennethmyhra/spark.git` (push feature branches here; PRs are created manually)
