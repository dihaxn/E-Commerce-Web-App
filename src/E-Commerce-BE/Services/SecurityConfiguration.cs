using Microsoft.Extensions.Configuration;

namespace E_Commerce_BE.Services
{
    public class SecurityConfiguration
    {
        public PasswordPolicy PasswordPolicy { get; set; } = new();
        public SessionSettings SessionSettings { get; set; } = new();
        public FileUploadSettings FileUploadSettings { get; set; } = new();

        public SecurityConfiguration(IConfiguration configuration)
        {
            configuration.GetSection("SecuritySettings:PasswordPolicy").Bind(PasswordPolicy);
            configuration.GetSection("SecuritySettings:SessionSettings").Bind(SessionSettings);
            configuration.GetSection("SecuritySettings:FileUploadSettings").Bind(FileUploadSettings);
        }
    }

    public class PasswordPolicy
    {
        public int RequiredLength { get; set; } = 12;
        public bool RequireNonAlphanumeric { get; set; } = true;
        public bool RequireUppercase { get; set; } = true;
        public bool RequireLowercase { get; set; } = true;
        public bool RequireDigit { get; set; } = true;
        public int MaxFailedAttempts { get; set; } = 5;
        public int LockoutDurationMinutes { get; set; } = 15;
    }

    public class SessionSettings
    {
        public int IdleTimeoutMinutes { get; set; } = 30;
        public int AbsoluteTimeoutMinutes { get; set; } = 480;
    }

    public class FileUploadSettings
    {
        public int MaxFileSizeMB { get; set; } = 5;
        public string[] AllowedExtensions { get; set; } = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        public int MaxFilesPerUpload { get; set; } = 1;
    }
}
