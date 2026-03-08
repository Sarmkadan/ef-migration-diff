#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using EfMigrationDiff.Repositories;
using EfMigrationDiff.Services;
using Microsoft.Extensions.DependencyInjection;

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

        // Register services
        services.AddSingleton<ConflictDetectionService>();
        services.AddSingleton<SchemaChangeDetectorService>();
        services.AddSingleton<MigrationParserService>();
        services.AddSingleton<MigrationDiffService>();
        services.AddSingleton<ReportGenerationService>();

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

        services.Configure(configureOptions);

        return services;
    }

    /// <summary>
    /// Creates a service provider with all required services.
    /// </summary>
    public static ServiceProvider CreateServiceProvider(string repositoryPath)
    {
        var services = new ServiceCollection();

        services.AddApplicationServices(settings =>
        {
            settings.RepositoryPath = repositoryPath;
        });

        return services.BuildServiceProvider();
    }

    /// <summary>
    /// Creates a service provider with custom settings.
    /// </summary>
    public static ServiceProvider CreateServiceProvider(Action<AppSettings> configureSettings)
    {
        var services = new ServiceCollection();
        services.AddApplicationServices(configureSettings);
        return services.BuildServiceProvider();
    }

    /// <summary>
    /// Gets a specific service from the provider.
    /// </summary>
    public static T? GetService<T>(this ServiceProvider provider) where T : class
    {
        return provider.GetService(typeof(T)) as T;
    }
}
