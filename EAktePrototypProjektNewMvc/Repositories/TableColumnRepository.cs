using EAktePrototypProjektNewMvc.Exceptions;
using EAktePrototypProjektNewMvc.Models;
using Microsoft.Data.SqlClient;

namespace EAktePrototypProjektNewMvc.Repositories
{
    public class TableColumnRepository
    {
        private readonly string _connectionString;

        public TableColumnRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        // Spalten für die Tabelle: global sichtbar + persönliche Einstellungen.
        public async Task<List<TableColumnDto>> GetColumnsAsync(string tableKey, string uid)
        {
            try
            {
                var merged = await LoadMergedAsync(tableKey, uid);
                return merged.Where(c => c.IsVisible).OrderBy(c => c.SortOrder).ToList();
            }
            catch (Exception ex)
            {
                throw new RepositoryException("Fehler in der GetColumnsAsync Funktion.", ex);
            }
        }

        // Für den Dialog: alle global sichtbaren Spalten, auch die vom Benutzer ausgeblendeten.
        public async Task<List<ColumnEditDto>> GetColumnsForEditAsync(string tableKey, string uid)
        {
            try
            {
                var merged = await LoadMergedAsync(tableKey, uid);
                return merged.OrderBy(c => c.SortOrder)
                    .Select(c => new ColumnEditDto { ColumnKey = c.ColumnKey, Title = c.Title, IsVisible = c.IsVisible })
                    .ToList();
            }
            catch (Exception ex)
            {
                throw new RepositoryException("Fehler in der GetColumnsForEditAsync Funktion.", ex);
            }
        }

        // Speichert die persönliche Konfiguration. Gibt false zurück, wenn die Eingabe ungültig ist.
        public async Task<bool> SaveUserColumnsAsync(string tableKey, string uid, List<UserColumnSetting> settings)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                // Nur Spalten zulassen, die global existieren und sichtbar sind.
                var allowed = (await LoadGlobalAsync(connection, tableKey)).Select(c => c.ColumnKey).ToHashSet();
                var clean = settings
                    .Where(s => s.ColumnKey != null && allowed.Contains(s.ColumnKey))
                    .GroupBy(s => s.ColumnKey).Select(g => g.First())
                    .ToList();

                if (clean.Count == 0 || !clean.Any(s => s.IsVisible))
                    return false; // mindestens eine sichtbare Spalte

                using var tx = (SqlTransaction)await connection.BeginTransactionAsync();

                using (var del = new SqlCommand("DELETE FROM [BenutzerTabellenSpalte] WHERE Uid = @Uid AND TabellenKey = @TabellenKey", connection, tx))
                {
                    del.Parameters.AddWithValue("@Uid", uid);
                    del.Parameters.AddWithValue("@TabellenKey", tableKey);
                    await del.ExecuteNonQueryAsync();
                }

                for (int i = 0; i < clean.Count; i++)
                {
                    using var ins = new SqlCommand(@"INSERT INTO [BenutzerTabellenSpalte] (Uid, TabellenKey, SpaltenKey, IstSichtbar, Reihenfolge)
                                                   VALUES (@Uid, @TabellenKey, @SpaltenKey, @IstSichtbar, @Reihenfolge)", connection, tx);

                    ins.Parameters.AddWithValue("@Uid", uid);
                    ins.Parameters.AddWithValue("@TabellenKey", tableKey);
                    ins.Parameters.AddWithValue("@SpaltenKey", clean[i].ColumnKey);
                    ins.Parameters.AddWithValue("@IstSichtbar", clean[i].IsVisible);
                    ins.Parameters.AddWithValue("@Reihenfolge", i + 1);
                    await ins.ExecuteNonQueryAsync();
                }

                await tx.CommitAsync();
                return true;
            }
            catch (Exception ex)
            {
                throw new RepositoryException("Fehler in der SaveUserColumnsAsync Funktion.", ex);
            }
        }

        // Zurücksetzen: persönliche Einträge löschen, Standard greift wieder.
        public async Task ResetUserColumnsAsync(string tableKey, string uid)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                using var cmd = new SqlCommand("DELETE FROM [BenutzerTabellenSpalte] WHERE Uid = @Uid AND TabellenKey = @TabellenKey", connection);

                cmd.Parameters.AddWithValue("@Uid", uid);
                cmd.Parameters.AddWithValue("@TabellenKey", tableKey);
                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                throw new RepositoryException("Fehler in der ResetUserColumnsAsync Funktion.", ex);
            }
        }

        // Setzt die persönliche Spaltenkonfiguration für ALLE Tabellen dieses Benutzers zurück.
        public async Task ResetAllUserColumnsAsync(string uid)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                using var cmd = new SqlCommand("DELETE FROM [BenutzerTabellenSpalte] WHERE Uid = @Uid", connection);

                cmd.Parameters.AddWithValue("@Uid", uid);
                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                throw new RepositoryException("Fehler in der ResetAllUserColumnsAsync Funktion.", ex);
            }
        }

        // Lädt die globalen Spalten und die Benutzereinstellungen und kombiniert sie.
        private async Task<List<TableColumnDto>> LoadMergedAsync(string tableKey, string uid)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var global = await LoadGlobalAsync(connection, tableKey);
            var user = await LoadUserAsync(connection, tableKey, uid);

            foreach (var c in global)
            {
                if (user.TryGetValue(c.ColumnKey, out var u))
                {
                    c.IsVisible = u.Visible;
                    c.SortOrder = u.Order;
                }
                else if (user.Count > 0)
                {
                    // Neue Spalte, die der Benutzer noch nicht kennt: ans Ende.
                    c.SortOrder += 10000;
                }
            }
            return global;
        }

        // Nur global sichtbare Spalten (globales Ausblenden ist eine harte Grenze).
        private static async Task<List<TableColumnDto>> LoadGlobalAsync(SqlConnection connection, string tableKey)
        {
            var list = new List<TableColumnDto>();
            var query = @"
                        SELECT SpaltenKey, Titel, Reihenfolge, IstSichtbar, IstSortierbar, IstSuchbar
                        FROM [TabellenSpalte]
                        WHERE TabellenKey = @TabellenKey AND IstSichtbar = 1
                        ORDER BY Reihenfolge";

            using var cmd = new SqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@TabellenKey", tableKey);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new TableColumnDto
                {
                    ColumnKey = reader["SpaltenKey"] as string,
                    Title = reader["Titel"] as string,
                    SortOrder = (int)reader["Reihenfolge"],
                    IsVisible = (bool)reader["IstSichtbar"],
                    IsSortable = (bool)reader["IstSortierbar"],
                    IsSearchable = (bool)reader["IstSuchbar"]
                });
            }
            return list;
        }

        // Benutzerspezifische Einstellungen: Sichtbarkeit und Reihenfolge.
        private static async Task<Dictionary<string, (bool Visible, int Order)>> LoadUserAsync(SqlConnection connection, string tableKey, string uid)
        {
            var dict = new Dictionary<string, (bool, int)>();
            var query = @"
                        SELECT SpaltenKey, IstSichtbar, Reihenfolge
                        FROM [BenutzerTabellenSpalte]
                        WHERE Uid = @Uid AND TabellenKey = @TabellenKey";

            using var cmd = new SqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@Uid", uid);
            cmd.Parameters.AddWithValue("@TabellenKey", tableKey);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                dict[(string)reader["SpaltenKey"]] = ((bool)reader["IstSichtbar"], (int)reader["Reihenfolge"]);
            }
            return dict;
        }
    }
}