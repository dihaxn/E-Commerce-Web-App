using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using E_Commerce_BE.Services;

namespace E_Commerce_BE.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HealthController : ControllerBase
    {
        private readonly MonitoringService _monitoringService;
        private readonly BackupService _backupService;
        private readonly ILogger<HealthController> _logger;

        public HealthController(
            MonitoringService monitoringService,
            BackupService backupService,
            ILogger<HealthController> logger)
        {
            _monitoringService = monitoringService;
            _backupService = backupService;
            _logger = logger;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetHealth()
        {
            try
            {
                var healthResult = await _monitoringService.PerformHealthCheckAsync();
                
                var response = new
                {
                    Status = healthResult.Status.ToString(),
                    Timestamp = healthResult.Timestamp,
                    Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown",
                    Version = GetApplicationVersion(),
                    Checks = healthResult.Checks.Select(c => new
                    {
                        Name = c.Name,
                        Status = c.Status.ToString(),
                        Description = c.Description,
                        ResponseTime = c.ResponseTime
                    })
                };

                return healthResult.Status == HealthStatus.Healthy 
                    ? Ok(response) 
                    : StatusCode(503, response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Health check failed");
                return StatusCode(500, new { Status = "Error", Message = "Health check failed" });
            }
        }

        [HttpGet("detailed")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> GetDetailedHealth()
        {
            try
            {
                var healthResult = await _monitoringService.PerformHealthCheckAsync();
                var systemMetrics = _monitoringService.GetSystemMetrics();
                var backupHistory = await _backupService.GetBackupHistoryAsync();

                var response = new
                {
                    Health = healthResult,
                    SystemMetrics = systemMetrics,
                    BackupHistory = backupHistory.Take(10), // Last 10 backups
                    Configuration = new
                    {
                        Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
                        MachineName = Environment.MachineName,
                        OSVersion = Environment.OSVersion.ToString(),
                        ProcessorCount = Environment.ProcessorCount,
                        WorkingSet = Environment.WorkingSet,
                        Version = GetApplicationVersion()
                    }
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Detailed health check failed");
                return StatusCode(500, new { Status = "Error", Message = "Detailed health check failed" });
            }
        }

        [HttpGet("metrics")]
        [Authorize(Roles = "admin")]
        public IActionResult GetMetrics()
        {
            try
            {
                var metrics = _monitoringService.GetSystemMetrics();
                return Ok(metrics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get system metrics");
                return StatusCode(500, new { Status = "Error", Message = "Failed to get metrics" });
            }
        }

        [HttpPost("backup")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> CreateBackup([FromQuery] string type = "full")
        {
            try
            {
                BackupResult result;
                
                switch (type.ToLower())
                {
                    case "database":
                        result = await _backupService.CreateDatabaseBackupAsync();
                        break;
                    case "files":
                        result = await _backupService.CreateFileBackupAsync();
                        break;
                    case "full":
                    default:
                        result = await _backupService.CreateFullBackupAsync();
                        break;
                }

                if (result.Status == BackupStatus.Successful)
                {
                    return Ok(new
                    {
                        Status = "Success",
                        Message = result.Message,
                        BackupId = Path.GetFileNameWithoutExtension(result.FilePath),
                        FileSize = result.FileSize,
                        Timestamp = result.Timestamp
                    });
                }
                else
                {
                    return BadRequest(new
                    {
                        Status = "Failed",
                        Message = result.Message
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Backup creation failed");
                return StatusCode(500, new { Status = "Error", Message = "Backup creation failed" });
            }
        }

        [HttpGet("backup/history")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> GetBackupHistory()
        {
            try
            {
                var backups = await _backupService.GetBackupHistoryAsync();
                return Ok(backups);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get backup history");
                return StatusCode(500, new { Status = "Error", Message = "Failed to get backup history" });
            }
        }

        [HttpGet("ping")]
        [AllowAnonymous]
        public IActionResult Ping()
        {
            return Ok(new
            {
                Status = "OK",
                Timestamp = DateTime.UtcNow,
                Message = "Service is running"
            });
        }

        private string GetApplicationVersion()
        {
            // This would typically come from assembly info
            return "1.0.0";
        }
    }
}
