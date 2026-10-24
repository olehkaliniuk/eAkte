using DreiF.Shared.Abstractions;
using DreiF.Shared.EAkte.Vorgaenge;
using EAktePrototypProjektNewMvc.Exceptions;
using EAktePrototypProjektNewMvc.Models;
using EAktePrototypProjektNewMvc.Models.Adressen;
using EAktePrototypProjektNewMvc.Models.CreateAkte;
using EAktePrototypProjektNewMvc.Models.CreateAkte.Ablaufdaten;
using EAktePrototypProjektNewMvc.Models.CreateAkte.Kontakte;
using EAktePrototypProjektNewMvc.Models.Dokumente;
using EAktePrototypProjektNewMvc.Models.Gebuehre;
using EAktePrototypProjektNewMvc.Models.KaHaNe;
using EAktePrototypProjektNewMvc.Models.Ordner;
using EAktePrototypProjektNewMvc.Models.Vorlagen;
using EAktePrototypProjektNewMvc.Services;
using Microsoft.Data.SqlClient;
using System.Data;
using static EAktePrototypProjektNewMvc.Controllers.HomeController;

namespace EAktePrototypProjektNewMvc.Repositories
{
    public class eAkteRepository
    {
        private readonly string _connectionString;
        private readonly ICurrentMandantProvider _mandantProvider;
        private readonly WordVorlagenService _wordVorlagenService;

        public eAkteRepository(string connectionString, ICurrentMandantProvider mandantProvider, WordVorlagenService wordVorlagenService)
        {
            _connectionString = connectionString;
            _mandantProvider = mandantProvider;
            _wordVorlagenService = wordVorlagenService;
        }


        // Dropdowns. 
        // Gibt alle Antragsarten aus der Datenbank zurück.
        public List<Antragsart> GetAntragsarten()
        {
            try
            {
                var list = new List<Antragsart>();

                using var connection = new SqlConnection(_connectionString);
                connection.Open();

                var query = "SELECT Id, Bezeichnung FROM [Antragsart] WHERE Bezeichnung IS NOT NULL";
                using var cmd = new SqlCommand(query, connection);
                using var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    list.Add(new Antragsart
                    {
                        Id = (int)reader["Id"],
                        Bezeichnung = reader["Bezeichnung"] as string
                    });
                }

                return list;
            }
            catch (Exception ex)
            {
                throw new RepositoryException("Fehler in der GetAntragsarten Funktion.", ex);
            }
        }

        // Gibt alle Vorgangstypen aus der Datenbank zurück.
        public List<Vorgangstyp> GetVorgangstypen()
        {
            try
            {
                var list = new List<Vorgangstyp>();

                using var connection = new SqlConnection(_connectionString);
                connection.Open();

                var query = "SELECT Id, Type FROM [Vorgangstyp]";
                using var cmd = new SqlCommand(query, connection);
                using var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    list.Add(new Vorgangstyp
                    {
                        Id = (int)reader["Id"],
                        Type = reader["Type"] as string
                    });
                }

                return list;
            }
            catch (Exception ex)
            {
                throw new RepositoryException("Fehler in der GetVorgangstypen Funktion.", ex);
            }
        }

        // Gibt alle Aktionstypen aus der Datenbank zurück.
        public List<Aktionstypen> GetAktionstypen()
        {
            try
            {
                var list = new List<Aktionstypen>();

                using var connection = new SqlConnection(_connectionString);
                connection.Open();

                var query = "SELECT Id, parameter_wert_string FROM [Aktionstypen]";
                using var cmd = new SqlCommand(query, connection);
                using var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    list.Add(new Aktionstypen
                    {
                        Id = (int)reader["Id"],
                        ParameterWertString = reader["parameter_wert_string"] as string
                    });
                }

                return list;
            }
            catch (Exception ex)
            {
                throw new RepositoryException("Fehler in der GetAktionstypen Funktion.", ex);
            }
        }



        // + Mandanten id.
        //Nebengruppe, Hauptgruppe. 
        // Gibt die Kategorie-, Nebengruppen-, Hauptgruppennummer und Bezeichnung anhand einer ObjektId zurück.
        public (int? NebengruppeNummer, int? GehoertZuHauptgruppeNummer, int? GehoertZuKategorieNummer, string? Bezeichnung) GetNebengruppeNummerByObjektId(Guid objektId)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                connection.Open();
                var query = "SELECT NebengruppeNummer, GehoertZuHauptgruppeNummer, GehoertZuKategorieNummer, Bezeichnung FROM [Nebengruppe] WHERE ObjektId = @ObjektId AND MandantId = @MandantId";
                using var cmd = new SqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@ObjektId", objektId);
                cmd.Parameters.AddWithValue("@MandantId", _mandantProvider.MandantId);
                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    var nebengruppeNummer = reader["NebengruppeNummer"] != DBNull.Value ? (int?)Convert.ToInt32(reader["NebengruppeNummer"]) : null;
                    var hauptgruppeNummer = reader["GehoertZuHauptgruppeNummer"] != DBNull.Value ? (int?)Convert.ToInt32(reader["GehoertZuHauptgruppeNummer"]) : null;
                    var kategorieNummer = reader["GehoertZuKategorieNummer"] != DBNull.Value ? (int?)Convert.ToInt32(reader["GehoertZuKategorieNummer"]) : null;
                    var bezeichnung = reader["Bezeichnung"] as string;
                    return (nebengruppeNummer, hauptgruppeNummer, kategorieNummer, bezeichnung);
                }
                return (null, null, null, null);
            }
            catch (Exception ex)
            {
                throw new RepositoryException("Fehler in der GetNebengruppeNummerByObjektId Funktion.", ex);
            }
        }

        // Gibt die höchste laufende Nummer einer Akte für eine Nebengruppe zurück.
        public int? GetMaxLfdNrByNebengruppe(string nebengruppeNummer)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                connection.Open();
                var query = "SELECT MAX(CAST(LfdNr AS INT)) FROM [Akte] WHERE NebengruppeNummer = @NebengruppeNummer AND MandantId = @MandantId";
                using var cmd = new SqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@NebengruppeNummer", nebengruppeNummer);
                cmd.Parameters.AddWithValue("@MandantId", _mandantProvider.MandantId);
                var result = cmd.ExecuteScalar();
                return result != DBNull.Value && result != null ? (int?)Convert.ToInt32(result) : null;
            }
            catch (Exception ex)
            {
                throw new RepositoryException("Fehler in der GetMaxLfdNrByNebengruppe Funktion.", ex);
            }
        }

        // Erhöht den LaufenderAktenzeichenZaehler der Hauptgruppe um 1.
        public void IncrementLaufenderAktenzeichenZaehler(int hauptgruppeNummer)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                connection.Open();
                var query = @"UPDATE [Hauptgruppe]  
                            SET LaufenderAktenzeichenZaehler = LaufenderAktenzeichenZaehler + 1 
                            WHERE HauptgruppeNummer = @HauptgruppeNummer
                              AND MandantId = @MandantId";
                using var cmd = new SqlCommand(query, connection);
                cmd.Parameters.Add("@HauptgruppeNummer", SqlDbType.Int).Value = hauptgruppeNummer;
                cmd.Parameters.Add("@MandantId", SqlDbType.Int).Value = _mandantProvider.MandantId;
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                throw new RepositoryException($"IncrementLaufenderAktenzeichenZaehler fehlgeschlagen. HauptgruppeNummer: {hauptgruppeNummer}. Fehler: {ex.Message}", ex);
            }
        }



        // + Mandanten id.
        // Antragsteller.  
        // Speichert die Antragsteller zu einem Aktenzeichen nach vorherigem Löschen.
        public void SaveAntragsteller(string aktenzeichen, List<Antragsteller> items)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                connection.Open();
                var deleteQuery = "DELETE FROM [Antragsteller] WHERE Aktenzeichen = @Aktenzeichen AND MandantId = @MandantId";
                using var deleteCmd = new SqlCommand(deleteQuery, connection);
                deleteCmd.Parameters.AddWithValue("@Aktenzeichen", aktenzeichen);
                deleteCmd.Parameters.AddWithValue("@MandantId", _mandantProvider.MandantId);
                deleteCmd.ExecuteNonQuery();
                var insertQuery = @"INSERT INTO [Antragsteller]
                                  (Aktenzeichen, Type, Adresse, AdresseZusatz, Email, Tel, Fax, Aktiv, MandantId)
                                  VALUES (@Aktenzeichen, @Type, @Adresse, @AdresseZusatz, @Email, @Tel, @Fax, @Aktiv, @MandantId)";
                foreach (var item in items)
                {
                    using var cmd = new SqlCommand(insertQuery, connection);
                    cmd.Parameters.AddWithValue("@Aktenzeichen", aktenzeichen);
                    cmd.Parameters.AddWithValue("@Type", item.Type ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@Adresse", item.Adresse ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@AdresseZusatz", item.AdresseZusatz ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@Email", item.Email ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@Tel", item.Tel ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@Fax", item.Fax ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@Aktiv", true);
                    cmd.Parameters.AddWithValue("@MandantId", _mandantProvider.MandantId);
                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                throw new RepositoryException("Fehler in der SaveAntragsteller Funktion.", ex);
            }
        }

        // Gibt alle Antragsteller-Einträge anhand des Aktenzeichens zurück.
        public List<Antragsteller> GetAntragstellerByAktenzeichen(string aktenzeichen)
        {
            try
            {
                var list = new List<Antragsteller>();
                using var connection = new SqlConnection(_connectionString);
                connection.Open();
                var query = "SELECT Id, Aktenzeichen, Type, Adresse, AdresseZusatz, Email, Tel, Fax, Aktiv FROM [Antragsteller] WHERE Aktenzeichen = @Aktenzeichen AND MandantId = @MandantId";
                using var cmd = new SqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@Aktenzeichen", aktenzeichen);
                cmd.Parameters.AddWithValue("@MandantId", _mandantProvider.MandantId);
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    list.Add(new Antragsteller
                    {
                        Id = (int)reader["Id"],
                        Aktenzeichen = reader["Aktenzeichen"] as string,
                        Type = reader["Type"] as string,
                        Adresse = reader["Adresse"] as string,
                        AdresseZusatz = reader["AdresseZusatz"] as string,
                        Email = reader["Email"] as string,
                        Tel = reader["Tel"] as string,
                        Fax = reader["Fax"] as string,
                        Aktiv = reader["Aktiv"] != DBNull.Value && (bool)reader["Aktiv"]
                    });
                }
                return list;
            }
            catch (Exception ex)
            {
                throw new RepositoryException("Fehler in der GetAntragstellerByAktenzeichen Funktion.", ex);
            }
        }



        // + Mandanten id.
        // Adressen.    
        // Gibt eine Adresse anhand ihrer ID zurück.
        public Adresse GetAdresseById(int id)
        {
            try
            {
                Adresse adresse = null;
                using var connection = new SqlConnection(_connectionString);
                connection.Open();
                var query = "SELECT * FROM [Adresse] WHERE Id = @Id AND MandantId = @MandantId";
                using var cmd = new SqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@Id", id);
                cmd.Parameters.AddWithValue("@MandantId", _mandantProvider.MandantId);
                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    adresse = new Adresse
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
                        IstInsolvent = reader["IstInsolvent"] != DBNull.Value ? (bool)reader["IstInsolvent"] : (bool?)null,
                        IstAktiv = reader["IstAktiv"] != DBNull.Value ? (bool)reader["IstAktiv"] : (bool?)null,
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
                    };
                }
                return adresse;
            }
            catch (Exception ex)
            {
                throw new RepositoryException("Fehler in der GetAdresseById Funktion.", ex);
            }
        }



        // + Mandanten id.
        // Kontakte.  
        // Gibt alle Kontakte nach Aktenzeichen zurück.
        public List<Kontakt> GetKontakteByAktenzeichen(string aktenzeichen)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(aktenzeichen))
                {
                    return new List<Kontakt>();
                }

                var result = new List<Kontakt>();

                using var connection = new SqlConnection(_connectionString);
                connection.Open();

                var query = @"SELECT Id, Anrede, Vorname, Name, EmailAdresse, EmailAnzeigename,
                     EmailAn, Telefon, Fax FROM [Kontakte]
                     WHERE Aktenzeichen = @Aktenzeichen
                       AND MandantId = @MandantId";
                using var cmd = new SqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@Aktenzeichen", aktenzeichen);
                cmd.Parameters.AddWithValue("@MandantId", _mandantProvider.MandantId);

                using var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    result.Add(new Kontakt
                    {
                        Id = reader["Id"] != DBNull.Value ? Convert.ToInt32(reader["Id"]) : 0,
                        Anrede = reader["Anrede"] != DBNull.Value ? reader["Anrede"].ToString() : null,
                        Vorname = reader["Vorname"] != DBNull.Value ? reader["Vorname"].ToString() : null,
                        Name = reader["Name"] != DBNull.Value ? reader["Name"].ToString() : null,
                        Email = reader["EmailAdresse"] != DBNull.Value ? reader["EmailAdresse"].ToString() : null,
                        EmailAnzeigename = reader["EmailAnzeigename"] != DBNull.Value ? reader["EmailAnzeigename"].ToString() : null,
                        EmailAn = reader["EmailAn"] != DBNull.Value ? reader["EmailAn"].ToString() : null,
                        Telefon = reader["Telefon"] != DBNull.Value ? reader["Telefon"].ToString() : null,
                        Fax = reader["Fax"] != DBNull.Value ? reader["Fax"].ToString() : null,
                    });
                }

                return result;
            }
            catch (Exception ex)
            {
                throw new RepositoryException("Fehler in der GetKontakteByAktenzeichen Funktion.", ex);
            }
        }

        // Fügt einen neuen Kontakt in die Datenbank ein.
        public void InsertKontakt(Kontakt dto)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                connection.Open();

                var query = @"INSERT INTO [Kontakte]
                     (Aktenzeichen, Anrede, Vorname, Name, EmailAdresse, EmailAnzeigename, EmailAn, Telefon, Fax, MandantId)
                     VALUES (@Aktenzeichen, @Anrede, @Vorname, @Name, @EmailAdresse, @EmailAnzeigename, @EmailAn, @Telefon, @Fax, @MandantId)";
                using var cmd = new SqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@Aktenzeichen", (object?)dto.Aktenzeichen ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Anrede", (object?)dto.Anrede ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Vorname", (object?)dto.Vorname ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Name", (object?)dto.Name ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@EmailAdresse", (object?)dto.Email ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@EmailAnzeigename", (object?)dto.EmailAnzeigename ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@EmailAn", (object?)dto.EmailAn ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Telefon", (object?)dto.Telefon ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Fax", (object?)dto.Fax ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@MandantId", _mandantProvider.MandantId);
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                throw new RepositoryException("Fehler in der InsertKontakt Funktion.", ex);
            }
        }

        // Aktualisiert einen vorhandenen Kontakt in der Datenbank.
        public void UpdateKontakt(Kontakt dto)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                connection.Open();

                var query = @"UPDATE [Kontakte] SET
                     Anrede = @Anrede, Vorname = @Vorname, Name = @Name,
                     EmailAdresse = @EmailAdresse, EmailAnzeigename = @EmailAnzeigename,
                     EmailAn = @EmailAn, Telefon = @Telefon, Fax = @Fax
                     WHERE Id = @Id
                       AND MandantId = @MandantId";
                using var cmd = new SqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@Id", dto.Id);
                cmd.Parameters.AddWithValue("@Anrede", (object?)dto.Anrede ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Vorname", (object?)dto.Vorname ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Name", (object?)dto.Name ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@EmailAdresse", (object?)dto.Email ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@EmailAnzeigename", (object?)dto.EmailAnzeigename ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@EmailAn", (object?)dto.EmailAn ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Telefon", (object?)dto.Telefon ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Fax", (object?)dto.Fax ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@MandantId", _mandantProvider.MandantId);
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                throw new RepositoryException("Fehler in der UpdateKontakt Funktion.", ex);
            }
        }

        // Löscht einen Kontakt anhand seiner ID aus der Datenbank.
        public void DeleteKontakt(int id)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                connection.Open();

                var query = "DELETE FROM [Kontakte] WHERE Id = @Id AND MandantId = @MandantId";
                using var cmd = new SqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@Id", id);
                cmd.Parameters.AddWithValue("@MandantId", _mandantProvider.MandantId);
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                throw new RepositoryException("Fehler in der DeleteKontakt Funktion.", ex);
            }
        }



        // + Mandanten id.
        // Ablaufdaten.   
        // Gibt alle Ablaufdaten-Einträge anhand des Aktenzeichens zurück.
        public List<Ablaufdaten> GetAblaufdatenByAktenzeichen(string aktenzeichen)
        {
            try
            {
                var list = new List<Ablaufdaten>();

                using var connection = new SqlConnection(_connectionString);
                connection.Open();

                var query = "SELECT Id, Aktenzeichen, EintragAmVom, EintragVon, AktionAm, AktionTyp, Bemerkung FROM [Ablaufdaten] WHERE Aktenzeichen = @Aktenzeichen AND MandantId = @MandantId";
                using var cmd = new SqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@Aktenzeichen", aktenzeichen);
                cmd.Parameters.AddWithValue("@MandantId", _mandantProvider.MandantId);

                using var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    list.Add(new Ablaufdaten
                    {
                        Id = (int)reader["Id"],
                        Aktenzeichen = reader["Aktenzeichen"] as string,
                        EintragAmVom = reader["EintragAmVom"] != DBNull.Value ? (DateTime?)reader["EintragAmVom"] : null,
                        EintragVon = reader["EintragVon"] as string,
                        AktionAm = reader["AktionAm"] != DBNull.Value ? (DateTime?)reader["AktionAm"] : null,
                        AktionTyp = reader["AktionTyp"] as string,
                        Bemerkung = reader["Bemerkung"] as string
                    });
                }

                return list;
            }
            catch (Exception ex)
            {
                throw new RepositoryException("Fehler in der GetAblaufdatenByAktenzeichen Funktion.", ex);
            }
        }

        // Fügt einen neuen Ablaufdaten-Eintrag in die Datenbank ein.
        public void InsertAblaufdaten(Ablaufdaten dto)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                connection.Open();

                var query = @"INSERT INTO [Ablaufdaten]
                            (Aktenzeichen, EintragAmVom, EintragVon, AktionAm, AktionTyp, Bemerkung, MandantId)
                            VALUES (@Aktenzeichen, @EintragAmVom, @EintragVon, @AktionAm, @AktionTyp, @Bemerkung, @MandantId)";
                using var cmd = new SqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@Aktenzeichen", (object?)dto.Aktenzeichen ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@EintragAmVom", (object?)dto.EintragAmVom ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@EintragVon", (object?)dto.EintragVon ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@AktionAm", (object?)dto.AktionAm ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@AktionTyp", (object?)dto.AktionTyp ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Bemerkung", (object?)dto.Bemerkung ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@MandantId", _mandantProvider.MandantId);
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                throw new RepositoryException("Fehler in der InsertAblaufdaten Funktion.", ex);
            }
        }

        // Aktualisiert einen vorhandenen Ablaufdaten-Eintrag in der Datenbank.
        public void UpdateAblaufdaten(Ablaufdaten dto)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                connection.Open();

                var query = @"UPDATE [Ablaufdaten] SET
                            Aktenzeichen = @Aktenzeichen,
                            EintragAmVom = @EintragAmVom,
                            EintragVon   = @EintragVon,
                            AktionAm     = @AktionAm,
                            AktionTyp    = @AktionTyp,
                            Bemerkung    = @Bemerkung
                            WHERE Id = @Id
                              AND MandantId = @MandantId";
                using var cmd = new SqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@Id", dto.Id);
                cmd.Parameters.AddWithValue("@Aktenzeichen", (object?)dto.Aktenzeichen ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@EintragAmVom", (object?)dto.EintragAmVom ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@EintragVon", (object?)dto.EintragVon ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@AktionAm", (object?)dto.AktionAm ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@AktionTyp", (object?)dto.AktionTyp ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Bemerkung", (object?)dto.Bemerkung ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@MandantId", _mandantProvider.MandantId);
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                throw new RepositoryException("Fehler in der UpdateAblaufdaten Funktion.", ex);
            }
        }

        // Löscht einen Ablaufdaten-Eintrag anhand seiner ID aus der Datenbank.
        public void DeleteAblaufdaten(int id)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                connection.Open();

                var query = "DELETE FROM [Ablaufdaten] WHERE Id = @Id AND MandantId = @MandantId";
                using var cmd = new SqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@Id", id);
                cmd.Parameters.AddWithValue("@MandantId", _mandantProvider.MandantId);
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                throw new RepositoryException("Fehler in der DeleteAblaufdaten Funktion.", ex);
            }
        }



        // + Mandanten id.
        // Akte / Vorgang erstellen.   
        // Erstellt eine Akte mit den übergebenen Parametern.
        public void CreateAkte(string zulassungsnummer, int lfdNr, int? hauptgruppeNummer, int? nebengruppeNummer, string zustand, string erzeugtVon, string geandertVon, byte[] icon, string abteilung, string referat)
        {
            try
            {
                // RestClass 
                var restClass = new DcmToWs.RestClass();
                var guid = restClass.CreateGuid();

                using var connection = new SqlConnection(_connectionString);
                connection.Open();

                var query = @"INSERT INTO [Akte] 
                     (Zulassungsnummer, LfdNr, HauptgruppeNummer, NebengruppeNummer, ObjektId, Zustand, ErzeugtVon, ErzeugtAm, GeandertVon, GeandertAm, Icon, Abteilung, Referat, MandantId) 
                     VALUES 
                     (@Zulassungsnummer, @LfdNr, @HauptgruppeNummer, @NebengruppeNummer, @ObjektId, @Zustand, @ErzeugtVon, @ErzeugtAm, @GeandertVon, @GeandertAm, @Icon, @Abteilung, @Referat, @MandantId)";

                using var cmd = new SqlCommand(query, connection);
                cmd.Parameters.Add("@Zulassungsnummer", SqlDbType.NVarChar).Value = zulassungsnummer;
                cmd.Parameters.Add("@LfdNr", SqlDbType.Int).Value = lfdNr;
                cmd.Parameters.Add("@HauptgruppeNummer", SqlDbType.Int).Value = (object)hauptgruppeNummer ?? DBNull.Value;
                cmd.Parameters.Add("@NebengruppeNummer", SqlDbType.Int).Value = (object)nebengruppeNummer ?? DBNull.Value;
                cmd.Parameters.Add("@ObjektId", SqlDbType.UniqueIdentifier).Value = guid;
                cmd.Parameters.Add("@Zustand", SqlDbType.NVarChar).Value = (object)zustand ?? DBNull.Value;
                cmd.Parameters.Add("@ErzeugtVon", SqlDbType.NVarChar).Value = (object)erzeugtVon ?? DBNull.Value;
                cmd.Parameters.Add("@ErzeugtAm", SqlDbType.DateTime2).Value = DateTime.Now;
                cmd.Parameters.Add("@GeandertVon", SqlDbType.NVarChar).Value = (object)geandertVon ?? DBNull.Value;
                cmd.Parameters.Add("@GeandertAm", SqlDbType.DateTime2).Value = DateTime.Now;
                cmd.Parameters.Add("@Icon", SqlDbType.VarBinary).Value = (object)icon ?? DBNull.Value;
                cmd.Parameters.Add("@Abteilung", SqlDbType.NVarChar).Value = (object)abteilung ?? DBNull.Value;
                cmd.Parameters.Add("@Referat", SqlDbType.NVarChar).Value = (object)referat ?? DBNull.Value;
                cmd.Parameters.Add("@MandantId", SqlDbType.Int).Value = _mandantProvider.MandantId;
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                throw new RepositoryException($"CreateAkte fehlgeschlagen. Zulassungsnummer: {zulassungsnummer}, LfdNr: {lfdNr}. Fehler: {ex.Message}", ex);
            }
        }

        // Erstellt einen Vorgang mit den übergebenen Parametern.
        public Guid CreateVorgang(string aktenzeichen, string userId, string stelle, string zulassungsnummer, DateTime datumAntrag, string antragsart, string bescheidtyp, string gegenstand, string status, string betreff, string bemerkung, Guid objektId, string aktenzustand, string erzeugtVon, bool veroeffentlicht, byte[] icon, string abteilung, string referat, bool antragVorhanden, bool bescheidNeufassung, string ansprechpartner)
        {
            try
            {
                var restClass = new DcmToWs.RestClass();
                var guid = restClass.CreateGuid();

                using var connection = new SqlConnection(_connectionString);
                connection.Open();

                var query = @"INSERT INTO [Vorgang] 
                     (Aktenzeichen, UserId, Stelle, Zulassungsnummer, DatumAntrag, Antragsart, Bescheidtyp, 
                     Gegenstand, Status, Betreff, Bemerkung, ObjektId, Aktenzustand, ErzeugtVon, ErzeugtAm, Veroeffentlicht, Icon, Abteilung, Referat,
                     AntragVorhanden, BescheidNeufassung, Ansprechpartner, MandantId) 
                     VALUES 
                     (@Aktenzeichen, @UserId, @Stelle, @Zulassungsnummer, @DatumAntrag, @Antragsart, @Bescheidtyp, 
                     @Gegenstand, @Status, @Betreff, @Bemerkung, @ObjektId, @Aktenzustand, @ErzeugtVon, @ErzeugtAm, @Veroeffentlicht, @Icon, @Abteilung, @Referat,
                     @AntragVorhanden, @BescheidNeufassung, @Ansprechpartner, @MandantId)";

                using var cmd = new SqlCommand(query, connection);
                cmd.Parameters.Add("@Aktenzeichen", SqlDbType.NVarChar).Value = aktenzeichen;
                cmd.Parameters.Add("@UserId", SqlDbType.NVarChar).Value = (object)userId ?? DBNull.Value;
                cmd.Parameters.Add("@Stelle", SqlDbType.NVarChar).Value = (object)stelle ?? DBNull.Value;
                cmd.Parameters.Add("@Zulassungsnummer", SqlDbType.NVarChar).Value = zulassungsnummer;
                cmd.Parameters.Add("@DatumAntrag", SqlDbType.DateTime2).Value = datumAntrag;
                cmd.Parameters.Add("@Antragsart", SqlDbType.NVarChar).Value = (object)antragsart ?? DBNull.Value;
                cmd.Parameters.Add("@Bescheidtyp", SqlDbType.NVarChar).Value = (object)bescheidtyp ?? DBNull.Value;
                cmd.Parameters.Add("@Gegenstand", SqlDbType.NVarChar).Value = (object)gegenstand ?? DBNull.Value;
                cmd.Parameters.Add("@Status", SqlDbType.NVarChar).Value = (object)status ?? DBNull.Value;
                cmd.Parameters.Add("@Betreff", SqlDbType.NVarChar).Value = (object)betreff ?? DBNull.Value;
                cmd.Parameters.Add("@Bemerkung", SqlDbType.NVarChar).Value = (object)bemerkung ?? DBNull.Value;
                cmd.Parameters.Add("@ObjektId", SqlDbType.UniqueIdentifier).Value = guid;
                cmd.Parameters.Add("@Aktenzustand", SqlDbType.NVarChar).Value = (object)aktenzustand ?? DBNull.Value;
                cmd.Parameters.Add("@ErzeugtVon", SqlDbType.NVarChar).Value = (object)erzeugtVon ?? DBNull.Value;
                cmd.Parameters.Add("@ErzeugtAm", SqlDbType.DateTime2).Value = DateTime.Now;
                cmd.Parameters.Add("@Veroeffentlicht", SqlDbType.Bit).Value = veroeffentlicht;
                cmd.Parameters.Add("@Icon", SqlDbType.VarBinary).Value = (object)icon ?? DBNull.Value;
                cmd.Parameters.Add("@Abteilung", SqlDbType.NVarChar).Value = (object)abteilung ?? DBNull.Value;
                cmd.Parameters.Add("@Referat", SqlDbType.NVarChar).Value = (object)referat ?? DBNull.Value;
                cmd.Parameters.Add("@AntragVorhanden", SqlDbType.Bit).Value = antragVorhanden;
                cmd.Parameters.Add("@BescheidNeufassung", SqlDbType.Bit).Value = bescheidNeufassung;
                cmd.Parameters.Add("@Ansprechpartner", SqlDbType.NVarChar).Value = (object)ansprechpartner ?? DBNull.Value;
                cmd.Parameters.Add("@MandantId", SqlDbType.Int).Value = _mandantProvider.MandantId;
                cmd.ExecuteNonQuery();

                return guid;
            }
            catch (Exception ex)
            {
                throw new RepositoryException($"CreateVorgang fehlgeschlagen. Aktenzeichen: {aktenzeichen}, Zulassungsnummer: {zulassungsnummer}. Fehler: {ex.Message}", ex);
            }
        }

        // Erstellt einen Ordner mit den übergebenen Parametern.
        public void CreateOrdner(Guid parentObjectId, string name, string type, string creatorName, string modifier, string ownerName, int ownerPermit, int groupPermit, int worldPermit, string hauptsachgebiet, string sachgebiet, string aktenzeichen, int lfdNr, string bemerkungen, string status, bool final, string path, string colorHex, string aclName, string aclDomain, bool isHidden, bool isDeleted, bool isPublic, bool isSigned)
        {
            try
            {
                // RestClass 
                var restClass = new DcmToWs.RestClass();
                var guid = restClass.CreateGuid();

                using var connection = new SqlConnection(_connectionString);
                connection.Open();

                var query = @"INSERT INTO [Ordner] 
                            (ObjektId, ParentObjectId, Name, Type, CreatorName, CreationDate, Modifier, ModifyDate, 
                            OwnerName, OwnerPermit, GroupPermit, WorldPermit, Hauptsachgebiet, Sachgebiet, 
                            Aktenzeichen, LfdNr, Bemerkungen, Status, Final, Path, ColorHex, ACLName, ACLDomain, 
                            IsHidden, IsDeleted, IsPublic, IsSigned, MandantId, SubType) 
                            VALUES 
                            (@ObjektId, @ParentObjectId, @Name, @Type, @CreatorName, @CreationDate, @Modifier, @ModifyDate, 
                            @OwnerName, @OwnerPermit, @GroupPermit, @WorldPermit, @Hauptsachgebiet, @Sachgebiet, 
                            @Aktenzeichen, @LfdNr, @Bemerkungen, @Status, @Final, @Path, @ColorHex, @ACLName, @ACLDomain, 
                            @IsHidden, @IsDeleted, @IsPublic, @IsSigned, @MandantId, @SubType)";

                using var cmd = new SqlCommand(query, connection);
                cmd.Parameters.Add("@ObjektId", SqlDbType.UniqueIdentifier).Value = guid;
                cmd.Parameters.Add("@ParentObjectId", SqlDbType.UniqueIdentifier).Value = (object)parentObjectId == null ? DBNull.Value : parentObjectId;
                cmd.Parameters.Add("@Name", SqlDbType.NVarChar).Value = name;
                cmd.Parameters.Add("@Type", SqlDbType.NVarChar).Value = (object)type ?? DBNull.Value;
                cmd.Parameters.Add("@CreatorName", SqlDbType.NVarChar).Value = (object)creatorName ?? DBNull.Value;
                cmd.Parameters.Add("@CreationDate", SqlDbType.DateTime2).Value = DateTime.Now;
                cmd.Parameters.Add("@Modifier", SqlDbType.NVarChar).Value = (object)modifier ?? DBNull.Value;
                cmd.Parameters.Add("@ModifyDate", SqlDbType.DateTime2).Value = DateTime.Now;
                cmd.Parameters.Add("@OwnerName", SqlDbType.NVarChar).Value = (object)ownerName ?? DBNull.Value;
                cmd.Parameters.Add("@OwnerPermit", SqlDbType.Int).Value = ownerPermit;
                cmd.Parameters.Add("@GroupPermit", SqlDbType.Int).Value = groupPermit;
                cmd.Parameters.Add("@WorldPermit", SqlDbType.Int).Value = worldPermit;
                cmd.Parameters.Add("@Hauptsachgebiet", SqlDbType.NVarChar).Value = (object)hauptsachgebiet ?? DBNull.Value;
                cmd.Parameters.Add("@Sachgebiet", SqlDbType.NVarChar).Value = (object)sachgebiet ?? DBNull.Value;
                cmd.Parameters.Add("@Aktenzeichen", SqlDbType.NVarChar).Value = (object)aktenzeichen ?? DBNull.Value;
                cmd.Parameters.Add("@LfdNr", SqlDbType.Int).Value = lfdNr;
                cmd.Parameters.Add("@Bemerkungen", SqlDbType.NVarChar).Value = (object)bemerkungen ?? DBNull.Value;
                cmd.Parameters.Add("@Status", SqlDbType.NVarChar).Value = (object)status ?? DBNull.Value;
                cmd.Parameters.Add("@Final", SqlDbType.Bit).Value = final;
                cmd.Parameters.Add("@Path", SqlDbType.NVarChar).Value = (object)path ?? DBNull.Value;
                cmd.Parameters.Add("@ColorHex", SqlDbType.NVarChar).Value = (object)colorHex ?? DBNull.Value;
                cmd.Parameters.Add("@ACLName", SqlDbType.NVarChar).Value = (object)aclName ?? DBNull.Value;
                cmd.Parameters.Add("@ACLDomain", SqlDbType.NVarChar).Value = (object)aclDomain ?? DBNull.Value;
                cmd.Parameters.Add("@IsHidden", SqlDbType.Bit).Value = isHidden;
                cmd.Parameters.Add("@IsDeleted", SqlDbType.Bit).Value = isDeleted;
                cmd.Parameters.Add("@IsPublic", SqlDbType.Bit).Value = isPublic; 
                cmd.Parameters.Add("@IsSigned", SqlDbType.Bit).Value = isSigned;
                cmd.Parameters.Add("@MandantId", SqlDbType.Int).Value = _mandantProvider.MandantId;
                cmd.Parameters.Add("@SubType", SqlDbType.NVarChar).Value = name;
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                throw new RepositoryException($"CreateOrdner fehlgeschlagen. Name: {name}, Aktenzeichen: {aktenzeichen}. Fehler: {ex.Message}", ex);
            }
        }



        // + Mandanten id.
        // Aktualisiert nur GeandertVon/GeandertAm einer Akte anhand der Zulassungsnummer.
        public void UpdateAkteGeandertInfo(string zulassungsnummer, string geandertVon, DateTime geandertAm)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                connection.Open();
                var query = @"UPDATE [Akte]
                       SET GeandertVon = @GeandertVon, GeandertAm = @GeandertAm
                       WHERE Zulassungsnummer = @Zulassungsnummer AND MandantId = @MandantId";
                using var cmd = new SqlCommand(query, connection);
                cmd.Parameters.Add("@Zulassungsnummer", SqlDbType.NVarChar).Value = zulassungsnummer;
                cmd.Parameters.Add("@GeandertVon", SqlDbType.NVarChar).Value = (object)geandertVon ?? DBNull.Value;
                cmd.Parameters.Add("@GeandertAm", SqlDbType.DateTime2).Value = geandertAm;
                cmd.Parameters.Add("@MandantId", SqlDbType.Int).Value = _mandantProvider.MandantId;
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                throw new RepositoryException("Fehler in der UpdateAkteGeandertInfo Funktion.", ex);
            }
        }

        // Aktualisiert einen Vorgang anhand des Aktenzeichens.
        public void UpdateVorgang(string aktenzeichen, DateTime datumAntrag, string antragsart, string bescheidtyp, string gegenstand, string status, string betreff, string bemerkung, string aktenzustand, bool veroeffentlicht, string erzeugtVon, DateTime erzeugtAm, bool antragVorhanden, bool bescheidNeufassung, string ansprechpartner)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                connection.Open();
                var query = @"UPDATE [Vorgang] 
                       SET DatumAntrag = @DatumAntrag, Antragsart = @Antragsart, 
                       Bescheidtyp = @Bescheidtyp, Gegenstand = @Gegenstand, Status = @Status, Betreff = @Betreff, 
                       Bemerkung = @Bemerkung, Aktenzustand = @Aktenzustand, Veroeffentlicht = @Veroeffentlicht, ErzeugtVon = @ErzeugtVon, ErzeugtAm = @ErzeugtAm,
                       AntragVorhanden = @AntragVorhanden, BescheidNeufassung = @BescheidNeufassung, Ansprechpartner = @Ansprechpartner
                       WHERE Aktenzeichen = @Aktenzeichen AND MandantId = @MandantId";
                using var cmd = new SqlCommand(query, connection);
                cmd.Parameters.Add("@DatumAntrag", SqlDbType.DateTime2).Value = datumAntrag;
                cmd.Parameters.Add("@Antragsart", SqlDbType.NVarChar).Value = (object)antragsart ?? DBNull.Value;
                cmd.Parameters.Add("@Bescheidtyp", SqlDbType.NVarChar).Value = (object)bescheidtyp ?? DBNull.Value;
                cmd.Parameters.Add("@Gegenstand", SqlDbType.NVarChar).Value = (object)gegenstand ?? DBNull.Value;
                cmd.Parameters.Add("@Status", SqlDbType.NVarChar).Value = (object)status ?? DBNull.Value;
                cmd.Parameters.Add("@Betreff", SqlDbType.NVarChar).Value = (object)betreff ?? DBNull.Value;
                cmd.Parameters.Add("@Bemerkung", SqlDbType.NVarChar).Value = (object)bemerkung ?? DBNull.Value;
                cmd.Parameters.Add("@Aktenzustand", SqlDbType.NVarChar).Value = (object)aktenzustand ?? DBNull.Value;
                cmd.Parameters.Add("@Veroeffentlicht", SqlDbType.Bit).Value = veroeffentlicht;
                cmd.Parameters.Add("@ErzeugtVon", SqlDbType.NVarChar).Value = (object)erzeugtVon ?? DBNull.Value;
                cmd.Parameters.Add("@ErzeugtAm", SqlDbType.DateTime2).Value = erzeugtAm;
                cmd.Parameters.Add("@AntragVorhanden", SqlDbType.Bit).Value = antragVorhanden;
                cmd.Parameters.Add("@BescheidNeufassung", SqlDbType.Bit).Value = bescheidNeufassung;
                cmd.Parameters.Add("@Ansprechpartner", SqlDbType.NVarChar).Value = (object)ansprechpartner ?? DBNull.Value;
                cmd.Parameters.Add("@Aktenzeichen", SqlDbType.NVarChar).Value = aktenzeichen;
                cmd.Parameters.Add("@MandantId", SqlDbType.Int).Value = _mandantProvider.MandantId;
                var result = cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                throw new RepositoryException("Fehler in der UpdateVorgang Funktion.", ex);
            }
        }



        // + Mandanten id.
        // Ordner.   
        // Erstellt einen Ordner anhand der übergebenen Ordner-Objekts.
        public void CreateOrdner(Ordner ordner)
        {
            try
            {
                var restClass = new DcmToWs.RestClass();
                var guid = restClass.CreateGuid();

                using var connection = new SqlConnection(_connectionString);
                connection.Open();

                // Überprüfen, ob der übergeordnete Ordner einen SubType hat.
                string? subType = ordner.SubType;
                if (ordner.ParentObjectId.HasValue)
                {
                    const string parentQuery = @"SELECT SubType FROM [Ordner] WHERE ObjektId = @ParentObjectId AND MandantId = @MandantId";

                    using var parentCmd = new SqlCommand(parentQuery, connection);
                    parentCmd.Parameters.Add("@ParentObjectId", SqlDbType.UniqueIdentifier).Value = ordner.ParentObjectId.Value;
                    parentCmd.Parameters.Add("@MandantId", SqlDbType.Int).Value = _mandantProvider.MandantId;

                    var result = parentCmd.ExecuteScalar();
                    if (result != null && result != DBNull.Value)
                    {
                        subType = (string)result;
                    }
                }

                var query = @"INSERT INTO [Ordner] 
                            (ObjektId, ParentObjectId, Name, CreationDate, Type, SubType, CreatorName, Modifier, ModifyDate, 
                            OwnerName, OwnerPermit, GroupPermit, WorldPermit, Hauptsachgebiet, Sachgebiet, 
                            Aktenzeichen, LfdNr, Bemerkungen, Status, Final, Path, ColorHex, 
                            ACLName, ACLDomain, IsHidden, IsDeleted, IsPublic, IsSigned, MandantId) 
                            VALUES 
                            (@ObjektId, @ParentObjectId, @Name, @CreationDate, @Type, @SubType, @CreatorName, @Modifier, @ModifyDate, 
                            @OwnerName, @OwnerPermit, @GroupPermit, @WorldPermit, @Hauptsachgebiet, @Sachgebiet, 
                            @Aktenzeichen, @LfdNr, @Bemerkungen, @Status, @Final, @Path, @ColorHex, 
                            @ACLName, @ACLDomain, @IsHidden, @IsDeleted, @IsPublic, @IsSigned, @MandantId)";

                using var cmd = new SqlCommand(query, connection);
                cmd.Parameters.Add("@ObjektId", SqlDbType.UniqueIdentifier).Value = guid;
                cmd.Parameters.Add("@ParentObjectId", SqlDbType.UniqueIdentifier).Value = (object?)ordner.ParentObjectId ?? DBNull.Value;
                cmd.Parameters.Add("@Name", SqlDbType.NVarChar).Value = ordner.Name;
                cmd.Parameters.Add("@CreationDate", SqlDbType.DateTime2).Value = DateTime.Now;
                cmd.Parameters.Add("@Type", SqlDbType.NVarChar).Value = (object?)ordner.Type ?? DBNull.Value;
                cmd.Parameters.Add("@SubType", SqlDbType.NVarChar).Value = (object?)subType ?? DBNull.Value;
                cmd.Parameters.Add("@CreatorName", SqlDbType.NVarChar).Value = (object?)ordner.CreatorName ?? DBNull.Value;
                cmd.Parameters.Add("@Modifier", SqlDbType.NVarChar).Value = (object?)ordner.Modifier ?? DBNull.Value;
                cmd.Parameters.Add("@ModifyDate", SqlDbType.DateTime2).Value = (object?)ordner.ModifyDate ?? DBNull.Value;
                cmd.Parameters.Add("@OwnerName", SqlDbType.NVarChar).Value = (object?)ordner.OwnerName ?? DBNull.Value;
                cmd.Parameters.Add("@OwnerPermit", SqlDbType.Int).Value = (object?)ordner.OwnerPermit ?? DBNull.Value;
                cmd.Parameters.Add("@GroupPermit", SqlDbType.Int).Value = (object?)ordner.GroupPermit ?? DBNull.Value;
                cmd.Parameters.Add("@WorldPermit", SqlDbType.Int).Value = (object?)ordner.WorldPermit ?? DBNull.Value;
                cmd.Parameters.Add("@Hauptsachgebiet", SqlDbType.NVarChar).Value = (object?)ordner.Hauptsachgebiet ?? DBNull.Value;
                cmd.Parameters.Add("@Sachgebiet", SqlDbType.NVarChar).Value = (object?)ordner.Sachgebiet ?? DBNull.Value;
                cmd.Parameters.Add("@Aktenzeichen", SqlDbType.NVarChar).Value = (object?)ordner.Aktenzeichen ?? DBNull.Value;
                cmd.Parameters.Add("@LfdNr", SqlDbType.Int).Value = (object?)ordner.LfdNr ?? DBNull.Value;
                cmd.Parameters.Add("@Bemerkungen", SqlDbType.NVarChar).Value = (object?)ordner.Bemerkungen ?? DBNull.Value;
                cmd.Parameters.Add("@Status", SqlDbType.NVarChar).Value = (object?)ordner.Status ?? DBNull.Value;
                cmd.Parameters.Add("@Final", SqlDbType.Bit).Value = ordner.Final;
                cmd.Parameters.Add("@Path", SqlDbType.NVarChar).Value = (object?)ordner.Path ?? DBNull.Value;
                cmd.Parameters.Add("@ColorHex", SqlDbType.NVarChar).Value = (object?)ordner.ColorHex ?? DBNull.Value;
                cmd.Parameters.Add("@ACLName", SqlDbType.NVarChar).Value = (object?)ordner.ACLName ?? DBNull.Value;
                cmd.Parameters.Add("@ACLDomain", SqlDbType.NVarChar).Value = (object?)ordner.ACLDomain ?? DBNull.Value;
                cmd.Parameters.Add("@IsHidden", SqlDbType.Bit).Value = ordner.IsHidden;
                cmd.Parameters.Add("@IsDeleted", SqlDbType.Bit).Value = ordner.IsDeleted;
                cmd.Parameters.Add("@IsPublic", SqlDbType.Bit).Value = ordner.IsPublic;
                cmd.Parameters.Add("@IsSigned", SqlDbType.Bit).Value = ordner.IsSigned;
                cmd.Parameters.Add("@MandantId", SqlDbType.Int).Value = _mandantProvider.MandantId;

                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                throw new RepositoryException($"CreateOrdner fehlgeschlagen. Name: {ordner.Name}, Aktenzeichen: {ordner.Aktenzeichen}. Fehler: {ex.Message}", ex);
            }
        }

        // Aktualisiert einen Ordner anhand der übergebenen Ordner-Objekts.
        public void UpdateOrdner(Ordner ordner)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                connection.Open();

                var query = @"UPDATE [Ordner] SET
                            Name        = @Name,
                            Bemerkungen = @Bemerkungen,
                            ColorHex    = @ColorHex,
                            Modifier    = @Modifier,
                            ModifyDate  = @ModifyDate
                            WHERE ObjektId  = @ObjektId AND MandantId = @MandantId";

                using var cmd = new SqlCommand(query, connection);
                cmd.Parameters.Add("@ObjektId", SqlDbType.UniqueIdentifier).Value = ordner.ObjektId;
                cmd.Parameters.Add("@Name", SqlDbType.NVarChar).Value = ordner.Name;
                cmd.Parameters.Add("@Bemerkungen", SqlDbType.NVarChar).Value = (object?)ordner.Bemerkungen ?? DBNull.Value;
                cmd.Parameters.Add("@ColorHex", SqlDbType.NVarChar).Value = (object?)ordner.ColorHex ?? DBNull.Value;
                cmd.Parameters.Add("@Modifier", SqlDbType.NVarChar).Value = (object?)ordner.Modifier ?? DBNull.Value;
                cmd.Parameters.Add("@ModifyDate", SqlDbType.DateTime2).Value = (object?)ordner.ModifyDate ?? DBNull.Value;
                cmd.Parameters.Add("@MandantId", SqlDbType.Int).Value = _mandantProvider.MandantId;

                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                throw new RepositoryException($"UpdateOrdner2 fehlgeschlagen. ObjektId: {ordner.ObjektId}, Name: {ordner.Name}. Fehler: {ex.Message}", ex);
            }
        }

        // Löscht einen Ordner, sofern keine Unterordner vorhanden sind.
        public void DeleteOrdner(Guid objektId)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                connection.Open();

                // Prüft, ob Unterordner vorhanden sind.
                var checkQuery = "SELECT COUNT(1) FROM [Ordner] WHERE ParentObjectId = @ObjektId AND MandantId = @MandantId";
                using var checkCmd = new SqlCommand(checkQuery, connection);
                checkCmd.Parameters.Add("@ObjektId", SqlDbType.UniqueIdentifier).Value = objektId;
                checkCmd.Parameters.Add("@MandantId", SqlDbType.Int).Value = _mandantProvider.MandantId;

                var childCount = (int)checkCmd.ExecuteScalar();
                if (childCount > 0)
                {
                    throw new InvalidOperationException("Ordner kann nicht gelöscht werden, da er Unterordner enthält.");
                }

                var deleteQuery = "DELETE FROM [Ordner] WHERE ObjektId = @ObjektId AND MandantId = @MandantId";
                using var deleteCmd = new SqlCommand(deleteQuery, connection);
                deleteCmd.Parameters.Add("@ObjektId", SqlDbType.UniqueIdentifier).Value = objektId;
                deleteCmd.Parameters.Add("@MandantId", SqlDbType.Int).Value = _mandantProvider.MandantId;
                deleteCmd.ExecuteNonQuery();
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new RepositoryException("Fehler in der DeleteOrdner Funktion.", ex);
            }
        }

        // Benennt einen Ordner um.
        public void RenameOrdner(Guid objektId, string name)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                connection.Open();

                var query = "UPDATE [Ordner] SET Name = @Name WHERE ObjektId = @ObjektId AND MandantId = @MandantId";
                using var cmd = new SqlCommand(query, connection);
                cmd.Parameters.Add("@Name", SqlDbType.NVarChar, 100).Value = name;
                cmd.Parameters.Add("@ObjektId", SqlDbType.UniqueIdentifier).Value = objektId;
                cmd.Parameters.Add("@MandantId", SqlDbType.Int).Value = _mandantProvider.MandantId;

                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                throw new RepositoryException("Fehler in der RenameOrdner Funktion.", ex);
            }
        }



        // + Mandanten id.
        // Dokumente. 
        // Erstellt ein Dokument anhand der übergebenen Dokument-Objekts.
        public void CreateDokument(Dokument dok)
        {
            try
            {
                var restClass = new DcmToWs.RestClass();
                var dokumentGuid = restClass.CreateGuid();
                var versionGuid = restClass.CreateGuid();

                using var connection = new SqlConnection(_connectionString);
                connection.Open();
                using var transaction = connection.BeginTransaction();

                try
                {
                    // 1) Dokument anlegen.
                    var dokQuery = @"
                                   INSERT INTO [Dokumente]
                                   (ObjektId, FolderId, Name, Type, ContentType, CreatorName, CreationDate,
                                   IsHidden, IsDeleted, IsPublic, IsSigned, Final,
                                   Aktenzeichen, ACLName, IsCheckedOut,
                                   Subject, Gegenstand, OwnerName, VersionLabel, Antragsteller, Eingangsdatum, MandantId, Absender, Empfaenger)
                                   VALUES
                                   (@ObjektId, @FolderId, @Name, @Type, @ContentType, @CreatorName, @CreationDate,
                                   @IsHidden, @IsDeleted, @IsPublic, @IsSigned, @Final,
                                   @Aktenzeichen, @ACLName, @IsCheckedOut,
                                   @Subject, @Gegenstand, @OwnerName, @VersionLabel, @Antragsteller, @Eingangsdatum, @MandantId, @Absender, @Empfaenger)";

                    using var dokCmd = new SqlCommand(dokQuery, connection, transaction);
                    dokCmd.Parameters.Add("@ObjektId", SqlDbType.UniqueIdentifier).Value = dokumentGuid;
                    dokCmd.Parameters.Add("@FolderId", SqlDbType.UniqueIdentifier).Value = dok.FolderId;
                    dokCmd.Parameters.Add("@Name", SqlDbType.NVarChar).Value = dok.Name;
                    dokCmd.Parameters.Add("@ContentType", SqlDbType.NVarChar).Value = (object?)dok.ContentType ?? DBNull.Value;
                    dokCmd.Parameters.Add("@CreatorName", SqlDbType.NVarChar).Value = (object?)dok.CreatorName ?? DBNull.Value;
                    dokCmd.Parameters.Add("@CreationDate", SqlDbType.DateTime2).Value = DateTime.Now;
                    dokCmd.Parameters.Add("@IsHidden", SqlDbType.Bit).Value = dok.IsHidden;
                    dokCmd.Parameters.Add("@IsDeleted", SqlDbType.Bit).Value = dok.IsDeleted;
                    dokCmd.Parameters.Add("@IsPublic", SqlDbType.Bit).Value = dok.IsPublic;
                    dokCmd.Parameters.Add("@IsSigned", SqlDbType.Bit).Value = dok.IsSigned;
                    dokCmd.Parameters.Add("@Final", SqlDbType.Bit).Value = dok.Final;
                    dokCmd.Parameters.Add("@Aktenzeichen", SqlDbType.NVarChar).Value = (object?)dok.Aktenzeichen ?? DBNull.Value;
                    dokCmd.Parameters.Add("@ACLName", SqlDbType.NVarChar).Value = (object?)dok.ACLName ?? DBNull.Value;
                    dokCmd.Parameters.Add("@Type", SqlDbType.NVarChar).Value = (object?)dok.Type ?? "Dokument";
                    dokCmd.Parameters.Add("@IsCheckedOut", SqlDbType.Bit).Value = false;

                    dokCmd.Parameters.Add("@Subject", SqlDbType.NVarChar).Value = (object?)dok.Subject ?? DBNull.Value;
                    dokCmd.Parameters.Add("@Gegenstand", SqlDbType.NVarChar).Value = (object?)dok.Gegenstand ?? DBNull.Value;
                    dokCmd.Parameters.Add("@OwnerName", SqlDbType.NVarChar).Value = (object?)dok.OwnerName ?? DBNull.Value;
                    dokCmd.Parameters.Add("@VersionLabel", SqlDbType.NVarChar).Value = (object?)dok.VersionLabel ?? DBNull.Value;
                    dokCmd.Parameters.Add("@Antragsteller", SqlDbType.NVarChar).Value = (object?)dok.Antragsteller ?? DBNull.Value;
                    dokCmd.Parameters.Add("@Eingangsdatum", SqlDbType.DateTime2).Value = (object?)dok.Eingangsdatum ?? DBNull.Value;

                    dokCmd.Parameters.Add("@Absender", SqlDbType.NVarChar).Value = (object?)dok.Absender ?? DBNull.Value;
                    dokCmd.Parameters.Add("@Empfaenger", SqlDbType.NVarChar).Value = (object?)dok.Empfaenger ?? DBNull.Value;

                    dokCmd.Parameters.Add("@MandantId", SqlDbType.Int).Value = _mandantProvider.MandantId;

                    dokCmd.ExecuteNonQuery();

                    // 2) Version 1.0 anlegen.
                    var v = dok.Versionen[0];
                    var verQuery = @"
                                   INSERT INTO [DokumentVersionen]
                                   (CurrentVersionId, DocumentObjektId, VersionNr, IsActive, IsArchived,
                                   CreatedDate, CreatedBy, FileStream, ContentType, ContentSize,
                                   FullContentSize, FileExtension, IsSigned, Final, Bemerkungen, Description)
                                   VALUES
                                   (@CurrentVersionId, @DocumentObjektId, @VersionNr, @IsActive, @IsArchived,
                                   @CreatedDate, @CreatedBy, @FileStream, @ContentType, @ContentSize,
                                   @FullContentSize, @FileExtension, @IsSigned, @Final, @Bemerkungen, @Description)";

                    using var verCmd = new SqlCommand(verQuery, connection, transaction);
                    verCmd.Parameters.Add("@CurrentVersionId", SqlDbType.UniqueIdentifier).Value = versionGuid;
                    verCmd.Parameters.Add("@DocumentObjektId", SqlDbType.UniqueIdentifier).Value = dokumentGuid;
                    verCmd.Parameters.Add("@VersionNr", SqlDbType.NVarChar).Value = "1.0";
                    verCmd.Parameters.Add("@IsActive", SqlDbType.Bit).Value = true;
                    verCmd.Parameters.Add("@IsArchived", SqlDbType.Bit).Value = false;
                    verCmd.Parameters.Add("@CreatedDate", SqlDbType.DateTime2).Value = DateTime.Now;
                    verCmd.Parameters.Add("@CreatedBy", SqlDbType.NVarChar).Value = (object?)v.CreatedBy ?? DBNull.Value;
                    verCmd.Parameters.Add("@FileStream", SqlDbType.VarBinary).Value = (object?)v.FileStream ?? DBNull.Value;
                    verCmd.Parameters.Add("@ContentType", SqlDbType.NVarChar).Value = (object?)v.ContentType ?? DBNull.Value;
                    verCmd.Parameters.Add("@ContentSize", SqlDbType.BigInt).Value = v.ContentSize;
                    verCmd.Parameters.Add("@FullContentSize", SqlDbType.BigInt).Value = v.FullContentSize;
                    verCmd.Parameters.Add("@FileExtension", SqlDbType.NVarChar).Value = (object?)v.FileExtension ?? DBNull.Value;
                    verCmd.Parameters.Add("@IsSigned", SqlDbType.Bit).Value = v.IsSigned;
                    verCmd.Parameters.Add("@Final", SqlDbType.Bit).Value = v.Final;
                    verCmd.Parameters.Add("@Bemerkungen", SqlDbType.NVarChar).Value = (object?)v.Bemerkungen ?? DBNull.Value;
                    verCmd.Parameters.Add("@Description", SqlDbType.NVarChar).Value = (object?)v.Description ?? DBNull.Value;
                    verCmd.ExecuteNonQuery();

                    transaction.Commit();
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
            catch (Exception ex)
            {
                throw new RepositoryException($"CreateDokument fehlgeschlagen. Name: {dok.Name}. Fehler: {ex.Message}", ex);
            }
        }

        // Aktualisiert ein Dokument anhand der übergebenen Dokument-Objekts.
        public void UpdateDokument(Guid dokumentObjektId, string name, string contentType, DokumentVersion newVersion, string modifierName)
        {
            try
            {
                var restClass = new DcmToWs.RestClass();
                var versionGuid = restClass.CreateGuid();
                var now = DateTime.Now;

                using var connection = new SqlConnection(_connectionString);
                connection.Open();
                using var transaction = connection.BeginTransaction();

                try
                {
                    // 0) Prüft, ob das Dokument zum aktuellen Mandanten gehört.
                    var checkQuery = "SELECT COUNT(1) FROM [Dokumente] WHERE ObjektId = @ObjektId AND MandantId = @MandantId";
                    using (var checkCmd = new SqlCommand(checkQuery, connection, transaction))
                    {
                        checkCmd.Parameters.Add("@ObjektId", SqlDbType.UniqueIdentifier).Value = dokumentObjektId;
                        checkCmd.Parameters.Add("@MandantId", SqlDbType.Int).Value = _mandantProvider.MandantId;

                        var exists = (int)checkCmd.ExecuteScalar();
                        if (exists == 0)
                        {
                            throw new RepositoryException($"Dokument nicht gefunden. ObjektId: {dokumentObjektId}.");
                        }
                    }

                    // 1) Aktuelle (aktive) VersionNr ermitteln und die Hauptversion (Ganzzahlteil) inkrementieren.
                    var getMaxVerQuery = @"
                                         SELECT TOP 1 VersionNr 
                                         FROM [DokumentVersionen]
                                         WHERE DocumentObjektId = @DocumentObjektId
                                         AND IsActive = 1
                                         ORDER BY CreatedDate DESC";

                    string nextVersionNr;
                    using (var getMaxVerCmd = new SqlCommand(getMaxVerQuery, connection, transaction))
                    {
                        getMaxVerCmd.Parameters.Add("@DocumentObjektId", SqlDbType.UniqueIdentifier).Value = dokumentObjektId;
                        var result = getMaxVerCmd.ExecuteScalar()?.ToString();

                        if (result != null &&
                            double.TryParse(result, System.Globalization.NumberStyles.Any,
                                System.Globalization.CultureInfo.InvariantCulture, out double ver))
                        {
                            // (int)ver schneidet die Nachkommastellen ab, unabhängig davon, wie groß der Minor-Anteil durch UpdateDokumentSameVersion inzwischen geworden ist.
                            nextVersionNr = $"{(int)ver + 1}.0";
                        }
                        else
                        {
                            nextVersionNr = "1.0";
                        }
                    }

                    // 2) Bisher aktive Version(en) deaktivieren: IsActive=0, Final=0, und als "geändert" markieren (ModifiedDate/ModifiedBy).
                    var deactivateQuery = @"
                                          UPDATE [DokumentVersionen]
                                          SET IsActive = 0,
                                              Final = 0,
                                              ModifiedDate = @ModifiedDate,
                                              ModifiedBy = @ModifiedBy
                                          WHERE DocumentObjektId = @DocumentObjektId
                                          AND IsActive = 1";

                    using (var deactivateCmd = new SqlCommand(deactivateQuery, connection, transaction))
                    {
                        deactivateCmd.Parameters.Add("@DocumentObjektId", SqlDbType.UniqueIdentifier).Value = dokumentObjektId;
                        deactivateCmd.Parameters.Add("@ModifiedDate", SqlDbType.DateTime2).Value = now;
                        deactivateCmd.Parameters.Add("@ModifiedBy", SqlDbType.NVarChar).Value = (object?)modifierName ?? DBNull.Value;
                        deactivateCmd.ExecuteNonQuery();
                    }

                    // 3) Neue Version einfügen.
                    var insertVerQuery = @"
                                         INSERT INTO [DokumentVersionen]
                                         (CurrentVersionId, DocumentObjektId, VersionNr, IsActive, IsArchived,
                                         CreatedDate, CreatedBy, FileStream, ContentType, ContentSize,
                                         FullContentSize, FileExtension, IsSigned, Final, Bemerkungen, Description)
                                         VALUES
                                         (@CurrentVersionId, @DocumentObjektId, @VersionNr, @IsActive, @IsArchived,
                                         @CreatedDate, @CreatedBy, @FileStream, @ContentType, @ContentSize,
                                         @FullContentSize, @FileExtension, @IsSigned, @Final, @Bemerkungen, @Description)";

                    using (var insertVerCmd = new SqlCommand(insertVerQuery, connection, transaction))
                    {
                        insertVerCmd.Parameters.Add("@CurrentVersionId", SqlDbType.UniqueIdentifier).Value = versionGuid;
                        insertVerCmd.Parameters.Add("@DocumentObjektId", SqlDbType.UniqueIdentifier).Value = dokumentObjektId;
                        insertVerCmd.Parameters.Add("@VersionNr", SqlDbType.NVarChar).Value = nextVersionNr;
                        insertVerCmd.Parameters.Add("@IsActive", SqlDbType.Bit).Value = true;
                        insertVerCmd.Parameters.Add("@IsArchived", SqlDbType.Bit).Value = false;
                        insertVerCmd.Parameters.Add("@CreatedDate", SqlDbType.DateTime2).Value = now;
                        insertVerCmd.Parameters.Add("@CreatedBy", SqlDbType.NVarChar).Value = (object?)newVersion.CreatedBy ?? DBNull.Value;
                        insertVerCmd.Parameters.Add("@FileStream", SqlDbType.VarBinary).Value = (object?)newVersion.FileStream ?? DBNull.Value;
                        insertVerCmd.Parameters.Add("@ContentType", SqlDbType.NVarChar).Value = (object?)newVersion.ContentType ?? DBNull.Value;
                        insertVerCmd.Parameters.Add("@ContentSize", SqlDbType.BigInt).Value = newVersion.ContentSize;
                        insertVerCmd.Parameters.Add("@FullContentSize", SqlDbType.BigInt).Value = newVersion.FullContentSize;
                        insertVerCmd.Parameters.Add("@FileExtension", SqlDbType.NVarChar).Value = (object?)newVersion.FileExtension ?? DBNull.Value;
                        insertVerCmd.Parameters.Add("@IsSigned", SqlDbType.Bit).Value = newVersion.IsSigned;
                        insertVerCmd.Parameters.Add("@Final", SqlDbType.Bit).Value = newVersion.Final;
                        insertVerCmd.Parameters.Add("@Bemerkungen", SqlDbType.NVarChar).Value = (object?)newVersion.Bemerkungen ?? DBNull.Value;
                        insertVerCmd.Parameters.Add("@Description", SqlDbType.NVarChar).Value = (object?)newVersion.Description ?? DBNull.Value;
                        insertVerCmd.ExecuteNonQuery();
                    }

                    // 4) Dokument-Metadaten aktualisieren: Name, ContentType, Modifier, ModifyDate.
                    var updateDokQuery = @"
                                         UPDATE [Dokumente]
                                         SET Name = @Name,
                                         ContentType = @ContentType,
                                         IsCheckedOut = 0,
                                         Modifier = @Modifier,
                                         ModifyDate = @ModifyDate
                                         WHERE ObjektId = @ObjektId AND MandantId = @MandantId";

                    using (var updateDokCmd = new SqlCommand(updateDokQuery, connection, transaction))
                    {
                        updateDokCmd.Parameters.Add("@Name", SqlDbType.NVarChar).Value = name;
                        updateDokCmd.Parameters.Add("@ContentType", SqlDbType.NVarChar).Value = (object?)contentType ?? DBNull.Value;
                        updateDokCmd.Parameters.Add("@ObjektId", SqlDbType.UniqueIdentifier).Value = dokumentObjektId;
                        updateDokCmd.Parameters.Add("@Modifier", SqlDbType.NVarChar).Value = (object?)modifierName ?? DBNull.Value;
                        updateDokCmd.Parameters.Add("@ModifyDate", SqlDbType.DateTime2).Value = now;
                        updateDokCmd.Parameters.Add("@MandantId", SqlDbType.Int).Value = _mandantProvider.MandantId;
                        updateDokCmd.ExecuteNonQuery();
                    }

                    transaction.Commit();
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
            catch (Exception ex)
            {
                throw new RepositoryException($"UpdateDokument fehlgeschlagen. ObjektId: {dokumentObjektId}. Fehler: {ex.Message}", ex);
            }
        }

        // Aktualisiert die aktuell aktive Version eines Dokuments, OHNE eine neue Version anzulegen.
        public void UpdateDokumentSameVersion(Guid dokumentObjektId, byte[] fileStream, string contentType, string modifierName)
        {
            try
            {
                var now = DateTime.Now;

                using var connection = new SqlConnection(_connectionString);
                connection.Open();
                using var transaction = connection.BeginTransaction();

                try
                {
                    // 0) Prüft, ob das Dokument zum aktuellen Mandanten gehört.
                    var checkQuery = "SELECT COUNT(1) FROM [Dokumente] WHERE ObjektId = @ObjektId AND MandantId = @MandantId";
                    using (var checkCmd = new SqlCommand(checkQuery, connection, transaction))
                    {
                        checkCmd.Parameters.Add("@ObjektId", SqlDbType.UniqueIdentifier).Value = dokumentObjektId;
                        checkCmd.Parameters.Add("@MandantId", SqlDbType.Int).Value = _mandantProvider.MandantId;

                        var exists = (int)checkCmd.ExecuteScalar();
                        if (exists == 0)
                        {
                            throw new RepositoryException($"Dokument nicht gefunden. ObjektId: {dokumentObjektId}.");
                        }
                    }

                    // 1) Aktuelle aktive VersionNr ermitteln (gleichzeitig Prüfung, ob eine aktive Version existiert).
                    var getActiveVerQuery = @"
                                            SELECT TOP 1 VersionNr
                                            FROM [DokumentVersionen]
                                            WHERE DocumentObjektId = @DocumentObjektId
                                            AND IsActive = 1";

                    string currentVersionNr;
                    using (var getActiveVerCmd = new SqlCommand(getActiveVerQuery, connection, transaction))
                    {
                        getActiveVerCmd.Parameters.Add("@DocumentObjektId", SqlDbType.UniqueIdentifier).Value = dokumentObjektId;
                        var result = getActiveVerCmd.ExecuteScalar()?.ToString();
                        if (result == null)
                        {
                            throw new RepositoryException($"Keine aktive Version gefunden. ObjektId: {dokumentObjektId}.");
                        }
                        currentVersionNr = result;
                    }

                    var newVersionNr = ComputeNextMinorVersion(currentVersionNr);

                    // 2) Aktive Version aktualisieren (Inhalt + Größen + Minor-VersionNr + Änderungsinfo), OHNE eine neue Zeile/Version anzulegen.
                    var updateVerQuery = @"
                                         UPDATE [DokumentVersionen]
                                         SET FileStream = @FileStream,
                                             ContentType = @ContentType,
                                             ContentSize = @ContentSize,
                                             FullContentSize = @FullContentSize,
                                             VersionNr = @VersionNr,
                                             ModifiedDate = @ModifiedDate,
                                             ModifiedBy = @ModifiedBy
                                         WHERE DocumentObjektId = @DocumentObjektId
                                         AND IsActive = 1";

                    using (var updateVerCmd = new SqlCommand(updateVerQuery, connection, transaction))
                    {
                        updateVerCmd.Parameters.Add("@DocumentObjektId", SqlDbType.UniqueIdentifier).Value = dokumentObjektId;
                        updateVerCmd.Parameters.Add("@FileStream", SqlDbType.VarBinary).Value = (object?)fileStream ?? DBNull.Value;
                        updateVerCmd.Parameters.Add("@ContentType", SqlDbType.NVarChar).Value = (object?)contentType ?? DBNull.Value;
                        updateVerCmd.Parameters.Add("@ContentSize", SqlDbType.BigInt).Value = fileStream?.LongLength ?? 0L;
                        updateVerCmd.Parameters.Add("@FullContentSize", SqlDbType.BigInt).Value = fileStream?.LongLength ?? 0L;
                        updateVerCmd.Parameters.Add("@VersionNr", SqlDbType.NVarChar).Value = newVersionNr;
                        updateVerCmd.Parameters.Add("@ModifiedDate", SqlDbType.DateTime2).Value = now;
                        updateVerCmd.Parameters.Add("@ModifiedBy", SqlDbType.NVarChar).Value = (object?)modifierName ?? DBNull.Value;
                        updateVerCmd.ExecuteNonQuery();
                    }

                    // 3) Dokument-Metadaten aktualisieren: ContentType, CheckOut-Status, Modifier, ModifyDate.
                    var updateDokQuery = @"
                                         UPDATE [Dokumente]
                                         SET ContentType = @ContentType,
                                         IsCheckedOut = 0,
                                         Modifier = @Modifier,
                                         ModifyDate = @ModifyDate
                                         WHERE ObjektId = @ObjektId AND MandantId = @MandantId";

                    using (var updateDokCmd = new SqlCommand(updateDokQuery, connection, transaction))
                    {
                        updateDokCmd.Parameters.Add("@ContentType", SqlDbType.NVarChar).Value = (object?)contentType ?? DBNull.Value;
                        updateDokCmd.Parameters.Add("@ObjektId", SqlDbType.UniqueIdentifier).Value = dokumentObjektId;
                        updateDokCmd.Parameters.Add("@Modifier", SqlDbType.NVarChar).Value = (object?)modifierName ?? DBNull.Value;
                        updateDokCmd.Parameters.Add("@ModifyDate", SqlDbType.DateTime2).Value = now;
                        updateDokCmd.Parameters.Add("@MandantId", SqlDbType.Int).Value = _mandantProvider.MandantId;
                        updateDokCmd.ExecuteNonQuery();
                    }

                    transaction.Commit();
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
            catch (Exception ex)
            {
                throw new RepositoryException($"UpdateDokumentSameVersion fehlgeschlagen. ObjektId: {dokumentObjektId}. Fehler: {ex.Message}", ex);
            }
        }

        // Berechnet die nächste Unterversion innerhalb derselben Hauptversion.
        private static string ComputeNextMinorVersion(string currentVersionNr)
        {
            var parts = currentVersionNr.Split('.', 2);
            var major = parts[0];
            var minorPart = parts.Length > 1 ? parts[1] : "0";

            if (!long.TryParse(minorPart, System.Globalization.NumberStyles.None,
                    System.Globalization.CultureInfo.InvariantCulture, out long minorValue))
            {
                minorValue = 0;
            }

            return $"{major}.{minorValue + 1}";
        }

        // Kompatibilitäts-Wrapper für Aufrufer, die weiterhin nur eine einzelne Id kennen.
        public void DeleteDokument(Guid objectId)
        {
            DeleteDokumente(new[] { objectId });
        }

        // Markiert Dokumente als gelöscht (Soft-Delete). Versionen bleiben unangetastet.
        public List<Guid> DeleteDokumente(IEnumerable<Guid> objectIds)
        {
            var ids = objectIds?.Where(id => id != Guid.Empty).Distinct().ToList() ?? new List<Guid>();
            if (ids.Count == 0) return new List<Guid>();

            var skippedLocked = new List<Guid>();

            try
            {
                using var connection = new SqlConnection(_connectionString);
                connection.Open();
                using var transaction = connection.BeginTransaction();

                try
                {
                    foreach (var objectId in ids)
                    {
                        // 0) Zugehörigkeit zum Mandanten prüfen und Sperrstatus in einem Rutsch lesen.
                        var checkQuery = "SELECT LockedAt FROM [Dokumente] WHERE ObjektId = @objectId AND MandantId = @MandantId";
                        object lockedAtValue;
                        using (var checkCmd = new SqlCommand(checkQuery, connection, transaction))
                        {
                            checkCmd.Parameters.Add("@objectId", SqlDbType.UniqueIdentifier).Value = objectId;
                            checkCmd.Parameters.Add("@MandantId", SqlDbType.Int).Value = _mandantProvider.MandantId;
                            lockedAtValue = checkCmd.ExecuteScalar();
                        }

                        if (lockedAtValue == null)
                        {
                            // Dokument existiert nicht (mehr) bzw. gehört nicht zu diesem Mandanten.
                            throw new RepositoryException($"Dokument nicht gefunden. ObjektId: {objectId}.");
                        }

                        if (lockedAtValue != DBNull.Value)
                        {
                            // Wird gerade bearbeitet — überspringen, nicht die ganze Transaktion abbrechen.
                            skippedLocked.Add(objectId);
                            continue;
                        }

                        // 1) Dokument als gelöscht markieren (Versionen werden nicht angefasst).
                        using (var cmd = new SqlCommand(
                            "UPDATE [Dokumente] SET IsDeleted = 1 WHERE ObjektId = @objectId AND MandantId = @MandantId",
                            connection, transaction))
                        {
                            cmd.Parameters.Add("@objectId", SqlDbType.UniqueIdentifier).Value = objectId;
                            cmd.Parameters.Add("@MandantId", SqlDbType.Int).Value = _mandantProvider.MandantId;
                            cmd.ExecuteNonQuery();
                        }
                    }

                    transaction.Commit();
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
            catch (RepositoryException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new RepositoryException("Fehler in der DeleteDokumente Funktion.", ex);
            }

            return skippedLocked;
        }

        // Sperrt ein Dokument für den angegebenen Benutzer.
        public void LockDokument(Guid dokumentObjektId, string userId, DateTime lockedAt)
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();
            using var cmd = new SqlCommand(@"
                                           UPDATE [Dokumente]
                                           SET LockedFrom = @UserId,
                                                LockedAt = @LockedAt,
                                                IsCheckedOut = 1
                                           WHERE ObjektId = @ObjektId AND MandantId = @MandantId", connection);

            cmd.Parameters.Add("@UserId", SqlDbType.NVarChar).Value = userId;
            cmd.Parameters.Add("@LockedAt", SqlDbType.DateTime2).Value = lockedAt;
            cmd.Parameters.Add("@ObjektId", SqlDbType.UniqueIdentifier).Value = dokumentObjektId;
            cmd.Parameters.Add("@MandantId", SqlDbType.Int).Value = _mandantProvider.MandantId;
            cmd.ExecuteNonQuery();
        }

        // Sperre eines Dokuments aufheben.
        public void UnlockDokument(Guid dokumentObjektId)
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();
            using var cmd = new SqlCommand(@"
                                           UPDATE [Dokumente]
                                           SET LockedFrom = NULL,
                                                LockedAt = NULL,
                                                IsCheckedOut = 0
                                           WHERE ObjektId = @ObjektId AND MandantId = @MandantId", connection);

            cmd.Parameters.Add("@ObjektId", SqlDbType.UniqueIdentifier).Value = dokumentObjektId;
            cmd.Parameters.Add("@MandantId", SqlDbType.Int).Value = _mandantProvider.MandantId;
            cmd.ExecuteNonQuery();
        }

        // Prüft, ob das Dokument von einem anderen Benutzer gesperrt ist. Gibt Parameter den sperrenden Benutzer zurück (null, falls nicht gesperrt).
        public bool IsLockedByOther(Guid dokumentObjektId, string currentUserId, out string? lockedBy)
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();
            using var cmd = new SqlCommand(@"
                                           SELECT LockedFrom FROM [Dokumente]
                                           WHERE ObjektId = @ObjektId AND MandantId = @MandantId", connection);

            cmd.Parameters.Add("@ObjektId", SqlDbType.UniqueIdentifier).Value = dokumentObjektId;
            cmd.Parameters.Add("@MandantId", SqlDbType.Int).Value = _mandantProvider.MandantId;

            using var reader = cmd.ExecuteReader();
            if (reader.Read() && !reader.IsDBNull(reader.GetOrdinal("LockedFrom")))
            {
                lockedBy = reader.GetString(reader.GetOrdinal("LockedFrom"));
                return !string.Equals(lockedBy, currentUserId, StringComparison.OrdinalIgnoreCase);
            }

            lockedBy = null;
            return false;
        }

        // Stellt Dokumente wieder her (Soft-Delete rückgängig machen).
        public void RestoreDokumente(IEnumerable<Guid> objectIds)
        {
            var ids = objectIds?.Where(id => id != Guid.Empty).Distinct().ToList() ?? new List<Guid>();
            if (ids.Count == 0) return;

            try
            {
                using var connection = new SqlConnection(_connectionString);
                connection.Open();
                using var transaction = connection.BeginTransaction();

                try
                {
                    foreach (var objectId in ids)
                    {
                        using var cmd = new SqlCommand("UPDATE [Dokumente] SET IsDeleted = 0 WHERE ObjektId = @objectId AND MandantId = @MandantId", connection, transaction);
                        cmd.Parameters.Add("@objectId", SqlDbType.UniqueIdentifier).Value = objectId;
                        cmd.Parameters.Add("@MandantId", SqlDbType.Int).Value = _mandantProvider.MandantId;

                        var affected = cmd.ExecuteNonQuery();
                        if (affected == 0)
                        {
                            // Dokument existiert nicht (mehr) bzw. gehört nicht zu diesem Mandanten.
                            throw new RepositoryException($"Dokument nicht gefunden. ObjektId: {objectId}.");
                        }
                    }

                    transaction.Commit();
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
            catch (RepositoryException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new RepositoryException("Fehler in der RestoreDokumente Funktion.", ex);
            }
        }

        // Löscht bereits als gelöscht markierte Dokumente endgültig samt Versionen.
        public void PurgeDokumente(IEnumerable<Guid> objectIds)
        {
            var ids = objectIds?.Where(id => id != Guid.Empty).Distinct().ToList() ?? new List<Guid>();
            if (ids.Count == 0) return;

            try
            {
                using var connection = new SqlConnection(_connectionString);
                connection.Open();
                using var transaction = connection.BeginTransaction();

                try
                {
                    foreach (var objectId in ids)
                    {
                        // 1) Versionen löschen. TODO: Tabellen-/Spaltennamen anpassen.
                        using (var cmd = new SqlCommand(
                            @"DELETE FROM [DokumentVersionen] WHERE DocumentObjektId = @objectId", connection, transaction))
                        {
                            cmd.Parameters.Add("@objectId", SqlDbType.UniqueIdentifier).Value = objectId;
                            cmd.ExecuteNonQuery();
                        }

                        // 2) Dokument löschen, nur wenn es wirklich bereits als gelöscht markiert ist.
                        using (var cmd = new SqlCommand("DELETE FROM [Dokumente] WHERE ObjektId = @objectId AND MandantId = @MandantId AND IsDeleted = 1", connection, transaction))
                        {
                            cmd.Parameters.Add("@objectId", SqlDbType.UniqueIdentifier).Value = objectId;
                            cmd.Parameters.Add("@MandantId", SqlDbType.Int).Value = _mandantProvider.MandantId;

                            if (cmd.ExecuteNonQuery() == 0)
                            {
                                throw new RepositoryException($"Dokument nicht gefunden oder nicht gelöscht. ObjektId: {objectId}.");
                            }
                        }
                    }

                    transaction.Commit();
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
            catch (RepositoryException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new RepositoryException("Fehler in der PurgeDokumente Funktion.", ex);
            }
        }





        // + Mandanten id.
        // Icon.
        // Aktualisiert das Icon eines Knotens anhand seiner ObjektId und des Node-Typs.
        public void UpdateIcon(Guid ObjektId, string nodeType, byte[] icon)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                connection.Open();

                string tableName = GetTableName(nodeType);
                var query = $"UPDATE {tableName} SET Icon = @Icon WHERE ObjektId = @ObjektId";
                using var cmd = new SqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@Icon", icon);
                cmd.Parameters.AddWithValue("@ObjektId", ObjektId);
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                throw new RepositoryException("Fehler in der UpdateIcon Funktion.", ex);
            }
        }

        // Löscht das Icon eines Knotens anhand seiner ObjektId und des Node-Typs.
        public void DeleteIcon(Guid ObjektId, string nodeType)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                connection.Open();

                string tableName = GetTableName(nodeType);
                var query = $"UPDATE {tableName} SET Icon = NULL WHERE ObjektId = @ObjektId";
                using var cmd = new SqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@ObjektId", ObjektId);
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                throw new RepositoryException("Fehler in der DeleteIcon Funktion.", ex);
            }
        }

        // Gibt den Tabellennamen anhand des Node-Typs zurück.
        private string GetTableName(string nodeType)
        {
            switch (nodeType)
            {
                case "Hauptgruppe":
                case "Nebengruppe":
                case "Akte":
                case "Vorgang":
                case "Kategorie":
                    return nodeType;
                default:
                    throw new Exception("Invalid nodeType");
            }
        }



        // + Mandanten id.
        // Abteilung / Referat.
        // Liefert die Liste aller Abteilungen.
        public List<string> GetAllAbteilungen()
        {
            var result = new List<string>();
            try
            {
                using var connection = new SqlConnection(_connectionString);
                connection.Open();
                var query = @"SELECT DISTINCT [name] FROM [Abteilung] WHERE MandantId = @MandantId";
                using var cmd = new SqlCommand(query, connection);
                cmd.Parameters.Add("@MandantId", SqlDbType.Int).Value = _mandantProvider.MandantId;
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    if (!reader.IsDBNull(0))
                    {
                        result.Add(reader.GetString(0));
                    }
                }
            }
            catch (Exception ex)
            {
                throw new RepositoryException($"GetAllAbteilungen fehlgeschlagen. Fehler: {ex.Message}", ex);
            }
            return result;
        }

        // Liefert die Liste der Abteilungen für ein bestimmtes ObjektId.
        public List<string> GetAbteilungenByObjektId(Guid objektId)
        {
            var result = new List<string>();
            try
            {
                using var connection = new SqlConnection(_connectionString);
                connection.Open();
                var query = @"SELECT DISTINCT [Abteilung] 
                            FROM [AbteilungReferat] 
                            WHERE ObjektId = @ObjektId AND MandantId = @MandantId";
                using var cmd = new SqlCommand(query, connection);
                cmd.Parameters.Add("@ObjektId", SqlDbType.UniqueIdentifier).Value = objektId;
                cmd.Parameters.Add("@MandantId", SqlDbType.Int).Value = _mandantProvider.MandantId;
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    if (!reader.IsDBNull(0))
                    {
                        result.Add(reader.GetString(0));
                    }
                }
            }
            catch (Exception ex)
            {
                throw new RepositoryException($"GetAbteilungenByObjektId fehlgeschlagen. ObjektId: {objektId}. Fehler: {ex.Message}", ex);
            }
            return result;
        }

        // Liefert die Liste der Referate für ein bestimmtes ObjektId.
        public List<string> GetReferateByObjektId(Guid objektId)
        {
            var result = new List<string>();
            try
            {
                using var connection = new SqlConnection(_connectionString);
                connection.Open();
                var query = @"SELECT [Referat] 
                            FROM [AbteilungReferat] 
                            WHERE ObjektId = @ObjektId AND MandantId = @MandantId";
                using var cmd = new SqlCommand(query, connection);
                cmd.Parameters.Add("@ObjektId", SqlDbType.UniqueIdentifier).Value = objektId;
                cmd.Parameters.Add("@MandantId", SqlDbType.Int).Value = _mandantProvider.MandantId;
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    if (!reader.IsDBNull(0))
                    {
                        result.Add(reader.GetString(0));
                    }
                }
            }
            catch (Exception ex)
            {
                throw new RepositoryException($"GetReferateByObjektId fehlgeschlagen. ObjektId: {objektId}. Fehler: {ex.Message}", ex);
            }
            return result;
        }

        // Liefert den AbteilungLang-Wert für ein bestimmtes ObjektId aus der Tabelle Vorgang.
        public string GetAbteilungLangFromVorgang(Guid objektId)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                connection.Open();
                var query = @"SELECT [Abteilung] 
                    FROM [Vorgang] 
                    WHERE ObjektId = @ObjektId AND MandantId = @MandantId";
                using var cmd = new SqlCommand(query, connection);
                cmd.Parameters.Add("@ObjektId", SqlDbType.UniqueIdentifier).Value = objektId;
                cmd.Parameters.Add("@MandantId", SqlDbType.Int).Value = _mandantProvider.MandantId;
                var result = cmd.ExecuteScalar();
                return result == DBNull.Value || result == null ? null : (string)result;
            }
            catch (Exception ex)
            {
                throw new RepositoryException($"GetAbteilungLangFromVorgang fehlgeschlagen. ObjektId: {objektId}. Fehler: {ex.Message}", ex);
            }
        }

        // Liefert den Referat-Wert für ein bestimmtes ObjektId aus der Tabelle Vorgang.
        public string GetReferatFromVorgang(Guid objektId)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                connection.Open();
                var query = @"SELECT [Referat] 
                    FROM [Vorgang] 
                    WHERE ObjektId = @ObjektId AND MandantId = @MandantId";
                using var cmd = new SqlCommand(query, connection);
                cmd.Parameters.Add("@ObjektId", SqlDbType.UniqueIdentifier).Value = objektId;
                cmd.Parameters.Add("@MandantId", SqlDbType.Int).Value = _mandantProvider.MandantId;
                var result = cmd.ExecuteScalar();
                return result == DBNull.Value || result == null ? null : (string)result;
            }
            catch (Exception ex)
            {
                throw new RepositoryException($"GetReferatFromVorgang fehlgeschlagen. ObjektId: {objektId}. Fehler: {ex.Message}", ex);
            }
        }

        // Liefert den AbteilungLang-Wert für ein bestimmtes ObjektId aus der Tabelle Akte.
        public string GetAbteilungLangFromAkte(Guid objektId)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                connection.Open();
                var query = @"SELECT [Abteilung] 
                    FROM [Akte] 
                    WHERE ObjektId = @ObjektId AND MandantId = @MandantId";
                using var cmd = new SqlCommand(query, connection);
                cmd.Parameters.Add("@ObjektId", SqlDbType.UniqueIdentifier).Value = objektId;
                cmd.Parameters.Add("@MandantId", SqlDbType.Int).Value = _mandantProvider.MandantId;
                var result = cmd.ExecuteScalar();
                return result == DBNull.Value || result == null ? null : (string)result;
            }
            catch (Exception ex)
            {
                throw new RepositoryException($"GetAbteilungLangFromAkte fehlgeschlagen. ObjektId: {objektId}. Fehler: {ex.Message}", ex);
            }
        }

        // Liefert den Referat-Wert für ein bestimmtes ObjektId aus der Tabelle Akte.
        public string GetReferatFromAkte(Guid objektId)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                connection.Open();
                var query = @"SELECT [Referat] 
                    FROM [Akte]
                    WHERE ObjektId = @ObjektId AND MandantId = @MandantId";
                using var cmd = new SqlCommand(query, connection);
                cmd.Parameters.Add("@ObjektId", SqlDbType.UniqueIdentifier).Value = objektId;
                cmd.Parameters.Add("@MandantId", SqlDbType.Int).Value = _mandantProvider.MandantId;
                var result = cmd.ExecuteScalar();
                return result == DBNull.Value || result == null ? null : (string)result;
            }
            catch (Exception ex)
            {
                throw new RepositoryException($"GetReferatFromAkte fehlgeschlagen. ObjektId: {objektId}. Fehler: {ex.Message}", ex);
            }
        }

        // Ersetzt die Abteilungen-Zuordnung einer Kategorie komplett durch die übergebene Liste (in die Tabelle "AbteilungReferat").
        public void SaveAbteilungenForKategorie(Guid objektId, List<string> abteilungen)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                connection.Open();
                using var transaction = connection.BeginTransaction();

                // Alte Zuordnungen für dieses Objekt entfernen (nur für den aktuellen Mandanten).
                var deleteQuery = @"DELETE FROM [AbteilungReferat] WHERE [ObjektId] = @ObjektId AND [MandantId] = @MandantId";
                using (var deleteCmd = new SqlCommand(deleteQuery, connection, transaction))
                {
                    deleteCmd.Parameters.Add("@ObjektId", SqlDbType.UniqueIdentifier).Value = objektId;
                    deleteCmd.Parameters.Add("@MandantId", SqlDbType.Int).Value = _mandantProvider.MandantId;
                    deleteCmd.ExecuteNonQuery();
                }

                // Neue Zuordnungen einfügen.
                if (abteilungen != null && abteilungen.Count > 0)
                {
                    var insertQuery = @"INSERT INTO [AbteilungReferat] (Abteilung, ObjektId, MandantId)
                                SELECT [value], @ObjektId, @MandantId FROM STRING_SPLIT(@Abteilungen, '|')";
                    using var insertCmd = new SqlCommand(insertQuery, connection, transaction);
                    insertCmd.Parameters.Add("@ObjektId", SqlDbType.UniqueIdentifier).Value = objektId;
                    insertCmd.Parameters.Add("@Abteilungen", SqlDbType.NVarChar).Value = string.Join("|", abteilungen);
                    insertCmd.Parameters.Add("@MandantId", SqlDbType.Int).Value = _mandantProvider.MandantId;
                    insertCmd.ExecuteNonQuery();
                }

                transaction.Commit();
            }
            catch (Exception ex)
            {
                throw new RepositoryException($"SaveAbteilungenFuerObjekt fehlgeschlagen. ObjektId: {objektId}. Fehler: {ex.Message}", ex);
            }
        }

        // Ersetzt die Referate-Zuordnung eines Objekts (z.B. Hauptgruppe) komplett durch die übergebene Liste von Referat-Ids.
        public void SaveReferateForHauptgruppe(Guid objektId, List<int> referatIds)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                connection.Open();
                using var transaction = connection.BeginTransaction();

                var deleteQuery = @"DELETE FROM [AbteilungReferat] WHERE [ObjektId] = @ObjektId AND [MandantId] = @MandantId";
                using (var deleteCmd = new SqlCommand(deleteQuery, connection, transaction))
                {
                    deleteCmd.Parameters.Add("@ObjektId", SqlDbType.UniqueIdentifier).Value = objektId;
                    deleteCmd.Parameters.Add("@MandantId", SqlDbType.Int).Value = _mandantProvider.MandantId;
                    deleteCmd.ExecuteNonQuery();
                }

                // Referat-Ids werden zusätzlich gegen MandantId gefiltert, damit keine fremden Ids eingeschleust werden.
                if (referatIds != null && referatIds.Count > 0)
                {
                    var insertQuery = @"INSERT INTO [AbteilungReferat] (Abteilung, Referat, ObjektId, MandantId)
                                SELECT a.Name, r.Name, @ObjektId, @MandantId
                                FROM [Referat] r
                                INNER JOIN [Abteilung] a ON a.Id = r.AbteilungId
                                WHERE r.Id IN (SELECT value FROM STRING_SPLIT(@ReferatIds, ','))
                                  AND r.MandantId = @MandantId
                                  AND a.MandantId = @MandantId";

                    using var insertCmd = new SqlCommand(insertQuery, connection, transaction);
                    insertCmd.Parameters.Add("@ObjektId", SqlDbType.UniqueIdentifier).Value = objektId;
                    insertCmd.Parameters.Add("@ReferatIds", SqlDbType.NVarChar).Value = string.Join(",", referatIds);
                    insertCmd.Parameters.Add("@MandantId", SqlDbType.Int).Value = _mandantProvider.MandantId;
                    insertCmd.ExecuteNonQuery();
                }

                transaction.Commit();
            }
            catch (Exception ex)
            {
                throw new RepositoryException($"SaveReferateFuerObjekt fehlgeschlagen. ObjektId: {objektId}. Fehler: {ex.Message}", ex);
            }
        }

        // Liefert alle Referate (mit zugehörigem Abteilungsnamen) für eine Liste von Abteilungsnamen.
        public List<ReferatDto> GetReferateByAbteilungNamen(List<string> abteilungNamen)
        {
            var result = new List<ReferatDto>();
            if (abteilungNamen == null || abteilungNamen.Count == 0) return result;

            try
            {
                using var connection = new SqlConnection(_connectionString);
                connection.Open();

                var query = @"SELECT r.Id, r.Name, a.Name
                      FROM [Referat] r
                      INNER JOIN [Abteilung] a ON a.Id = r.AbteilungId
                      WHERE a.Name IN (SELECT value FROM STRING_SPLIT(@AbteilungNamen, '|'))
                        AND r.MandantId = @MandantId
                        AND a.MandantId = @MandantId";

                using var cmd = new SqlCommand(query, connection);
                cmd.Parameters.Add("@AbteilungNamen", SqlDbType.NVarChar).Value = string.Join("|", abteilungNamen);
                cmd.Parameters.Add("@MandantId", SqlDbType.Int).Value = _mandantProvider.MandantId;

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    result.Add(new ReferatDto
                    {
                        Id = reader.GetInt32(0),
                        Name = reader.GetString(1),
                        AbteilungName = reader.GetString(2)
                    });
                }
            }
            catch (Exception ex)
            {
                throw new RepositoryException($"GetReferateByAbteilungNamen fehlgeschlagen. Fehler: {ex.Message}", ex);
            }
            return result;
        }

        // Liefert die ausgewählten Abteilung/Referat-Paare für ein bestimmtes ObjektId (z.B. Hauptgruppe).
        public List<AbteilungReferatDto> GetAbteilungReferatByObjektId(Guid objektId)
        {
            var result = new List<AbteilungReferatDto>();
            try
            {
                using var connection = new SqlConnection(_connectionString);
                connection.Open();
                var query = @"SELECT Abteilung, Referat
                      FROM [AbteilungReferat]
                      WHERE ObjektId = @ObjektId AND MandantId = @MandantId";
                using var cmd = new SqlCommand(query, connection);
                cmd.Parameters.Add("@ObjektId", SqlDbType.UniqueIdentifier).Value = objektId;
                cmd.Parameters.Add("@MandantId", SqlDbType.Int).Value = _mandantProvider.MandantId;
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    result.Add(new AbteilungReferatDto
                    {
                        Abteilung = reader.IsDBNull(0) ? null : reader.GetString(0),
                        Referat = reader.IsDBNull(1) ? null : reader.GetString(1)
                    });
                }
            }
            catch (Exception ex)
            {
                throw new RepositoryException($"GetAbteilungReferatByObjektId fehlgeschlagen. ObjektId: {objektId}. Fehler: {ex.Message}", ex);
            }
            return result;
        }



        // + Mandanten id.
        // Liefert mögliche Ansprechpartner (Personen), deren Rolle (PersonRollen) zu einer der übergebenen Abteilung/Referat-Kombinationen passt.
        public List<Ansprechpartner> GetAnsprechpartnerByAbteilungReferatPaare(List<AbteilungReferatDto> paare)
        {
            var result = new List<Ansprechpartner>();
            if (paare == null || paare.Count == 0) return result;
            try
            {
                using var connection = new SqlConnection(_connectionString);
                connection.Open();
                // Alle aktuell gültigen PersonRollen abrufen (Tabelle hat keine MandantId — Filterung erfolgt weiter unten über Personen).
                var rollenQuery = @"SELECT PersonId, Abteilung, Referat
                            FROM [PersonRollen]
                            WHERE GueltigBis IS NULL OR GueltigBis > GETDATE()";
                var personRollen = new List<(int PersonId, string Abteilung, string Referat)>();
                using (var cmd = new SqlCommand(rollenQuery, connection))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        personRollen.Add((
                            reader.GetInt32(0),
                            reader.IsDBNull(1) ? null : reader.GetString(1),
                            reader.IsDBNull(2) ? null : reader.GetString(2)
                        ));
                    }
                }
                // Nur Rollen, deren Abteilung/Referat-Paar zu den übergebenen passt.
                var relevantePersonIds = personRollen
                    .Where(pr => paare.Any(p => p.Abteilung == pr.Abteilung && p.Referat == pr.Referat))
                    .Select(pr => pr.PersonId)
                    .Distinct()
                    .ToList();
                if (relevantePersonIds.Count == 0) return result;
                // Zugehörige Personen (Name, Vorname) abrufen — zusätzlich auf den aktuellen Mandanten eingeschränkt,
                // damit Personen fremder Mandanten nicht als Ansprechpartner zurückgegeben werden.
                var paramNames = relevantePersonIds.Select((_, i) => "@Id" + i).ToList();
                var personenQuery = $@"SELECT Id, Name, Vorname
                              FROM [Personen]
                              WHERE Id IN ({string.Join(",", paramNames)})
                                AND MandantId = @MandantId";
                using var personenCmd = new SqlCommand(personenQuery, connection);
                for (int i = 0; i < relevantePersonIds.Count; i++)
                {
                    personenCmd.Parameters.Add(paramNames[i], SqlDbType.Int).Value = relevantePersonIds[i];
                }
                personenCmd.Parameters.Add("@MandantId", SqlDbType.Int).Value = _mandantProvider.MandantId;
                using var personenReader = personenCmd.ExecuteReader();
                while (personenReader.Read())
                {
                    var personId = personenReader.GetInt32(0);
                    var rolle = personRollen.First(pr => pr.PersonId == personId);
                    result.Add(new Ansprechpartner
                    {
                        PersonId = personId,
                        Name = personenReader.IsDBNull(1) ? null : personenReader.GetString(1),
                        Vorname = personenReader.IsDBNull(2) ? null : personenReader.GetString(2),
                        Abteilung = rolle.Abteilung,
                        Referat = rolle.Referat
                    });
                }
            }
            catch (Exception ex)
            {
                throw new RepositoryException($"GetAnsprechpartnerByAbteilungReferatPaare fehlgeschlagen: {ex.Message}", ex);
            }
            return result;
        }




        // + Mandanten id.
        // Kategorie / Hauptgruppe / Nebngruppe.
        // Kategorie erstellen.
        public Guid CreateKategorie(string bezeichnung, string kuerzel, string beschreibung, byte[] icon)
        {
            try
            {
                var restClass = new DcmToWs.RestClass();
                var guid = restClass.CreateGuid();
                using var connection = new SqlConnection(_connectionString);
                connection.Open();
                // Nächste KategorieNummer ermitteln — pro Mandant, um Kollisionen/Informationslecks zwischen Mandanten zu vermeiden.
                var nummerQuery = "SELECT ISNULL(MAX(KategorieNummer), 0) + 1 FROM [Kategorie] WHERE MandantId = @MandantId";

                using var nummerCmd = new SqlCommand(nummerQuery, connection);
                nummerCmd.Parameters.Add("@MandantId", SqlDbType.Int).Value = _mandantProvider.MandantId;
                int naechsteNummer = (int)nummerCmd.ExecuteScalar();
                var query = @"INSERT INTO [Kategorie]
                    (KategorieNummer, Bezeichnung, ObjektId, Beschreibung, Kuerzel, Icon, MandantId)
                    VALUES
                    (@KategorieNummer, @Bezeichnung, @ObjektId, @Beschreibung, @Kuerzel, @Icon, @MandantId)";

                using var cmd = new SqlCommand(query, connection);
                cmd.Parameters.Add("@KategorieNummer", SqlDbType.Int).Value = naechsteNummer;
                cmd.Parameters.Add("@Bezeichnung", SqlDbType.NVarChar).Value = (object)bezeichnung ?? DBNull.Value;
                cmd.Parameters.Add("@ObjektId", SqlDbType.UniqueIdentifier).Value = guid;
                cmd.Parameters.Add("@Beschreibung", SqlDbType.NVarChar).Value = (object)beschreibung ?? DBNull.Value;
                cmd.Parameters.Add("@Kuerzel", SqlDbType.NVarChar).Value = (object)kuerzel ?? DBNull.Value;
                cmd.Parameters.Add("@Icon", SqlDbType.VarBinary).Value = (object)icon ?? DBNull.Value;
                cmd.Parameters.Add("@MandantId", SqlDbType.Int).Value = _mandantProvider.MandantId;
                cmd.ExecuteNonQuery();

                return guid;
            }
            catch (Exception ex)
            {
                throw new RepositoryException($"CreateKategorie fehlgeschlagen. Bezeichnung: {bezeichnung}. Fehler: {ex.Message}", ex);
            }
        }

        // Kategorie aktualisieren.
        public void UpdateKategorie(Guid objektId, string bezeichnung, string kuerzel, string beschreibung, byte[] icon, bool touchIcon)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                connection.Open();

                var iconSql = touchIcon ? ", Icon = @Icon" : "";

                var query = $@"UPDATE [Kategorie]
                    SET Bezeichnung = @Bezeichnung,
                        Beschreibung = @Beschreibung,
                        Kuerzel = @Kuerzel
                        {iconSql}
                    WHERE ObjektId = @ObjektId AND MandantId = @MandantId";
                using var cmd = new SqlCommand(query, connection);
                cmd.Parameters.Add("@ObjektId", SqlDbType.UniqueIdentifier).Value = objektId;
                cmd.Parameters.Add("@Bezeichnung", SqlDbType.NVarChar).Value = (object)bezeichnung ?? DBNull.Value;
                cmd.Parameters.Add("@Beschreibung", SqlDbType.NVarChar).Value = (object)beschreibung ?? DBNull.Value;
                cmd.Parameters.Add("@Kuerzel", SqlDbType.NVarChar).Value = (object)kuerzel ?? DBNull.Value;
                cmd.Parameters.Add("@MandantId", SqlDbType.Int).Value = _mandantProvider.MandantId;
                if (touchIcon)
                {
                    cmd.Parameters.Add("@Icon", SqlDbType.VarBinary).Value = (object)icon ?? DBNull.Value;
                }
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                throw new RepositoryException($"UpdateKategorie fehlgeschlagen. ObjektId: {objektId}. Fehler: {ex.Message}", ex);
            }
        }

        // Hauptgruppe erstellen.
        public Guid CreateHauptgruppe(int gehoertZuKategorieNummer, string bezeichnung, string kuerzel, string beschreibung, byte[] icon)
        {
            try
            {
                var restClass = new DcmToWs.RestClass();
                var guid = restClass.CreateGuid();

                using var connection = new SqlConnection(_connectionString);
                connection.Open();

                // Nächste HauptgruppeNummer ermitteln — pro Mandant.
                var nummerQuery = @"SELECT ISNULL(MAX(HauptgruppeNummer), 0) + 1 FROM [Hauptgruppe] WHERE MandantId = @MandantId";

                using var nummerCmd = new SqlCommand(nummerQuery, connection);
                nummerCmd.Parameters.Add("@MandantId", SqlDbType.Int).Value = _mandantProvider.MandantId;
                int naechsteNummer = (int)nummerCmd.ExecuteScalar();

                var query = @"INSERT INTO [Hauptgruppe]
                    (GehoertZuKategorieNummer, Bezeichnung, Kuerzel, Beschreibung, HauptgruppeNummer, LaufenderAktenzeichenZaehler, ObjektId, Icon, MandantId)
                    VALUES
                    (@GehoertZuKategorieNummer, @Bezeichnung, @Kuerzel, @Beschreibung, @HauptgruppeNummer, @LaufenderAktenzeichenZaehler, @ObjektId, @Icon, @MandantId)";

                using var cmd = new SqlCommand(query, connection);
                cmd.Parameters.Add("@GehoertZuKategorieNummer", SqlDbType.Int).Value = gehoertZuKategorieNummer;
                cmd.Parameters.Add("@Bezeichnung", SqlDbType.NVarChar).Value = (object)bezeichnung ?? DBNull.Value;
                cmd.Parameters.Add("@Kuerzel", SqlDbType.NVarChar).Value = (object)kuerzel ?? DBNull.Value;
                cmd.Parameters.Add("@Beschreibung", SqlDbType.NVarChar).Value = (object)beschreibung ?? DBNull.Value;
                cmd.Parameters.Add("@HauptgruppeNummer", SqlDbType.Int).Value = naechsteNummer;
                cmd.Parameters.Add("@LaufenderAktenzeichenZaehler", SqlDbType.Int).Value = 0;
                cmd.Parameters.Add("@ObjektId", SqlDbType.UniqueIdentifier).Value = guid;
                cmd.Parameters.Add("@Icon", SqlDbType.VarBinary).Value = (object)icon ?? DBNull.Value;
                cmd.Parameters.Add("@MandantId", SqlDbType.Int).Value = _mandantProvider.MandantId;
                cmd.ExecuteNonQuery();

                return guid;
            }
            catch (Exception ex)
            {
                throw new RepositoryException($"CreateHauptgruppe fehlgeschlagen. Bezeichnung: {bezeichnung}, KategorieNummer: {gehoertZuKategorieNummer}. Fehler: {ex.Message}", ex);
            }
        }

        // Hauptgruppe aktualisieren.
        public void UpdateHauptgruppe(Guid objektId, string bezeichnung, string kuerzel, string beschreibung, byte[] icon, bool touchIcon)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                connection.Open();

                var iconSql = touchIcon ? ", Icon = @Icon" : "";

                var query = $@"UPDATE [Hauptgruppe]
                    SET Bezeichnung = @Bezeichnung,
                        Kuerzel = @Kuerzel,
                        Beschreibung = @Beschreibung
                        {iconSql}
                    WHERE ObjektId = @ObjektId AND MandantId = @MandantId";

                using var cmd = new SqlCommand(query, connection);
                cmd.Parameters.Add("@ObjektId", SqlDbType.UniqueIdentifier).Value = objektId;
                cmd.Parameters.Add("@Bezeichnung", SqlDbType.NVarChar).Value = (object)bezeichnung ?? DBNull.Value;
                cmd.Parameters.Add("@Kuerzel", SqlDbType.NVarChar).Value = (object)kuerzel ?? DBNull.Value;
                cmd.Parameters.Add("@Beschreibung", SqlDbType.NVarChar).Value = (object)beschreibung ?? DBNull.Value;
                cmd.Parameters.Add("@MandantId", SqlDbType.Int).Value = _mandantProvider.MandantId;
                if (touchIcon)
                {
                    cmd.Parameters.Add("@Icon", SqlDbType.VarBinary).Value = (object)icon ?? DBNull.Value;
                }
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                throw new RepositoryException($"UpdateHauptgruppe fehlgeschlagen. ObjektId: {objektId}. Fehler: {ex.Message}", ex);
            }
        }

        // Nebengruppe erstellen.
        public void CreateNebengruppe(int gehoertZuKategorieNummer, int gehoertZuHauptgruppeNummer, string bezeichnung, string kuerzel, string beschreibung, byte[] icon)
        {
            try
            {
                var restClass = new DcmToWs.RestClass();
                var guid = restClass.CreateGuid();

                using var connection = new SqlConnection(_connectionString);
                connection.Open();

                // Nächste NebengruppeNummer ermitteln — pro Mandant.
                var nummerQuery = @"SELECT ISNULL(MAX(NebengruppeNummer), 0) + 1 FROM [Nebengruppe] WHERE MandantId = @MandantId";

                using var nummerCmd = new SqlCommand(nummerQuery, connection);
                nummerCmd.Parameters.Add("@MandantId", SqlDbType.Int).Value = _mandantProvider.MandantId;
                int naechsteNummer = (int)nummerCmd.ExecuteScalar();

                var query = @"INSERT INTO [Nebengruppe]
                    (GehoertZuKategorieNummer, GehoertZuHauptgruppeNummer, Bezeichnung, Kuerzel, Beschreibung, NebengruppeNummer, ObjektId, Icon, MandantId)
                    VALUES
                    (@GehoertZuKategorieNummer, @GehoertZuHauptgruppeNummer, @Bezeichnung, @Kuerzel, @Beschreibung, @NebengruppeNummer, @ObjektId, @Icon, @MandantId)";

                using var cmd = new SqlCommand(query, connection);
                cmd.Parameters.Add("@GehoertZuKategorieNummer", SqlDbType.Int).Value = gehoertZuKategorieNummer;
                cmd.Parameters.Add("@GehoertZuHauptgruppeNummer", SqlDbType.Int).Value = gehoertZuHauptgruppeNummer;
                cmd.Parameters.Add("@Bezeichnung", SqlDbType.NVarChar).Value = (object)bezeichnung ?? DBNull.Value;
                cmd.Parameters.Add("@Kuerzel", SqlDbType.NVarChar).Value = (object)kuerzel ?? DBNull.Value;
                cmd.Parameters.Add("@Beschreibung", SqlDbType.NVarChar).Value = (object)beschreibung ?? DBNull.Value;
                cmd.Parameters.Add("@NebengruppeNummer", SqlDbType.Int).Value = naechsteNummer;
                cmd.Parameters.Add("@ObjektId", SqlDbType.UniqueIdentifier).Value = guid;
                cmd.Parameters.Add("@Icon", SqlDbType.VarBinary).Value = (object)icon ?? DBNull.Value;
                cmd.Parameters.Add("@MandantId", SqlDbType.Int).Value = _mandantProvider.MandantId;
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                throw new RepositoryException($"CreateNebengruppe fehlgeschlagen. Bezeichnung: {bezeichnung}, HauptgruppeNummer: {gehoertZuHauptgruppeNummer}. Fehler: {ex.Message}", ex);
            }
        }

        // Nebengruppe aktualisieren.
        public void UpdateNebengruppe(Guid objektId, string bezeichnung, string kuerzel, string beschreibung, byte[] icon, bool touchIcon)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                connection.Open();

                var iconSql = touchIcon ? ", Icon = @Icon" : "";

                var query = $@"UPDATE [Nebengruppe]
                    SET Bezeichnung = @Bezeichnung,
                        Kuerzel = @Kuerzel,
                        Beschreibung = @Beschreibung
                        {iconSql}
                    WHERE ObjektId = @ObjektId AND MandantId = @MandantId";
                using var cmd = new SqlCommand(query, connection);
                cmd.Parameters.Add("@ObjektId", SqlDbType.UniqueIdentifier).Value = objektId;
                cmd.Parameters.Add("@Bezeichnung", SqlDbType.NVarChar).Value = (object)bezeichnung ?? DBNull.Value;
                cmd.Parameters.Add("@Kuerzel", SqlDbType.NVarChar).Value = (object)kuerzel ?? DBNull.Value;
                cmd.Parameters.Add("@Beschreibung", SqlDbType.NVarChar).Value = (object)beschreibung ?? DBNull.Value;
                cmd.Parameters.Add("@MandantId", SqlDbType.Int).Value = _mandantProvider.MandantId;
                if (touchIcon)
                {
                    cmd.Parameters.Add("@Icon", SqlDbType.VarBinary).Value = (object)icon ?? DBNull.Value;
                }
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                throw new RepositoryException($"UpdateNebengruppe fehlgeschlagen. ObjektId: {objektId}. Fehler: {ex.Message}", ex);
            }
        }

        // Kategorie löschen.
        public void DeleteKategorie(Guid objektId)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                connection.Open();

                // KategorieNummer anhand ObjektId + MandantId ermitteln, um Kinder (Hauptgruppen) zu prüfen.
                var kategorieNummerQuery = "SELECT KategorieNummer FROM [Kategorie] WHERE ObjektId = @ObjektId AND MandantId = @MandantId";
                using var kategorieNummerCmd = new SqlCommand(kategorieNummerQuery, connection);
                kategorieNummerCmd.Parameters.Add("@ObjektId", SqlDbType.UniqueIdentifier).Value = objektId;
                kategorieNummerCmd.Parameters.Add("@MandantId", SqlDbType.Int).Value = _mandantProvider.MandantId;
                var kategorieNummerResult = kategorieNummerCmd.ExecuteScalar();
                if (kategorieNummerResult == null)
                {
                    throw new RepositoryException($"Kategorie mit ObjektId {objektId} wurde nicht gefunden.");
                }
                int kategorieNummer = (int)kategorieNummerResult;

                // Prüfen, ob Hauptgruppen (Kinder) existieren — im selben Mandanten.
                var childCountQuery = "SELECT COUNT(*) FROM [Hauptgruppe] WHERE GehoertZuKategorieNummer = @KategorieNummer AND MandantId = @MandantId";
                using var childCountCmd = new SqlCommand(childCountQuery, connection);
                childCountCmd.Parameters.Add("@KategorieNummer", SqlDbType.Int).Value = kategorieNummer;
                childCountCmd.Parameters.Add("@MandantId", SqlDbType.Int).Value = _mandantProvider.MandantId;
                int childCount = (int)childCountCmd.ExecuteScalar();
                if (childCount > 0)
                {
                    throw new RepositoryHasChildrenException($"Kategorie kann nicht gelöscht werden, da noch {childCount} Hauptgruppe(n) zugeordnet sind.");
                }

                // Kategorie löschen.
                var deleteQuery = "DELETE FROM [Kategorie] WHERE ObjektId = @ObjektId AND MandantId = @MandantId";
                using var deleteCmd = new SqlCommand(deleteQuery, connection);
                deleteCmd.Parameters.Add("@ObjektId", SqlDbType.UniqueIdentifier).Value = objektId;
                deleteCmd.Parameters.Add("@MandantId", SqlDbType.Int).Value = _mandantProvider.MandantId;
                deleteCmd.ExecuteNonQuery();

                // Zugehörige Einträge in AbteilungReferat löschen (anhand ObjektId + MandantId).
                var deleteAbteilungReferatQuery = "DELETE FROM [AbteilungReferat] WHERE ObjektId = @ObjektId AND MandantId = @MandantId";
                using var deleteAbteilungReferatCmd = new SqlCommand(deleteAbteilungReferatQuery, connection);
                deleteAbteilungReferatCmd.Parameters.Add("@ObjektId", SqlDbType.UniqueIdentifier).Value = objektId;
                deleteAbteilungReferatCmd.Parameters.Add("@MandantId", SqlDbType.Int).Value = _mandantProvider.MandantId;
                deleteAbteilungReferatCmd.ExecuteNonQuery();
            }
            catch (RepositoryHasChildrenException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new RepositoryException($"DeleteKategorie fehlgeschlagen. ObjektId: {objektId}. Fehler: {ex.Message}", ex);
            }
        }

        // Hauptgruppe löschen.
        public void DeleteHauptgruppe(Guid objektId)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                connection.Open();

                // HauptgruppeNummer anhand ObjektId + MandantId ermitteln, um Kinder (Nebengruppen) zu prüfen.
                var hauptgruppeNummerQuery = "SELECT HauptgruppeNummer FROM [Hauptgruppe] WHERE ObjektId = @ObjektId AND MandantId = @MandantId";
                using var hauptgruppeNummerCmd = new SqlCommand(hauptgruppeNummerQuery, connection);
                hauptgruppeNummerCmd.Parameters.Add("@ObjektId", SqlDbType.UniqueIdentifier).Value = objektId;
                hauptgruppeNummerCmd.Parameters.Add("@MandantId", SqlDbType.Int).Value = _mandantProvider.MandantId;
                var hauptgruppeNummerResult = hauptgruppeNummerCmd.ExecuteScalar();
                if (hauptgruppeNummerResult == null)
                {
                    throw new RepositoryException($"Hauptgruppe mit ObjektId {objektId} wurde nicht gefunden.");
                }
                int hauptgruppeNummer = (int)hauptgruppeNummerResult;

                // Prüfen, ob Nebengruppen (Kinder) existieren — im selben Mandanten.
                var childCountQuery = "SELECT COUNT(*) FROM [Nebengruppe] WHERE GehoertZuHauptgruppeNummer = @HauptgruppeNummer AND MandantId = @MandantId";
                using var childCountCmd = new SqlCommand(childCountQuery, connection);
                childCountCmd.Parameters.Add("@HauptgruppeNummer", SqlDbType.Int).Value = hauptgruppeNummer;
                childCountCmd.Parameters.Add("@MandantId", SqlDbType.Int).Value = _mandantProvider.MandantId;
                int childCount = (int)childCountCmd.ExecuteScalar();
                if (childCount > 0)
                {
                    throw new RepositoryHasChildrenException($"Hauptgruppe kann nicht gelöscht werden, da noch {childCount} Nebengruppe(n) zugeordnet sind.");
                }

                // Hauptgruppe löschen.
                var deleteQuery = "DELETE FROM [Hauptgruppe] WHERE ObjektId = @ObjektId AND MandantId = @MandantId";
                using var deleteCmd = new SqlCommand(deleteQuery, connection);
                deleteCmd.Parameters.Add("@ObjektId", SqlDbType.UniqueIdentifier).Value = objektId;
                deleteCmd.Parameters.Add("@MandantId", SqlDbType.Int).Value = _mandantProvider.MandantId;
                deleteCmd.ExecuteNonQuery();

                // Zugehörige Einträge in AbteilungReferat löschen (anhand ObjektId + MandantId).
                var deleteAbteilungReferatQuery = "DELETE FROM [AbteilungReferat] WHERE ObjektId = @ObjektId AND MandantId = @MandantId";
                using var deleteAbteilungReferatCmd = new SqlCommand(deleteAbteilungReferatQuery, connection);
                deleteAbteilungReferatCmd.Parameters.Add("@ObjektId", SqlDbType.UniqueIdentifier).Value = objektId;
                deleteAbteilungReferatCmd.Parameters.Add("@MandantId", SqlDbType.Int).Value = _mandantProvider.MandantId;
                deleteAbteilungReferatCmd.ExecuteNonQuery();
            }
            catch (RepositoryHasChildrenException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new RepositoryException($"DeleteHauptgruppe fehlgeschlagen. ObjektId: {objektId}. Fehler: {ex.Message}", ex);
            }
        }

        // Nebengruppe löschen.
        public void DeleteNebengruppe(Guid objektId)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                connection.Open();

                // NebengruppeNummer + GehoertZuHauptgruppeNummer anhand ObjektId + MandantId ermitteln, um Kinder zu prüfen.
                var nebengruppeQuery = @"SELECT NebengruppeNummer, GehoertZuHauptgruppeNummer 
                               FROM [Nebengruppe] 
                               WHERE ObjektId = @ObjektId AND MandantId = @MandantId";
                using var nebengruppeCmd = new SqlCommand(nebengruppeQuery, connection);
                nebengruppeCmd.Parameters.Add("@ObjektId", SqlDbType.UniqueIdentifier).Value = objektId;
                nebengruppeCmd.Parameters.Add("@MandantId", SqlDbType.Int).Value = _mandantProvider.MandantId;

                int nebengruppeNummer;
                int hauptgruppeNummer;
                using (var reader = nebengruppeCmd.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        throw new RepositoryException($"Nebengruppe mit ObjektId {objektId} wurde nicht gefunden.");
                    }
                    nebengruppeNummer = reader.GetInt32(reader.GetOrdinal("NebengruppeNummer"));
                    hauptgruppeNummer = reader.GetInt32(reader.GetOrdinal("GehoertZuHauptgruppeNummer"));
                }

                // Prüfen, ob Akten (Kinder) existieren — im selben Mandanten.
                var childCountQuery = "SELECT COUNT(*) FROM [Akte] WHERE NebengruppeNummer = @NebengruppeNummer AND MandantId = @MandantId";
                using var childCountCmd = new SqlCommand(childCountQuery, connection);
                childCountCmd.Parameters.Add("@NebengruppeNummer", SqlDbType.Int).Value = nebengruppeNummer;
                childCountCmd.Parameters.Add("@MandantId", SqlDbType.Int).Value = _mandantProvider.MandantId;
                int childCount = (int)childCountCmd.ExecuteScalar();

                if (childCount > 0)
                {
                    throw new RepositoryHasChildrenException($"Nebengruppe kann nicht gelöscht werden, da noch {childCount} Akte(n) zugeordnet sind.");
                }

                var deleteQuery = "DELETE FROM [Nebengruppe] WHERE ObjektId = @ObjektId AND MandantId = @MandantId";
                using var deleteCmd = new SqlCommand(deleteQuery, connection);
                deleteCmd.Parameters.Add("@ObjektId", SqlDbType.UniqueIdentifier).Value = objektId;
                deleteCmd.Parameters.Add("@MandantId", SqlDbType.Int).Value = _mandantProvider.MandantId;
                deleteCmd.ExecuteNonQuery();
            }
            catch (RepositoryHasChildrenException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new RepositoryException($"DeleteNebengruppe fehlgeschlagen. ObjektId: {objektId}. Fehler: {ex.Message}", ex);
            }
        }




        // + Mandanten id.
        // Vorlagen.
        // Liest SubType (Ablageklasse) UND Aktenzeichen eines Ordners anhand ObjektId.
        public OrdnerInfoDto? GetOrdnerInfo(Guid objektId)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                connection.Open();

                const string query = @"
                                     SELECT [SubType], [Aktenzeichen]
                                     FROM [Ordner]
                                     WHERE [ObjektId] = @ObjektId AND [MandantId] = @MandantId";

                using var cmd = new SqlCommand(query, connection);
                cmd.Parameters.Add("@ObjektId", SqlDbType.UniqueIdentifier).Value = objektId;
                cmd.Parameters.Add("@MandantId", SqlDbType.Int).Value = _mandantProvider.MandantId;

                using var reader = cmd.ExecuteReader();
                if (!reader.Read())
                {
                    return null;
                }

                return new OrdnerInfoDto
                {
                    SubType = reader["SubType"] as string,
                    Aktenzeichen = reader["Aktenzeichen"] as string
                };
            }
            catch (Exception ex)
            {
                throw new RepositoryException($"GetOrdnerInfo fehlgeschlagen. ObjektId: {objektId}. Fehler: {ex.Message}", ex);
            }
        }

        // Findet den VorgangsTyp-Namen (Type) über Aktenzeichen - Vorgang.Bescheidtyp - Vorgangstyp.Id. Gibt null zurück, wenn kein passender Vorgang/Vorgangstyp existiert.
        public string? GetVorgangsTypNameByAktenzeichen(string aktenzeichen)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                connection.Open();

                const string query = @"
                                     SELECT TOP (1) vt.[Type]
                                     FROM [Vorgang] v
                                     INNER JOIN [Vorgangstyp] vt ON v.[Bescheidtyp] = vt.[Id]
                                     WHERE v.[Aktenzeichen] = @Aktenzeichen
                                      AND v.[MandantId] = @MandantId
                                     ORDER BY v.[ErzeugtAm] DESC";

                using var cmd = new SqlCommand(query, connection);
                cmd.Parameters.Add("@Aktenzeichen", SqlDbType.NVarChar).Value = aktenzeichen;
                cmd.Parameters.Add("@MandantId", SqlDbType.Int).Value = _mandantProvider.MandantId;

                var result = cmd.ExecuteScalar();
                return result == null || result == DBNull.Value ? null : (string)result;
            }
            catch (Exception ex)
            {
                throw new RepositoryException($"GetVorgangsTypNameByAktenzeichen fehlgeschlagen. Aktenzeichen: {aktenzeichen}. Fehler: {ex.Message}", ex);
            }
        }




        // Vorlagen laden - gefiltert nach Ablageklasse und (falls in der Vorlage gepflegt) VorgangsTyp.
        public List<VorlageVersionDto> GetVorlageVersionen(string? ablageklasse, string? vorgangsTypName = null)
        {
            try
            {
                var result = new List<VorlageVersionDto>();

                using var connection = new SqlConnection(_connectionString);
                connection.Open();

                // Ablageklasse muss immer passen.
                // Zusätzlich: Ist [VorgangsTypNamen] bei der Vorlage NICHT gepflegt (NULL), reicht die Ablageklasse allein aus.
                // Ist [VorgangsTypNamen] gepflegt, muss zusätzlich der übergebene VorgangsTypName darin vorkommen (beide Kriterien müssen matchen).
                const string query = @"
                                     SELECT 
                                        [VorlageVersionId], 
                                        [VorlageName], 
                                        [VorlageObjektTypName] AS VorlageObjektTyp, 
                                        [IstTagBefuellungAktiv] 
                                     FROM [dbo].[VorlagenVerwaltung] 
                                     WHERE [IstAktiv] = 1 
                                     AND (@Ablageklasse IS NULL OR ',' + [AblageBereicheNamen] + ',' LIKE '%,' + @Ablageklasse + ',%')
                                     AND ([VorgangsTypNamen] IS NULL OR (@VorgangsTypName IS NOT NULL AND ',' + [VorgangsTypNamen] + ',' LIKE '%,' + @VorgangsTypName + ',%'))
                                     ORDER BY [VorlageName]";

                using var cmd = new SqlCommand(query, connection);
                cmd.Parameters.Add("@Ablageklasse", SqlDbType.NVarChar).Value = (object?)ablageklasse ?? DBNull.Value;
                cmd.Parameters.Add("@VorgangsTypName", SqlDbType.NVarChar).Value = (object?)vorgangsTypName ?? DBNull.Value;

                using var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    result.Add(new VorlageVersionDto
                    {
                        VorlageVersionId = (int)reader["VorlageVersionId"],
                        VorlageName = reader["VorlageName"] as string ?? string.Empty,
                        VorlageObjektTyp = reader["VorlageObjektTyp"] as string ?? string.Empty,
                        IstTagBefuellungAktiv = reader["IstTagBefuellungAktiv"] != DBNull.Value && (bool)reader["IstTagBefuellungAktiv"]
                    });
                }

                return result;
            }
            catch (Exception ex)
            {
                throw new RepositoryException("Fehler in der GetVorlageVersionen Funktion.", ex);
            }
        }

        public int? GetVorlageObjektTypId(int vorlageVersionId)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                connection.Open();

                const string query = @"
                                     SELECT TOP (1) v.[VorlageObjektTypId]
                                     FROM [EAkteDb].[dbo].[Vorlage] v
                                     INNER JOIN [EAkteDb].[dbo].[VorlageVersion] vv ON vv.[VorlageId] = v.[Id]
                                     WHERE vv.[Id] = @VorlageVersionId";

                using var cmd = new SqlCommand(query, connection);
                cmd.Parameters.Add("@VorlageVersionId", SqlDbType.Int).Value = vorlageVersionId;

                var result = cmd.ExecuteScalar();
                return result == null || result == DBNull.Value ? null : Convert.ToInt32(result);
            }
            catch (Exception ex)
            {
                throw new RepositoryException("Fehler in GetVorlageObjektTypId.", ex);
            }
        }

        // Erzeugt ein Dokument aus einer Vorlage und speichert es in der Datenbank. Metadaten kommen aus der View [VorlagenVerwaltung], der FileStream aus [VorlageVersion].
        public void ErzeugeDokumentAusVorlage(string name, int vorlageVersionId, Guid folderId, string ersteller, string aktenzeichen, Dictionary<string, string> platzhalter)
        {
            try
            {
                // Erstellt eine neue GUID für das Dokument.
                var restClass = new DcmToWs.RestClass();
                var dokumentGuid = restClass.CreateGuid();
                var neueVersionId = restClass.CreateGuid();

                using var connection = new SqlConnection(_connectionString);
                connection.Open();
                using var transaction = connection.BeginTransaction();

                try
                {
                    // 1) Vorlagen-Metadaten aus der View [VorlagenVerwaltung] laden.
                    var vorlageInfoQuery = @"
                                           SELECT TOP (1)
                                           [VorlageId],
                                           [VorlageVersionId],
                                           [VorlageName],
                                           [VorlageVersion],
                                           [VorlageBereichName],
                                           [VorlageObjektTypName],
                                           [VorlageVersionDateiFormat],
                                           [VorlageVersionZielDateiFormat],
                                           [VorlageVersionBeschreibung],
                                           [VorlageBeschreibung]
                                           FROM [EAkteDb].[dbo].[VorlagenVerwaltung]
                                           WHERE [VorlageVersionId] = @VorlageVersionId AND [IstAktiv] = 1";

                    string vorlageName = null;
                    string vorlageBereich = null;
                    string vorlageVersionNr = null;
                    string dateiFormat = null;
                    string zielFormat = null;
                    string vorlageVersionBeschreibung = null;
                    string vorlageBeschreibung = null;

                    using (var infoCmd = new SqlCommand(vorlageInfoQuery, connection, transaction))
                    {
                        infoCmd.Parameters.Add("@VorlageVersionId", SqlDbType.Int).Value = vorlageVersionId;

                        using var reader = infoCmd.ExecuteReader();
                        if (!reader.Read())
                        {
                            throw new RepositoryException($"Keine aktive VorlageVersion mit Id={vorlageVersionId} in [VorlagenVerwaltung] gefunden.");
                        }

                        vorlageName = reader["VorlageName"] as string;
                        vorlageBereich = reader["VorlageBereichName"] as string;
                        vorlageVersionNr = reader["VorlageVersion"]?.ToString();
                        dateiFormat = reader["VorlageVersionDateiFormat"] as string;
                        zielFormat = reader["VorlageVersionZielDateiFormat"] as string;
                        vorlageVersionBeschreibung = reader["VorlageVersionBeschreibung"] as string;
                        vorlageBeschreibung = reader["VorlageBeschreibung"] as string;
                    }

                    // 2) FileStream separat aus [VorlageVersion] laden (die View liefert nur die Größe, nicht die Bytes).
                    var fileStreamQuery = @"
                                          SELECT [FileStream]
                                          FROM [EAkteDb].[dbo].[VorlageVersion]
                                          WHERE [Id] = @VorlageVersionId AND [IstAktiv] = 1";

                    byte[] fileStream = null;
                    using (var fileCmd = new SqlCommand(fileStreamQuery, connection, transaction))
                    {
                        fileCmd.Parameters.Add("@VorlageVersionId", SqlDbType.Int).Value = vorlageVersionId;

                        using var reader = fileCmd.ExecuteReader();
                        if (reader.Read())
                        {
                            fileStream = reader["FileStream"] as byte[];
                        }
                    }

                    var testInhalt = System.Text.Encoding.UTF8.GetBytes("TEST INHALT - Platzhalter");
                    var contentBytes = (fileStream != null && fileStream.Length > 0) ? fileStream : testInhalt;

                    var vorlageExt = NormalizeFileExtension(dateiFormat)?.TrimStart('.').ToLowerInvariant();
                    if (vorlageExt == "docx" && fileStream is { Length: > 0 } && platzhalter != null)
                    {
                        contentBytes = _wordVorlagenService.FillContentControls(
                            contentBytes, platzhalter, ignoreTagIfValueIsEmptyString: false);
                    }

                    // Zielformat/Erweiterung: bevorzugt ZielDateiFormat, sonst DateiFormat.
                    var effektivesFormatRaw = !string.IsNullOrWhiteSpace(zielFormat) ? zielFormat : dateiFormat;
                    var fileExtension = NormalizeFileExtension(effektivesFormatRaw);
                    var mimeContentType = GetMimeType(fileExtension);

                    // Beschreibung: bevorzugt Beschreibung der Version, sonst der Vorlage, sonst Standardtext.
                    var beschreibung = !string.IsNullOrWhiteSpace(vorlageVersionBeschreibung)
                        ? vorlageVersionBeschreibung
                        : (!string.IsNullOrWhiteSpace(vorlageBeschreibung) ? vorlageBeschreibung : "Erzeugt aus Vorlage");

                    var jetzt = DateTime.UtcNow;

                    // 3) Dokumente-Eintrag erzeugen (nur real existierende Spalten von [Dokumente]).
                    var dokQuery = @"
                                   INSERT INTO [Dokumente]
                                   (Name, Type, Subject, CreatorName, CreationDate, Modifier, ModifyDate,
                                   OwnerName, VersionLabel, Aktenzeichen, Gegenstand, ContentType,
                                   IsHidden, IsDeleted, IsPublic, IsSigned, Final, Eingangsdatum,
                                   FolderId, ObjektId, ACLName, IsCheckedOut, IstAusVorlagenverwaltung, MandantId)
                                   VALUES
                                   (@Name, @Type, @Subject, @CreatorName, @CreationDate, @Modifier, @ModifyDate,
                                   @OwnerName, @VersionLabel, @Aktenzeichen, @Gegenstand, @ContentType,
                                   0, 0, 0, 0, 0, @Eingangsdatum,
                                   @FolderId, @ObjektId, @ACLName, 0, 1, @MandantId)";

                    using (var dokCmd = new SqlCommand(dokQuery, connection, transaction))
                    {
                        dokCmd.Parameters.Add("@Name", SqlDbType.NVarChar, 255).Value = name;
                        dokCmd.Parameters.Add("@Type", SqlDbType.NVarChar, 100).Value = "Dokument"; // immer fest "Dokument"
                        dokCmd.Parameters.Add("@Subject", SqlDbType.NVarChar, 255).Value = (object)vorlageName ?? DBNull.Value;
                        dokCmd.Parameters.Add("@CreatorName", SqlDbType.NVarChar, 100).Value = ersteller;
                        dokCmd.Parameters.Add("@CreationDate", SqlDbType.DateTime).Value = jetzt;
                        dokCmd.Parameters.Add("@Modifier", SqlDbType.NVarChar, 100).Value = ersteller;
                        dokCmd.Parameters.Add("@ModifyDate", SqlDbType.DateTime).Value = jetzt;
                        dokCmd.Parameters.Add("@OwnerName", SqlDbType.NVarChar, 100).Value = ersteller;
                        dokCmd.Parameters.Add("@VersionLabel", SqlDbType.NVarChar, 50).Value = (object)vorlageVersionNr ?? "1.0";
                        dokCmd.Parameters.Add("@Aktenzeichen", SqlDbType.NVarChar, 100).Value = (object)aktenzeichen ?? DBNull.Value;
                        dokCmd.Parameters.Add("@Gegenstand", SqlDbType.NVarChar, 255).Value = (object)vorlageBereich ?? DBNull.Value;
                        dokCmd.Parameters.Add("@Eingangsdatum", SqlDbType.DateTime).Value = jetzt;
                        dokCmd.Parameters.Add("@ContentType", SqlDbType.NVarChar, 71).Value = mimeContentType;
                        dokCmd.Parameters.Add("@FolderId", SqlDbType.UniqueIdentifier).Value = folderId;
                        dokCmd.Parameters.Add("@ObjektId", SqlDbType.UniqueIdentifier).Value = dokumentGuid;
                        dokCmd.Parameters.Add("@ACLName", SqlDbType.NVarChar, 255).Value = DBNull.Value;
                        dokCmd.Parameters.AddWithValue("@MandantId", _mandantProvider.MandantId);

                        dokCmd.ExecuteNonQuery();
                    }

                    // 4) DokumentVersionen-Eintrag erzeugen (nur real existierende Spalten von [DokumentVersionen]).
                    var versQuery = @"
                                    INSERT INTO [DokumentVersionen]
                                    (VersionNr, IsActive, IsArchived, Description, CreatedDate, CreatedBy,
                                    ModifiedDate, ModifiedBy, FileStream, Bemerkungen, Dokumentdatum,
                                    DocumentObjektId, CurrentVersionId,
                                    ContentType, ContentSize, FullContentSize, IsSigned, Final, FileExtension)
                                    VALUES
                                    (@VersionNr, 1, 0, @Description, @CreatedDate, @CreatedBy,
                                    @ModifiedDate, @ModifiedBy, @FileStream, @Bemerkungen, @Dokumentdatum,
                                    @DocumentObjektId, @CurrentVersionId,
                                    @ContentType, @ContentSize, @FullContentSize, @IsSigned, @Final, @FileExtension)";
                    using (var versCmd = new SqlCommand(versQuery, connection, transaction))
                    {
                        versCmd.Parameters.Add("@VersionNr", SqlDbType.VarChar, 10).Value = "1.0"; // erste Version des Dokuments.
                        versCmd.Parameters.Add("@Description", SqlDbType.NVarChar).Value = beschreibung;
                        versCmd.Parameters.Add("@CreatedDate", SqlDbType.DateTime).Value = jetzt;
                        versCmd.Parameters.Add("@CreatedBy", SqlDbType.VarChar, 100).Value = ersteller;
                        versCmd.Parameters.Add("@ModifiedDate", SqlDbType.DateTime).Value = jetzt;
                        versCmd.Parameters.Add("@ModifiedBy", SqlDbType.VarChar, 100).Value = ersteller;
                        versCmd.Parameters.Add("@FileStream", SqlDbType.VarBinary).Value = contentBytes;
                        versCmd.Parameters.Add("@Bemerkungen", SqlDbType.NVarChar).Value =
                            (object)$"Erzeugt aus Vorlage: {vorlageName ?? "-"} (VorlageVersionId={vorlageVersionId})" ?? DBNull.Value;
                        versCmd.Parameters.Add("@Dokumentdatum", SqlDbType.DateTime).Value = jetzt;
                        versCmd.Parameters.Add("@DocumentObjektId", SqlDbType.UniqueIdentifier).Value = dokumentGuid;
                        versCmd.Parameters.Add("@CurrentVersionId", SqlDbType.UniqueIdentifier).Value = neueVersionId;
                        versCmd.Parameters.Add("@ContentType", SqlDbType.NVarChar, 255).Value = mimeContentType;
                        versCmd.Parameters.Add("@ContentSize", SqlDbType.BigInt).Value = contentBytes.Length;
                        versCmd.Parameters.Add("@FullContentSize", SqlDbType.BigInt).Value = contentBytes.Length;
                        versCmd.Parameters.Add("@IsSigned", SqlDbType.Bit).Value = 0;
                        versCmd.Parameters.Add("@Final", SqlDbType.Bit).Value = 1;
                        versCmd.Parameters.Add("@FileExtension", SqlDbType.VarChar, 10).Value = fileExtension; // nie NULL, s. NormalizeFileExtension

                        versCmd.ExecuteNonQuery();
                    }

                    transaction.Commit();
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
            catch (Exception ex)
            {
                throw new RepositoryException("Fehler beim Erzeugen des Dokuments aus Vorlage.", ex);
            }
        }

        // Ordnet einer normalisierten Dateiendung den passenden MIME-Typ zu.
        private static readonly Dictionary<string, string> ExtensionToMimeType = new(StringComparer.OrdinalIgnoreCase)
        {
            [".pdf"] = "application/pdf",
            [".doc"] = "application/msword",
            [".docx"] = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            [".xls"] = "application/vnd.ms-excel",
            [".xlsx"] = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            [".ppt"] = "application/vnd.ms-powerpoint",
            [".pptx"] = "application/vnd.openxmlformats-officedocument.presentationml.presentation",
            [".txt"] = "text/plain",
            [".rtf"] = "application/rtf",
        };
        private static string GetMimeType(string normalizedFileExtension)
        {
            if (string.IsNullOrWhiteSpace(normalizedFileExtension))
            {
                return "application/octet-stream";
            }

            return ExtensionToMimeType.TryGetValue(normalizedFileExtension, out var mime)
                ? mime
                : "application/octet-stream";
        }
        // Wandelt DateiFormat/ZielDateiFormat aus der Vorlage oder ein MIME-Typ in ein einheitliches Format  um.
        private static string NormalizeFileExtension(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return string.Empty;
            }

            var ext = raw.Trim().ToLowerInvariant();

            // MIME-Typ wie "application/pdf" - "pdf"
            var slashIndex = ext.LastIndexOf('/');
            if (slashIndex >= 0 && slashIndex < ext.Length - 1)
            {
                ext = ext.Substring(slashIndex + 1);
            }

            if (!ext.StartsWith("."))
            {
                ext = "." + ext;
            }

            return ext;
        }




        // Gebühre. TODO: MandantenId. 
        // Gibt alle Gebühren zu einem Aktenzeichen zurück. Leere Liste, wenn keine vorhanden.
        public List<Gebuehr> GetGebuehrenByAktenzeichen(string aktenzeichen)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                connection.Open();

                const string query = @"
                                     SELECT g.[Aktenzeichen], g.[LfdNr], g.[Kassenzeichen], g.[Typ], g.[Status],
                                            g.[Tarifstelle], g.[Ermittlung], g.[Bemerkung], g.[IBAN], g.[BIC],
                                            g.[GebuehrenbescheidAn], g.[Gebuehr], g.[GezahlteGebuehr], g.[BeantragtAm],
                                            g.[GebuehrGezahltAm], g.[AnnahmeanordnungVom], g.[GebuehrenbescheidVom],
                                            g.[KassenwirksamAm], g.[StorniertAm], g.[SollStorniertWerden],
                                            g.[AngelegtVon], g.[AngelegtAm], g.[GeaendertVon], g.[GeaendertAm],
                                            g.[Referenznummer], g.[Belegnummer], g.[UebergabeAm], g.[UebergabeStelle],
                                            g.[EingelesenAm], g.[StornoStelle], g.[StatusProzess],
                                            g.[KassenzeichenSollGeaendertWerden], g.[LfdNrAnnahmeanordnung],
                                            g.[LfdNrGebuehrenbescheid], g.[AnnahmeanordnungBefugter],
                                            g.[UnterschriebenVon], g.[FreigegebenVon]
                                     FROM [Gebuehr] g
                                     WHERE g.[Aktenzeichen] = @Aktenzeichen
                                     ORDER BY g.[LfdNr]";

                using var cmd = new SqlCommand(query, connection);
                cmd.Parameters.Add("@Aktenzeichen", SqlDbType.NVarChar).Value = aktenzeichen;

                var result = new List<Gebuehr>();
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    result.Add(new Gebuehr
                    {
                        Aktenzeichen = (string)reader["Aktenzeichen"],
                        LfdNr = (int)reader["LfdNr"],
                        Kassenzeichen = reader["Kassenzeichen"] as string,
                        Typ = reader["Typ"] as string,
                        Status = reader["Status"] as string,
                        Tarifstelle = reader["Tarifstelle"] as string,
                        Ermittlung = reader["Ermittlung"] as string,
                        Bemerkung = reader["Bemerkung"] as string,
                        IBAN = reader["IBAN"] as string,
                        BIC = reader["BIC"] as string,
                        GebuehrenbescheidAn = reader["GebuehrenbescheidAn"] as string,
                        Gebuehr_ = reader["Gebuehr"] == DBNull.Value ? null : (decimal)reader["Gebuehr"],
                        GezahlteGebuehr = reader["GezahlteGebuehr"] == DBNull.Value ? null : (decimal)reader["GezahlteGebuehr"],
                        BeantragtAm = reader["BeantragtAm"] == DBNull.Value ? null : (DateTime)reader["BeantragtAm"],
                        GebuehrGezahltAm = reader["GebuehrGezahltAm"] == DBNull.Value ? null : (DateTime)reader["GebuehrGezahltAm"],
                        AnnahmeanordnungVom = reader["AnnahmeanordnungVom"] == DBNull.Value ? null : (DateTime)reader["AnnahmeanordnungVom"],
                        GebuehrenbescheidVom = reader["GebuehrenbescheidVom"] == DBNull.Value ? null : (DateTime)reader["GebuehrenbescheidVom"],
                        KassenwirksamAm = reader["KassenwirksamAm"] == DBNull.Value ? null : (DateTime)reader["KassenwirksamAm"],
                        StorniertAm = reader["StorniertAm"] == DBNull.Value ? null : (DateTime)reader["StorniertAm"],
                        SollStorniertWerden = reader["SollStorniertWerden"] != DBNull.Value && (bool)reader["SollStorniertWerden"],
                        AngelegtVon = reader["AngelegtVon"] as string,
                        AngelegtAm = reader["AngelegtAm"] == DBNull.Value ? null : (DateTime)reader["AngelegtAm"],
                        GeaendertVon = reader["GeaendertVon"] as string,
                        GeaendertAm = reader["GeaendertAm"] == DBNull.Value ? null : (DateTime)reader["GeaendertAm"],
                        Referenznummer = reader["Referenznummer"] as string,
                        Belegnummer = reader["Belegnummer"] as string,
                        UebergabeAm = reader["UebergabeAm"] == DBNull.Value ? null : (DateTime)reader["UebergabeAm"],
                        UebergabeStelle = reader["UebergabeStelle"] as string,
                        EingelesenAm = reader["EingelesenAm"] == DBNull.Value ? null : (DateTime)reader["EingelesenAm"],
                        StornoStelle = reader["StornoStelle"] as string,
                        StatusProzess = reader["StatusProzess"] as string,
                        KassenzeichenSollGeaendertWerden = reader["KassenzeichenSollGeaendertWerden"] != DBNull.Value && (bool)reader["KassenzeichenSollGeaendertWerden"],
                        LfdNrAnnahmeanordnung = reader["LfdNrAnnahmeanordnung"] == DBNull.Value ? null : (int)reader["LfdNrAnnahmeanordnung"],
                        LfdNrGebuehrenbescheid = reader["LfdNrGebuehrenbescheid"] == DBNull.Value ? null : (int)reader["LfdNrGebuehrenbescheid"],
                        AnnahmeanordnungBefugter = reader["AnnahmeanordnungBefugter"] as string,
                        UnterschriebenVon = reader["UnterschriebenVon"] as string,
                        FreigegebenVon = reader["FreigegebenVon"] as string
                    });
                }
                return result;
            }
            catch (Exception ex)
            {
                throw new RepositoryException($"GetGebuehrenByAktenzeichen fehlgeschlagen. Aktenzeichen: {aktenzeichen}. Fehler: {ex.Message}", ex);
            }
        }

        // Gibt alle Titel zurück, bei denen Aktenzeichen und LfdNr übereinstimmen.
        public List<GebuehrTitel> GetGebuehrTitelByAktenzeichenAndLfdNr(string aktenzeichen, int lfdNr)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                connection.Open();

                const string query = @"
                                     SELECT t.[Aktenzeichen], t.[LfdNr], t.[TitelNr], t.[Betrag], t.[BetragBezahlt], t.[Kassenzeichen], t.[Typ]
                                     FROM [GebuehrTitel] t
                                     WHERE t.[Aktenzeichen] = @Aktenzeichen
                                     AND t.[LfdNr] = @LfdNr
                                     ORDER BY t.[TitelNr]";

                using var cmd = new SqlCommand(query, connection);
                cmd.Parameters.Add("@Aktenzeichen", SqlDbType.NVarChar).Value = aktenzeichen;
                cmd.Parameters.Add("@LfdNr", SqlDbType.Int).Value = lfdNr;

                var result = new List<GebuehrTitel>();
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    result.Add(new GebuehrTitel
                    {
                        Aktenzeichen = (string)reader["Aktenzeichen"],
                        LfdNr = (int)reader["LfdNr"],
                        TitelNr = reader["TitelNr"] as string,
                        Betrag = reader["Betrag"] == DBNull.Value ? null : (decimal)reader["Betrag"],
                        BetragBezahlt = reader["BetragBezahlt"] == DBNull.Value ? null : (decimal)reader["BetragBezahlt"],
                        Kassenzeichen = reader["Kassenzeichen"] as string,
                        Typ = reader["Typ"] as string
                    });
                }
                return result;
            }
            catch (Exception ex)
            {
                throw new RepositoryException($"GetGebuehrTitelByAktenzeichenAndLfdNr fehlgeschlagen. Aktenzeichen: {aktenzeichen}, LfdNr: {lfdNr}. Fehler: {ex.Message}", ex);
            }
        }

        // Gibt alle erlaubten Funktionen für Typ + Status aus GebuehrStatusValidation zurück.
        public List<string> GetErlaubteFunktionen(string typ, string? status)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                connection.Open();

                const string query = @"
                                     SELECT DISTINCT v.[Funktion]
                                     FROM [GebuehrStatusValidation] v
                                     WHERE v.[Typ] = @Typ
                                     AND (v.[Status] = @Status OR (@Status = '' AND (v.[Status] IS NULL OR v.[Status] = '')))
                                     AND v.[Erlaubt] = 1
                                     ORDER BY v.[Funktion]";

                using var cmd = new SqlCommand(query, connection);
                cmd.Parameters.Add("@Typ", SqlDbType.NVarChar).Value = typ;
                cmd.Parameters.Add("@Status", SqlDbType.NVarChar).Value = status ?? string.Empty;

                var result = new List<string>();
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    if (reader["Funktion"] != DBNull.Value)
                        result.Add((string)reader["Funktion"]);
                }
                return result;
            }
            catch (Exception ex)
            {
                throw new RepositoryException($"GetErlaubteFunktionen fehlgeschlagen. Typ: {typ}, Status: {status}. Fehler: {ex.Message}", ex);
            }
        }

        // Löscht die Gebühr samt Titeln in einer Transaktion.
        public void LoescheGebuehr(string aktenzeichen, int lfdNr)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                connection.Open();
                using var tx = connection.BeginTransaction();

                foreach (var tabelle in new[] { "GebuehrTitel", "Gebuehr" })   // erst Titel, dann Gebühr
                {
                    using var cmd = new SqlCommand($"DELETE FROM [{tabelle}] WHERE [Aktenzeichen] = @Aktenzeichen AND [LfdNr] = @LfdNr", connection, tx);
                    cmd.Parameters.Add("@Aktenzeichen", SqlDbType.NVarChar).Value = aktenzeichen;
                    cmd.Parameters.Add("@LfdNr", SqlDbType.Int).Value = lfdNr;
                    cmd.ExecuteNonQuery();
                }

                tx.Commit();
            }
            catch (Exception ex)
            {
                throw new RepositoryException($"LoescheGebuehr fehlgeschlagen. Aktenzeichen: {aktenzeichen}, LfdNr: {lfdNr}. Fehler: {ex.Message}", ex);
            }
        }

        // Setzt den Status der Gebühr auf "S" (STORNIERT).
        public void StorniereGebuehr(string aktenzeichen, int lfdNr, string? user)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                connection.Open();

                const string query = @"
                                     UPDATE [Gebuehr]
                                     SET [Status] = 'S',
                                     [GeaendertVon] = @User,
                                     [GeaendertAm] = GETDATE()
                                     WHERE [Aktenzeichen] = @Aktenzeichen AND [LfdNr] = @LfdNr";

                using var cmd = new SqlCommand(query, connection);
                cmd.Parameters.Add("@Aktenzeichen", SqlDbType.NVarChar).Value = aktenzeichen;
                cmd.Parameters.Add("@LfdNr", SqlDbType.Int).Value = lfdNr;
                cmd.Parameters.Add("@User", SqlDbType.NVarChar).Value = (object?)user ?? DBNull.Value;
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                throw new RepositoryException($"StorniereGebuehr fehlgeschlagen. Aktenzeichen: {aktenzeichen}, LfdNr: {lfdNr}. Fehler: {ex.Message}", ex);
            }
        }

        // Legt Gebühr/Vorschuss (Status A) samt Titeln in einer Transaktion an.
        public void CreateGebuehr(GebuehrNeuRequest req, string? user)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                connection.Open();
                using var tx = connection.BeginTransaction();

                // Nächste LfdNr (gesperrt, damit parallele Anlagen keine doppelte Nummer bekommen).
                int lfdNr;
                using (var cmd = new SqlCommand("SELECT ISNULL(MAX([LfdNr]), 0) + 1 FROM [Gebuehr] WITH (UPDLOCK, HOLDLOCK) WHERE [Aktenzeichen] = @Aktenzeichen", connection, tx))
                {
                    cmd.Parameters.Add("@Aktenzeichen", SqlDbType.NVarChar).Value = req.Aktenzeichen;
                    lfdNr = (int)cmd.ExecuteScalar()!;
                }

                var typ = req.Art == "V" ? "Vorschussforderung" : "Gebührenforderung";
                var summe = req.Titel.Sum(t => t.Betrag);

                var bescheidAn = GetGebuehrenbescheidAn(connection, tx, req.Aktenzeichen);

                using (var cmd = new SqlCommand(@"
                                                INSERT INTO [Gebuehr] ([Aktenzeichen], [LfdNr], [Typ], [Status], [Tarifstelle], [Ermittlung],
                                                [Bemerkung], [IBAN], [BIC], [Gebuehr], [BeantragtAm], [AngelegtVon], [AngelegtAm], [GebuehrenbescheidAn])
                                                VALUES (@Aktenzeichen, @LfdNr, @Typ, 'A', @Tarifstelle, @Ermittlung,
                                                @Bemerkung, @IBAN, @BIC, @Gebuehr, GETDATE(), @User, GETDATE(), @BescheidAn)", connection, tx))
                {
                    cmd.Parameters.Add("@Aktenzeichen", SqlDbType.NVarChar).Value = req.Aktenzeichen;
                    cmd.Parameters.Add("@LfdNr", SqlDbType.Int).Value = lfdNr;
                    cmd.Parameters.Add("@Typ", SqlDbType.NVarChar).Value = typ;
                    cmd.Parameters.Add("@Tarifstelle", SqlDbType.NVarChar).Value = (object?)req.Tarifstelle ?? DBNull.Value;
                    cmd.Parameters.Add("@Ermittlung", SqlDbType.NVarChar).Value = (object?)req.Ermittlung ?? DBNull.Value;
                    cmd.Parameters.Add("@Bemerkung", SqlDbType.NVarChar).Value = (object?)req.Bemerkung ?? DBNull.Value;
                    cmd.Parameters.Add("@IBAN", SqlDbType.NVarChar).Value = (object?)req.Iban ?? DBNull.Value;
                    cmd.Parameters.Add("@BIC", SqlDbType.NVarChar).Value = (object?)req.Bic ?? DBNull.Value;
                    var p = cmd.Parameters.Add("@Gebuehr", SqlDbType.Decimal); p.Precision = 18; p.Scale = 2; p.Value = summe;
                    cmd.Parameters.Add("@User", SqlDbType.NVarChar).Value = (object?)user ?? DBNull.Value;
                    cmd.Parameters.Add("@BescheidAn", SqlDbType.NVarChar).Value = (object?)bescheidAn ?? DBNull.Value;
                    cmd.ExecuteNonQuery();
                }

                foreach (var t in req.Titel)
                {
                    using var cmd = new SqlCommand(@"
                                                   INSERT INTO [GebuehrTitel] ([Aktenzeichen], [LfdNr], [TitelNr], [Betrag], [BetragBezahlt], [Typ])
                                                   VALUES (@Aktenzeichen, @LfdNr, @TitelNr, @Betrag, 0, @Typ)", connection, tx);

                    cmd.Parameters.Add("@Aktenzeichen", SqlDbType.NVarChar).Value = req.Aktenzeichen;
                    cmd.Parameters.Add("@LfdNr", SqlDbType.Int).Value = lfdNr;
                    cmd.Parameters.Add("@TitelNr", SqlDbType.NVarChar).Value = t.TitelNr;
                    var p = cmd.Parameters.Add("@Betrag", SqlDbType.Decimal); p.Precision = 18; p.Scale = 2; p.Value = t.Betrag;
                    cmd.Parameters.Add("@Typ", SqlDbType.NVarChar).Value = typ;
                    cmd.ExecuteNonQuery();
                }

                tx.Commit();
            }
            catch (Exception ex)
            {
                throw new RepositoryException($"CreateGebuehr fehlgeschlagen. Aktenzeichen: {req.Aktenzeichen}. Fehler: {ex.Message}", ex);
            }
        }

        // Aktualisiert Gebühr/Vorschuss und gleicht die Titel ab (Update, Neuanlage, Löschen) in einer Transaktion.
        public void UpdateGebuehr(GebuehrEditRequest req, string? user)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                connection.Open();
                using var tx = connection.BeginTransaction();

                var bescheidAn = GetGebuehrenbescheidAn(connection, tx, req.Aktenzeichen);

                // 1. Kopfdaten.
                using (var cmd = new SqlCommand(@"
                                                UPDATE [Gebuehr]
                                                SET [Tarifstelle] = @Tarifstelle, [Ermittlung] = @Ermittlung, [Bemerkung] = @Bemerkung,
                                                [IBAN] = @IBAN, [BIC] = @BIC, [Gebuehr] = @Gebuehr,
                                                [GeaendertVon] = @User, [GeaendertAm] = GETDATE(), [GebuehrenbescheidAn] = @BescheidAn
                                                WHERE [Aktenzeichen] = @Aktenzeichen AND [LfdNr] = @LfdNr", connection, tx))
                {
                    cmd.Parameters.Add("@Aktenzeichen", SqlDbType.NVarChar).Value = req.Aktenzeichen;
                    cmd.Parameters.Add("@LfdNr", SqlDbType.Int).Value = req.LfdNr;
                    cmd.Parameters.Add("@Tarifstelle", SqlDbType.NVarChar).Value = (object?)req.Tarifstelle ?? DBNull.Value;
                    cmd.Parameters.Add("@Ermittlung", SqlDbType.NVarChar).Value = (object?)req.Ermittlung ?? DBNull.Value;
                    cmd.Parameters.Add("@Bemerkung", SqlDbType.NVarChar).Value = (object?)req.Bemerkung ?? DBNull.Value;
                    cmd.Parameters.Add("@IBAN", SqlDbType.NVarChar).Value = (object?)req.Iban ?? DBNull.Value;
                    cmd.Parameters.Add("@BIC", SqlDbType.NVarChar).Value = (object?)req.Bic ?? DBNull.Value;
                    var p = cmd.Parameters.Add("@Gebuehr", SqlDbType.Decimal); p.Precision = 18; p.Scale = 2; p.Value = req.Titel.Sum(t => t.Betrag);
                    cmd.Parameters.Add("@User", SqlDbType.NVarChar).Value = (object?)user ?? DBNull.Value;
                    cmd.Parameters.Add("@BescheidAn", SqlDbType.NVarChar).Value = (object?)bescheidAn ?? DBNull.Value;
                    cmd.ExecuteNonQuery();
                }

                // 2. Titel, die nicht mehr in der Liste sind, löschen.
                var namen = req.Titel.Select((_, i) => "@T" + i).ToList();
                using (var cmd = new SqlCommand($@"
                                                DELETE FROM [GebuehrTitel]
                                                WHERE [Aktenzeichen] = @Aktenzeichen AND [LfdNr] = @LfdNr
                                                AND [TitelNr] NOT IN ({string.Join(",", namen)})", connection, tx))
                {
                    cmd.Parameters.Add("@Aktenzeichen", SqlDbType.NVarChar).Value = req.Aktenzeichen;
                    cmd.Parameters.Add("@LfdNr", SqlDbType.Int).Value = req.LfdNr;
                    for (int i = 0; i < req.Titel.Count; i++)
                        cmd.Parameters.Add(namen[i], SqlDbType.NVarChar).Value = req.Titel[i].TitelNr;
                    cmd.ExecuteNonQuery();
                }

                // 3. Vorhandene Titel aktualisieren (BetragBezahlt bleibt erhalten), neue anlegen.
                foreach (var t in req.Titel)
                {
                    using var cmd = new SqlCommand(@"
                                                   UPDATE [GebuehrTitel] SET [Betrag] = @Betrag
                                                   WHERE [Aktenzeichen] = @Aktenzeichen AND [LfdNr] = @LfdNr AND [TitelNr] = @TitelNr;
                                                   IF @@ROWCOUNT = 0
                                                   INSERT INTO [GebuehrTitel] ([Aktenzeichen], [LfdNr], [TitelNr], [Betrag], [BetragBezahlt], [Typ])
                                                   SELECT @Aktenzeichen, @LfdNr, @TitelNr, @Betrag, 0, g.[Typ]
                                                   FROM [Gebuehr] g WHERE g.[Aktenzeichen] = @Aktenzeichen AND g.[LfdNr] = @LfdNr;",
                        connection, tx);
                    cmd.Parameters.Add("@Aktenzeichen", SqlDbType.NVarChar).Value = req.Aktenzeichen;
                    cmd.Parameters.Add("@LfdNr", SqlDbType.Int).Value = req.LfdNr;
                    cmd.Parameters.Add("@TitelNr", SqlDbType.NVarChar).Value = t.TitelNr;
                    var p = cmd.Parameters.Add("@Betrag", SqlDbType.Decimal); p.Precision = 18; p.Scale = 2; p.Value = t.Betrag;
                    cmd.ExecuteNonQuery();
                }

                tx.Commit();
            }
            catch (Exception ex)
            {
                throw new RepositoryException($"UpdateGebuehr fehlgeschlagen. Aktenzeichen: {req.Aktenzeichen}, LfdNr: {req.LfdNr}. Fehler: {ex.Message}", ex);
            }
        }

        // Liest den Empfänger des Gebührenbescheids aus Antragsteller (Typ "Gebührenbescheid an").
        // Gibt null zurück, wenn kein Eintrag vorhanden ist.
        private static string? GetGebuehrenbescheidAn(SqlConnection connection, SqlTransaction tx, string aktenzeichen)
        {
            using var cmd = new SqlCommand(@"
                                           SELECT TOP (1) [Adresse]
                                           FROM [Antragsteller]
                                           WHERE [Aktenzeichen] = @Aktenzeichen AND [Type] = 'Gebührenbescheid an'
                                           ORDER BY [Id]", connection, tx);
            cmd.Parameters.Add("@Aktenzeichen", SqlDbType.NVarChar).Value = aktenzeichen;
            return cmd.ExecuteScalar() as string;
        }

        // Schreibt einen Eintrag in die Historie (Ablaufdaten).
        public void SchreibeAblaufdatum(string aktenzeichen, string aktionTyp, string? bemerkung, string? user)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                connection.Open();

                using var cmd = new SqlCommand(@"
                                               INSERT INTO [Ablaufdaten] ([Aktenzeichen], [EintragAmVom], [EintragVon], [AktionAm], [AktionTyp], [Bemerkung], [MandantId])
                                               VALUES (@Aktenzeichen, GETDATE(), @User, GETDATE(), @AktionTyp, @Bemerkung, @MandantId)", connection);

                cmd.Parameters.Add("@Aktenzeichen", SqlDbType.NVarChar).Value = aktenzeichen;
                cmd.Parameters.Add("@User", SqlDbType.NVarChar).Value = string.IsNullOrWhiteSpace(user) ? "System" : user;
                cmd.Parameters.Add("@AktionTyp", SqlDbType.NVarChar).Value = aktionTyp;
                cmd.Parameters.Add("@Bemerkung", SqlDbType.NVarChar).Value = (object?)bemerkung ?? DBNull.Value;
                cmd.Parameters.Add("@MandantId", SqlDbType.Int).Value = _mandantProvider.MandantId;
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                throw new RepositoryException($"SchreibeAblaufdatum fehlgeschlagen. Aktenzeichen: {aktenzeichen}, Aktion: {aktionTyp}. Fehler: {ex.Message}", ex);
            }
        }

        // Setzt nur die Spalte Status der Gebühr.
        public void SetzeGebuehrStatus(string aktenzeichen, int lfdNr, string status, string? user)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                connection.Open();

                using var cmd = new SqlCommand(@"
                                               UPDATE [Gebuehr]
                                               SET [Status] = @Status, [GeaendertVon] = @User, [GeaendertAm] = GETDATE()
                                               WHERE [Aktenzeichen] = @Aktenzeichen AND [LfdNr] = @LfdNr", connection);

                cmd.Parameters.Add("@Aktenzeichen", SqlDbType.NVarChar).Value = aktenzeichen;
                cmd.Parameters.Add("@LfdNr", SqlDbType.Int).Value = lfdNr;
                cmd.Parameters.Add("@Status", SqlDbType.NVarChar).Value = status;
                cmd.Parameters.Add("@User", SqlDbType.NVarChar).Value = (object?)user ?? DBNull.Value;
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                throw new RepositoryException($"SetzeGebuehrStatus fehlgeschlagen. Aktenzeichen: {aktenzeichen}, LfdNr: {lfdNr}, Status: {status}. Fehler: {ex.Message}", ex);
            }
        }
























        //???

        // Vorgangsstatus / Umlauf-Übersicht.
        // Liefert Vorgangsstatus + letzten Durchlauf (GeaendertAm/GeaendertVon) + nächste offene Aktion (AktionHinweis/AktionNachricht/ErstelltVon/AktionTitel) für ein Aktenzeichen.
        public VorgangStatusUebersicht GetVorgangStatusUebersicht(string aktenzeichen)
        {
            VorgangStatusUebersicht result = null;
            try
            {
                using var connection = new SqlConnection(_connectionString);
                connection.Open();

                var query = @"
                            SELECT TOP (1)
                                 status.[Aktenzeichen], status.[Zulassungsnummer], status.[Antragsbestätigung], status.[SVA-Ergebnis],
                                 status.[Zwischeninformation], status.[Unterlagen vollständig], status.[Anlagen vollständig],
                                 status.[i.O. Bearbeiter], status.[Zur Unterschrift], status.[Schlusszeichnung], status.[Reinschrift],
                                 status.[zum Versand], status.[Bescheid an AS], status.[Widerspruchsfrist], status.[Widerspruch Gebühren],
                                 status.[Widerspruch Zulassung], status.[Bestandskräftig], status.[Widerspruch bearbeitet],
                                 letzterUmlauf.[GeaendertAm], letzterUmlauf.[GeaendertVon], letzterUmlauf.[AktionHinweis],
                                 letzterUmlauf.[AktionNachricht], letzterUmlauf.[An], letzterUmlauf.[AktionTitel]
                            FROM [EAkteDb].[dbo].[Vorgangsstatus] status
                            OUTER APPLY (
                                SELECT TOP (1)
                                    u.[GeaendertAm], u.[GeaendertVon], u.[AktionHinweis],
                                    u.[AktionNachricht], u.[An], u.[AktionTitel]
                                FROM [EAkteDb].[dbo].[AktiveUmlaeufeUebersicht] u
                                WHERE u.[Aktenzeichen] = status.[Aktenzeichen]
                                ORDER BY u.[TypID] DESC, u.[Durchlauf] DESC
                            ) letzterUmlauf
                            WHERE status.[Aktenzeichen] = @Aktenzeichen;";

                using var cmd = new SqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@Aktenzeichen", aktenzeichen);

                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    result = new VorgangStatusUebersicht
                    {
                        Aktenzeichen = reader["Aktenzeichen"] as string,
                        Zulassungsnummer = reader["Zulassungsnummer"] as string,
                        Antragsbestaetigung = reader["Antragsbestätigung"] as DateTime?,
                        SvaErgebnis = reader["SVA-Ergebnis"] as DateTime?,
                        Zwischeninformation = reader["Zwischeninformation"] as DateTime?,
                        UnterlagenVollstaendig = reader["Unterlagen vollständig"] as DateTime?,
                        AnlagenVollstaendig = reader["Anlagen vollständig"] as DateTime?,
                        IOBearbeiter = reader["i.O. Bearbeiter"] as DateTime?,
                        ZurUnterschrift = reader["Zur Unterschrift"] as DateTime?,
                        Schlusszeichnung = reader["Schlusszeichnung"] as DateTime?,
                        Reinschrift = reader["Reinschrift"] as DateTime?,
                        ZumVersand = reader["zum Versand"] as DateTime?,
                        BescheidAnAS = reader["Bescheid an AS"] as DateTime?,
                        Widerspruchsfrist = reader["Widerspruchsfrist"] as DateTime?,
                        WiderspruchGebuehren = reader["Widerspruch Gebühren"] as DateTime?,
                        WiderspruchZulassung = reader["Widerspruch Zulassung"] as DateTime?,
                        Bestandskraeftig = reader["Bestandskräftig"] as DateTime?,
                        WiderspruchBearbeitet = reader["Widerspruch bearbeitet"] as DateTime?,
                        GeaendertAm = reader["GeaendertAm"] as DateTime?,
                        GeaendertVon = reader["GeaendertVon"] as string,
                        AktionHinweis = reader["AktionHinweis"] as string,
                        AktionNachricht = reader["AktionNachricht"] as string,
                        An = reader["An"] as string,
                        AktionTitelNextId = reader["AktionTitel"] as string  
                    };
                }
            }
            catch (Exception ex)
            {
                throw new RepositoryException($"GetVorgangStatusUebersicht fehlgeschlagen. Fehler: {ex.Message}", ex);
            }
            return result;
        }


        // Dokument herunterladen.
        public DokumentVersion? GetVersionByVersionId(Guid versionId)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                connection.Open();

                var query = @"SELECT 
                            v.VersionNr, v.IsActive, v.IsArchived, v.Description, v.CreatedDate,
                            v.CreatedBy, v.ModifiedDate, v.ModifiedBy, v.FileStream, v.Bemerkungen,
                            v.Dokumentdatum, v.DocumentObjektId, v.CurrentVersionId, v.ContentType AS VersionContentType,
                            v.ContentSize, v.FullContentSize, v.IsSigned AS VersionIsSigned, v.Final AS VersionFinal, 
                            v.FileExtension, d.Name
                            FROM [DokumentVersionen] v
                            INNER JOIN [Dokumente] d ON d.ObjektId = v.DocumentObjektId
                            WHERE v.CurrentVersionId = @versionId";
                using var cmd = new SqlCommand(query, connection);
                cmd.Parameters.Add("@versionId", SqlDbType.UniqueIdentifier).Value = versionId;

                using var reader = cmd.ExecuteReader();

                if (!reader.Read())
                {
                    return null;
                }

                return new DokumentVersion
                {
                    VersionNr = reader.GetString(reader.GetOrdinal("VersionNr")),
                    IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
                    IsArchived = reader.GetBoolean(reader.GetOrdinal("IsArchived")),
                    Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? null : reader.GetString(reader.GetOrdinal("Description")),
                    CreatedDate = reader.GetDateTime(reader.GetOrdinal("CreatedDate")),
                    CreatedBy = reader.IsDBNull(reader.GetOrdinal("CreatedBy")) ? null : reader.GetString(reader.GetOrdinal("CreatedBy")),
                    ModifiedDate = reader.IsDBNull(reader.GetOrdinal("ModifiedDate")) ? null : reader.GetDateTime(reader.GetOrdinal("ModifiedDate")),
                    ModifiedBy = reader.IsDBNull(reader.GetOrdinal("ModifiedBy")) ? null : reader.GetString(reader.GetOrdinal("ModifiedBy")),
                    FileStream = reader.IsDBNull(reader.GetOrdinal("FileStream")) ? null : (byte[])reader["FileStream"],
                    Bemerkungen = reader.IsDBNull(reader.GetOrdinal("Bemerkungen")) ? null : reader.GetString(reader.GetOrdinal("Bemerkungen")),
                    Dokumentdatum = reader.IsDBNull(reader.GetOrdinal("Dokumentdatum")) ? null : reader.GetDateTime(reader.GetOrdinal("Dokumentdatum")),
                    DocumentObjektId = reader.GetGuid(reader.GetOrdinal("DocumentObjektId")),
                    CurrentVersionId = versionId,
                    ContentType = reader.IsDBNull(reader.GetOrdinal("VersionContentType")) ? null : reader.GetString(reader.GetOrdinal("VersionContentType")),
                    ContentSize = reader.GetInt64(reader.GetOrdinal("ContentSize")),
                    FullContentSize = reader.GetInt64(reader.GetOrdinal("FullContentSize")),
                    IsSigned = reader.GetBoolean(reader.GetOrdinal("VersionIsSigned")),
                    Final = reader.GetBoolean(reader.GetOrdinal("VersionFinal")),
                    FileExtension = reader.IsDBNull(reader.GetOrdinal("FileExtension")) ? null : reader.GetString(reader.GetOrdinal("FileExtension")),
                    DokumentName = reader.IsDBNull(reader.GetOrdinal("Name")) ? null : reader.GetString(reader.GetOrdinal("Name"))
                };
            }
            catch (Exception ex)
            {
                throw new RepositoryException($"GetVersionByVersionId fehlgeschlagen. VersionId: {versionId}. Fehler: {ex.Message}", ex);
            }
        }


        // Neueste Version anhand der VersionNr (Format "Major.Minor", z.B. "10.4") ermitteln.
        public DokumentVersion? GetLatestVersionByDocumentObjektId(Guid documentObjektId)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                connection.Open();

                var query = @"SELECT TOP 1
                            v.VersionNr, v.IsActive, v.IsArchived, v.Description, v.CreatedDate,
                            v.CreatedBy, v.ModifiedDate, v.ModifiedBy, v.FileStream, v.Bemerkungen,
                            v.Dokumentdatum, v.DocumentObjektId, v.CurrentVersionId, v.ContentType AS VersionContentType,
                            v.ContentSize, v.FullContentSize, v.IsSigned AS VersionIsSigned, v.Final AS VersionFinal, 
                            v.FileExtension, d.Name
                            FROM [DokumentVersionen] v
                            INNER JOIN [Dokumente] d ON d.ObjektId = v.DocumentObjektId
                            WHERE v.DocumentObjektId = @documentObjektId
                            ORDER BY 
                                TRY_CAST(PARSENAME(v.VersionNr, 2) AS INT) DESC,
                                TRY_CAST(PARSENAME(v.VersionNr, 1) AS INT) DESC";

                using var cmd = new SqlCommand(query, connection);
                cmd.Parameters.Add("@documentObjektId", SqlDbType.UniqueIdentifier).Value = documentObjektId;

                using var reader = cmd.ExecuteReader();

                if (!reader.Read())
                {
                    return null;
                }

                return new DokumentVersion
                {
                    VersionNr = reader.GetString(reader.GetOrdinal("VersionNr")),
                    IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
                    IsArchived = reader.GetBoolean(reader.GetOrdinal("IsArchived")),
                    Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? null : reader.GetString(reader.GetOrdinal("Description")),
                    CreatedDate = reader.GetDateTime(reader.GetOrdinal("CreatedDate")),
                    CreatedBy = reader.IsDBNull(reader.GetOrdinal("CreatedBy")) ? null : reader.GetString(reader.GetOrdinal("CreatedBy")),
                    ModifiedDate = reader.IsDBNull(reader.GetOrdinal("ModifiedDate")) ? null : reader.GetDateTime(reader.GetOrdinal("ModifiedDate")),
                    ModifiedBy = reader.IsDBNull(reader.GetOrdinal("ModifiedBy")) ? null : reader.GetString(reader.GetOrdinal("ModifiedBy")),
                    FileStream = reader.IsDBNull(reader.GetOrdinal("FileStream")) ? null : (byte[])reader["FileStream"],
                    Bemerkungen = reader.IsDBNull(reader.GetOrdinal("Bemerkungen")) ? null : reader.GetString(reader.GetOrdinal("Bemerkungen")),
                    Dokumentdatum = reader.IsDBNull(reader.GetOrdinal("Dokumentdatum")) ? null : reader.GetDateTime(reader.GetOrdinal("Dokumentdatum")),
                    DocumentObjektId = reader.GetGuid(reader.GetOrdinal("DocumentObjektId")),
                    CurrentVersionId = reader.GetGuid(reader.GetOrdinal("CurrentVersionId")),
                    ContentType = reader.IsDBNull(reader.GetOrdinal("VersionContentType")) ? null : reader.GetString(reader.GetOrdinal("VersionContentType")),
                    ContentSize = reader.GetInt64(reader.GetOrdinal("ContentSize")),
                    FullContentSize = reader.GetInt64(reader.GetOrdinal("FullContentSize")),
                    IsSigned = reader.GetBoolean(reader.GetOrdinal("VersionIsSigned")),
                    Final = reader.GetBoolean(reader.GetOrdinal("VersionFinal")),
                    FileExtension = reader.IsDBNull(reader.GetOrdinal("FileExtension")) ? null : reader.GetString(reader.GetOrdinal("FileExtension")),
                    DokumentName = reader.IsDBNull(reader.GetOrdinal("Name")) ? null : reader.GetString(reader.GetOrdinal("Name"))
                };
            }
            catch (Exception ex)
            {
                throw new RepositoryException($"GetLatestVersionByDocumentObjektId fehlgeschlagen. DocumentObjektId: {documentObjektId}. Fehler: {ex.Message}", ex);
            }
        }

        // Aktualisiert den Inhalt einer bestehenden Version (Testmodus: keine neue Version, kein Locking).
        public bool UpdateVersionContent(Guid versionId, byte[] fileBytes, string? contentType, long contentSize)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                connection.Open();

                var query = @"UPDATE [DokumentVersionen]
                      SET FileStream = @fileStream,
                          ContentType = @contentType,
                          ContentSize = @contentSize,
                          FullContentSize = @contentSize,
                          ModifiedDate = @modifiedDate,
                          ModifiedBy = @modifiedBy
                      WHERE CurrentVersionId = @versionId";

                using var cmd = new SqlCommand(query, connection);
                cmd.Parameters.Add("@versionId", SqlDbType.UniqueIdentifier).Value = versionId;
                cmd.Parameters.Add("@fileStream", SqlDbType.VarBinary).Value = fileBytes;
                cmd.Parameters.Add("@contentType", SqlDbType.NVarChar).Value = (object?)contentType ?? DBNull.Value;
                cmd.Parameters.Add("@contentSize", SqlDbType.BigInt).Value = contentSize;
                cmd.Parameters.Add("@modifiedDate", SqlDbType.DateTime2).Value = DateTime.UtcNow;
                cmd.Parameters.Add("@modifiedBy", SqlDbType.NVarChar).Value = "TestUser"; // позже: User.Identity.Name

                var rowsAffected = cmd.ExecuteNonQuery();
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                throw new RepositoryException($"UpdateVersionContent fehlgeschlagen. VersionId: {versionId}. Fehler: {ex.Message}", ex);
            }
        }




       

    }
}