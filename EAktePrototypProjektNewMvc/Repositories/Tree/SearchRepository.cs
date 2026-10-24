using DreiF.Shared.Abstractions;
using EAktePrototypProjektNewMvc.Exceptions;
using EAktePrototypProjektNewMvc.Models.GlobalSearch;
using Microsoft.Data.SqlClient;
using System.Data;

namespace EAktePrototypProjektNewMvc.Repositories.Tree
{
    public class SearchRepository
    {
        private readonly string _connectionString;
        private readonly ICurrentMandantProvider _mandantProvider;

        public SearchRepository(IConfiguration configuration, ICurrentMandantProvider mandantProvider)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
            _mandantProvider = mandantProvider;
        }

        // Sucht Objekte anhand eines Namens (Teilstring-Suche, max. 20 Treffer).
        public List<SearchResult> FindObjectsByName(string name)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name))
                {
                    return new List<SearchResult>();
                }

                var results = new List<SearchResult>();
                using var connection = new SqlConnection(_connectionString);
                connection.Open();

                var query = @"
                            SELECT TOP 20 ObjektId, Name, NodeType, Icon, ColorHex, MandantId
                            FROM [vw_SearchIndex]
                            WHERE Name LIKE @name
                              AND NodeType != 'Ordner'
                              AND MandantId = @MandantId
                            ORDER BY Name";

                using var cmd = new SqlCommand(query, connection);
                cmd.Parameters.Add("@name", SqlDbType.NVarChar).Value = "%" + name.Trim() + "%";
                cmd.Parameters.Add("@MandantId", SqlDbType.Int).Value = _mandantProvider.MandantId;

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    results.Add(new SearchResult
                    {
                        ObjectId = reader.GetGuid(reader.GetOrdinal("ObjektId")),
                        Name = reader["Name"].ToString(),
                        NodeType = reader["NodeType"].ToString(),
                        Icon = reader["Icon"] as byte[],
                        ColorHex = reader["ColorHex"]?.ToString(),
                        MandantId = reader.GetInt32(reader.GetOrdinal("MandantId"))
                    });
                }

                return results;
            }
            catch (Exception ex)
            {
                throw new RepositoryException("Fehler in der FindObjectsByName Funktion.", ex);
            }
        }
    }
}