using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace EAktePrototypProjektNewMvc.Helpers
{
    // Check if user exist to prevent access to controllers without login.
    public class SessionAuthorizeAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {

            // Check if the action has the TokenAuthorizeAttribute, if so, skip the session check.
            var hasTokenAuthorize = context.ActionDescriptor.EndpointMetadata
            .OfType<TokenAuthorizeAttribute>()
            .Any();
            if (hasTokenAuthorize)
            {
                return;
            }

            var userId = context.HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                // Ignore Login controller.
                var controller = context.RouteData.Values["controller"]?.ToString();
                if (controller == "Login" || controller == "ExceptionsError")
                {
                    return;
                }

                context.HttpContext.Session.Clear();

                // AJAX — 401.
                if (context.HttpContext.Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    context.Result = new StatusCodeResult(401);
                    return;
                }

                // Redirect.
                // Ziel-URL merken (nur bei GET), damit der Benutzer nach dem Login dorthin zurückkehrt.
                string? returnUrl = context.HttpContext.Request.Method == HttpMethods.Get ? context.HttpContext.Request.PathBase + context.HttpContext.Request.Path + context.HttpContext.Request.QueryString : null;

                context.Result = new RedirectToActionResult("Login", "Login", new { returnUrl });
            }
        }
    }
}
