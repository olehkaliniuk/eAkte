using EAktePrototypProjektNewMvc.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace EAktePrototypProjektNewMvc.Helpers
{

    public enum TokenSessionSource
    {
        DokumentEdit,
        PdfConverter
    }

    public class TokenAuthorizeAttribute : ActionFilterAttribute
    {
        private readonly TokenSessionSource _source;

        public TokenAuthorizeAttribute(TokenSessionSource source = TokenSessionSource.DokumentEdit)
        {
            _source = source;
        }

        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            Guid token = Guid.Empty;
            bool found = false;



            // Zuerst suchen wir nach einem expliziten Parameter "token" in den Action.
            foreach (var kvp in context.ActionArguments)
            {
                if (kvp.Value is Guid g && kvp.Key.Equals("token", StringComparison.OrdinalIgnoreCase))
                {
                    token = g;
                    found = true;
                    break;
                }
            }

            // Wenn kein expliziter Parameter gefunden wurde, suchen wir nach einem "Token"-Property in den Action-Argumenten.
            if (!found)
            {
                foreach (var arg in context.ActionArguments.Values)
                {
                    if (arg != null && arg is not Guid)
                    {
                        var tokenProp = arg.GetType().GetProperty("Token");
                        if (tokenProp != null && tokenProp.PropertyType == typeof(Guid)
                            && tokenProp.GetValue(arg) is Guid tokenFromBody)
                        {
                            token = tokenFromBody;
                            found = true;
                            break;
                        }
                    }
                }
            }

            // Wenn immer noch kein Token gefunden wurde, suchen wir in den Formulardaten und der Query-String.
            if (!found)
            {
                var http = context.HttpContext;
                Console.WriteLine($"[TokenAuthorize] Method={http.Request.Method}, ContentType={http.Request.ContentType}, ContentLength={http.Request.ContentLength}, Path={http.Request.Path}");
                Console.WriteLine($"[TokenAuthorize] ActionDescriptor.Parameters: {string.Join(", ", context.ActionDescriptor.Parameters.Select(p => p.Name))}");
                string? tokenRaw = null;

                if (http.Request.HasFormContentType)
                {
                    var form = await http.Request.ReadFormAsync();
                    tokenRaw = form["token"].ToString();
                }

                if (string.IsNullOrEmpty(tokenRaw))
                {
                    tokenRaw = http.Request.Query["token"].ToString();
                }

                found = Guid.TryParse(tokenRaw, out token);
            }

            if (!found || token == Guid.Empty)
            {
                Console.WriteLine($"[TokenAuthorize] Token NICHT gefunden. Source={_source}, ActionArguments={string.Join(", ", context.ActionArguments.Keys)}");
                context.Result = new UnauthorizedResult();
                return;
            }

            Console.WriteLine($"[TokenAuthorize] Token gefunden: {token}, Source={_source}");

            var services = context.HttpContext.RequestServices;
            DateTime? expiresAt = _source switch
            {
                TokenSessionSource.PdfConverter => services.GetRequiredService<PdfConverterSessionRepository>().GetByToken(token)?.ExpiresAt,
                TokenSessionSource.DokumentEdit => services.GetRequiredService<EditSessionRepository>().GetByToken(token)?.ExpiresAt,
                _ => throw new NotSupportedException($"Unbekannte TokenSessionSource: {_source}")
            };


            Console.WriteLine($"[TokenAuthorize] ExpiresAt aus DB: {expiresAt?.ToString() ?? "NULL (Session nicht gefunden)"}, Jetzt: {DateTime.Now}");

            if (expiresAt == null || expiresAt < DateTime.Now)
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            await next();
        }
    }
}