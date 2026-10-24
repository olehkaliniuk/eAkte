using DreiF.Shared.Abstractions;
using EAktePrototypProjektNewMvc.Models.Dokumente.Sessions;
using EAktePrototypProjektNewMvc.Models.Kladde;
using Microsoft.Data.SqlClient;
using System.Data;

namespace EAktePrototypProjektNewMvc.Repositories
{
    public class KladdeSessionRepository
    {
        private readonly string _connectionString;
        private readonly ICurrentMandantProvider _mandantProvider;

        public KladdeSessionRepository(string connectionString, ICurrentMandantProvider mandantProvider)
        {
            _connectionString = connectionString;
            _mandantProvider = mandantProvider;
        }

        // KladdeEditSession erstellen.
        public void Create(KladdeSession s)
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            var query = @"
                        INSERT INTO [dbo].[KladdeSessions]
                        (TokenId, Uid, IssuedAt, ExpiresAt, LockToken)
                        VALUES
                        (@TokenId, @Uid, @IssuedAt, @ExpiresAt, @LockToken)";

            using var cmd = new SqlCommand(query, connection);

            cmd.Parameters.Add("@TokenId", SqlDbType.UniqueIdentifier).Value = s.TokenId;
            cmd.Parameters.Add("@Uid", SqlDbType.NVarChar, 450).Value = s.Uid;
            cmd.Parameters.Add("@IssuedAt", SqlDbType.DateTime2).Value = DateTime.Now;
            cmd.Parameters.Add("@ExpiresAt", SqlDbType.DateTime2).Value = DateTime.Now.AddMinutes(10);
            cmd.Parameters.Add("@LockToken", SqlDbType.NVarChar, 450).Value = 0;

            cmd.ExecuteNonQuery();
        }


    }
}