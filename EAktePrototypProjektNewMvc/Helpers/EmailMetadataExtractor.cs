using MimeKit;
using MsgReader.Outlook;

namespace EAktePrototypProjektNewMvc.Helpers
{

    public static class EmailMetadataExtractor
    {
        public record EmailMeta(string? Subject, string? Absender, string? Empfaenger);

        // Diese Methode versucht, die Metadaten einer E-Mail-Datei zu extrahieren. Sie gibt null zurück, wenn die Extraktion fehlschlägt oder der Dateityp nicht unterstützt wird.
        public static EmailMeta? TryExtract(byte[] fileBytes, string fileExtension)
        {
            var ext = fileExtension.ToLowerInvariant();

            try
            {
                if (ext == ".eml")
                {
                    return ExtractFromEml(fileBytes);
                }

                if (ext == ".msg")
                {
                    return ExtractFromMsg(fileBytes);
                }
            }
            catch
            {

                return null;
            }

            return null;
        }

        // Extrahiert Metadaten aus einer EML-Datei.
        private static EmailMeta ExtractFromEml(byte[] fileBytes)
        {
            using var ms = new MemoryStream(fileBytes);
            var message = MimeMessage.Load(ms);

            var absender = message.From?.ToString();
            var empfaenger = message.To?.Any() == true
                ? string.Join("; ", message.To.Select(a => a.ToString()))
                : null;

            return new EmailMeta(message.Subject, absender, empfaenger);
        }

        // Extrahiert Metadaten aus einer MSG-Datei.
        private static EmailMeta ExtractFromMsg(byte[] fileBytes)
        {
            var tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".msg");
            try
            {
                File.WriteAllBytes(tempPath, fileBytes);
                using var msg = new Storage.Message(tempPath);

                var absender = !string.IsNullOrWhiteSpace(msg.Sender?.Email)
                    ? $"{msg.Sender.DisplayName} <{msg.Sender.Email}>"
                    : msg.Sender?.DisplayName;

                var empfaenger = msg.Recipients?.Count > 0
                    ? string.Join("; ", msg.Recipients.Select(r =>
                        !string.IsNullOrWhiteSpace(r.Email) ? $"{r.DisplayName} <{r.Email}>" : r.DisplayName))
                    : null;

                return new EmailMeta(msg.Subject, absender, empfaenger);
            }
            finally
            {
                if (File.Exists(tempPath)) File.Delete(tempPath);
            }
        }
    }
}
