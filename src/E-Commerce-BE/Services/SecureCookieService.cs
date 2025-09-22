using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace E_Commerce_BE.Services
{
    public class SecureCookieService : ISecureCookieService
    {
        private readonly IConfiguration _configuration;
        private readonly string _encryptionKey;

        public SecureCookieService(IConfiguration configuration)
        {
            _configuration = configuration;
            // In production, this should come from environment variables or Azure Key Vault
            _encryptionKey = _configuration["CookieEncryptionKey"] ?? "DefaultKeyForDevelopmentOnly123!@#";
        }

        public void SetSecureCookie(HttpResponse response, string key, string value, CookieOptions options)
        {
            var encryptedValue = EncryptString(value);
            response.Cookies.Append(key, encryptedValue, options);
        }

        public bool TryGetSecureCookie(HttpRequest request, string key, out string? value)
        {
            var encryptedValue = request.Cookies[key];
            if (string.IsNullOrEmpty(encryptedValue))
            {
                value = null;
                return false;
            }

            try
            {
                value = DecryptString(encryptedValue);
                return true;
            }
            catch
            {
                value = null;
                return false;
            }
        }

        public void SetSecureCookie(HttpResponse response, string key, object value, int? maxAge = null)
        {
            var jsonValue = JsonSerializer.Serialize(value);
            var encryptedValue = EncryptString(jsonValue);
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true, // Requires HTTPS
                SameSite = SameSiteMode.Strict,
                MaxAge = maxAge.HasValue ? TimeSpan.FromSeconds(maxAge.Value) : TimeSpan.FromHours(24),
                Path = "/"
            };

            response.Cookies.Append(key, encryptedValue, cookieOptions);
        }

        public T? GetSecureCookie<T>(HttpRequest request, string key) where T : class
        {
            var encryptedValue = request.Cookies[key];
            if (string.IsNullOrEmpty(encryptedValue))
                return null;

            try
            {
                var decryptedValue = DecryptString(encryptedValue);
                return JsonSerializer.Deserialize<T>(decryptedValue);
            }
            catch
            {
                // If decryption fails, remove the invalid cookie
                return null;
            }
        }

        public void DeleteSecureCookie(HttpResponse response, string key)
        {
            response.Cookies.Delete(key, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Path = "/"
            });
        }

        public void SetShoppingCartCookie(HttpResponse response, Dictionary<int, int> cartData)
        {
            SetSecureCookie(response, "shopping_cart", cartData, (int)TimeSpan.FromDays(30).TotalSeconds);
        }

        public Dictionary<int, int> GetShoppingCartCookie(HttpRequest request)
        {
            return GetSecureCookie<Dictionary<int, int>>(request, "shopping_cart") ?? new Dictionary<int, int>();
        }

        private string EncryptString(string plainText)
        {
            using var aes = Aes.Create();
            aes.Key = Encoding.UTF8.GetBytes(_encryptionKey.PadRight(32, '0').Substring(0, 32));
            aes.GenerateIV();

            using var encryptor = aes.CreateEncryptor();
            using var msEncrypt = new MemoryStream();
            using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
            using (var swEncrypt = new StreamWriter(csEncrypt))
            {
                swEncrypt.Write(plainText);
            }

            var encrypted = msEncrypt.ToArray();
            var result = new byte[aes.IV.Length + encrypted.Length];
            Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
            Buffer.BlockCopy(encrypted, 0, result, aes.IV.Length, encrypted.Length);

            return Convert.ToBase64String(result);
        }

        private string DecryptString(string cipherText)
        {
            var fullCipher = Convert.FromBase64String(cipherText);
            using var aes = Aes.Create();
            aes.Key = Encoding.UTF8.GetBytes(_encryptionKey.PadRight(32, '0').Substring(0, 32));

            var iv = new byte[aes.IV.Length];
            var cipher = new byte[fullCipher.Length - aes.IV.Length];

            Buffer.BlockCopy(fullCipher, 0, iv, 0, iv.Length);
            Buffer.BlockCopy(fullCipher, iv.Length, cipher, 0, cipher.Length);

            aes.IV = iv;

            using var decryptor = aes.CreateDecryptor();
            using var msDecrypt = new MemoryStream(cipher);
            using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
            using var srDecrypt = new StreamReader(csDecrypt);
            return srDecrypt.ReadToEnd();
        }
    }
}
