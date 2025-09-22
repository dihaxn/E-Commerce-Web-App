using System.Collections.Concurrent;

namespace E_Commerce_BE.Services
{
    public class RateLimitingService : IRateLimitingService
    {
        private readonly ConcurrentDictionary<string, RateLimitInfo> _rateLimitStore = new();
        private readonly int _maxAttempts;
        private readonly int _lockoutDurationMinutes;

        public RateLimitingService(IConfiguration configuration)
        {
            _maxAttempts = configuration.GetValue<int>("SecuritySettings:PasswordPolicy:MaxFailedAttempts", 5);
            _lockoutDurationMinutes = configuration.GetValue<int>("SecuritySettings:PasswordPolicy:LockoutDurationMinutes", 15);
        }

        public void ResetRateLimiter(string ipAddress, string action)
        {
            var fullKey = $"{ipAddress}:{action}";
            if (_rateLimitStore.TryGetValue(fullKey, out var info))
            {
                info.Reset();
            }
        }

        public bool IsRateLimited(string ipAddress, string action)
        {
            var fullKey = $"{ipAddress}:{action}";

            if (_rateLimitStore.TryGetValue(fullKey, out var info))
            {
                // Check if still locked out
                if (info.IsLockedOut && DateTime.UtcNow < info.LockoutEnd)
                {
                    return true;
                }

                // Reset if lockout period has passed
                if (info.IsLockedOut && DateTime.UtcNow >= info.LockoutEnd)
                {
                    info.Reset();
                }
            }

            return false;
        }

        public void RecordFailedAttempt(string ipAddress, string action)
        {
            var fullKey = $"{ipAddress}:{action}";

            var info = _rateLimitStore.GetOrAdd(fullKey, _ => new RateLimitInfo());
            info.RecordFailedAttempt();

            // Check if should be locked out
            if (info.FailedAttempts >= _maxAttempts)
            {
                info.Lockout(DateTime.UtcNow.AddMinutes(_lockoutDurationMinutes));
            }
        }

        public void RecordSuccessfulAttempt(string ipAddress, string action)
        {
            var fullKey = $"{ipAddress}:{action}";

            if (_rateLimitStore.TryGetValue(fullKey, out var info))
            {
                info.Reset();
            }
        }
        public RateLimitStatus GetRateLimitStatus(string ipAddress, string action)
        {
            var fullKey = $"{ipAddress}:{action}";

            if (_rateLimitStore.TryGetValue(fullKey, out var info))
            {
                return new RateLimitStatus
                {
                    IsLockedOut = info.IsLockedOut,
                    FailedAttempts = info.FailedAttempts,
                    RemainingAttempts = Math.Max(0, _maxAttempts - info.FailedAttempts),
                    LockoutEnd = info.LockoutEnd,
                    TimeUntilReset = info.IsLockedOut && info.LockoutEnd.HasValue ? info.LockoutEnd.Value - DateTime.UtcNow : TimeSpan.Zero
                };
            }

            return new RateLimitStatus
            {
                IsLockedOut = false,
                FailedAttempts = 0,
                RemainingAttempts = _maxAttempts,
                LockoutEnd = null,
                TimeUntilReset = TimeSpan.Zero
            };
        }

        public void CleanupExpiredEntries()
        {
            var now = DateTime.UtcNow;
            var keysToRemove = new List<string>();

            foreach (var kvp in _rateLimitStore)
            {
                if (kvp.Value.LockoutEnd.HasValue && now > kvp.Value.LockoutEnd.Value.AddMinutes(60))
                {
                    keysToRemove.Add(kvp.Key);
                }
            }

            foreach (var key in keysToRemove)
            {
                _rateLimitStore.TryRemove(key, out _);
            }
        }

        private class RateLimitInfo
        {
            public int FailedAttempts { get; private set; }
            public bool IsLockedOut { get; private set; }
            public DateTime? LockoutEnd { get; private set; }

            public void RecordFailedAttempt()
            {
                FailedAttempts++;
            }

            public void Lockout(DateTime lockoutEnd)
            {
                IsLockedOut = true;
                LockoutEnd = lockoutEnd;
            }

            public void Reset()
            {
                FailedAttempts = 0;
                IsLockedOut = false;
                LockoutEnd = null;
            }
        }
    }

    public class RateLimitStatus
    {
        public bool IsLockedOut { get; set; }
        public int FailedAttempts { get; set; }
        public int RemainingAttempts { get; set; }
        public DateTime? LockoutEnd { get; set; }
        public TimeSpan TimeUntilReset { get; set; }
    }
}
