using DreiF.Shared.Abstractions;
using EAktePrototypProjektNewMvc.Exceptions;
using Microsoft.Data.SqlClient;
using System.Data;

namespace EAktePrototypProjektNewMvc.Repositories.Tree
{
    public class AsyncBescheidRepository
    {
        private readonly string _connectionString;
        private readonly ICurrentMandantProvider _mandantProvider;

        public AsyncBescheidRepository(string connectionString, ICurrentMandantProvider mandantProvider)
        {
            _connectionString = connectionString;
            _mandantProvider = mandantProvider;
        }

        // Ermittelt NodeType.
        public async Task<string?> DetectNodeType(Guid objectId, CancellationToken cancellationToken = default)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync(cancellationToken);

                var query = @"
                            SELECT TOP 1 NodeType
                            FROM vw_SearchIndex
                            WHERE ObjektId = @ObjektId
                              AND MandantId = @MandantId";
                using var cmd = new SqlCommand(query, connection);
                cmd.Parameters.Add(new SqlParameter("@ObjektId", SqlDbType.UniqueIdentifier)
                {
                    Value = objectId
                });
                cmd.Parameters.Add(new SqlParameter("@MandantId", SqlDbType.Int)
                {
                    Value = _mandantProvider.MandantId
                });

                var result = await cmd.ExecuteScalarAsync(cancellationToken);
                return result == null || result == DBNull.Value ? null : result.ToString();
            }
            catch (Exception ex)
            {
                throw new RepositoryException("Fehler beim Erkennen des NodeTypes.", ex);
            }
        }

        // Ermittelt, ob Ordner Kinder (Unterordner oder Dokumente) haben.
        // Gibt ein Dictionary zurück: Key = FolderId, Value = true wenn Kinder existieren.
        public async Task<Dictionary<Guid, bool>> GetFolderHasChildrenMap(List<Guid> folderIds)
        {
            try
            {
                var result = new Dictionary<Guid, bool>();

                // Wenn keine IDs übergeben wurden, wird ein leeres Ergebnis zurückgegeben.
                if (folderIds == null || folderIds.Count == 0)
                {
                    return result;
                }

                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                // Parameternamen anhand der Position erzeugen (z.B. @FolderId0, @FolderId1, ...).
                var paramNames = new List<string>();

                using var cmd = connection.CreateCommand();
                for (int i = 0; i < folderIds.Count; i++)
                {
                    var paramName = "@FolderId" + i;
                    cmd.Parameters.Add(new SqlParameter(paramName, SqlDbType.UniqueIdentifier)
                    {
                        Value = folderIds[i]
                    });
                    paramNames.Add(paramName);
                }

                cmd.Parameters.Add(new SqlParameter("@MandantId", SqlDbType.Int)
                {
                    Value = _mandantProvider.MandantId
                });

                // Abfrage prüft, ob Unterordner oder Dokumente existieren.
                // Dieselben Parameter werden in beiden IN-Klauseln wiederverwendet.
                var inClause = string.Join(",", paramNames);
                cmd.CommandText = $@"
                                  SELECT ParentObjectId AS Id
                                  FROM [Ordner]
                                  WHERE ParentObjectId IN ({inClause})
                                    AND MandantId = @MandantId
                                  UNION
                                  SELECT FolderId AS Id
                                  FROM [Dokumente]
                                  WHERE FolderId IN ({inClause})
                                    AND IsDeleted = 0
                                    AND MandantId = @MandantId";

                using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    var id = reader.GetGuid(0);
                    result[id] = true;
                }

                return result;
            }
            catch (Exception ex)
            {
                throw new RepositoryException("Fehler bei der Suche nach den Kindern.", ex);
            }
        }

        // Ermittelt, ob Objekt in der Mitzeichnung ist.
        public bool IsMitzeichnung(Guid objectId, string NodeType)
        {
            try
            {
                string query;
                switch (NodeType)
                {
                    case "Vorgang":
                        query = @"SELECT TOP 1 1 
                                FROM [AbteilungReferatMitzeichnung] arm
                                INNER JOIN [Vorgang] v ON v.ObjektId = arm.ObjektId
                                WHERE arm.ObjektId = @ObjektId
                                  AND v.MandantId = @MandantId";
                        break;

                    case "UnterOrdner":
                    case "Ordner":
                        query = @"SELECT TOP 1 1 
                                FROM [AbteilungReferatMitzeichnung] arm
                                INNER JOIN [Vorgang] v ON v.ObjektId = arm.ObjektId
                                INNER JOIN [Ordner] uo ON uo.Aktenzeichen = v.Aktenzeichen
                                WHERE uo.ObjektId = @ObjektId
                                  AND uo.MandantId = @MandantId";
                        break;

                    case "Dokument":
                        query = @"SELECT TOP 1 1 
                                FROM [AbteilungReferatMitzeichnung] arm
                                INNER JOIN [Vorgang] v ON v.ObjektId = arm.ObjektId
                                INNER JOIN [Dokumente] d ON d.Aktenzeichen = v.Aktenzeichen
                                WHERE d.ObjektId = @ObjektId
                                  AND d.MandantId = @MandantId";
                        break;

                    default:
                        return false;
                }

                using var connection = new SqlConnection(_connectionString);
                connection.Open();

                using var cmd = new SqlCommand(query, connection);
                cmd.Parameters.Add(new SqlParameter("@ObjektId", SqlDbType.UniqueIdentifier)
                {
                    Value = objectId
                });
                cmd.Parameters.Add(new SqlParameter("@MandantId", SqlDbType.Int)
                {
                    Value = _mandantProvider.MandantId
                });

                return cmd.ExecuteScalar() != null;
            }
            catch (Exception ex)
            {
                throw new RepositoryException("Fehler bei der Ermittlung des Modus.", ex);
            }
        }

    }
}