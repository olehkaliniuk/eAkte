using Microsoft.Extensions.Logging;

namespace EAktePrototypProjektNewMvc.Middleware
{
    public static class LogEvents
    {
        public static readonly EventId UnhandledException = new EventId(5000, nameof(UnhandledException));

        public static readonly EventId SqlException = new EventId(5001, nameof(SqlException));

        public static readonly EventId ArgumentException = new EventId(5002, nameof(ArgumentException));
    }
}
