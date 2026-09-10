using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Platform.Testing.AspNetCore;

namespace Platform.Testing.Tests.AspNetCore;

public sealed class PlatformTestWebApplicationFactoryTests
{
    [Fact]
    public async Task Factory_provides_test_server_with_default_testing_environment()
    {
        await using var factory = new PlatformTestWebApplicationFactory();

        var environment = factory.Services.GetRequiredService<IHostEnvironment>();
        Assert.Equal(PlatformTestEnvironments.Testing, environment.EnvironmentName);
    }

    [Fact]
    public async Task In_memory_configuration_is_visible_to_the_host()
    {
        await using var factory = new PlatformTestWebApplicationFactory()
            .WithConfiguration("Platform:Test", "ok");

        var configuration = factory.Services.GetRequiredService<IConfiguration>();
        Assert.Equal("ok", configuration["Platform:Test"]);
    }

    [Fact]
    public async Task Test_services_callback_can_replace_registered_services()
    {
        var marker = new ServiceMarker();
        await using var factory = new PlatformTestWebApplicationFactory()
            .ConfigureTestServices((services, _) =>
            {
                services.AddSingleton(marker);
            });

        var resolved = factory.Services.GetRequiredService<ServiceMarker>();
        Assert.Same(marker, resolved);
    }

    [Fact]
    public async Task CreateClient_returns_a_test_client_against_the_in_memory_server()
    {
        await using var factory = new PlatformTestWebApplicationFactory();
        using var client = factory.CreateClient();
        var response = await client.GetAsync("/");
        response.EnsureSuccessStatusCode();
        var body = (await response.Content.ReadAsStringAsync()).Trim('"');
        Assert.Equal("ok", body);
    }

    [Fact]
    public void Static_environment_constant_is_Testing()
    {
        Assert.Equal("Testing", PlatformTestEnvironments.Testing);
    }

    private sealed class ServiceMarker
    {
    }
}
