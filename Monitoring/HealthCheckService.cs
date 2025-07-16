// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace EfMigrationDiff.Monitoring;

/// <summary>
/// Health check service for monitoring application and dependency health.
/// Runs configurable health checks and reports overall system status.
/// </summary>
public class HealthCheckService
{
    private readonly Dictionary<string, IHealthCheck> _checks = new();
    private readonly Dictionary<string, HealthCheckResult> _lastResults = new();
    private readonly TimeSpan _cacheTimeout;

    public HealthCheckService(TimeSpan? cacheTimeout = null)
    {
        _cacheTimeout = cacheTimeout ?? TimeSpan.FromSeconds(30);
    }

    /// <summary>
    /// Registers a health check.
    /// </summary>
    public void RegisterCheck(string name, IHealthCheck check)
    {
        if (string.IsNullOrEmpty(name))
            throw new ArgumentException("Check name cannot be empty", nameof(name));

        _checks[name] = check ?? throw new ArgumentNullException(nameof(check));
    }

    /// <summary>
    /// Runs a specific health check.
    /// </summary>
    public async Task<HealthCheckResult> RunCheckAsync(string checkName)
    {
        if (!_checks.TryGetValue(checkName, out var check))
            throw new KeyNotFoundException($"Health check not found: {checkName}");

        try
        {
            var startTime = DateTime.UtcNow;
            var checkResult = await check.CheckHealthAsync();
            checkResult.Duration = DateTime.UtcNow - startTime;
            checkResult.CheckName = checkName;

            _lastResults[checkName] = checkResult;
            return checkResult;
        }
        catch (Exception ex)
        {
            var result = new HealthCheckResult
            {
                CheckName = checkName,
                Status = HealthStatus.Unhealthy,
                Message = $"Check failed: {ex.Message}",
                Duration = TimeSpan.Zero
            };

            _lastResults[checkName] = result;
            return result;
        }
    }

    /// <summary>
    /// Runs all registered health checks.
    /// </summary>
    public async Task<HealthCheckSummary> RunAllChecksAsync()
    {
        var results = new List<HealthCheckResult>();
        var startTime = DateTime.UtcNow;

        foreach (var checkName in _checks.Keys)
        {
            var result = await RunCheckAsync(checkName);
            results.Add(result);
        }

        return new HealthCheckSummary
        {
            CheckedAt = DateTime.UtcNow,
            TotalDuration = DateTime.UtcNow - startTime,
            Results = results
        };
    }

    /// <summary>
    /// Gets the last result from a check (may be cached).
    /// </summary>
    public HealthCheckResult? GetLastResult(string checkName)
    {
        if (_lastResults.TryGetValue(checkName, out var result))
        {
            // Check if result is still valid (not expired)
            if (DateTime.UtcNow - result.CheckedAt <= _cacheTimeout)
                return result;
        }

        return null;
    }

    /// <summary>
    /// Gets all last results.
    /// </summary>
    public IEnumerable<HealthCheckResult> GetAllLastResults()
    {
        return _lastResults.Values.Where(r => DateTime.UtcNow - r.CheckedAt <= _cacheTimeout);
    }

    /// <summary>
    /// Clears all cached results.
    /// </summary>
    public void ClearCache()
    {
        _lastResults.Clear();
    }
}

/// <summary>
/// Interface for health check implementations.
/// </summary>
public interface IHealthCheck
{
    Task<HealthCheckResult> CheckHealthAsync();
}

/// <summary>
/// Result of a health check.
/// </summary>
public class HealthCheckResult
{
    public string CheckName { get; set; } = string.Empty;
    public HealthStatus Status { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime CheckedAt { get; set; } = DateTime.UtcNow;
    public TimeSpan Duration { get; set; }
    public Dictionary<string, object?> Data { get; set; } = new();

    public bool IsHealthy => Status == HealthStatus.Healthy;
}

/// <summary>
/// Summary of multiple health checks.
/// </summary>
public class HealthCheckSummary
{
    public DateTime CheckedAt { get; set; }
    public TimeSpan TotalDuration { get; set; }
    public List<HealthCheckResult> Results { get; set; } = new();

    public bool IsHealthy => !Results.Any(r => r.Status == HealthStatus.Unhealthy);

    public HealthStatus OverallStatus => Results.Any(r => r.Status == HealthStatus.Unhealthy)
        ? HealthStatus.Unhealthy
        : Results.Any(r => r.Status == HealthStatus.Degraded)
            ? HealthStatus.Degraded
            : HealthStatus.Healthy;

    public int HealthyCount => Results.Count(r => r.IsHealthy);
    public int UnhealthyCount => Results.Count(r => r.Status == HealthStatus.Unhealthy);
}

public enum HealthStatus
{
    Healthy,
    Degraded,
    Unhealthy
}

/// <summary>
/// Base implementation of health check.
/// </summary>
public abstract class HealthCheckBase : IHealthCheck
{
    public string CheckName { get; set; } = string.Empty;

    public abstract Task<HealthCheckResult> CheckHealthAsync();
}
