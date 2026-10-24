using DreiF.Shared.Abstractions;
using EAktePrototypProjektNewMvc.Exceptions;
using EAktePrototypProjektNewMvc.Models.Adressen;
using Microsoft.Data.SqlClient;
namespace EAktePrototypProjektNewMvc.Repositories
{
    public class AdresseRepository
    {
        private readonly string _connectionString;
        private readonly ICurrentMandantProvider _mandantProvider;
        public AdresseRepository(string connectionString, ICurrentMandantProvider mandantProvider)
        {
            _connectionString = connectionString;
            _mandantProvider = mandantProvider;
        }
        // Gibt alle Adressen aus der Datenbank zurück.
        public List<Adresse> GetAdresses()
        {
            try
            {
                var list = new List<Adresse>();
                using var connection = new SqlConnection(_connectionString);
                connection.Open();
                var query = "SELECT * FROM [Adresse] WHERE MandantId = @MandantId";
                using var cmd = new SqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@MandantId", _mandantProvider.MandantId);
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    list.Add(new Adresse
                    {
                        Id = (int)reader["Id"],
                        Land = reader["Land"] as string,
                        Strasse = reader["Strasse"] as string,
                        Ort = reader["Ort"] as string,
                        StateOrPrefecture = reader["StateOrPrefecture"] as string,
                        PLZ = reader["PLZ"] as string,
                        Gebaude = reader["Gebaude"] as string,
                        Wohnung = reader["Wohnung"] as string,
                        Organisation = reader["Organisation"] as string,
                        Name = reader["Name"] as string,
                        Bezeichnung = reader["Bezeichnung"] as string,
                        Iban = reader["Iban"] as string,
                        Bic = reader["Bic"] as string,
                        IstInsolvent = (bool)reader["IstInsolvent"],
                        IstAktiv = (bool)reader["IstAktiv"],
                        Ansprechpartner = reader["Ansprechpartner"] as string,
                        AnsprechpartnerTel = reader["AnsprechpartnerTel"] as string,
                        AnsprechpartnerEmail = reader["AnsprechpartnerEmail"] as string,
                        RechnungsStrasse = reader["RechnungsStrasse"] as string,
                        RechnungsOrt = reader["RechnungsOrt"] as string,
                        RechnungsPLZ = reader["RechnungsPLZ"] as string,
                        RechnungsLand = reader["RechnungsLand"] as string,
                        RechnungsStateOrPrefecture = reader["RechnungsStateOrPrefecture"] as string,
                        RechnungsOrganisation = reader["RechnungsOrganisation"] as string,
                        RechnungsName = reader["RechnungsName"] as string,
                        Gesamtadresse = reader["Gesamtadresse"] as string,
                        Fax = reader["Fax"] as string
                    });
                }
                return list;
            }
            catch (Exception ex)
            {
                throw new RepositoryException("Fehler in der GetAdresses Funktion.", ex);
            }
        }
    }
}