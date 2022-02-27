using Hl7.Fhir.Model;
using Spark.Engine.Core;
using System;
using System.Collections.Generic;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace Spark.PostgreSql.Tests
{
    public class PostgreSqlStoreTests
    {
        [Fact]
        public async Task AddResourceToStoreWithPutHttpMethod()
        {
            var id = "example";
            var typeName = "Patient";
            var store = new PostgreSqlStore("Host=localhost;Username=postgres;Password=84f-Pr-e;Database=incendi_fhir");
            var entry = await store.GetAsync(Key.Create(typeName, id));
            Resource resource;

            if(entry == null || entry is { Resource: null })
            {
                resource = new Patient
                {
                    Id = "example",
                    Meta = new Meta { VersionId = "1" },
                    Name = new List<HumanName>
                    {
                        new HumanName { Given = new []{ "Kenneth" }, Family = "Myhra" }
                    }
                };
            }
            else
            {
                resource = entry.Resource;
                var versionId = int.Parse(resource.Meta.VersionId);
                resource.Meta.VersionId = (++versionId).ToString();
            }

            var key = Key.Create(resource.TypeName, resource.Id, resource?.Meta?.VersionId);
            var entryToStore = Entry.Create(Bundle.HTTPVerb.PUT, key, resource, DateTimeOffset.UtcNow);

            await store.AddAsync(entryToStore);
        }

        [Fact]
        public async Task AddResourceToStoreWithDeleteHttpMethod()
        {
            var id = "example";
            var typeName = "Patient";
            var store = new PostgreSqlStore("Host=localhost;Username=postgres;Password=84f-Pr-e;Database=incendi_fhir");
            var entry = await store.GetAsync(Key.Create(typeName, id));
            if (entry?.Resource == null)
                throw new InvalidOperationException("Test 'AddResourceToStoreWithDeleteHttpMethod' requires an existing resource to perform the delete operation");

            var versionId = int.Parse(entry.Resource.Meta.VersionId);

            var entryToStore = Entry.Create(Bundle.HTTPVerb.DELETE, Key.Create(typeName, id, (++versionId).ToString()), DateTimeOffset.UtcNow);
            await store.AddAsync(entryToStore);
        }
    }
}