// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace EfMigrationDiff.Utilities;

/// <summary>
/// Tracks and reports performance metrics for operations.
/// Measures execution time, memory usage, and operation counts.
/// </summary>
public class PerformanceMetrics
{
    private readonly Dictionary<string, OperationMetrics> _metrics = new();
    private readonly ReaderWriterLockSlim _lock = new();

    /// <summary>
    /// Starts measuring an operation. Use with 'using' statement.
    /// </summary>
    public PerformationTimer StartOperation(string operationName)
    {
        return new PerformationTimer(operationName, this);
    }

    /// <summary>
    /// Records a measurement for an operation.
    /// </summary>
    internal void RecordMeasurement(string operationName, TimeSpan duration, long? memoryDelta = null)
    {
        _lock.EnterWriteLock();
        try
        {
            if (!_metrics.TryGetValue(operationName, out var metric))
            {
                metric = new OperationMetrics { OperationName = operationName };
                _metrics[operationName] = metric;
            }

            metric.Measurements.Add(duration);
            if (memoryDelta.HasValue)
                metric.MemoryDelta += memoryDelta.Value;

            metric.Count++;
            metric.TotalDuration += duration;

            if (metric.MaxDuration < duration)
                metric.MaxDuration = duration;

            if (metric.MinDuration > duration)
                metric.MinDuration = duration;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Gets metrics for a specific operation.
    /// </summary>
    public OperationMetrics? GetMetrics(string operationName)
    {
        _lock.EnterReadLock();
        try
        {
            return _metrics.TryGetValue(operationName, out var metric) ? metric : null;
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <summary>
    /// Gets all recorded metrics.
    /// </summary>
    public Dictionary<string, OperationMetrics> GetAllMetrics()
    {
        _lock.EnterReadLock();
        try
        {
            return new Dictionary<string, OperationMetrics>(_metrics);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <summary>
    /// Generates a report of all metrics.
    /// </summary>
    public string GenerateReport()
    {
        var sb = new System.Text.StringBuilder();

        _lock.EnterReadLock();
        try
        {
            sb.AppendLine("\n╔════════════════════════════════════════════════════════════╗");
            sb.AppendLine("║            Performance Metrics Report                       ║");
            sb.AppendLine("╚════════════════════════════════════════════════════════════╝\n");

            foreach (var metric in _metrics.Values.OrderByDescending(m => m.TotalDuration))
            {
                var avgMs = metric.AverageDuration.TotalMilliseconds;
                var totalMs = metric.TotalDuration.TotalMilliseconds;

                sb.AppendLine($"Operation: {metric.OperationName}");
                sb.AppendLine($"  Executions     : {metric.Count}");
                sb.AppendLine($"  Total Time     : {totalMs:F2}ms");
                sb.AppendLine($"  Average Time   : {avgMs:F2}ms");
                sb.AppendLine($"  Min Time       : {metric.MinDuration.TotalMilliseconds:F2}ms");
                sb.AppendLine($"  Max Time       : {metric.MaxDuration.TotalMilliseconds:F2}ms");

                if (metric.MemoryDelta > 0)
                    sb.AppendLine($"  Memory Delta   : {metric.MemoryDelta / 1024}KB");

                sb.AppendLine();
            }
        }
        finally
        {
            _lock.ExitReadLock();
        }

        return sb.ToString();
    }

    /// <summary>
    /// Clears all recorded metrics.
    /// </summary>
    public void Clear()
    {
        _lock.EnterWriteLock();
        try
        {
            _metrics.Clear();
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }
}

/// <summary>
/// Metrics for a single operation.
/// </summary>
public class OperationMetrics
{
    public string OperationName { get; set; } = string.Empty;
    public int Count { get; set; }
    public TimeSpan TotalDuration { get; set; }
    public TimeSpan MinDuration { get; set; } = TimeSpan.MaxValue;
    public TimeSpan MaxDuration { get; set; } = TimeSpan.Zero;
    public long MemoryDelta { get; set; }
    public List<TimeSpan> Measurements { get; set; } = new();

    public TimeSpan AverageDuration => Count > 0 ? TimeSpan.FromMilliseconds(TotalDuration.TotalMilliseconds / Count) : TimeSpan.Zero;
}

/// <summary>
/// Disposable timer for measuring operation duration.
/// </summary>
public class PerformationTimer : IDisposable
{
    private readonly string _operationName;
    private readonly PerformanceMetrics _metrics;
    private readonly DateTime _startTime;
    private readonly long _initialMemory;

    public PerformanceTimer(string operationName, PerformanceMetrics metrics)
    {
        _operationName = operationName;
        _metrics = metrics;
        _startTime = DateTime.UtcNow;
        _initialMemory = GC.GetTotalMemory(false);
    }

    public void Dispose()
    {
        var duration = DateTime.UtcNow - _startTime;
        var currentMemory = GC.GetTotalMemory(false);
        var memoryDelta = currentMemory - _initialMemory;

        _metrics.RecordMeasurement(_operationName, duration, memoryDelta);
    }
}
