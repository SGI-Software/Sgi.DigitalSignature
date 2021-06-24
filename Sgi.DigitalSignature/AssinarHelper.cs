using Microsoft.AspNetCore.Http;

namespace Sgi.DigitalSignature
{
    public class AssinarHelper
    {
        public const string FORM_KEY_PDF= "pdf";
        public const string FORM_KEY_CERTIFICADO_PFX = "certificadoPfx";
        public const string FORM_KEY_JSON = "json";
        private const string ROTA_API_NO_SERVIDOR = "assinaturadigital/api/assinar/";

        public static string BASE_URL(HttpRequest request, bool ehProducao) 
            => ehProducao ? $"{request.Scheme}://{request.Host}/{ROTA_API_NO_SERVIDOR}" : $"{request.Scheme}://{request.Host}/" ;
    }
}
