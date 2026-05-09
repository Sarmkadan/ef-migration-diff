#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using EfMigrationDiff.Extensions;
using EfMigrationDiff.Repositories;
using EfMigrationDiff.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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

        // Register configuration
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
    public static Microsoft.Extensions.DependencyInjection.ServiceProvider CreateServiceProvider(Action<AppSettings> configureSettings)
    {
        IServiceCollection services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
        services.AddApplicationServices(configureSettings);
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
