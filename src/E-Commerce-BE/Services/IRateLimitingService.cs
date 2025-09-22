using E_Commerce_BE.Models;

namespace E_Commerce_BE.Services
{
    public interface IRateLimitingService
    {
        bool IsRateLimited(string ipAddress, string action);
        RateLimitStatus GetRateLimitStatus(string ipAddress, string action);
        void ResetRateLimiter(string ipAddress, string action);
        void RecordFailedAttempt(string ipAddress, string action);
        void RecordSuccessfulAttempt(string ipAddress, string action);
    }
}
