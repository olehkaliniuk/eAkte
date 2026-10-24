using DreiF.Shared.Abstractions;
using Microsoft.AspNetCore.Http;

namespace EAktePrototypProjektNewMvc.Services
{
    public class SessionMandantProvider : ICurrentMandantProvider
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public SessionMandantProvider(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public int MandantId
        {
            get
            {
                var session = _httpContextAccessor.HttpContext?.Session;

                if (session == null)
                {
                    throw new InvalidOperationException("Keine aktive Session gefunden.");
                }

                var activeMandant = session.GetInt32("ActiveMandantId");
                if (activeMandant.HasValue)
                {
                    return activeMandant.Value;
                }

                var loginMandant = session.GetInt32("LoginMandantId");
                if (loginMandant.HasValue)
                {
                    return loginMandant.Value;
                }

                throw new InvalidOperationException("MandantId ist in der Session nicht gesetzt.");
            }
        }
    }
}