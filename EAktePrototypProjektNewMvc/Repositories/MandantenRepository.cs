using EAktePrototypProjektNewMvc.Exceptions;
using EAktePrototypProjektNewMvc.Models.Mandanten;
using Microsoft.Data.SqlClient;
using System.Data;

namespace EAktePrototypProjektNewMvc.Repositories
{
    public class MandantenRepository
    {
        private readonly string _connectionString;

        public MandantenRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        // Gibt alle Mandanten aus der Datenbank zurück (für das Dropdown).
        public List<Mandant> GetAllMandanten()
        {
            try
            {
                var list = new List<Mandant>();
                using var connection = new SqlConnection(_connectionString);
                connection.Open();
                var query = @"SELECT TOP (1000) Id,Name,Beschreibung FROM [Mandanten] ORDER BY Name";
                using var cmd = new SqlCommand(query, connection);
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    list.Add(new Mandant
                    {
                        Id = (int)reader["Id"],
                        Name = reader["Name"] as string,
                        Beschreibung = reader["Beschreibung"] as string
                    });
                }
                return list;
            }
            catch (Exception ex)
            {
                throw new RepositoryException("Fehler in der GetAllMandanten Funktion.", ex);
            }
        }
    }
}