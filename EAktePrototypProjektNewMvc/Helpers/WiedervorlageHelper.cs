using System;
using System.Net;
using System.Text;

namespace EAktePrototypProjektNewMvc.Helpers
{
    public static class WiedervorlageHelper
    {
        /// <summary>
        /// Erstellt eine ICS-Datei für eine Wiedervorlage.
        /// "remark" ist die reine Bemerkung des Nutzers (ohne Link) —
        /// der Link auf das Objekt wird separat als eigene, klar beschriftete Zeile angehängt.
        /// </summary>
        public static string BuildIcs(string summary, string remark, string url, DateTime start, DateTime end)
        {
            var plainDescription = BuildPlainDescription(remark, url);
            var htmlDescription = BuildHtmlDescription(remark, url);
            var stamp = DateTime.UtcNow.ToString("yyyyMMdd'T'HHmmss'Z'");

            var sb = new StringBuilder();

            sb.Append("BEGIN:VCALENDAR\r\n");
            sb.Append("VERSION:2.0\r\n");
            sb.Append("PRODID:-//MeinProjekt//Wiedervorlage//DE\r\n");
            sb.Append("CALSCALE:GREGORIAN\r\n");
            sb.Append("METHOD:PUBLISH\r\n");

            AppendTimezone(sb);

            sb.Append("BEGIN:VEVENT\r\n");
            sb.Append($"UID:{Guid.NewGuid()}@meinprojekt\r\n");
            sb.Append($"DTSTAMP:{stamp}\r\n");
            sb.Append(Fold($"DTSTART;TZID=W. Europe Standard Time:{start:yyyyMMdd'T'HHmmss}"));
            sb.Append(Fold($"DTEND;TZID=W. Europe Standard Time:{end:yyyyMMdd'T'HHmmss}"));
            sb.Append(Fold("SUMMARY:" + Esc(summary)));
            sb.Append(Fold("DESCRIPTION:" + Esc(plainDescription)));
            sb.Append(Fold("X-ALT-DESC;FMTTYPE=text/html:" + Esc(htmlDescription)));
            sb.Append("CATEGORIES:Wiedervorlage\r\n");
            sb.Append("PRIORITY:5\r\n");
            sb.Append("TRANSP:OPAQUE\r\n");
            sb.Append("X-MICROSOFT-CDO-BUSYSTATUS:FREE\r\n");

            // Erinnerung 15 Minuten vorher.
            sb.Append("BEGIN:VALARM\r\nTRIGGER:-PT15M\r\nACTION:DISPLAY\r\n");
            sb.Append("DESCRIPTION:Erinnerung\r\nEND:VALARM\r\n");

            sb.Append("END:VEVENT\r\n");
            sb.Append("END:VCALENDAR\r\n");
            return sb.ToString();
        }

        // Klartext-Variante für Clients ohne HTML-Unterstützung (DESCRIPTION).
        // Aufbau:
        //   <Bemerkung, falls vorhanden>
        //
        //   Objekt-Link: <url>
        private static string BuildPlainDescription(string remark, string url)
        {
            var sb = new StringBuilder();

            if (!string.IsNullOrWhiteSpace(remark))
            {
                sb.Append(remark.Trim());
                sb.Append("\n\n");
            }

            sb.Append("Link auf das Objekt: ").Append(url);
            return sb.ToString();
        }

        // HTML-Variante für Outlook (X-ALT-DESC) — Bemerkung und Link optisch getrennt,
        // Link als klickbarer Button/Link statt als reiner Text.
        private static string BuildHtmlDescription(string remark, string url)
        {
            var sb = new StringBuilder();
            sb.Append("<html><body>");

            if (!string.IsNullOrWhiteSpace(remark))
            {
                sb.Append("<p><b>Bemerkung:</b><br/>");
                sb.Append(WebUtility.HtmlEncode(remark.Trim()).Replace("\n", "<br/>"));
                sb.Append("</p>");
            }

            sb.Append("<p><b>Link auf das Objekt:</b><br/>");
            sb.Append($"<a href=\"{url}\">{WebUtility.HtmlEncode(url)}</a>");
            sb.Append("</p>");

            sb.Append("</body></html>");
            return sb.ToString();
        }

        private static void AppendTimezone(StringBuilder sb)
        {
            // VTIMEZONE Europe/Berlin – damit Outlook die Ortszeit korrekt interpretiert.
            sb.Append("BEGIN:VTIMEZONE\r\n");
            sb.Append("TZID:W. Europe Standard Time\r\n");
            sb.Append("BEGIN:STANDARD\r\n");
            sb.Append("DTSTART:16011028T030000\r\n");
            sb.Append("TZOFFSETFROM:+0200\r\nTZOFFSETTO:+0100\r\n");
            sb.Append("RRULE:FREQ=YEARLY;BYDAY=-1SU;BYMONTH=10\r\n");
            sb.Append("END:STANDARD\r\n");
            sb.Append("BEGIN:DAYLIGHT\r\n");
            sb.Append("DTSTART:16010325T020000\r\n");
            sb.Append("TZOFFSETFROM:+0100\r\nTZOFFSETTO:+0200\r\n");
            sb.Append("RRULE:FREQ=YEARLY;BYDAY=-1SU;BYMONTH=3\r\n");
            sb.Append("END:DAYLIGHT\r\n");
            sb.Append("END:VTIMEZONE\r\n");
        }

        // Escaping nach RFC 5545.
        private static string Esc(string s) =>
            (s ?? "").Replace("\\", "\\\\")
                     .Replace(";", "\\;")
                     .Replace(",", "\\,")
                     .Replace("\r\n", "\\n")
                     .Replace("\n", "\\n");

        // Zeilen auf 75 Oktette falten.
        private static string Fold(string line)
        {
            const int max = 73;
            var sb = new StringBuilder();
            while (Encoding.UTF8.GetByteCount(line) > max)
            {
                int len = max;
                while (Encoding.UTF8.GetByteCount(line.Substring(0, len)) > max) len--;
                sb.Append(line.Substring(0, len)).Append("\r\n ");
                line = line.Substring(len);
            }
            sb.Append(line).Append("\r\n");
            return sb.ToString();
        }
    }
}