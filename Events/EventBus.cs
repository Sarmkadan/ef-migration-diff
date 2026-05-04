// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace EfMigrationDiff.Events;

/// <summary>
/// Event bus for publishing and subscribing to application events.
/// Supports both synchronous and asynchronous event handling with middleware-style pipeline.
/// Thread-safe for concurrent access from multiple handlers.
/// </summary>
public class EventBus : IDisposable
{
    private readonly Dictionary<Type, List<Delegate>> _subscribers = new();
    private readonly ReaderWriterLockSlim _lock = new();
    private readonly List<IEventMiddleware> _middlewares = new();

    /// <summary>
    /// Subscribes to an event type with a synchronous handler.
    /// </summary>
    public void Subscribe<TEvent>(Action<TEvent> handler) where TEvent : IEvent
    {
        if (handler == null)
            throw new ArgumentNullException(nameof(handler));

        _lock.EnterWriteLock();
        try
        {
            var eventType = typeof(TEvent);
            if (!_subscribers.ContainsKey(eventType))
                _subscribers[eventType] = new List<Delegate>();

            _subscribers[eventType].Add(handler);
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Subscribes to an event type with an async handler.
    /// </summary>
    public void Subscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : IEvent
    {
        if (handler == null)
            throw new ArgumentNullException(nameof(handler));

        _lock.EnterWriteLock();
        try
        {
            var eventType = typeof(TEvent);
            if (!_subscribers.ContainsKey(eventType))
                _subscribers[eventType] = new List<Delegate>();

            _subscribers[eventType].Add(handler);
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Unsubscribes a handler from an event type.
    /// </summary>
    public bool Unsubscribe<TEvent>(Delegate handler) where TEvent : IEvent
    {
        _lock.EnterWriteLock();
        try
        {
            var eventType = typeof(TEvent);
            if (_subscribers.TryGetValue(eventType, out var handlers))
            {
                return handlers.Remove(handler);
            }
            return false;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Publishes an event to all subscribed handlers synchronously.
    /// </summary>
    public async Task PublishAsync<TEvent>(TEvent @event) where TEvent : IEvent
    {
        if (@event == null)
            throw new ArgumentNullException(nameof(@event));

        // Run through middleware pipeline
        foreach (var middleware in _middlewares)
        {
            var continueProcessing = await middleware.OnEventPublishedAsync(@event);
            if (!continueProcessing)
                return;
        }

        _lock.EnterReadLock();
        try
        {
            var eventType = @event.GetType();
            if (_subscribers.TryGetValue(eventType, out var handlers))
            {
                var tasks = new List<Task>();

                foreach (var handler in handlers.ToList())
                {
                    try
                    {
                        if (handler is Action<TEvent> syncHandler)
                        {
                            syncHandler(@event);
                        }
                        else if (handler is Func<TEvent, Task> asyncHandler)
                        {
                            tasks.Add(asyncHandler(@event));
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log handler errors but continue processing
                        await OnHandlerErrorAsync(@event, handler, ex);
                    }
                }

                // Wait for all async handlers
                if (tasks.Any())
                    await Task.WhenAll(tasks);
            }
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <summary>
    /// Registers event middleware for pre/post processing.
    /// </summary>
    public void UseMiddleware(IEventMiddleware middleware)
    {
        if (middleware == null)
            throw new ArgumentNullException(nameof(middleware));

        _middlewares.Add(middleware);
    }

    /// <summary>
    /// Gets the count of subscribers for an event type.
    /// </summary>
    public int GetSubscriberCount<TEvent>() where TEvent : IEvent
    {
        _lock.EnterReadLock();
        try
        {
            var eventType = typeof(TEvent);
            return _subscribers.TryGetValue(eventType, out var handlers) ? handlers.Count : 0;
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <summary>
    /// Clears all subscribers for a specific event type.
    /// </summary>
    public void ClearSubscribers<TEvent>() where TEvent : IEvent
    {
        _lock.EnterWriteLock();
        try
        {
            _subscribers.Remove(typeof(TEvent));
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Clears all subscribers for all event types.
    /// </summary>
    public void ClearAllSubscribers()
    {
        _lock.EnterWriteLock();
        try
        {
            _subscribers.Clear();
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Called when an event handler throws an exception.
    /// Can be overridden to implement custom error handling.
    /// </summary>
    protected virtual Task OnHandlerErrorAsync(IEvent @event, Delegate handler, Exception exception)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"Error in event handler: {exception.Message}");
        Console.ResetColor();
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _lock?.Dispose();
    }
}

/// <summary>
/// Interface for events published through the event bus.
/// </summary>
public interface IEvent
{
    DateTime Timestamp { get; }
    string EventType { get; }
}

/// <summary>
/// Base class for implementing events with common properties.
/// </summary>
public abstract class EventBase : IEvent
{
    public DateTime Timestamp { get; } = DateTime.UtcNow;
    public string EventType => GetType().Name;
}

/// <summary>
/// Middleware for event processing pipeline.
/// </summary>
public interface IEventMiddleware
{
    /// <summary>
    /// Called when an event is published. Return false to stop processing.
    /// </summary>
    Task<bool> OnEventPublishedAsync(IEvent @event);
}
