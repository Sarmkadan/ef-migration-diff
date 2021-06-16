# ConfigurationBuilder

A builder class for configuring dependency injection services and application settings in .NET applications, particularly for setting up command execution pipelines and middleware components.

## API

### `WithAppSettings`
Configures the builder to load application settings from the `appsettings.json` file.
- **Parameters**: None
- **Return value**: `ConfigurationBuilder` (the current instance for method chaining)
- **Throws**: `FileNotFoundException` if `appsettings.json` is missing or inaccessible.

### `AddCommand`
Registers a command type with the dependency injection container.
- **Parameters**: `Type commandType` – the concrete command type to register.
- **Return value**: `ConfigurationBuilder` (the current instance for method chaining)
- **Throws**: `ArgumentNullException` if `commandType` is `null` or not a valid command type.

### `AddMiddleware`
Registers a middleware component with the dependency injection container.
- **Parameters**: `Type middlewareType` – the concrete middleware type to register.
- **Return value**: `ConfigurationBuilder` (the current instance for method chaining)
- **Throws**: `ArgumentNullException` if `middlewareType` is `null` or not a valid middleware type.

### `AddCommandValidator`
Registers a validator for a command type with the dependency injection container.
- **Parameters**: `Type validatorType` – the concrete validator type to register.
- **Return value**: `ConfigurationBuilder` (the current instance for method chaining)
- **Throws**: `ArgumentNullException` if `validatorType` is `null` or not a valid validator type.

### `AddLogging`
Registers logging services with the dependency injection container.
- **Parameters**: None
- **Return value**: `ConfigurationBuilder` (the current instance for method chaining)
- **Throws**: None

### `AddValidation`
Registers validation services with the dependency injection container.
- **Parameters**: None
- **Return value**: `ConfigurationBuilder` (the current instance for method chaining)
- **Throws**: None

### `AddErrorHandling`
Registers error handling services with the dependency injection container.
- **Parameters**: None
- **Return value**: `ConfigurationBuilder` (the current instance for method chaining)
- **Throws**: None

### `AddSingleton<T>`
Registers a singleton service of type `T` with the dependency injection container.
- **Parameters**:
  - `Type serviceType` – the service type to register.
  - `object? instance` – an optional instance to use as the singleton.
  - `Delegate? factory` – an optional factory delegate to create the instance.
- **Return value**: `ConfigurationBuilder` (the current instance for method chaining)
- **Throws**:
  - `ArgumentNullException` if `serviceType` is `null`.
  - `InvalidOperationException` if both `instance` and `factory` are provided.

### `(CommandExecutor executor, IServiceProvider services) Build`
Finalizes the configuration and constructs a `CommandExecutor` with the configured services.
- **Parameters**:
  - `CommandExecutor executor` – the base executor to configure.
  - `IServiceProvider services` – the service provider containing registered services.
- **Return value**: A tuple `(CommandExecutor executor, IServiceProvider services)` with the configured executor and service provider.
- **Throws**: `ArgumentNullException` if `executor` or `services` is `null`.

### `BuildServiceProvider`
Constructs and returns an `IServiceProvider` with all registered services.
- **Parameters**: None
- **Return value**: `ServiceProvider` – the configured service provider.
- **Throws**: `InvalidOperationException` if required services are missing or misconfigured.

### `ServiceType`
Gets the service type of the current service descriptor.
- **Parameters**: None
- **Return value**: `Type` – the service type.
- **Throws**: None

### `Instance`
Gets the instance of the current service descriptor.
- **Parameters**: None
- **Return value**: `object?` – the registered instance, if any.
- **Throws**: None

### `Factory`
Gets the factory delegate of the current service descriptor.
- **Parameters**: None
- **Return value**: `Delegate?` – the factory delegate, if any.
- **Return value**: `ServiceDescriptor` – the service descriptor.
- **Throws**: None

### `GetService`
Retrieves a service instance from the service provider.
- **Parameters**: `Type serviceType` – the type of service to retrieve.
- **Return value**: `object?` – the service instance, or `null` if not found.
- **Throws**: `ArgumentNullException` if `serviceType` is `null`.

## Usage

### Example 1: Basic Command Configuration
