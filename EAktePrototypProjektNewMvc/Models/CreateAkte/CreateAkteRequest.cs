using EAktePrototypProjektNewMvc.Models.Vorgange;
using EAktePrototypProjektNewMvc.Models.Ordner;
using EAktePrototypProjektNewMvc.Models.Akten;

namespace EAktePrototypProjektNewMvc.Models.CreateAkte
{
    public class CreateAkteRequest
    {
        public Akte? Akte { get; set; }
        public Vorgang? Vorgang { get; set; }
        public Ordner.Ordner? Ordner { get; set; }
    }
}
