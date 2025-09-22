namespace E_Commerce_BE.Services
{
    public interface ISecureCookieService
    {
        void SetSecureCookie(HttpResponse response, string key, string value, CookieOptions options);
        bool TryGetSecureCookie(HttpRequest request, string key, out string? value);
        void SetShoppingCartCookie(HttpResponse response, Dictionary<int, int> cartData);
        Dictionary<int, int> GetShoppingCartCookie(HttpRequest request);
        void DeleteSecureCookie(HttpResponse response, string key);
    }
}
