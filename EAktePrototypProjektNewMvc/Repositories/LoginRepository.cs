using EAktePrototypProjektNewMvc.Exceptions;
using EAktePrototypProjektNewMvc.Models.Persone;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Security.Cryptography;
using System.Text;

namespace EAktePrototypProjektNewMvc.Repositories
{
    public class LoginRepository
    {
        private readonly string _connectionString;

        public LoginRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        // Versucht, einen Benutzer mit der angegebenen E-Mail und dem Passwort anzumelden.
        public (bool Success, string Message, int LockoutSeconds) TryLogin(string email, string kennwort)
        {
            try
            {
                if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(kennwort))
                {
                    return (false, "Email oder Passwort darf nicht leer sein.", 0);
                }

                using var connection = new SqlConnection(_connectionString);
                connection.Open();

                // Aktuellen Zustand des Benutzers als Personen-Objekt laden.
                Personen? person = GetLoginDataByEmail(email, connection);

                if (person == null)
                {
                    return (false, "Ein Konto mit dieser E-Mail-Adresse existiert nicht.", 0);
                }

                // Wenn die Sperrzeit bereits abgelaufen ist, wird nur der Versuchszähler zurückgesetzt.
                // Die Sperrdauer (LockoutMinutes) wird hier um 2 Minuten erhöht, für die nächste, potenziell erneute Sperre.
                // So wird die nächste Sperre länger, falls der Benutzer weiterhin falsche Passwörter eingibt.
                if (person.FailedLoginAttempts >= 3 && person.LastFailedAttempt.HasValue && person.LastFailedAttempt.Value.AddMinutes(person.LockoutMinutes) <= DateTime.UtcNow)
                {
                    int nextLockoutMinutes = person.LockoutMinutes + 2;
                    ResetFailedAttemptsAndIncreaseLockout(email, nextLockoutMinutes, connection);
                    person.FailedLoginAttempts = 0;
                    person.LastFailedAttempt = null;
                    person.LockoutMinutes = nextLockoutMinutes;
                }

                // Wenn das Konto noch gesperrt ist, wird der Login sofort abgelehnt, ohne das Passwort zu prüfen, und die verbleibende Sperrzeit zurückgegeben.
                if (IsCurrentlyLocked(person))
                {
                    var remaining = person.LastFailedAttempt!.Value.AddMinutes(person.LockoutMinutes) - DateTime.UtcNow;
                    int totalSeconds = Math.Max(0, (int)remaining.TotalSeconds);

                    return (false, "Ihr Konto ist vorübergehend gesperrt.", totalSeconds);
                }

                // Passwort prüfen: entweder das Hauptpasswort oder das temporäre Passwort (falls dessen Gültigkeit noch nicht abgelaufen ist).
                string hashedPassword = HashPassword(kennwort);
                bool passwordValid = person.Kennwort == hashedPassword
                    || (person.TKennwort == hashedPassword && person.TempPasswordExpiry.HasValue && person.TempPasswordExpiry.Value > DateTime.UtcNow);

                if (passwordValid)
                {
                    // Erfolgreicher Login: alles zurücksetzen, inklusive der Sperrdauer, die wieder auf die Basiszeit von 2 Minuten gesetzt wird.
                    ResetLoginAttempts(email, connection);
                    return (true, "Anmeldung erfolgreich.", 0);
                }

                // Falsches Passwort: Zähler für fehlgeschlagene Versuche erhöhen.
                person.FailedLoginAttempts++;
                int remainingAttempts = Math.Max(0, 3 - person.FailedLoginAttempts);

                if (remainingAttempts == 0)
                {
                    // Dritter fehlgeschlagener Versuch in Folge - Konto wird gesperrt.
                    // Die Sperrdauer wurde bereits beim Ablauf der vorherigen Sperre erhöht (siehe oben).
                    UpdateLoginAttempts(email, person.FailedLoginAttempts, person.LockoutMinutes, connection);

                    return (false, $"Drei falsche Versuche. Das Konto wurde für {person.LockoutMinutes} Minuten gesperrt.", person.LockoutMinutes * 60);
                }

                // Noch keine Sperre erreicht, nur den aktualisierten Versuchszähler speichern.
                UpdateLoginAttempts(email, person.FailedLoginAttempts, person.LockoutMinutes, connection);

                return (false, $"Falsches Passwort. Verbleibende Versuche: {remainingAttempts}.", 0);
            }
            catch (Exception ex)
            {
                throw new RepositoryException("Unbekannter Fehler beim Laden.", ex);
            }
        }

        // Prüft anhand des Personen-Objekts, ob das Konto aktuell gesperrt ist.
        private bool IsCurrentlyLocked(Personen person)
        {
            return person.FailedLoginAttempts >= 3
                && person.LastFailedAttempt.HasValue
                && person.LastFailedAttempt.Value.AddMinutes(person.LockoutMinutes) > DateTime.UtcNow;
        }

        // Lädt die für den Login relevanten Daten eines Benutzers als Personen-Objekt.
        private Personen? GetLoginDataByEmail(string email, SqlConnection connection)
        {
            var query = @"
                        SELECT FailedLoginAttempts, LastFailedAttempt, Kennwort, TKennwort, TempPasswordExpiry, LockoutMinutes
                        FROM [Personen]
                        WHERE Email = @Email";

            using var cmd = new SqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@Email", email);
            using var reader = cmd.ExecuteReader();

            if (!reader.Read())
            {
                return null;
            }

            return new Personen
            {
                Email = email,
                FailedLoginAttempts = reader["FailedLoginAttempts"] != DBNull.Value ? Convert.ToInt32(reader["FailedLoginAttempts"]) : 0,
                LastFailedAttempt = reader["LastFailedAttempt"] != DBNull.Value ? Convert.ToDateTime(reader["LastFailedAttempt"]) : (DateTime?)null,
                Kennwort = reader["Kennwort"] as string,
                TKennwort = reader["TKennwort"] as string,
                TempPasswordExpiry = reader["TempPasswordExpiry"] != DBNull.Value ? Convert.ToDateTime(reader["TempPasswordExpiry"]) : (DateTime?)null,
                LockoutMinutes = reader["LockoutMinutes"] != DBNull.Value ? Convert.ToInt32(reader["LockoutMinutes"]) : 2,
            };
        }

        // Speichert die neue Anzahl fehlgeschlagener Versuche, den Zeitpunkt desletzten Versuchs und die aktuelle Sperrdauer (in Minuten) für den Benutzer.
        private void UpdateLoginAttempts(string email, int failedAttempts, int lockoutMinutes, SqlConnection connection)
        {
            var query = @"
                        UPDATE [Personen]
                        SET FailedLoginAttempts = @Count, LastFailedAttempt = GETUTCDATE(), LockoutMinutes = @LockoutMinutes
                        WHERE Email = @Email";
            using var cmd = new SqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@Count", failedAttempts);
            cmd.Parameters.AddWithValue("@LockoutMinutes", lockoutMinutes);
            cmd.Parameters.AddWithValue("@Email", email);
            cmd.ExecuteNonQuery();
        }

        // Wird nur dann verwendet, wenn die Sperrzeit bereits abgelaufen ist. Setzt den Versuchszähler und den Zeitpunkt des letzten Versuchs zurück und erhöht die Sperrdauer für die nächste, potenziell erneute Sperre.
        private void ResetFailedAttemptsAndIncreaseLockout(string email, int lockoutMinutes, SqlConnection connection)
        {
            var query = @"
                        UPDATE [Personen]
                        SET FailedLoginAttempts = 0, LastFailedAttempt = NULL, LockoutMinutes = @LockoutMinutes
                        WHERE Email = @Email";
            using var cmd = new SqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@LockoutMinutes", lockoutMinutes);
            cmd.Parameters.AddWithValue("@Email", email);
            cmd.ExecuteNonQuery();
        }

        // Wird nur bei einem ERFOLGREICHEN Login verwendet. Setzt wirklich alles zurück: Versuchszähler, Zeitpunkt des letzten Versuchs und die Sperrdauer, die wieder auf die Basiszeit von 2 Minuten gesetzt wird.
        private void ResetLoginAttempts(string email, SqlConnection connection)
        {
            var query = @"
                        UPDATE [Personen]
                        SET FailedLoginAttempts = 0, LastFailedAttempt = NULL, LockoutMinutes = 2
                        WHERE Email = @Email";
            using var cmd = new SqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@Email", email);
            cmd.ExecuteNonQuery();
        }

        // Hashes the password using SHA256.
        private string HashPassword(string password)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = Encoding.UTF8.GetBytes(password);
                byte[] hash = sha256.ComputeHash(bytes);
                return BitConverter.ToString(hash).Replace("-", "").ToLower();
            }
        }

        // Gibt einen Benutzer anhand seiner E-Mail-Adresse zurück.
        public Personen? GetUserByEmail(string email)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                connection.Open();

                // Abteilung und Referat kommen aus PersonRollen, da eine Rolle mehrere Abteilung/Referat-Paare haben kann.
                var query = @"
                            SELECT 
                                p.Id, p.Titel, p.Name, p.Vorname, p.Email, p.UId, p.Stelle, p.Kennwort, p.MandantId,
                                pr.Rolle, pr.GueltigAb, pr.GueltigBis, pr.Abteilung, pr.Referat
                            FROM [Personen] p
                            LEFT JOIN [PersonRollen] pr ON pr.PersonId = p.Id
                                AND pr.GueltigAb <= GETDATE()
                                AND (pr.GueltigBis IS NULL OR pr.GueltigBis >= GETDATE())
                            WHERE p.Email = @Email";

                using var cmd = new SqlCommand(query, connection);
                cmd.Parameters.Add("@Email", SqlDbType.NVarChar, 500).Value = email;
                using var reader = cmd.ExecuteReader();

                Personen? person = null;

                while (reader.Read())
                {
                    // Grunddaten der Person nur beim ersten Datensatz setzen.
                    if (person == null)
                    {
                        person = new Personen
                        {
                            Id = (int)reader["Id"],
                            Titel = reader["Titel"] as string,
                            Name = reader["Name"] as string,
                            Vorname = reader["Vorname"] as string,
                            Email = reader["Email"] as string,
                            UId = reader["UId"] as string,
                            Stelle = reader["Stelle"] as string,
                            Kennwort = reader["Kennwort"] as string,
                            MandantId = (int)reader["MandantId"]
                        };
                    }

                    string? rolle = reader["Rolle"] as string;
                    string abteilung = reader["Abteilung"] as string ?? "";
                    string referat = reader["Referat"] as string ?? "";

                    // Rolle hinzufügen, aber nur wenn sie noch nicht in der Liste vorhanden ist (verhindert doppelte Rollen).
                    if (!string.IsNullOrEmpty(rolle) && !person.Rollen.Any(r => r.Rolle == rolle))
                    {
                        person.Rollen.Add(new PersonRolle
                        {
                            Rolle = rolle,
                            Abteilung = reader["Abteilung"] as string,
                            Referat = reader["Referat"] as string,
                            GueltigAb = reader["GueltigAb"] as DateTime?,
                            GueltigBis = reader["GueltigBis"] as DateTime?,
                        });
                    }

                    // Abteilung/Referat-Paar hinzufügen, aber nur wenn dieses Paar noch nicht vorhanden ist.
                    if ((!string.IsNullOrWhiteSpace(abteilung) || !string.IsNullOrWhiteSpace(referat)) && !person.AbteilungReferate.Any(x => x.Abteilung == abteilung && x.Referat == referat))
                    {
                        person.AbteilungReferate.Add((abteilung, referat));
                    }
                }

                return person;
            }
            catch (Exception ex)
            {
                throw new RepositoryException("Fehler in der GetUserByEmail Funktion.", ex);
            }
        }

    }
}