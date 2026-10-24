namespace EAktePrototypProjektNewMvc.Helpers
{
    public static class UrlHelperExtensions
    {
        public static string GetBaseUrl(this HttpRequest request)
        {
            return $"{request.Scheme}://{request.Host}{request.PathBase}";
        }
    }
}
