using EAktePrototypProjektNewMvc.Models.Dokumente.Sessions;
using EAktePrototypProjektNewMvc.Repositories;

namespace EAktePrototypProjektNewMvc.Middleware
{
    public class WebDavEditMiddleware
    {
        private const string BasePath = "/webdav/dokumente";

        private readonly RequestDelegate _next;
        public WebDavEditMiddleware(RequestDelegate next) => _next = next;

        public async Task InvokeAsync(HttpContext context, EditSessionRepository editSessionRepo, eAkteRepository eAkteRepo, ConfigRepository configRepo)
        {
            // Nur für WebDAV-Requests auf /webdav/dokumente/* zuständig, alles andere weiterreichen.
            if (!context.Request.Path.StartsWithSegments(BasePath))
            {
                await _next(context); // start.
                return;
            }


            // Debug.
            Console.WriteLine($"[WebDAV] {context.Request.Method} {context.Request.Path}{context.Request.QueryString}");

            // OPTIONS-Requests werden von Windows/Word als "Probe" geschickt, um die Fähigkeiten des Servers zu ermitteln.
            if (context.Request.Method.Equals("OPTIONS", StringComparison.OrdinalIgnoreCase))
            {
                context.Response.Headers["DAV"] = "1,2"; // WebDAV Level 1 und 2 unterstützen. 1= PROPFIND, GET, PUT und so weiter, 2=LOCK/UNLOCK.
                context.Response.Headers["Allow"] = "OPTIONS, GET, HEAD, PUT, PROPFIND, LOCK, UNLOCK"; // Unterstützte Methoden angeben.
                context.Response.Headers["MS-Author-Via"] = "DAV"; // Microsoft-spezifischer Header, der angibt, dass WebDAV für die Bearbeitung verwendet wird.
                context.Response.StatusCode = 200; // OK.
                return;
            }



            // Segmente des Pfads analysieren, um zu bestimmen, ob es sich um eine Collection oder eine Datei handelt.

            // Den Pfad nach dem Basis-Pfad aufteilen, um die einzelnen Segmente zu analysieren.
            var remainder = context.Request.Path.Value!.Length > BasePath.Length ? context.Request.Path.Value!.Substring(BasePath.Length) : string.Empty;
            // Trimmen von führenden und nachgestellten Slashes und Aufteilen in Segmente.
            var segments = remainder.Trim('/').Length == 0 ? Array.Empty<string>() : remainder.Trim('/').Split('/');




            // Die Anzahl der Segmente bestimmt, ob es sich um eine Collection oder eine Datei handelt: segment 0 = Collection, segment 1 = Token-Ordner, segment 2 = Datei. 
            // Gultig ist nur "2". 0 und 1 müssen nur überprüft werden, um die Collection-Ebene zu bedienen. Alles andere ist 404. 
            switch (segments.Length)
            {

                // Fall 1: Request auf die Collection selbst - "/webdav/dokumente"
                // Word/Windows fragt das ab, um die Collection als gültiges WebDAV-Ziel zu erkennen.
                case 0:
                    await HandleCollectionLevel(context, "dokumente");
                    return;

                // Fall 2: Request auf den Token-"Ordner" - "/webdav/dokumente/{token}"
                // Wird von Windows als Zwischen-Collection abgefragt, bevor es auf die Datei zugreift.
                case 1:
                    {
                        // Konfiguration laden, um die Lock-Dauer zu bestimmen.
                        var config = configRepo.LoadConfig();
                        var lockDuration = TimeSpan.FromHours(config.DokumentEditLockStunden ?? 24);

                        if (!Guid.TryParse(segments[0], out var folderToken))
                        {
                            context.Response.StatusCode = 404; // Not Found.
                            return;
                        }
                        var folderSession = editSessionRepo.GetByToken(folderToken);
                        if (folderSession == null || folderSession.ExpiresAt < DateTime.Now)
                        {
                            context.Response.StatusCode = 401; // Unauthorized.
                            return;
                        }
                        editSessionRepo.RefreshTtl(folderToken, lockDuration);
                        await HandleCollectionLevel(context, folderToken.ToString());
                        return;
                    }

                // Fall 3: Request auf die eigentliche Datei - "/webdav/dokumente/{token}/{fileName}"
                case 2:
                    {
                        // Konfiguration laden, um die Lock-Dauer zu bestimmen.
                        var config = configRepo.LoadConfig();
                        var lockDuration = TimeSpan.FromHours(config.DokumentEditLockStunden ?? 24);

                        if (!Guid.TryParse(segments[0], out var token))
                        {
                            context.Response.StatusCode = 404;
                            return;
                        }

                        var session = editSessionRepo.GetByToken(token);
                        if (session == null || session.ExpiresAt < DateTime.Now)
                        {
                            context.Response.StatusCode = 401;
                            await context.Response.WriteAsync("Token abgelaufen oder ungültig.");
                            return;
                        }

                        // Session verlängern, damit sie nicht abläuft, solange der Benutzer aktiv ist. + 6 Stunden.
                        editSessionRepo.RefreshTtl(token, lockDuration);

                        // Word schickt uns HTTP Requests.
                        switch (context.Request.Method.ToUpperInvariant())
                        {
                            case "PROPFIND": // XML Request, um die Eigenschaften der Datei abzufragen. 
                                await HandlePropfind(context, session, eAkteRepo);
                                break;

                            case "HEAD": // Lange und Typ ohne Body.
                                await HandleHead(context, session, eAkteRepo);
                                break;

                            case "GET": // Bytes.
                                await HandleGet(context, session, eAkteRepo);
                                break;

                            case "PUT": // Body lesen und in den Staging-Bereich der Session schreiben.
                                await HandlePut(context, session, editSessionRepo);
                                break;

                            case "LOCK": // Datei sperren.
                                await HandleLock(context, session, editSessionRepo);
                                break;

                            case "UNLOCK": // Datei entsperren.
                                editSessionRepo.SetLockToken(session.TokenId, null);
                                context.Response.StatusCode = 204;
                                break;

                            default:
                                context.Response.StatusCode = StatusCodes.Status405MethodNotAllowed;
                                break;
                        }
                        return;
                    }

                // Alles Tiefere kennt der Schema nicht.
                default:
                    context.Response.StatusCode = 404;
                    return;
            }
        }



        // HandleCollectionLevel behandelt die WebDAV-Methoden auf Collection-Ebene ("/webdav/dokumente" oder "/webdav/dokumente/{token}") o oder 1 (überprüfen um tiefer zu gehen).
        private async Task HandleCollectionLevel(HttpContext context, string displayName)
        {
            switch (context.Request.Method.ToUpperInvariant())
            {
                // Eigenschaften.
                case "PROPFIND":
                    {
                        var href = GetFullHref(context, context.Request.Path.Value!.TrimEnd('/') + "/");
                        var xml = $@"<?xml version=""1.0"" encoding=""utf-8""?>
                                    <D:multistatus xmlns:D=""DAV:"">
                                      <D:response>
                                        <D:href>{System.Net.WebUtility.HtmlEncode(href)}</D:href>
                                        <D:propstat>
                                          <D:prop>
                                            <D:resourcetype><D:collection/></D:resourcetype>
                                            <D:displayname>{System.Net.WebUtility.HtmlEncode(displayName)}</D:displayname>
                                            <D:supportedlock>
                                              <D:lockentry>
                                                <D:lockscope><D:exclusive/></D:lockscope>
                                                <D:locktype><D:write/></D:locktype>
                                              </D:lockentry>
                                            </D:supportedlock>
                                          </D:prop>
                                          <D:status>HTTP/1.1 200 OK</D:status>
                                        </D:propstat>
                                      </D:response>
                                    </D:multistatus>";

                        context.Response.StatusCode = 207;
                        context.Response.ContentType = "application/xml; charset=utf-8";
                        await context.Response.WriteAsync(xml); // Antwort schreiben (an Word).
                        break;
                    }

                case "HEAD":
                case "GET":
                    context.Response.StatusCode = 200; // OK, aber kein Body.
                    break;

                default:
                    context.Response.StatusCode = StatusCodes.Status405MethodNotAllowed;
                    break;
            }
        }



        // Die folgenden Methoden behandeln die verschiedenen WebDAV-Methoden auf Datei-Ebene (2).
        private async Task HandlePropfind(HttpContext context, DokumentEditSession session, eAkteRepository eAkteRepo)
        {
            var length = GetCurrentBytes(session, eAkteRepo).Length;
            var modified = (session.LastUsedAt ?? session.IssuedAt).ToUniversalTime().ToString("R");

            var href = GetFullHref(context);

            // Wenn die Datei gesperrt ist, wird der Lock-Owner und das Lock-Token in der Antwort zurückgegeben.
            var lockOwner = session.LockToken != null
                ? $@"<D:activelock>
                        <D:locktype><D:write/></D:locktype>
                        <D:lockscope><D:exclusive/></D:lockscope>
                        <D:depth>0</D:depth>
                        <D:owner>{System.Net.WebUtility.HtmlEncode(session.UserId)}</D:owner>
                        <D:timeout>Second-3600</D:timeout>
                        <D:locktoken><D:href>{session.LockToken}</D:href></D:locktoken>
                    </D:activelock>"
                : "";

            var xml = $@"<?xml version=""1.0"" encoding=""utf-8""?>
                        <D:multistatus xmlns:D=""DAV:"">
                          <D:response>
                            <D:href>{System.Net.WebUtility.HtmlEncode(href)}</D:href>
                            <D:propstat>
                              <D:prop>
                                <D:displayname>{System.Net.WebUtility.HtmlEncode(session.FileName)}</D:displayname>
                                <D:getcontentlength>{length}</D:getcontentlength>
                                <D:getcontenttype>{session.ContentType}</D:getcontenttype>
                                <D:getlastmodified>{modified}</D:getlastmodified>
                                <D:resourcetype/>
                                <D:supportedlock>
                                  <D:lockentry>
                                    <D:lockscope><D:exclusive/></D:lockscope>
                                    <D:locktype><D:write/></D:locktype>
                                  </D:lockentry>
                                </D:supportedlock>
                                <D:lockdiscovery>{lockOwner}</D:lockdiscovery>
                              </D:prop>
                              <D:status>HTTP/1.1 200 OK</D:status>
                            </D:propstat>
                          </D:response>
                        </D:multistatus>";

            context.Response.StatusCode = 207;
            context.Response.ContentType = "application/xml; charset=utf-8";
            await context.Response.WriteAsync(xml);
        }



        // Die HandleHead-Methode liest die aktuellen Bytes der Datei (entweder aus dem Staging-Bereich oder aus der Originalversion) und setzt die Content-Length im Response-Header, ohne den Body zu schreiben.
        private async Task HandleHead(HttpContext context, DokumentEditSession session, eAkteRepository eAkteRepo)
        {
            var bytes = GetCurrentBytes(session, eAkteRepo);
            context.Response.ContentType = session.ContentType ?? "application/octet-stream";
            context.Response.ContentLength = bytes.Length;
            context.Response.StatusCode = 200;
            await Task.CompletedTask;
        }



        // Die HandleGet-Methode liest die aktuellen Bytes der Datei (entweder aus dem Staging-Bereich oder aus der Originalversion) und schreibt sie in den Response-Body.
        private async Task HandleGet(HttpContext context, DokumentEditSession session, eAkteRepository eAkteRepo)
        {
            var bytes = GetCurrentBytes(session, eAkteRepo);
            context.Response.ContentType = session.ContentType ?? "application/octet-stream";
            context.Response.ContentLength = bytes.Length;
            await context.Response.Body.WriteAsync(bytes);
        }



        // Die HandlePut-Methode liest den Request-Body in einen MemoryStream und speichert die Bytes im Staging-Bereich der Session. Der Statuscode 204 (No Content) wird zurückgegeben, um anzuzeigen, dass die Operation erfolgreich war, aber keine Inhalte zurückgegeben werden.
        private async Task HandlePut(HttpContext context, DokumentEditSession session, EditSessionRepository repo)
        {

            if (session.IsViewOnly)
            {
                context.Response.StatusCode = 403; // Forbidden - Speichern nicht erlaubt.
                return;
            }

            using var ms = new MemoryStream();
            await context.Request.Body.CopyToAsync(ms);
            repo.UpdateStagedContent(session.TokenId, ms.ToArray());
            context.Response.StatusCode = 204;
        }



        // Die HandleLock-Methode erstellt ein neues Lock-Token, speichert es in der Session und gibt die Lock-Informationen im Response-Body zurück.
        private async Task HandleLock(HttpContext context, DokumentEditSession session, EditSessionRepository repo)
        {

            if (session.IsViewOnly)
            {
                // Word bekommt hier explizit "kein Lock möglich" - zeigt "SCHREIBGESCHÜTZT" an.
                context.Response.StatusCode = 423; // Locked.
                return;
            }

            var lockToken = $"urn:uuid:{Guid.NewGuid()}";
            repo.SetLockToken(session.TokenId, lockToken);

            context.Response.Headers["Lock-Token"] = $"<{lockToken}>";
            context.Response.ContentType = "application/xml; charset=utf-8";

            var xml = $@"<?xml version=""1.0"" encoding=""utf-8""?>
                            <D:prop xmlns:D=""DAV:"">
                              <D:lockdiscovery>
                                <D:activelock>
                                  <D:locktype><D:write/></D:locktype>
                                  <D:lockscope><D:exclusive/></D:lockscope>
                                  <D:depth>0</D:depth>
                                  <D:owner>{System.Net.WebUtility.HtmlEncode(session.UserId)}</D:owner>
                                  <D:timeout>Second-3600</D:timeout>
                                  <D:locktoken><D:href>{lockToken}</D:href></D:locktoken>
                                </D:activelock>
                              </D:lockdiscovery>
                            </D:prop>";
            context.Response.StatusCode = 200;
            await context.Response.WriteAsync(xml);
        }



        // Hilfsmethode, um die aktuellen Bytes der Datei zu erhalten, entweder aus dem Staging-Bereich oder aus der Originalversion.
        private byte[] GetCurrentBytes(DokumentEditSession session, eAkteRepository eAkteRepo)
        {
            if (session.StagedFileStream != null) return session.StagedFileStream;
            var original = eAkteRepo.GetVersionByVersionId(session.OriginalVersionId);
            return original?.FileStream ?? Array.Empty<byte>();
        }

        // Hilfsmethode: baut aus dem aktuellen Request die vollständige, korrekte URL,
        // inklusive PathBase (z.B. "/testEakte"), damit Word den Ressourcen-Pfad
        // exakt so wiedererkennt, wie er ihn ursprünglich aufgerufen hat.
        private static string GetFullHref(HttpContext context, string? overridePath = null)
        {
            var path = overridePath ?? context.Request.Path.Value!;
            return $"{context.Request.Scheme}://{context.Request.Host}{context.Request.PathBase}{path}";
        }
    }
}



