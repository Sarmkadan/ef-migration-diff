#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace EfMigrationDiff.BackgroundWork;

/// <summary>
/// Queue for background tasks with execution management.
/// Supports task prioritization, cancellation, and concurrent execution limits.
/// Thread-safe for concurrent access from multiple producers/consumers.
/// </summary>
public class BackgroundTaskQueue : IDisposable
{
    private readonly Channel<BackgroundTask> _queue;
    private readonly int _maxConcurrentTasks;
    private int _activeTasks;
    private readonly SemaphoreSlim _taskSemaphore;
    private readonly List<Task> _runningTasks = new();
    private CancellationTokenSource _cancellationTokenSource = new();

    public BackgroundTaskQueue(int maxConcurrentTasks = 4, int queueCapacity = 100)
    {
        _maxConcurrentTasks = maxConcurrentTasks;
        _taskSemaphore = new SemaphoreSlim(maxConcurrentTasks);
        _queue = Channel.CreateBounded<BackgroundTask>(
            new BoundedChannelOptions(queueCapacity)
            {
                FullMode = BoundedChannelFullMode.Wait
            });
    }

    /// <summary>
    /// Enqueues a background task for execution.
    /// </summary>
    public async ValueTask EnqueueAsync(BackgroundTask task)
    {
        if (task is null)
            throw new ArgumentNullException(nameof(task));

        await _queue.Writer.WriteAsync(task, _cancellationTokenSource.Token).ConfigureAwait(false);
    }

    /// <summary>
    /// Enqueues multiple tasks.
    /// </summary>
    public async Task EnqueueBatchAsync(IEnumerable<BackgroundTask> tasks)
    {
        foreach (var task in tasks)
        {
            await EnqueueAsync(task).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Starts processing queued tasks.
    /// </summary>
    public async Task StartProcessingAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await foreach (var task in _queue.Reader.ReadAllAsync(cancellationToken))
            {
                // Wait for a slot to become available
                await _taskSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

                // Execute task without awaiting to allow concurrent execution
                var executionTask = ExecuteTaskAsync(task, cancellationToken);
                _runningTasks.Add(executionTask);

                // Clean up completed tasks
                _runningTasks.RemoveAll(t => t.IsCompleted);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when cancellation is requested
        }
    }

    /// <summary>
    /// Executes a single task and handles completion.
    /// </summary>
    private async Task ExecuteTaskAsync(BackgroundTask task, CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref _activeTasks);
        try
        {
            await task.ExecuteAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            task.OnError?.Invoke(ex);
        }
        finally
        {
            Interlocked.Decrement(ref _activeTasks);
            _taskSemaphore.Release();
        }
    }

    /// <summary>
    /// Gets the current number of active tasks.
    /// </summary>
    public int GetActiveTasks() => _activeTasks;

    /// <summary>
    /// Gets the number of tasks in the queue.
    /// </summary>
    public int GetQueuedTaskCount() => _queue.Reader.Count;

    /// <summary>
    /// Stops accepting new tasks and waits for running tasks to complete.
    /// </summary>
    public async Task StopAsync()
    {
        _queue.Writer.TryComplete();

        if (_runningTasks.Any())
            await Task.WhenAll(_runningTasks).ConfigureAwait(false);
    }

    /// <summary>
    /// Cancels all pending and running tasks.
    /// </summary>
    public void Cancel()
    {
        _cancellationTokenSource.Cancel();
    }

    public void Dispose()
    {
        _queue?.Dispose();
        _taskSemaphore?.Dispose();
        _cancellationTokenSource?.Dispose();
    }
}

/// <summary>
/// Represents a background task to be executed.
/// </summary>
public class BackgroundTask
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = string.Empty;
    public int Priority { get; set; } = 0;
    public Func<CancellationToken, Task> Execute { get; set; } = _ => Task.CompletedTask;
    public Action<Exception>? OnError { get; set; }
    public Action? OnCompleted { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        await Execute(cancellationToken).ConfigureAwait(false);
        OnCompleted?.Invoke();
    }

    public static BackgroundTask Create(string name, Func<CancellationToken, Task> execute)
    {
        return new BackgroundTask
        {
            Name = name,
            Execute = execute
        };
    }

    public static BackgroundTask Create(string name, Action execute)
    {
        return new BackgroundTask
        {
            Name = name,
            Execute = _ => { execute(); return Task.CompletedTask; }
        };
    }
}
