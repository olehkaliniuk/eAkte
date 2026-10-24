using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Data.SqlClient;

namespace EAktePrototypProjektNewMvc.Middleware
{
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;

        public GlobalExceptionMiddleware(
            RequestDelegate next,
            ILogger<GlobalExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                LogException(context, ex);
                context.Response.Clear();
                context.Response.StatusCode = 500;

                // AJAX return 500.
                if (context.Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    context.Response.ContentType = "text/plain";
                    await context.Response.WriteAsync(ex.Message);
                    return;
                }

                // Redirect.
                var message = Uri.EscapeDataString(ex.Message);
                context.Response.Redirect($"/ExceptionsError/Index?message={message}");
            }
        }

        private EventId GetEventId(Exception ex)
        {
            return ex switch
            {
                SqlException => LogEvents.SqlException,
                ArgumentException => LogEvents.ArgumentException,
                _ => LogEvents.UnhandledException
            };
        }

        private void LogException(HttpContext context, Exception ex)
        {
            var eventId = GetEventId(ex);

            var email = context.Session?.GetString("UserEmail");
            var name = context.Session?.GetString("UserName");
            var userInfo = (!string.IsNullOrWhiteSpace(email) || !string.IsNullOrWhiteSpace(name)) ? $"{email} {name}".Trim() : "Unknown";

            _logger.LogError(eventId, ex,
                "UNHANDLED EXCEPTION | Time: {Time} | Path: {Path} | Method: {Method} | UserInfo: {UserInfo} | IP: {IP}",
                DateTime.UtcNow,
                context.Request.Path,
                context.Request.Method,
                userInfo,
                context.Connection.RemoteIpAddress?.ToString()
            );
        }
    }
}
