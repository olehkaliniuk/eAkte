using DreiF.Shared.Abstractions;
using EAktePrototypProjektNewMvc.Models.Dokumente.Sessions;
using Microsoft.Data.SqlClient;
using System.Data;

namespace EAktePrototypProjektNewMvc.Repositories
{
    public class EditSessionRepository
    {
        private readonly string _connectionString;
        private readonly ICurrentMandantProvider _mandantProvider;

        public EditSessionRepository(string connectionString, ICurrentMandantProvider mandantProvider)
        {
            _connectionString = connectionString;
            _mandantProvider = mandantProvider;
        }

        // DokumentEditSession erstellen.
        public void Create(DokumentEditSession s)
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();
            var query = @"
                        INSERT INTO [DokumentEditSessions]
                        (TokenId, DocumentObjektId, OriginalVersionId, UserId, FileName, ContentType, HasPendingChanges, IssuedAt, ExpiresAt, MandantId, IsViewOnly)
                        VALUES
                        (@TokenId, @DocumentObjektId, @OriginalVersionId, @UserId, @FileName, @ContentType, 0, @IssuedAt, @ExpiresAt, @MandantId, @IsViewOnly)";

            using var cmd = new SqlCommand(query, connection);
            cmd.Parameters.Add("@TokenId", SqlDbType.UniqueIdentifier).Value = s.TokenId;
            cmd.Parameters.Add("@DocumentObjektId", SqlDbType.UniqueIdentifier).Value = s.DocumentObjektId;
            cmd.Parameters.Add("@OriginalVersionId", SqlDbType.UniqueIdentifier).Value = s.OriginalVersionId;
            cmd.Parameters.Add("@UserId", SqlDbType.NVarChar).Value = s.UserId;
            cmd.Parameters.Add("@FileName", SqlDbType.NVarChar).Value = s.FileName;
            cmd.Parameters.Add("@ContentType", SqlDbType.NVarChar).Value = (object?)s.ContentType ?? DBNull.Value;
            cmd.Parameters.Add("@IssuedAt", SqlDbType.DateTime2).Value = s.IssuedAt;
            cmd.Parameters.Add("@ExpiresAt", SqlDbType.DateTime2).Value = s.ExpiresAt;
            cmd.Parameters.Add("@MandantId", SqlDbType.Int).Value = s.MandantId;
            cmd.Parameters.Add("@IsViewOnly", SqlDbType.Bit).Value = s.IsViewOnly;
            cmd.ExecuteNonQuery();
        }

        // DokumentEditSession anhand des Tokens abrufen.
        public DokumentEditSession? GetByToken(Guid tokenId)
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();
            using var cmd = new SqlCommand("SELECT * FROM [DokumentEditSessions] WHERE TokenId = @TokenId", connection);

            cmd.Parameters.Add("@TokenId", SqlDbType.UniqueIdentifier).Value = tokenId;
            using var reader = cmd.ExecuteReader();
            return reader.Read() ? Map(reader) : null;
        }

        // DokumentEditSession löschen.
        public void Delete(Guid tokenId)
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();
            using var cmd = new SqlCommand("DELETE FROM [DokumentEditSessions] WHERE TokenId = @TokenId", connection);

            cmd.Parameters.Add("@TokenId", SqlDbType.UniqueIdentifier).Value = tokenId;
            cmd.ExecuteNonQuery();
        }



        // DokumentEditSession aktualisieren, um den StagedFileStream zu setzen und HasPendingChanges auf true zu setzen.
        public void UpdateStagedContent(Guid tokenId, byte[] content)
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();
            using var cmd = new SqlCommand(@"UPDATE [DokumentEditSessions] SET StagedFileStream = @Content, HasPendingChanges = 1, LastUsedAt = @Now WHERE TokenId = @TokenId", connection);

            cmd.Parameters.Add("@Content", SqlDbType.VarBinary).Value = content;
            cmd.Parameters.Add("@Now", SqlDbType.DateTime2).Value = DateTime.Now;
            cmd.Parameters.Add("@TokenId", SqlDbType.UniqueIdentifier).Value = tokenId;
            cmd.ExecuteNonQuery();
        }

        // TTL (Time To Live) der DokumentEditSession aktualisieren.
        public void RefreshTtl(Guid tokenId, TimeSpan extendBy)
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();
            using var cmd = new SqlCommand(@"UPDATE [DokumentEditSessions] SET ExpiresAt = @NewExpiry, LastUsedAt = @Now WHERE TokenId = @TokenId", connection);

            cmd.Parameters.Add("@NewExpiry", SqlDbType.DateTime2).Value = DateTime.Now.Add(extendBy);
            cmd.Parameters.Add("@Now", SqlDbType.DateTime2).Value = DateTime.Now;
            cmd.Parameters.Add("@TokenId", SqlDbType.UniqueIdentifier).Value = tokenId;
            cmd.ExecuteNonQuery();
        }

        // LockToken für die DokumentEditSession setzen.
        public void SetLockToken(Guid tokenId, string? lockToken)
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();
            using var cmd = new SqlCommand("UPDATE [DokumentEditSessions] SET LockToken = @LockToken WHERE TokenId = @TokenId", connection);

            cmd.Parameters.Add("@LockToken", SqlDbType.NVarChar).Value = (object?)lockToken ?? DBNull.Value;
            cmd.Parameters.Add("@TokenId", SqlDbType.UniqueIdentifier).Value = tokenId;
            cmd.ExecuteNonQuery();
        }



        // Aktive DokumentEditSession für ein Dokument abrufen. Wird beim Neuladen der Seite verwendet, um die Sitzung wiederherzustellen.
        public DokumentEditSession? GetActiveByDocumentObjektId(Guid documentObjektId)
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();
            using var cmd = new SqlCommand(@"
                                           SELECT TOP 1 * FROM [DokumentEditSessions]
                                           WHERE DocumentObjektId = @DocumentObjektId
                                             AND MandantId = @MandantId
                                             AND ExpiresAt > @Now
                                           ORDER BY IssuedAt DESC", connection);

            cmd.Parameters.Add("@DocumentObjektId", SqlDbType.UniqueIdentifier).Value = documentObjektId;
            cmd.Parameters.Add("@MandantId", SqlDbType.Int).Value = _mandantProvider.MandantId;
            cmd.Parameters.Add("@Now", SqlDbType.DateTime2).Value = DateTime.Now;

            using var reader = cmd.ExecuteReader();
            return reader.Read() ? Map(reader) : null;
        }

        private static DokumentEditSession Map(SqlDataReader r) => new()
        {
            TokenId = r.GetGuid(r.GetOrdinal("TokenId")),
            DocumentObjektId = r.GetGuid(r.GetOrdinal("DocumentObjektId")),
            OriginalVersionId = r.GetGuid(r.GetOrdinal("OriginalVersionId")),
            UserId = r.GetString(r.GetOrdinal("UserId")),
            FileName = r.GetString(r.GetOrdinal("FileName")),
            ContentType = r.IsDBNull(r.GetOrdinal("ContentType")) ? null : r.GetString(r.GetOrdinal("ContentType")),
            StagedFileStream = r.IsDBNull(r.GetOrdinal("StagedFileStream")) ? null : (byte[])r["StagedFileStream"],
            HasPendingChanges = r.GetBoolean(r.GetOrdinal("HasPendingChanges")),
            IssuedAt = r.GetDateTime(r.GetOrdinal("IssuedAt")),
            ExpiresAt = r.GetDateTime(r.GetOrdinal("ExpiresAt")),
            LastUsedAt = r.IsDBNull(r.GetOrdinal("LastUsedAt")) ? null : r.GetDateTime(r.GetOrdinal("LastUsedAt")),
            LockToken = r.IsDBNull(r.GetOrdinal("LockToken")) ? null : r.GetString(r.GetOrdinal("LockToken")),
            MandantId = r.GetInt32(r.GetOrdinal("MandantId")),
            IsViewOnly = r.IsDBNull(r.GetOrdinal("IsViewOnly")) ? false : r.GetBoolean(r.GetOrdinal("IsViewOnly"))
        };
    }
}