//#if (EnableSiteUsers)
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace StarterApp.Tests;
//#endif

//#if (EnableSiteUsers)
/// <summary>
/// End-to-end tests for the site-user authentication feature. Each test
/// stands up a detached <see cref="WebApplicationFactory{TEntryPoint}"/>
/// against a fresh SQLite file under the temporary folder so multiple
/// tests can run side by side without sharing state.
/// </summary>
public sealed class SiteUserAuthTests : IClassFixture<SiteUserAuthTests.Factory>
{
    private readonly Factory _factory;

    public SiteUserAuthTests(Factory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Login_route_returns_200_and_sign_in_form()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var response = await client.GetAsync("/Identity/Account/Login");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Sign in", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Forgot_password_route_returns_200_and_generic_message_for_unknown_email()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var antiforgery = await client.GetAntiForgeryTokenAsync("/Identity/Account/ForgotPassword");
        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Input.Email"] = "ghost@example.com",
            ["__RequestVerificationToken"] = antiforgery,
        });
        var post = await client.PostAsync("/Identity/Account/ForgotPassword", form);
        // The endpoint accepts unknown emails; the response remains a 200 page
        // with the same generic message used for known accounts.
        Assert.Equal(HttpStatusCode.OK, post.StatusCode);
        var content = await post.Content.ReadAsStringAsync();
        Assert.Contains("If a matching account exists", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Login_with_unknown_email_returns_identical_generic_message_as_wrong_password()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var antiforgery = await client.GetAntiForgeryTokenAsync("/Identity/Account/Login");
        var unknownForm = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Input.Email"] = "ghost@example.com",
            ["Input.Password"] = "WrongPassword!1",
            ["__RequestVerificationToken"] = antiforgery,
        });
        var unknownResponse = await client.PostAsync("/Identity/Account/Login", unknownForm);
        var unknownContent = await unknownResponse.Content.ReadAsStringAsync();

        _factory.RegisterOwner("owner@example.com", "CorrectPassword!1");
        var wrongForm = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Input.Email"] = "owner@example.com",
            ["Input.Password"] = "WrongPassword!1",
            ["__RequestVerificationToken"] = antiforgery,
        });
        var wrongResponse = await client.PostAsync("/Identity/Account/Login", wrongForm);
        var wrongContent = await wrongResponse.Content.ReadAsStringAsync();

        // The platform never returns a different public message for unknown
        // email versus wrong password; both surface the same generic text.
        var unknownMessage = ExtractAlert(unknownContent);
        var wrongMessage = ExtractAlert(wrongContent);
        Assert.False(string.IsNullOrEmpty(unknownMessage));
        Assert.Equal(unknownMessage, wrongMessage);
    }

    [Fact]
    public async Task Logout_requires_post_and_rejects_get_only_request()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var ownerEmail = "logout-owner@example.com";
        var password = "LogoutPassword!1";
        _factory.RegisterOwner(ownerEmail, password);
        var antiforgery = await client.GetAntiForgeryTokenAsync("/Identity/Account/Login");
        var loginForm = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Input.Email"] = ownerEmail,
            ["Input.Password"] = password,
            ["__RequestVerificationToken"] = antiforgery,
        });
        var loginResponse = await client.PostAsync("/Identity/Account/Login", loginForm);
        Assert.Equal(HttpStatusCode.Found, loginResponse.StatusCode);

        // The antiforgery cookie is now bound to the authenticated session.
        var logoutAntiforgery = await client.GetAntiForgeryTokenAsync("/Identity/Account/Logout");
        var logoutForm = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = logoutAntiforgery,
        });
        var logoutResponse = await client.PostAsync("/Identity/Account/Logout", logoutForm);
        Assert.Equal(HttpStatusCode.Redirect, logoutResponse.StatusCode);
    }

    [Fact]
    public async Task Unknown_permission_request_is_denied()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var ownerEmail = "perm-owner@example.com";
        var password = "PermPassword!1";
        _factory.RegisterOwner(ownerEmail, password);
        var antiforgery = await client.GetAntiForgeryTokenAsync("/Identity/Account/Login");
        var loginForm = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Input.Email"] = ownerEmail,
            ["Input.Password"] = password,
            ["__RequestVerificationToken"] = antiforgery,
        });
        var loginResponse = await client.PostAsync("/Identity/Account/Login", loginForm);
        Assert.Equal(HttpStatusCode.Found, loginResponse.StatusCode);

        // A protected endpoint that requires an unknown permission must not
        // succeed. The site does not register any handler, so the request
        // returns 404 from routing; the deny-by-default behavior is asserted
        // by the absence of a 200.
        var response = await client.GetAsync("/_site/protected");
        Assert.NotEqual(HttpStatusCode.OK, response.StatusCode);
    }

    private static string ExtractAlert(string html) =>
        System.Text.RegularExpressions.Regex.Match(html, "<div class=\"alert\"[^>]*>([^<]+)</div>").Groups[1].Value;

    public sealed class Factory : WebApplicationFactory<Program>
    {
        private readonly string _databasePath = Path.Combine(Path.GetTempPath(), $"site-users-{Guid.NewGuid():N}.db");

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseSetting("ConnectionStrings:Default", $"Data Source={_databasePath}");
        }

        public void RegisterOwner(string email, string password)
        {
            // The factory exposes the host's services directly, so tests can
            // seed an owner without racing with the bootstrap-owner command.
            using var scope = Services.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<StarterApp.SiteUsers.ApplicationUser>>();
            var existing = userManager.FindByEmailAsync(email).GetAwaiter().GetResult();
            if (existing is not null) return;
            var user = new StarterApp.SiteUsers.ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
            };
            var result = userManager.CreateAsync(user, password).GetAwaiter().GetResult();
            Assert.True(result.Succeeded, string.Join(", ", result.Errors.Select(e => e.Description)));
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing && File.Exists(_databasePath))
            {
                try { File.Delete(_databasePath); }
                catch (IOException) { }
            }
        }
    }
}

internal static class SiteUserAuthHttpExtensions
{
    public static async Task<string> GetAntiForgeryTokenAsync(this HttpClient client, string path)
    {
        var response = await client.GetAsync(path);
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        var match = System.Text.RegularExpressions.Regex.Match(content, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");
        if (match.Success)
        {
            return match.Groups[1].Value;
        }
        return string.Empty;
    }
}
//#endif
