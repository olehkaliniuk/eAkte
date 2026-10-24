namespace EAktePrototypProjektNewMvc.Exceptions
{
    public class RepositoryHasChildrenException : Exception
    {
        public RepositoryHasChildrenException(string message) : base(message) { }
    }
}