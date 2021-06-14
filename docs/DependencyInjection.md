# DependencyInjection

The `DependencyInjection` static class provides helper methods for configuring and retrieving services in the EF migration diff tool. It simplifies service collection setup, service provider creation, and service resolution, allowing application code to compose dependencies with minimal boilerplate.

## API

### AddApplicationServices(IServiceCollection services)
Registers the core application services (e.g., DbContext, logging, configuration) into the supplied service collection. Returns the same `IServiceCollection` instance to enable fluent chaining. Throws `ArgumentNullException` if `services` is `null`.

### AddApplicationServices(IServiceCollection services, IConfiguration configuration)
Registers core application services while using the provided `IConfiguration` to configure options such as connection strings. Returns the same `IServiceCollection`. Throws `ArgumentNullException` if either `services` or `configuration` is `null`.

### CreateServiceProvider(IServiceCollection services)
Builds a new `ServiceProvider` from the service collection after the application services have been registered. Returns the created `ServiceProvider`. Throws `ArgumentNullException` if `services` is `null`; may throw `InvalidOperationException` if required services are missing.

### CreateServiceProvider(IServiceCollection services, Action<DbContextOptionsBuilder> optionsAction)
Builds a `ServiceProvider` after applying additional DbContext options via `optionsAction`. Returns the created `ServiceProvider`. Throws `ArgumentNullException` if `services` or `optionsAction` is `null`.

### CreateServiceProviderWithOptions(IServiceCollection services, IConfiguration configuration)
Builds a `ServiceProvider` after reading options (e.g., provider-specific settings) from the supplied `IConfiguration`. Returns the created `ServiceProvider`. Throws `ArgumentNullException` if `services` or `configuration` is `null`.

### GetService<T>(IServiceProvider provider)
Attempts to retrieve a service of type `T` from the given `IServiceProvider`. Returns the service instance, or `null` if the service is not registered. For reference types this is `null`; for nullable value types it is `null` as well. Throws `ArgumentNullException` if `provider` is `null`.

## Usage

```csharp
using Microsoft.Extensions.DependencyInjection;
using EfMigrationDiff.DependencyInjection;

// Basic service collection setup
var services = new ServiceCollection();
services.AddApplicationServices(); // overload without configuration
var provider = services.CreateServiceProvider();

// Resolve a service
var logger = provider.GetService<ILogger<Program>>();
```

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using EfMigrationDiff.DependencyInjection;

var services = new ServiceCollection();
// Register services with configuration
services.AddApplicationServices(services, configuration);
// Provide DbContext options
var provider = services.CreateServiceProvider(options =>
{
    options.UseSqlServer(configuration.GetConnectionString("Default"));
});

// Use resolved services
var dbContext = provider.GetService<MyDbContext>();
```

## Notes

- All methods are safe for concurrent invocation as long as the supplied `IServiceCollection` instance is not being modified simultaneously by another thread.
- The `CreateServiceProvider` overloads assume that all necessary services have been registered beforehand; invoking them prior to registration may result in missing services and resolution failures.
- `GetService<T>` follows the .NET convention of returning `null` when a service is not present, rather than throwing. Callers should verify the result before use.
- The class contains no static state; each method operates solely on its parameters, making it thread‑safe for stateless usage.
- Passing a `null` argument to any method results in an `ArgumentNullException`; no default values are substituted.
