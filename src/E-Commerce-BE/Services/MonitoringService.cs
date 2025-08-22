using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace E_Commerce_BE.Services
{
    public class MonitoringService
    {
        private readonly ILogger<MonitoringService> _logger;
        private readonly IConfiguration _configuration;
        private readonly Dictionary<string, DateTime> _lastHealthCheck = new();
        private readonly Dictionary<string, int> _errorCounts = new();

        public MonitoringService(ILogger<MonitoringService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        public async Task<HealthCheckResult> PerformHealthCheckAsync()
        {
            var result = new HealthCheckResult
            {
                Timestamp = DateTime.UtcNow,
                Status = HealthStatus.Healthy,
                Checks = new List<HealthCheckItem>()
            };

            try
            {
                // Database health check
                var dbHealth = await CheckDatabaseHealthAsync();
                result.Checks.Add(dbHealth);

                // Memory health check
                var memoryHealth = CheckMemoryHealth();
                result.Checks.Add(memoryHealth);

                // Disk space health check
                var diskHealth = CheckDiskHealth();
                result.Checks.Add(diskHealth);

                // Overall status
                if (result.Checks.Any(c => c.Status == HealthStatus.Unhealthy))
                {
                    result.Status = HealthStatus.Unhealthy;
                }
                else if (result.Checks.Any(c => c.Status == HealthStatus.Degraded))
                {
                    result.Status = HealthStatus.Degraded;
                }

                _logger.LogInformation($"Health check completed. Status: {result.Status}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Health check failed");
                result.Status = HealthStatus.Unhealthy;
                result.Checks.Add(new HealthCheckItem
                {
                    Name = "HealthCheckService",
                    Status = HealthStatus.Unhealthy,
                    Description = $"Health check service error: {ex.Message}"
                });
            }

            return result;
        }

        private async Task<HealthCheckItem> CheckDatabaseHealthAsync()
        {
            try
            {
                // This would integrate with your actual database service
                // For now, we'll simulate a database check
                await Task.Delay(100); // Simulate async operation
                
                return new HealthCheckItem
                {
                    Name = "Database",
                    Status = HealthStatus.Healthy,
                    Description = "Database connection is healthy",
                    ResponseTime = 100
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database health check failed");
                return new HealthCheckItem
                {
                    Name = "Database",
                    Status = HealthStatus.Unhealthy,
                    Description = $"Database error: {ex.Message}"
                };
            }
        }

        private HealthCheckItem CheckMemoryHealth()
        {
            try
            {
                var process = Process.GetCurrentProcess();
                var memoryUsageMB = process.WorkingSet64 / 1024 / 1024;
                var maxMemoryMB = _configuration.GetValue<int>("Monitoring:MaxMemoryMB", 1024);

                var status = memoryUsageMB > maxMemoryMB ? HealthStatus.Degraded : HealthStatus.Healthy;
                var description = $"Memory usage: {memoryUsageMB}MB / {maxMemoryMB}MB";

                return new HealthCheckItem
                {
                    Name = "Memory",
                    Status = status,
                    Description = description,
                    ResponseTime = 0
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Memory health check failed");
                return new HealthCheckItem
                {
                    Name = "Memory",
                    Status = HealthStatus.Unhealthy,
                    Description = $"Memory check error: {ex.Message}"
                };
            }
        }

        private HealthCheckItem CheckDiskHealth()
        {
            try
            {
                var drive = new DriveInfo(Path.GetPathRoot(Environment.CurrentDirectory));
                var freeSpaceGB = drive.AvailableFreeSpace / 1024 / 1024 / 1024;
                var totalSpaceGB = drive.TotalSize / 1024 / 1024 / 1024;
                var freeSpacePercent = (double)freeSpaceGB / totalSpaceGB * 100;

                var status = freeSpacePercent < 10 ? HealthStatus.Degraded : HealthStatus.Healthy;
                var description = $"Disk space: {freeSpaceGB:F1}GB free of {totalSpaceGB:F1}GB ({freeSpacePercent:F1}%)";

                return new HealthCheckItem
                {
                    Name = "Disk",
                    Status = status,
                    Description = description,
                    ResponseTime = 0
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Disk health check failed");
                return new HealthCheckItem
                {
                    Name = "Disk",
                    Status = HealthStatus.Unhealthy,
                    Description = $"Disk check error: {ex.Message}"
                };
            }
        }

        public void LogSecurityEvent(string eventType, string description, string userId = null, string ipAddress = null)
        {
            var logMessage = $"SECURITY_EVENT: {eventType} - {description}";
            if (!string.IsNullOrEmpty(userId))
                logMessage += $" | User: {userId}";
            if (!string.IsNullOrEmpty(ipAddress))
                logMessage += $" | IP: {ipAddress}";

            _logger.LogWarning(logMessage);

            // Track error counts for rate limiting
            var key = $"{eventType}_{ipAddress ?? "unknown"}";
            if (_errorCounts.ContainsKey(key))
                _errorCounts[key]++;
            else
                _errorCounts[key] = 1;

            // Log to external monitoring system if configured
            LogToExternalSystem(eventType, description, userId, ipAddress);
        }

        public void LogPerformanceMetric(string metricName, double value, string unit = "")
        {
            var logMessage = $"PERFORMANCE: {metricName} = {value}{unit}";
            _logger.LogInformation(logMessage);

            // Track metrics for alerting
            TrackMetric(metricName, value);
        }

        private void LogToExternalSystem(string eventType, string description, string userId, string ipAddress)
        {
            // This would integrate with external monitoring systems like:
            // - Application Insights
            // - Log Analytics
            // - Splunk
            // - ELK Stack
            try
            {
                // Implementation depends on your monitoring infrastructure
                _logger.LogDebug($"External logging: {eventType} - {description}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log to external system");
            }
        }

        private void TrackMetric(string metricName, double value)
        {
            // This would integrate with metrics collection systems
            try
            {
                // Implementation depends on your metrics infrastructure
                _logger.LogDebug($"Metric tracked: {metricName} = {value}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to track metric");
            }
        }

        public Dictionary<string, object> GetSystemMetrics()
        {
            var process = Process.GetCurrentProcess();
            var metrics = new Dictionary<string, object>
            {
                ["ProcessId"] = process.Id,
                ["MemoryUsageMB"] = process.WorkingSet64 / 1024 / 1024,
                ["CpuTime"] = process.TotalProcessorTime,
                ["ThreadCount"] = process.Threads.Count,
                ["HandleCount"] = process.HandleCount,
                ["StartTime"] = process.StartTime,
                ["Uptime"] = DateTime.UtcNow - process.StartTime.ToUniversalTime()
            };

            return metrics;
        }
    }

    public class HealthCheckResult
    {
        public DateTime Timestamp { get; set; }
        public HealthStatus Status { get; set; }
        public List<HealthCheckItem> Checks { get; set; } = new();
    }

    public class HealthCheckItem
    {
        public string Name { get; set; } = "";
        public HealthStatus Status { get; set; }
        public string Description { get; set; } = "";
        public long ResponseTime { get; set; }
    }

    public enum HealthStatus
    {
        Healthy,
        Degraded,
        Unhealthy
    }
}
