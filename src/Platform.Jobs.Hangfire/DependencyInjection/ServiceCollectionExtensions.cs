using Hangfire;
using Hangfire.InMemory;
using Hangfire.PostgreSql;
using Hangfire.PostgreSql.Factories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Platform.Core.Time;

namespace Platform.Jobs.Hangfire.DependencyInjection;

/// <summary>
/// Opt-in registration for the Hangfire jobs adapter. The registration
/// never overwrites an application-owned <see cref="IJobDispatcher"/> or
/// <see cref="IRecurringJobRegistry"/>: the adapter implementations are
/// added with <c>TryAdd</c> so consumer registrations made before this call
/// always win. Calling the registration twice is a no-op.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the validated <see cref="HangfireJobsOptions"/>, wires the
    /// application-selected Hangfire storage, the scoped job activator, the
    /// context-capture filter, the Hangfire server, and the platform
    /// dispatcher/registry/health seams.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">The optional configuration delegate.</param>
    /// <returns>The same <paramref name="services"/> for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">The configured options are invalid.</exception>
    public static IServiceCollection AddPlatformHangfireJobs(
        this IServiceCollection services,
        Action<HangfireJobsOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        if (services.Any(descriptor => descriptor.ServiceType == typeof(HangfireJobsRegistrationMarker)))
        {
            return services;
        }

        Action<HangfireJobsOptions> configureDelegate = configure ?? DefaultConfigure;
        var validated = new HangfireJobsOptions();
        configureDelegate(validated);
        validated.Validate();

        services.AddOptions<HangfireJobsOptions>().Configure(configureDelegate);
        services.AddSingleton(new HangfireJobsRegistrationMarker());

        services.TryAddSingleton<IClock>(_ => new SystemClock());

        services.AddHangfire((provider, configuration) =>
        {
            // The storage is created inside the configuration callback so it
            // is constructed after Hangfire has bound the host's log
            // provider; Hangfire resolves JobStorage lazily from
            // JobStorage.Current once the configuration has run.
            configuration.UseStorage(CreateStorage(validated));
            configuration.UseActivator(new ScopedJobActivator(provider.GetRequiredService<IServiceScopeFactory>()));
            configuration.UseFilter(new JobContextCaptureFilter(provider));
        });

        services.AddHangfireServer(serverOptions =>
        {
            serverOptions.WorkerCount = validated.WorkerCount;
            serverOptions.Queues = validated.Queues.ToArray();
            serverOptions.SchedulePollingInterval = validated.SchedulePollingInterval;
            serverOptions.HeartbeatInterval = validated.HeartbeatInterval;
        });

        services.TryAddTransient<HangfireJobExecutor>();
        services.TryAddSingleton<IJobDispatcher, HangfireJobDispatcher>();
        services.TryAddSingleton<IRecurringJobRegistry, HangfireRecurringJobRegistry>();
        services.TryAddSingleton<IHealthCheck, HangfireStorageHealthCheck>();

        return services;
    }

    private static void DefaultConfigure(HangfireJobsOptions options)
    {
    }

    private static JobStorage CreateStorage(HangfireJobsOptions options) => options.Storage switch
    {
        HangfireStorageKind.InMemory => new InMemoryStorage(),
        HangfireStorageKind.PostgreSql => new PostgreSqlStorage(
            new NpgsqlConnectionFactory(options.PostgreSqlConnectionString!, new PostgreSqlStorageOptions(), null),
            new PostgreSqlStorageOptions()),
        _ => throw new ArgumentOutOfRangeException(nameof(options), options.Storage, "The Hangfire storage kind is not supported."),
    };

    private sealed class HangfireJobsRegistrationMarker
    {
    }
}
