using DreiF.Shared.Abstractions;
using EAktePrototypProjektNewMvc.Exceptions;
using EAktePrototypProjektNewMvc.Models.Akten;
using EAktePrototypProjektNewMvc.Models.Dokumente;
using EAktePrototypProjektNewMvc.Models.Ordner;
using EAktePrototypProjektNewMvc.Models.Vorgange;
using Microsoft.Data.SqlClient;

namespace EAktePrototypProjektNewMvc.Repositories.Tree
{
    public class TypeSearchRepository
    {
        private readonly string _connectionString;
        private readonly ICurrentMandantProvider _mandantProvider;

        public TypeSearchRepository(string connectionString, ICurrentMandantProvider mandantProvider)
        {
            _connectionString = connectionString;
            _mandantProvider = mandantProvider;
        }

        // Führt flexible Suchabfragen für Akten anhand optionaler Filter aus.
        public IQueryable<Akte> GetAktenSearchQuery(AkteSearchModel searchModel, string? sortColumn = "HauptgruppeNummer", string? sortDirection = "asc")
        {
            try
            {
                var allowedColumns = new HashSet<string>
                {
                    "Zulassungsnummer", "HauptgruppeNummer", "NebengruppeNummer",
                    "Zustand", "ErzeugtVon", "ErzeugtAm", "GeandertVon", "GeandertAm", "ObjektId"
                };

                var column = allowedColumns.Contains(sortColumn) ? sortColumn : "HauptgruppeNummer";
                var direction = sortDirection == "desc" ? "DESC" : "ASC";

                var list = new List<Akte>();

                using var connection = new SqlConnection(_connectionString);
                connection.Open();

                var query = $@"SELECT Zulassungsnummer, LfdNr, HauptgruppeNummer, NebengruppeNummer, ObjektId, Zustand,
                             ErzeugtVon, ErzeugtAm, GeandertVon, GeandertAm
                             FROM [Akte]
                             WHERE
                             (@Zulassungsnummer IS NULL OR Zulassungsnummer LIKE '%' + @Zulassungsnummer + '%')
                             AND (@LfdNr IS NULL OR LfdNr LIKE '%' + @LfdNr + '%')
                             AND (@HauptgruppeNummer IS NULL OR HauptgruppeNummer LIKE '%' + @HauptgruppeNummer + '%')
                             AND (@NebengruppeNummer IS NULL OR NebengruppeNummer LIKE '%' + @NebengruppeNummer + '%')
                             AND (@Zustand IS NULL OR Zustand LIKE '%' + @Zustand + '%')
                             AND (@ErzeugtVon IS NULL OR ErzeugtVon LIKE '%' + @ErzeugtVon + '%')
                             AND (@GeandertVon IS NULL OR GeandertVon LIKE '%' + @GeandertVon + '%')
                             AND (@ObjektId IS NULL OR CAST(ObjektId AS NVARCHAR(36)) LIKE '%' + @ObjektId + '%')
                             AND (@ErzeugtAmVon IS NULL OR ErzeugtAm >= @ErzeugtAmVon)
                             AND (@ErzeugtAmBis IS NULL OR ErzeugtAm < DATEADD(day, 1, @ErzeugtAmBis))
                             AND (@GeandertAmVon IS NULL OR GeandertAm >= @GeandertAmVon)
                             AND (@GeandertAmBis IS NULL OR GeandertAm < DATEADD(day, 1, @GeandertAmBis))
                             AND MandantId = @MandantId
                             ORDER BY {column} {direction}";

                using var cmd = new SqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@Zulassungsnummer", (object?)searchModel.Zulassungsnummer ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@LfdNr", (object?)searchModel.LfdNr?.ToString() ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@HauptgruppeNummer", (object?)searchModel.HauptgruppeNummer ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@NebengruppeNummer", (object?)searchModel.NebengruppeNummer ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Zustand", (object?)searchModel.Zustand ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ErzeugtVon", (object?)searchModel.ErzeugtVon ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@GeandertVon", (object?)searchModel.GeandertVon ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ObjektId", (object?)searchModel.ObjektId?.ToString() ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ErzeugtAmVon", (object?)searchModel.ErzeugtAmVon ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ErzeugtAmBis", (object?)searchModel.ErzeugtAmBis ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@GeandertAmVon", (object?)searchModel.GeandertAmVon ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@GeandertAmBis", (object?)searchModel.GeandertAmBis ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@MandantId", _mandantProvider.MandantId);

                using var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    list.Add(new Akte
                    {
                        Zulassungsnummer = reader["Zulassungsnummer"] as string,
                        LfdNr = reader["LfdNr"] as int? ?? 0,
                        HauptgruppeNummer = reader["HauptgruppeNummer"] as int? ?? 0,
                        NebengruppeNummer = reader["NebengruppeNummer"] as int? ?? 0,
                        ObjektId = reader.GetGuid(reader.GetOrdinal("ObjektId")),
                        Zustand = reader["Zustand"] as string,
                        ErzeugtVon = reader["ErzeugtVon"] as string,
                        ErzeugtAm = reader["ErzeugtAm"] != DBNull.Value ? (DateTime?)reader["ErzeugtAm"] : null,
                        GeandertVon = reader["GeandertVon"] as string,
                        GeandertAm = reader["GeandertAm"] != DBNull.Value ? (DateTime?)reader["GeandertAm"] : null
                    });
                }

                return list.AsQueryable();
            }
            catch (Exception ex)
            {
                throw new RepositoryException("Fehler in der GetAktenSearchQuery Funktion.", ex);
            }
        }

        // Führt flexible Suchabfragen für Vorgänge anhand optionaler Filter aus.
        public IQueryable<Vorgang> GetVorgaengeSearchQuery(VorgangSearchModel searchModel, string? sortColumn = "ErzeugtAm", string? sortDirection = "asc")
        {
            try
            {
                var allowedColumns = new HashSet<string>
                {
                    "Aktenzeichen", "Zulassungsnummer", "UserID", "Abteilung",
                    "Stelle", "Status", "Gegenstand", "Betreff", "Bemerkung", "ErzeugtAm", "ObjektId"
                };

                var column = allowedColumns.Contains(sortColumn) ? sortColumn : "ErzeugtAm";
                var direction = sortDirection == "desc" ? "DESC" : "ASC";

                var list = new List<Vorgang>();

                using var connection = new SqlConnection(_connectionString);
                connection.Open();

                var query = $@"SELECT Aktenzeichen, UserID, Abteilung, Stelle, Zulassungsnummer, Status, 
                            ErzeugtVon, ErzeugtAm, ObjektId, Antragsart, Bescheidtyp, 
                            Gegenstand, Betreff, Bemerkung, DatumAntrag
                            FROM [Vorgang]
                            WHERE 
                                (@Aktenzeichen      IS NULL OR Aktenzeichen     LIKE '%' + @Aktenzeichen      + '%')
                            AND (@UserID            IS NULL OR UserID           LIKE '%' + @UserID            + '%')
                            AND (@Abteilung         IS NULL OR Abteilung        LIKE '%' + @Abteilung         + '%')
                            AND (@Stelle            IS NULL OR Stelle           LIKE '%' + @Stelle            + '%')
                            AND (@Zulassungsnummer  IS NULL OR Zulassungsnummer LIKE '%' + @Zulassungsnummer  + '%')
                            AND (@Status            IS NULL OR Status           LIKE '%' + @Status            + '%')
                            AND (@ErzeugtVon        IS NULL OR ErzeugtVon       LIKE '%' + @ErzeugtVon        + '%')
                            AND (@ObjektId          IS NULL OR CAST(ObjektId AS NVARCHAR(36)) LIKE '%' + @ObjektId + '%')
                            AND (@Antragsart        IS NULL OR Antragsart       LIKE '%' + @Antragsart        + '%')
                            AND (@Bescheidtyp       IS NULL OR Bescheidtyp      LIKE '%' + @Bescheidtyp       + '%')
                            AND (@Gegenstand        IS NULL OR Gegenstand       LIKE '%' + @Gegenstand        + '%')
                            AND (@Betreff           IS NULL OR Betreff          LIKE '%' + @Betreff           + '%')
                            AND (@Bemerkung         IS NULL OR Bemerkung        LIKE '%' + @Bemerkung         + '%')
                            AND (@ErzeugtAmVon      IS NULL OR ErzeugtAm >= @ErzeugtAmVon)
                            AND (@ErzeugtAmBis      IS NULL OR ErzeugtAm < DATEADD(day, 1, @ErzeugtAmBis))
                            AND MandantId = @MandantId
                            ORDER BY {column} {direction}";

                using var cmd = new SqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@Aktenzeichen", (object?)searchModel.Aktenzeichen ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@UserID", (object?)searchModel.UserID ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Abteilung", (object?)searchModel.Abteilung ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Stelle", (object?)searchModel.Stelle ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Zulassungsnummer", (object?)searchModel.Zulassungsnummer ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Status", (object?)searchModel.Status ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ErzeugtVon", (object?)searchModel.ErzeugtVon ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ObjektId", (object?)searchModel.ObjektId?.ToString() ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Antragsart", (object?)searchModel.Antragsart ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Bescheidtyp", (object?)searchModel.Bescheidtyp ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Gegenstand", (object?)searchModel.Gegenstand ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Betreff", (object?)searchModel.Betreff ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Bemerkung", (object?)searchModel.Bemerkung ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ErzeugtAmVon", (object?)searchModel.ErzeugtAmVon ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ErzeugtAmBis", (object?)searchModel.ErzeugtAmBis ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@MandantId", _mandantProvider.MandantId);

                using var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    list.Add(new Vorgang
                    {
                        Aktenzeichen = reader["Aktenzeichen"] as string,
                        UserID = reader["UserID"] as string,
                        Abteilung = reader["Abteilung"] as string,
                        Stelle = reader["Stelle"] as string,
                        Zulassungsnummer = reader["Zulassungsnummer"] as string,
                        Status = reader["Status"] as string,
                        ErzeugtVon = reader["ErzeugtVon"] as string,
                        ErzeugtAm = reader["ErzeugtAm"] != DBNull.Value ? (DateTime?)reader["ErzeugtAm"] : null,
                        ObjektId = reader.GetGuid(reader.GetOrdinal("ObjektId")),
                        Antragsart = reader["Antragsart"] as string,
                        Bescheidtyp = reader["Bescheidtyp"] as string,
                        Gegenstand = reader["Gegenstand"] as string,
                        Betreff = reader["Betreff"] as string,
                        Bemerkung = reader["Bemerkung"] as string,
                        DatumAntrag = reader["DatumAntrag"] != DBNull.Value ? (DateTime?)reader["DatumAntrag"] : null,
                    });
                }

                return list.AsQueryable();
            }
            catch (Exception ex)
            {
                throw new RepositoryException("Fehler in der GetVorgaengeSearchQuery Funktion.", ex);
            }
        }

        // Führt flexible Suchabfragen für Ordner anhand optionaler Filter aus.
        public IQueryable<Ordner> GetFoldersSearchQuery(OrdnerSearchModel searchModel, string? sortColumn = "Name", string? sortDirection = "asc")
        {
            try
            {
                var allowedColumns = new HashSet<string>
                {
                    "Name", "Type", "Aktenzeichen", "Hauptsachgebiet",
                    "Sachgebiet", "Bemerkungen", "OwnerName", "Status", "CreationDate", "ObjektId"
                };

                var column = allowedColumns.Contains(sortColumn) ? sortColumn : "Name";
                var direction = sortDirection == "desc" ? "DESC" : "ASC";

                var list = new List<Ordner>();

                using var connection = new SqlConnection(_connectionString);
                connection.Open();

                var query = $@"SELECT ObjektId, ParentObjectId, Name, Type, CreatorName, CreationDate, 
                            Modifier, ModifyDate, OwnerName, OwnerPermit, GroupPermit,
                            WorldPermit, Hauptsachgebiet, Sachgebiet, Aktenzeichen, LfdNr, 
                            Bemerkungen, ColorHex, Status, Final, Path,
                            AclName, AclDomain, IsHidden, IsDeleted, IsPublic, IsSigned
                            FROM [Ordner]
                            WHERE 
                                (@Name              IS NULL OR Name         LIKE '%' + @Name              + '%')
                            AND (@Type              IS NULL OR Type         LIKE '%' + @Type              + '%')
                            AND (@Aktenzeichen      IS NULL OR Aktenzeichen LIKE '%' + @Aktenzeichen      + '%')
                            AND (@OwnerName         IS NULL OR OwnerName    LIKE '%' + @OwnerName         + '%')
                            AND (@Status            IS NULL OR Status       LIKE '%' + @Status            + '%')
                            AND (@Path              IS NULL OR Path         LIKE '%' + @Path              + '%')
                            AND (@Hauptsachgebiet   IS NULL OR Hauptsachgebiet LIKE '%' + @Hauptsachgebiet + '%')
                            AND (@Sachgebiet        IS NULL OR Sachgebiet   LIKE '%' + @Sachgebiet        + '%')
                            AND (@Bemerkungen       IS NULL OR Bemerkungen  LIKE '%' + @Bemerkungen       + '%')
                            AND (@CreatorName       IS NULL OR CreatorName  LIKE '%' + @CreatorName       + '%')
                            AND (@Modifier          IS NULL OR Modifier     LIKE '%' + @Modifier          + '%')
                            AND (@ObjektId          IS NULL OR CAST(ObjektId AS NVARCHAR(36))       LIKE '%' + @ObjektId       + '%')
                            AND (@ParentObjectId    IS NULL OR CAST(ParentObjectId AS NVARCHAR(36)) LIKE '%' + @ParentObjectId + '%')
                            AND (@CreationDateVon   IS NULL OR CreationDate >= @CreationDateVon)
                            AND (@CreationDateBis   IS NULL OR CreationDate < DATEADD(day, 1, @CreationDateBis))
                            AND MandantId = @MandantId
                            ORDER BY {column} {direction}";

                using var cmd = new SqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@Name", (object?)searchModel.Name ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Type", (object?)searchModel.Type ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Aktenzeichen", (object?)searchModel.Aktenzeichen ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@OwnerName", (object?)searchModel.OwnerName ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Status", (object?)searchModel.Status ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Path", (object?)searchModel.Path ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Hauptsachgebiet", (object?)searchModel.Hauptsachgebiet ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Sachgebiet", (object?)searchModel.Sachgebiet ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Bemerkungen", (object?)searchModel.Bemerkungen ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@CreatorName", (object?)searchModel.CreatorName ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Modifier", (object?)searchModel.Modifier ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ObjektId", (object?)searchModel.ObjektId?.ToString() ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ParentObjectId", (object?)searchModel.ParentObjectId?.ToString() ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@CreationDateVon", (object?)searchModel.CreationDateVon ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@CreationDateBis", (object?)searchModel.CreationDateBis ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@MandantId", _mandantProvider.MandantId);

                using var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    list.Add(new Ordner
                    {
                        ObjektId = reader.GetGuid(reader.GetOrdinal("ObjektId")),
                        ParentObjectId = reader.IsDBNull(reader.GetOrdinal("ParentObjectId")) ? (Guid?)null : reader.GetGuid(reader.GetOrdinal("ParentObjectId")),
                        Name = reader["Name"] as string,
                        Type = reader["Type"] as string,
                        CreatorName = reader["CreatorName"] as string,
                        CreationDate = reader.GetDateTime(reader.GetOrdinal("CreationDate")),
                        Modifier = reader["Modifier"] as string,
                        ModifyDate = reader.IsDBNull(reader.GetOrdinal("ModifyDate")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("ModifyDate")),
                        OwnerName = reader["OwnerName"] as string,
                        OwnerPermit = reader.IsDBNull(reader.GetOrdinal("OwnerPermit")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("OwnerPermit")),
                        GroupPermit = reader.IsDBNull(reader.GetOrdinal("GroupPermit")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("GroupPermit")),
                        WorldPermit = reader.IsDBNull(reader.GetOrdinal("WorldPermit")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("WorldPermit")),
                        Hauptsachgebiet = reader["Hauptsachgebiet"] as string,
                        Sachgebiet = reader["Sachgebiet"] as string,
                        Aktenzeichen = reader["Aktenzeichen"] as string,
                        LfdNr = reader.IsDBNull(reader.GetOrdinal("LfdNr")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("LfdNr")),
                        Bemerkungen = reader["Bemerkungen"] as string,
                        Status = reader["Status"] as string,
                        Final = reader.GetBoolean(reader.GetOrdinal("Final")),
                        Path = reader["Path"] as string,
                        ColorHex = reader["ColorHex"] as string,
                        IsHidden = reader.IsDBNull(reader.GetOrdinal("IsHidden")) ? false : reader.GetBoolean(reader.GetOrdinal("IsHidden")),
                        IsDeleted = reader.IsDBNull(reader.GetOrdinal("IsDeleted")) ? false : reader.GetBoolean(reader.GetOrdinal("IsDeleted")),
                        IsPublic = reader.IsDBNull(reader.GetOrdinal("IsPublic")) ? false : reader.GetBoolean(reader.GetOrdinal("IsPublic")),
                        IsSigned = reader.IsDBNull(reader.GetOrdinal("IsSigned")) ? false : reader.GetBoolean(reader.GetOrdinal("IsSigned")),
                    });
                }

                return list.AsQueryable();
            }
            catch (Exception ex)
            {
                throw new RepositoryException("Fehler in der GetFoldersSearchQuery Funktion.", ex);
            }
        }

        // Führt flexible Suchabfragen für Dokumente anhand optionaler Filter aus.
        public IQueryable<DokumentListItem> GetDokumenteSearchQuery(DokumentSearchModel searchModel, string? sortColumn = "CreationDate", string? sortDirection = "asc")
        {
            try
            {

                var columnMap = new Dictionary<string, string>
                {
                    ["Name"] = "d.Name",
                    ["CreatorName"] = "d.CreatorName",
                    ["CreationDate"] = "d.CreationDate",
                    ["ModifyDate"] = "d.ModifyDate",
                    ["ContentType"] = "d.ContentType",
                    ["VersionNr"] = "dv.VersionNr",
                    ["ObjektId"] = "d.ObjektId"
                };

                var column = (sortColumn != null && columnMap.ContainsKey(sortColumn)) ? columnMap[sortColumn] : "d.CreationDate";
                var direction = sortDirection == "desc" ? "DESC" : "ASC";

                var list = new List<DokumentListItem>();

                using var connection = new SqlConnection(_connectionString);
                connection.Open();

                var query = $@"SELECT 
                            d.ObjektId,
                            d.Name,
                            d.ContentType,
                            d.CreatorName,
                            d.CreationDate,
                            d.ModifyDate,
                            dv.VersionNr,
                            d.Subject,
                            d.Gegenstand,
                            d.Antragsteller,
                            d.OwnerName,
                            d.Aktenzeichen,
                            d.ACLName,
                            d.Modifier,
                            d.Eingangsdatum,
                            d.IsPublic,
                            d.IsSigned,
                            d.Final,
                            d.IsHidden,
                            d.IsDeleted,
                            dv.Description,
                            dv.Bemerkungen,
                            dv.FileExtension,
                            dv.ContentSize,
                            dv.CreatedBy AS VersionCreatedBy,
                            dv.Dokumentdatum,
                            d.IstAusVorlagenverwaltung
                            FROM [Dokumente] d
                            INNER JOIN [DokumentVersionen] dv
                                ON dv.DocumentObjektId = d.ObjektId
                                AND dv.IsActive = 1
                            WHERE
                                (@Name IS NULL OR d.Name LIKE '%' + @Name + '%')
                                AND (@CreatorName IS NULL OR d.CreatorName LIKE '%' + @CreatorName + '%')
                                AND (@CreationDateVon IS NULL OR d.CreationDate >= @CreationDateVon)
                                AND (@CreationDateBis IS NULL OR d.CreationDate < DATEADD(day, 1, @CreationDateBis))
                                AND (@ModifyDateVon IS NULL OR d.ModifyDate >= @ModifyDateVon)
                                AND (@ModifyDateBis IS NULL OR d.ModifyDate < DATEADD(day, 1, @ModifyDateBis))
                                AND d.MandantId = @MandantId
                            ORDER BY {column} {direction}";

                using var cmd = new SqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@Name", (object?)searchModel.Name ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@CreatorName", (object?)searchModel.CreatorName ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@CreationDateVon", (object?)searchModel.CreationDateVon ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@CreationDateBis", (object?)searchModel.CreationDateBis ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ModifyDateVon", (object?)searchModel.ModifyDateVon ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ModifyDateBis", (object?)searchModel.ModifyDateBis ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@MandantId", _mandantProvider.MandantId);

                using var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    list.Add(new DokumentListItem
                    {
                        ObjektId = reader.GetGuid(reader.GetOrdinal("ObjektId")),
                        Name = reader["Name"] as string,
                        ContentType = reader["ContentType"] as string,
                        CreatorName = reader["CreatorName"] as string,
                        CreationDate = reader["CreationDate"] != DBNull.Value ? (DateTime?)reader["CreationDate"] : null,
                        ModifyDate = reader["ModifyDate"] != DBNull.Value ? (DateTime?)reader["ModifyDate"] : null,
                        VersionNr = reader["VersionNr"] != DBNull.Value ? reader["VersionNr"].ToString() : null,
                        Subject = reader["Subject"] as string,
                        Gegenstand = reader["Gegenstand"] as string,
                        Antragsteller = reader["Antragsteller"] as string,
                        OwnerName = reader["OwnerName"] as string,
                        Aktenzeichen = reader["Aktenzeichen"] as string,
                        ACLName = reader["ACLName"] as string,
                        Modifier = reader["Modifier"] as string,
                        Eingangsdatum = reader["Eingangsdatum"] != DBNull.Value ? (DateTime?)reader["Eingangsdatum"] : null,
                        IsPublic = reader["IsPublic"] as bool? ?? false,
                        IsSigned = reader["IsSigned"] as bool? ?? false,
                        Final = reader["Final"] as bool? ?? false,
                        IsHidden = reader["IsHidden"] as bool? ?? false,
                        IsDeleted = reader["IsDeleted"] as bool? ?? false,
                        Description = reader["Description"] as string,
                        Bemerkungen = reader["Bemerkungen"] as string,
                        FileExtension = reader["FileExtension"] as string,
                        ContentSize = reader["ContentSize"] != DBNull.Value ? Convert.ToInt64(reader["ContentSize"]) : 0,
                        VersionCreatedBy = reader["VersionCreatedBy"] as string,
                        Dokumentdatum = reader["Dokumentdatum"] != DBNull.Value ? (DateTime?)reader["Dokumentdatum"] : null,
                        IstAusVorlagenverwaltung = reader["IstAusVorlagenverwaltung"] as bool? ?? false
                    });
                }

                return list.AsQueryable();
            }
            catch (Exception ex)
            {
                throw new RepositoryException("Fehler in der GetDokumenteSearchQuery Funktion.", ex);
            }
        }
    }
}