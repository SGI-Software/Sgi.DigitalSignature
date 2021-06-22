using Microsoft.AspNetCore.Http;

namespace Sgi.DigitalSignature
{
    public class AssinarHelper
    {
        public const string FORM_KEY_PDF= "pdf";
        public const string FORM_KEY_CERTIFICADO_PFX = "certificadoPfx";
        public const string FORM_KEY_JSON = "json";
        public static string BASE_URL(HttpRequest request) => $"{request.Scheme}://{request.Host}/";
    }
}
