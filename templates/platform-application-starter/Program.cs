using Platform.Starter.DependencyInjection;
//#if (EnablePersistence)
using Microsoft.EntityFrameworkCore;
using Platform.Persistence.EfCore.DependencyInjection;
using Platform.Persistence.EfCore.Interceptors;
using StarterApp.Data;
//#endif

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddPlatformApplication(options =>
{
//#if (EnableIdentity)
    options.EnableIdentity = true;
//#endif
    options.EnableAdmin = false;
});
//#if (EnablePersistence)
builder.Services.AddPlatformPersistenceEfCore();
builder.Services.AddDbContext<AppDbContext>((provider, options) =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Default"))
        .AddInterceptors(provider.GetRequiredService<PlatformSaveChangesInterceptor>()));
//#endif

var app = builder.Build();
app.UsePlatformApplication();
app.MapGet("/", () => Results.Json(new { application = "StarterApp", status = "ok" }));
app.MapPlatformApplicationEndpoints();
app.Run();

/// <summary>Entry point marker for integration hosting.</summary>
public partial class Program;
