#nullable enable
using EfMigrationDiff.Extensions;
using EfMigrationDiff.Repositories;
using EfMigrationDiff.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EfMigrationDiff.Configuration;

/// <summary>
/// Dependency injection configuration for the application.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers all application services and repositories.
    /// </summary>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Register repositories
        services.AddSingleton<MigrationRepository>();
        services.AddSingleton<DbContextRepository>();
        services.AddTransient<GitRepository>();

        // Register logging (required by MigrationAutoResolverService)
        services.AddLogging(configure => configure.AddConsole());

        // Register services
        services.AddSingleton<ConflictDetectionService>();
        services.AddSingleton<SchemaChangeDetectorService>();
        services.AddSingleton<MigrationParserService>();
        services.AddSingleton<MigrationDiffService>();
        services.AddSingleton<ReportGenerationService>();
        services.AddSingleton<MigrationDependencyGraphService>();
        services.AddSingleton<MigrationAutoResolverService>();

        // Register visual diff v2 services
        services.AddSchemaDiffServices();
        services.AddSchemaDiffPipeline();

        // Register configuration with IOptions pattern
        services.AddOptions<EfMigrationDiffOptions>()
            .BindConfiguration("EfMigrationDiff")
            .ValidateOnStart();

        services.AddSingleton<AppSettings>();

        return services;
    }

    /// <summary>
    /// Registers application services with custom configuration.
    /// </summary>
    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services,
        Action<AppSettings> configureOptions)
    {
        services.AddApplicationServices();

        var settings = new AppSettings();
        configureOptions(settings);
        services.AddSingleton(settings);

        return services;
    }

    /// <summary>
    /// Creates a service provider with all required services.
    /// </summary>
    /// <param name="repositoryPath">The path to the repository.</param>
    /// <returns>A configured service provider.</returns>
    public static Microsoft.Extensions.DependencyInjection.ServiceProvider CreateServiceProvider(string repositoryPath)
    {
        IServiceCollection services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();

        services.AddApplicationServices(settings =>
        {
            settings.RepositoryPath = repositoryPath;
        });

        return services.BuildServiceProvider();
    }

    /// <summary>
    /// Creates a service provider with custom settings.
    /// </summary>
    /// <param name="configureSettings">Action to configure settings.</param>
    /// <returns>A configured service provider.</returns>
    public static Microsoft.Extensions.DependencyInjection.ServiceProvider CreateServiceProvider(Action<AppSettings> configureSettings)
    {
        IServiceCollection services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
        services.AddApplicationServices(configureSettings);
        return services.BuildServiceProvider();
    }

    /// <summary>
    /// Creates a service provider with IOptions configuration.
    /// </summary>
    /// <param name="configureOptions">Action to configure EfMigrationDiffOptions.</param>
    /// <returns>A configured service provider.</returns>
    public static Microsoft.Extensions.DependencyInjection.ServiceProvider CreateServiceProviderWithOptions(
        Action<EfMigrationDiffOptions> configureOptions)
    {
        IServiceCollection services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();

        services.AddOptions<EfMigrationDiffOptions>()
            .BindConfiguration("EfMigrationDiff")
            .ValidateOnStart();

        configureOptions?.Invoke(new EfMigrationDiffOptions());

        services.AddApplicationServices();
        services.AddSingleton<AppSettings>();

        return services.BuildServiceProvider();
    }

    /// <summary>
    /// Gets a specific service from the provider.
    /// </summary>
    public static T? GetService<T>(this Microsoft.Extensions.DependencyInjection.ServiceProvider provider) where T : class
    {
        return provider.GetService(typeof(T)) as T;
    }
}
