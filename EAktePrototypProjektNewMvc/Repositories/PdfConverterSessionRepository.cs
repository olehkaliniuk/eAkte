using EAktePrototypProjektNewMvc.Exceptions;
using EAktePrototypProjektNewMvc.Models.PDF.Session;
using Microsoft.Data.SqlClient;
using System.Data;

namespace EAktePrototypProjektNewMvc.Repositories
{
    public class PdfConverterSessionRepository
    {
        private readonly string _connectionString;

        public PdfConverterSessionRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public PdfConverterSession CreateSession(Guid objektId, string userId, TimeSpan lifetime, string name)
        {
            try
            {
                var session = new PdfConverterSession
                {
                    TokenId = Guid.NewGuid(),
                    DocumentObjektId = objektId,
                    UserId = userId,
                    IssuedAt = DateTime.Now,
                    ExpiresAt = DateTime.Now.Add(lifetime),
                    LastUsedAt = null,
                    Name = name
                };

                using var connection = new SqlConnection(_connectionString);
                connection.Open();
                using var transaction = connection.BeginTransaction();

                try
                {
                    var query = @"
                                INSERT INTO [PdfConverterSessions]
                                (TokenId, DocumentObjektId, UserId, IssuedAt, ExpiresAt, LastUsedAt, Name)
                                VALUES
                                (@TokenId, @DocumentObjektId, @UserId, @IssuedAt, @ExpiresAt, @LastUsedAt, @Name)";

                    using var cmd = new SqlCommand(query, connection, transaction);
                    cmd.Parameters.Add("@TokenId", SqlDbType.UniqueIdentifier).Value = session.TokenId;
                    cmd.Parameters.Add("@DocumentObjektId", SqlDbType.UniqueIdentifier).Value = session.DocumentObjektId;
                    cmd.Parameters.Add("@UserId", SqlDbType.NVarChar).Value = session.UserId;
                    cmd.Parameters.Add("@IssuedAt", SqlDbType.DateTime2).Value = session.IssuedAt;
                    cmd.Parameters.Add("@ExpiresAt", SqlDbType.DateTime2).Value = session.ExpiresAt;
                    cmd.Parameters.Add("@LastUsedAt", SqlDbType.DateTime2).Value = (object?)session.LastUsedAt ?? DBNull.Value;
                    cmd.Parameters.Add("@Name", SqlDbType.NVarChar).Value = session.Name;

                    cmd.ExecuteNonQuery();

                    transaction.Commit();
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }

                return session;
            }
            catch (Exception ex)
            {
                throw new RepositoryException($"CreateSession fehlgeschlagen. ObjektId: {objektId}, UserId: {userId}. Fehler: {ex.Message}", ex);
            }
        }

        public PdfConverterSession? GetByToken(Guid tokenId)
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();
            using var cmd = new SqlCommand(@"
                                           SELECT TokenId, DocumentObjektId, UserId, IssuedAt, ExpiresAt, LastUsedAt, Name
                                           FROM [PdfConverterSessions]
                                           WHERE TokenId = @TokenId", connection);

            cmd.Parameters.Add("@TokenId", SqlDbType.UniqueIdentifier).Value = tokenId;
            using var reader = cmd.ExecuteReader();
            return reader.Read() ? Map(reader) : null;
        }


        public byte[]? GetStagedFileStream(Guid tokenId)
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();
            using var cmd = new SqlCommand(@"
                                           SELECT StagedFileStream
                                           FROM [PdfConverterSessions]
                                           WHERE TokenId = @TokenId", connection);

            cmd.Parameters.Add("@TokenId", SqlDbType.UniqueIdentifier).Value = tokenId;

            using var reader = cmd.ExecuteReader(CommandBehavior.SequentialAccess);
            if (!reader.Read())
                return null;

            var ordinal = reader.GetOrdinal("StagedFileStream");
            return reader.IsDBNull(ordinal) ? null : (byte[])reader[ordinal];
        }

        private static PdfConverterSession Map(SqlDataReader r) => new()
        {
            TokenId = r.GetGuid(r.GetOrdinal("TokenId")),
            DocumentObjektId = r.GetGuid(r.GetOrdinal("DocumentObjektId")),
            UserId = r.GetString(r.GetOrdinal("UserId")),
            IssuedAt = r.GetDateTime(r.GetOrdinal("IssuedAt")),
            ExpiresAt = r.GetDateTime(r.GetOrdinal("ExpiresAt")),
            LastUsedAt = r.IsDBNull(r.GetOrdinal("LastUsedAt")) ? null : r.GetDateTime(r.GetOrdinal("LastUsedAt")),
            Name = r.GetString(r.GetOrdinal("Name"))
        };
    }
}