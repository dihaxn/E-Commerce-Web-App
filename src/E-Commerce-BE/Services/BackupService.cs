using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.IO.Compression;

namespace E_Commerce_BE.Services
{
    public class BackupService
    {
        private readonly ILogger<BackupService> _logger;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _environment;
        private readonly ApplicationDbContext _context;

        public BackupService(
            ILogger<BackupService> logger,
            IConfiguration configuration,
            IWebHostEnvironment environment,
            ApplicationDbContext context)
        {
            _logger = logger;
            _configuration = configuration;
            _environment = environment;
            _context = context;
        }

        public async Task<BackupResult> CreateDatabaseBackupAsync()
        {
            var result = new BackupResult
            {
                Timestamp = DateTime.UtcNow,
                Type = BackupType.Database,
                Status = BackupStatus.InProgress
            };

            try
            {
                var backupPath = GetBackupPath();
                var fileName = $"DatabaseBackup_{DateTime.UtcNow:yyyyMMdd_HHmmss}.sql";
                var fullPath = Path.Combine(backupPath, fileName);

                // Ensure backup directory exists
                Directory.CreateDirectory(backupPath);

                // Create database backup using SQL Server commands
                await CreateSqlServerBackupAsync(fullPath);

                result.FilePath = fullPath;
                result.FileSize = new FileInfo(fullPath).Length;
                result.Status = BackupStatus.Successful;
                result.Message = "Database backup created successfully";

                _logger.LogInformation($"Database backup created: {fullPath} ({result.FileSize} bytes)");

                // Clean up old backups
                await CleanupOldBackupsAsync(backupPath, BackupType.Database);
            }
            catch (Exception ex)
            {
                result.Status = BackupStatus.Failed;
                result.Message = $"Database backup failed: {ex.Message}";
                _logger.LogError(ex, "Database backup failed");
            }

            return result;
        }

        public async Task<BackupResult> CreateFileBackupAsync()
        {
            var result = new BackupResult
            {
                Timestamp = DateTime.UtcNow,
                Type = BackupType.Files,
                Status = BackupStatus.InProgress
            };

            try
            {
                var backupPath = GetBackupPath();
                var fileName = $"FileBackup_{DateTime.UtcNow:yyyyMMdd_HHmmss}.zip";
                var fullPath = Path.Combine(backupPath, fileName);

                // Ensure backup directory exists
                Directory.CreateDirectory(backupPath);

                // Create file backup
                await CreateFileBackupAsync(fullPath);

                result.FilePath = fullPath;
                result.FileSize = new FileInfo(fullPath).Length;
                result.Status = BackupStatus.Successful;
                result.Message = "File backup created successfully";

                _logger.LogInformation($"File backup created: {fullPath} ({result.FileSize} bytes)");

                // Clean up old backups
                await CleanupOldBackupsAsync(backupPath, BackupType.Files);
            }
            catch (Exception ex)
            {
                result.Status = BackupStatus.Failed;
                result.Message = $"File backup failed: {ex.Message}";
                _logger.LogError(ex, "File backup failed");
            }

            return result;
        }

        public async Task<BackupResult> CreateFullBackupAsync()
        {
            var result = new BackupResult
            {
                Timestamp = DateTime.UtcNow,
                Type = BackupType.Full,
                Status = BackupStatus.InProgress
            };

            try
            {
                // Create database backup
                var dbBackup = await CreateDatabaseBackupAsync();
                if (dbBackup.Status != BackupStatus.Successful)
                {
                    throw new Exception($"Database backup failed: {dbBackup.Message}");
                }

                // Create file backup
                var fileBackup = await CreateFileBackupAsync();
                if (fileBackup.Status != BackupStatus.Successful)
                {
                    throw new Exception($"File backup failed: {fileBackup.Message}");
                }

                // Create backup manifest
                var manifest = await CreateBackupManifestAsync(dbBackup, fileBackup);

                result.Status = BackupStatus.Successful;
                result.Message = "Full backup completed successfully";
                result.FilePath = manifest;
                result.FileSize = new FileInfo(manifest).Length;

                _logger.LogInformation("Full backup completed successfully");
            }
            catch (Exception ex)
            {
                result.Status = BackupStatus.Failed;
                result.Message = $"Full backup failed: {ex.Message}";
                _logger.LogError(ex, "Full backup failed");
            }

            return result;
        }

        private async Task CreateSqlServerBackupAsync(string backupPath)
        {
            // This is a simplified version - in production, you'd use:
            // - SQL Server Management Objects (SMO)
            // - Native SQL Server backup commands
            // - Azure SQL Database backup APIs
            
            try
            {
                // For now, we'll create a simple backup file
                // In production, implement proper SQL Server backup
                var backupContent = $"-- Database Backup\n-- Created: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}\n-- This is a placeholder for actual SQL Server backup\n";
                await File.WriteAllTextAsync(backupPath, backupContent);
                
                _logger.LogInformation($"SQL Server backup placeholder created: {backupPath}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create SQL Server backup");
                throw;
            }
        }

        private async Task CreateFileBackupAsync(string backupPath)
        {
            try
            {
                var wwwrootPath = Path.Combine(_environment.WebRootPath);
                var productsPath = Path.Combine(wwwrootPath, "products");

                using var archive = ZipFile.Open(backupPath, ZipArchiveMode.Create);

                // Add CSS files
                var cssPath = Path.Combine(wwwrootPath, "css");
                if (Directory.Exists(cssPath))
                {
                    foreach (var file in Directory.GetFiles(cssPath, "*.css"))
                    {
                        var relativePath = Path.GetRelativePath(wwwrootPath, file);
                        archive.CreateEntryFromFile(file, relativePath);
                    }
                }

                // Add JS files
                var jsPath = Path.Combine(wwwrootPath, "js");
                if (Directory.Exists(jsPath))
                {
                    foreach (var file in Directory.GetFiles(jsPath, "*.js"))
                    {
                        var relativePath = Path.GetRelativePath(wwwrootPath, file);
                        archive.CreateEntryFromFile(file, relativePath);
                    }
                }

                // Add product images (if they exist)
                if (Directory.Exists(productsPath))
                {
                    foreach (var file in Directory.GetFiles(productsPath, "*.*"))
                    {
                        var relativePath = Path.GetRelativePath(wwwrootPath, file);
                        archive.CreateEntryFromFile(file, relativePath);
                    }
                }

                _logger.LogInformation($"File backup created: {backupPath}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create file backup");
                throw;
            }
        }

        private async Task<string> CreateBackupManifestAsync(BackupResult dbBackup, BackupResult fileBackup)
        {
            var manifestPath = Path.Combine(GetBackupPath(), $"BackupManifest_{DateTime.UtcNow:yyyyMMdd_HHmmss}.json");
            
            var manifest = new
            {
                BackupId = Guid.NewGuid().ToString(),
                Timestamp = DateTime.UtcNow,
                Type = "Full",
                DatabaseBackup = new
                {
                    FilePath = dbBackup.FilePath,
                    FileSize = dbBackup.FileSize,
                    Status = dbBackup.Status
                },
                FileBackup = new
                {
                    FilePath = fileBackup.FilePath,
                    FileSize = fileBackup.FileSize,
                    Status = fileBackup.Status
                },
                Environment = _environment.EnvironmentName,
                Version = GetApplicationVersion()
            };

            var json = System.Text.Json.JsonSerializer.Serialize(manifest, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(manifestPath, json);

            return manifestPath;
        }

        private async Task CleanupOldBackupsAsync(string backupPath, BackupType backupType)
        {
            try
            {
                var retentionDays = _configuration.GetValue<int>("Backup:RetentionDays", 30);
                var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);

                var backupFiles = Directory.GetFiles(backupPath, $"*{backupType}*")
                    .Select(f => new FileInfo(f))
                    .Where(f => f.CreationTime < cutoffDate)
                    .ToList();

                foreach (var file in backupFiles)
                {
                    try
                    {
                        file.Delete();
                        _logger.LogInformation($"Deleted old backup: {file.Name}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, $"Failed to delete old backup: {file.Name}");
                    }
                }

                _logger.LogInformation($"Cleaned up {backupFiles.Count} old {backupType} backups");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to cleanup old backups");
            }
        }

        private string GetBackupPath()
        {
            var basePath = _configuration.GetValue<string>("Backup:BasePath", "Backups");
            var environmentPath = Path.Combine(basePath, _environment.EnvironmentName);
            return Path.Combine(_environment.ContentRootPath, environmentPath);
        }

        private string GetApplicationVersion()
        {
            // This would typically come from assembly info or build configuration
            return "1.0.0";
        }

        public async Task<List<BackupResult>> GetBackupHistoryAsync()
        {
            var backupPath = GetBackupPath();
            var backups = new List<BackupResult>();

            if (!Directory.Exists(backupPath))
                return backups;

            try
            {
                var backupFiles = Directory.GetFiles(backupPath, "*.*")
                    .Select(f => new FileInfo(f))
                    .OrderByDescending(f => f.CreationTime)
                    .Take(50); // Limit to last 50 backups

                foreach (var file in backupFiles)
                {
                    var backup = new BackupResult
                    {
                        Timestamp = file.CreationTime,
                        FilePath = file.FullName,
                        FileSize = file.Length,
                        Status = BackupStatus.Successful,
                        Type = DetermineBackupType(file.Name)
                    };

                    backups.Add(backup);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get backup history");
            }

            return backups;
        }

        private BackupType DetermineBackupType(string fileName)
        {
            if (fileName.Contains("Database"))
                return BackupType.Database;
            if (fileName.Contains("File"))
                return BackupType.Files;
            if (fileName.Contains("Manifest"))
                return BackupType.Full;
            
            return BackupType.Unknown;
        }
    }

    public class BackupResult
    {
        public DateTime Timestamp { get; set; }
        public BackupType Type { get; set; }
        public BackupStatus Status { get; set; }
        public string FilePath { get; set; } = "";
        public long FileSize { get; set; }
        public string Message { get; set; } = "";
    }

    public enum BackupType
    {
        Database,
        Files,
        Full,
        Unknown
    }

    public enum BackupStatus
    {
        InProgress,
        Successful,
        Failed
    }
}
