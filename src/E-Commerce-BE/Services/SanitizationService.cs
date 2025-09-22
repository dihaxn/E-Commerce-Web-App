using Ganss.Xss;

namespace E_Commerce_BE.Services
{
    public class SanitizationService : ISanitizationService
    {
        private readonly IHtmlSanitizer _sanitizer;

        public SanitizationService()
        {
            _sanitizer = new HtmlSanitizer();
        }

        public string Sanitize(string input)
        {
            return _sanitizer.Sanitize(input);
        }
    }
}
