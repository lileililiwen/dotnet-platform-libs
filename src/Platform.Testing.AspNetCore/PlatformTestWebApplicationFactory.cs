using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Platform.Testing.AspNetCore;

/// <summary>
/// Configuration callback used by <see cref="PlatformTestWebApplicationFactory"/>.
/// The platform passes the service collection and configuration builder
/// so callers can swap services for fakes or extend the configuration
/// surface.
/// </summary>
public delegate void PlatformTestHostConfiguration(IServiceCollection services, IConfigurationBuilder configuration);

/// <summary>
/// Test host factory. Builds an in-memory <see cref="IHost"/> with
/// <see cref="TestServer"/> as the server, applies in-memory
/// configuration, and exposes a fluent API to replace services with
/// fakes. Tests should construct the factory once per scenario and
/// dispose it with <c>await using</c>.
/// </summary>
public sealed class PlatformTestWebApplicationFactory : IAsyncDisposable
{
    private readonly List<PlatformTestHostConfiguration> _configurations = new();
    private readonly Dictionary<string, string?> _inMemoryConfiguration = new(StringComparer.OrdinalIgnoreCase);
    private WebApplication? _host;

    /// <summary>Gets the test host. Built lazily on first access.</summary>
    public IHost Host => _host ??= BuildHost();

    /// <summary>Gets the <see cref="IServiceProvider"/> from the test host.</summary>
    public IServiceProvider Services => Host.Services;

    /// <summary>Adds an in-memory configuration value visible to the host.</summary>
    public PlatformTestWebApplicationFactory WithConfiguration(string key, string? value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        _inMemoryConfiguration[key] = value;
        _host = null;
        return this;
    }

    /// <summary>
    /// Registers a configuration callback that runs after the host
    /// has been built. Use this to swap platform services for fakes
    /// (e.g. <c>IEventBus</c> -&gt; <c>RecordingEventBus</c>).
    /// </summary>
    public PlatformTestWebApplicationFactory ConfigureTestServices(PlatformTestHostConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        _configurations.Add(configuration);
        _host = null;
        return this;
    }

    /// <summary>Returns a test client wired to the in-memory server.</summary>
    public HttpClient CreateClient() => Host.GetTestClient();

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_host is not null)
        {
            await _host.StopAsync().ConfigureAwait(false);
            await _host.DisposeAsync().ConfigureAwait(false);
        }
    }

    private WebApplication BuildHost()
    {
        var options = new WebApplicationOptions
        {
            EnvironmentName = PlatformTestEnvironments.Testing,
            ContentRootPath = AppContext.BaseDirectory,
        };
        var builder = WebApplication.CreateBuilder(options);
        builder.WebHost.UseTestServer();
        builder.Configuration.AddInMemoryCollection(_inMemoryConfiguration);
        foreach (var configuration in _configurations)
        {
            configuration(builder.Services, builder.Configuration);
        }
        var app = builder.Build();
        app.MapGet("/", () => Microsoft.AspNetCore.Http.Results.Ok("ok"));
        app.Start();
        return app;
    }
}

/// <summary>Stable, public environment names used by the testing toolkit.</summary>
public static class PlatformTestEnvironments
{
    /// <summary>Environment name used by <see cref="PlatformTestWebApplicationFactory"/>.</summary>
    public const string Testing = "Testing";
}
