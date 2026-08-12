#nullable enable
using EfMigrationDiff.CLI;
using EfMigrationDiff.Middleware;

namespace EfMigrationDiff.Configuration;

/// <summary>
/// Fluent builder for configuring the application with commands, middleware, and services.
/// Simplifies dependency injection setup and command registration.
/// </summary>
public class ConfigurationBuilder
{
    private readonly ServiceCollection _services = new();
    private readonly CommandExecutor _commandExecutor = new();
    private readonly ValidationMiddleware _validationMiddleware = new();
    private AppSettings _appSettings = new();

    /// <summary>
    /// Adds AppSettings configuration.
    /// </summary>
    public ConfigurationBuilder WithAppSettings(Action<AppSettings> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        configure(_appSettings);
        _services.AddSingleton(_appSettings);
        return this;
    }

    /// <summary>
    /// Registers a command with the executor.
    /// </summary>
    public ConfigurationBuilder AddCommand(string name, ICommand command)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentNullException.ThrowIfNull(command);
        _commandExecutor.RegisterCommand(name, command);
        return this;
    }

    /// <summary>
    /// Registers command middleware.
    /// </summary>
    public ConfigurationBuilder AddMiddleware(ICommandMiddleware middleware)
    {
        ArgumentNullException.ThrowIfNull(middleware);
        _commandExecutor.RegisterMiddleware(middleware);
        return this;
    }

    /// <summary>
    /// Adds validation for a specific command.
    /// </summary>
    public ConfigurationBuilder AddCommandValidator(string commandName, Action<CommandValidator> configure)
    {
        ArgumentException.ThrowIfNullOrEmpty(commandName);
        ArgumentNullException.ThrowIfNull(configure);
        var validator = new CommandValidator();
        configure(validator);
        _validationMiddleware.RegisterValidator(commandName, validator);
        return this;
    }

    /// <summary>
    /// Registers logging middleware.
    /// </summary>
    public ConfigurationBuilder AddLogging(bool verbose = false)
    {
        ArgumentException.ThrowIfNullOrEmpty(verbose.ToString());
        var logging = new RequestLoggingMiddleware(isVerbose: verbose);
        _commandExecutor.RegisterMiddleware(logging);
        return this;
    }

    /// <summary>
    /// Registers validation middleware.
    /// </summary>
    public ConfigurationBuilder AddValidation()
    {
        _commandExecutor.RegisterMiddleware(_validationMiddleware);
        return this;
    }

    /// <summary>
    /// Registers error handling middleware.
    /// </summary>
    public ConfigurationBuilder AddErrorHandling(bool includeStackTrace = false)
    {
        ArgumentException.ThrowIfNullOrEmpty(includeStackTrace.ToString());
        var errorHandler = new ErrorHandlingMiddleware(includeStackTrace);
        // Error handling is typically done in CommandExecutor, not as middleware
        return this;
    }

    /// <summary>
    /// Adds a singleton service.
    /// </summary>
    public ConfigurationBuilder AddSingleton<T>(T instance) where T : class
    {
        ArgumentNullException.ThrowIfNull(instance);
        _services.AddSingleton(instance);
        return this;
    }

    /// <summary>
    /// Adds a singleton service with factory.
    /// </summary>
    public ConfigurationBuilder AddSingleton<T>(Func<IServiceProvider, T> factory) where T : class
    {
        _services.AddSingleton(factory);
        return this;
    }

    /// <summary>
    /// Builds the configuration and returns the command executor and service provider.
    /// </summary>
    public (CommandExecutor executor, IServiceProvider services) Build()
    {
        var serviceProvider = _services.BuildServiceProvider();
        return (_commandExecutor, serviceProvider);
    }
}

/// <summary>
/// Simple service collection for dependency injection.
/// </summary>
public class ServiceCollection
{
    private readonly Dictionary<Type, ServiceDescriptor> _services = new();

    public void AddSingleton<T>(T instance) where T : class
    {
        _services[typeof(T)] = new ServiceDescriptor(typeof(T), instance);
    }

    public void AddSingleton<T>(Func<IServiceProvider, T> factory) where T : class
    {
        _services[typeof(T)] = new ServiceDescriptor(typeof(T), factory);
    }

    public ServiceProvider BuildServiceProvider()
    {
        return new ServiceProvider(_services);
    }
}

/// <summary>
/// Service descriptor for DI.
/// </summary>
public class ServiceDescriptor
{
    public Type ServiceType { get; set; }
    public object? Instance { get; set; }
    public Delegate? Factory { get; set; }

    public ServiceDescriptor(Type serviceType, object instance)
    {
        ServiceType = serviceType;
        Instance = instance;
    }

    public ServiceDescriptor(Type serviceType, Delegate factory)
    {
        ServiceType = serviceType;
        Factory = factory;
    }

    public override string ToString() => $"ServiceDescriptor {{ ServiceType = {ServiceType}, Instance = {Instance}, Factory = {Factory} }}";
}

/// <summary>
/// Simple service provider for dependency resolution.
/// </summary>
public class ServiceProvider : IServiceProvider
{
    private readonly Dictionary<Type, ServiceDescriptor> _services;

    public ServiceProvider(Dictionary<Type, ServiceDescriptor> services)
    {
        _services = services;
    }

    public object? GetService(Type serviceType)
    {
        if (_services.TryGetValue(serviceType, out var descriptor))
        {
            if (descriptor.Instance is not null)
                return descriptor.Instance;

            if (descriptor.Factory is not null)
            {
                return descriptor.Factory.DynamicInvoke(this);
            }
        }

        return null;
    }
}
