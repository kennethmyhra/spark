using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Hl7.Fhir.Utility;
using Npgsql;
using Spark.Engine.Core;
using System;
using System.Net;
using System.Threading.Tasks;

namespace Spark.PostgreSql
{
    public static class NpgsqlDataReaderExtensions
    {
        public static async Task<Entry> ToEntryAsync(this NpgsqlDataReader reader)
        {
            if (reader == null) return null;

            try
            {
                var entry = reader.ExtractMetadata();
                if (entry.IsPresent)
                {
                    entry.Resource = await reader.ParseResourceAsync();
                }

                return entry;
            }
            catch (Exception e)
            {
                throw new SparkException(HttpStatusCode.InternalServerError, $"Unexpected error. Message: {e.Message}. Stack Trace: {e.StackTrace}");
            }
        }

        internal static Entry ExtractMetadata(this NpgsqlDataReader reader)
        {
            var httpMethod = EnumUtility.ParseLiteral<Bundle.HTTPVerb>(reader.GetString(reader.GetOrdinal(FieldName.HttpMethod)));
            var key = reader.GetKey();
            var timestamp = reader.GetDateTime(reader.GetOrdinal(FieldName.Timestamp));

            return Entry.Create(httpMethod.HasValue ? httpMethod.Value : Bundle.HTTPVerb.POST, key, timestamp);
        }

        internal static IKey GetKey(this NpgsqlDataReader reader)
        {
            return new Key
            {
                TypeName = reader.GetString(reader.GetOrdinal(FieldName.TypeName)),
                ResourceId = reader.GetString(reader.GetOrdinal(FieldName.Id)),
                VersionId = reader.GetString(reader.GetOrdinal(FieldName.VersionId)),
            };
        }

        internal static async Task<Resource> ParseResourceAsync(this NpgsqlDataReader reader)
        {
            var jsonAsString = reader.GetString(reader.GetOrdinal(FieldName.Content));
            // FIXME: ParserSettings set in Configuration needs to somehow be propogated here.
            var parser = new FhirJsonParser();
            return await parser.ParseAsync<Resource>(jsonAsString);
        }
    }
}