using Hl7.Fhir.Serialization;
using Hl7.Fhir.Utility;
using Npgsql;
using Npgsql.Internal;
using NpgsqlTypes;
using Spark.Engine.Core;
using Spark.Engine.Store.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Spark.PostgreSql
{
    public class PostgreSqlStore : IFhirStore
    {
        private readonly string _connectionString;

        public PostgreSqlStore(string connectionString)
        {
            _connectionString = connectionString;
        }

        internal async Task DeactivatePreviousVersionAsync(IKey key, NpgsqlConnection connection)
        {
            using var command = new NpgsqlCommand($"UPDATE resources SET {FieldName.Active}=FALSE WHERE {FieldName.Id}=@id AND {FieldName.TypeName}=@type_name AND {FieldName.Active}=TRUE", connection);
            command.Parameters.AddWithValue("id", key.ResourceId);
            command.Parameters.AddWithValue("type_name", key.TypeName);
            await command.ExecuteNonQueryAsync();
        }

        public async Task AddAsync(Entry entry)
        {

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            await using var transaction = connection.BeginTransaction();

            try
            {
                await DeactivatePreviousVersionAsync(entry.Key, connection);

                using var command = new NpgsqlCommand("INSERT INTO resources ( id, type_name, http_method, active, timestamp, content, version_id ) VALUES ( @id, @type_name, @http_method, @active, @timestamp, @content, @version_id ) ", connection);
                command.Parameters.AddWithValue("id", entry.Key.ResourceId);
                command.Parameters.AddWithValue("type_name", entry.Key.TypeName);
                command.Parameters.AddWithValue("http_method", entry.Method.GetLiteral());
                command.Parameters.AddWithValue("active", true);
                command.Parameters.AddWithValue("timestamp", entry.When);

                if (entry.Resource == null)
                    command.Parameters.AddWithValue("content", DBNull.Value);
                else
                    command.Parameters.AddWithValue("content", NpgsqlDbType.Json, entry.Resource.ToJson());

                if (entry.Key.VersionId == null)
                    command.Parameters.AddWithValue("version_id", DBNull.Value);
                else
                    command.Parameters.AddWithValue("version_id", entry.Key.VersionId);

                await command.ExecuteNonQueryAsync();
            }
            finally
            {
                await transaction.CommitAsync();
            }
        }

        public async Task<Entry> GetAsync(IKey key)
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            await using var transaction = connection.BeginTransaction();

            try
            {
                var sql = "SELECT * FROM resources WHERE id=@id AND type_name=@type_name";
                if (key.HasVersionId())
                    sql += " AND version_id=@version_id";
                else
                    sql += " AND active=TRUE";
                using var command = new NpgsqlCommand(sql, connection);
                command.Parameters.AddWithValue("id", key.ResourceId);
                command.Parameters.AddWithValue("type_name", key.TypeName);
                if (key.HasVersionId())
                    command.Parameters.AddWithValue("version_id", key.VersionId);

                await using var reader = await command.ExecuteReaderAsync();

                if (reader.HasRows)
                {
                    await reader.ReadAsync();
                    return await reader.ToEntryAsync();
                }
                else
                    return null;
            }
            finally
            {
                await transaction.CommitAsync();
            }
        }

        public Task<IList<Entry>> GetAsync(IEnumerable<IKey> localIdentifiers) => throw new NotImplementedException();

        public void Add(Entry entry) => throw new NotImplementedException();
        public Entry Get(IKey key) => throw new NotImplementedException();
        public IList<Entry> Get(IEnumerable<IKey> localIdentifiers) => throw new NotImplementedException();
    }
}