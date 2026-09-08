using Platform.Starter.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddPlatformApplication(options =>
{
    options.EnableIdentity = true;
    options.EnableAdmin = false;
});

var app = builder.Build();
app.UsePlatformApplication();
app.MapGet("/", () => Results.Json(new { application = "StarterApp", client = "__CLIENT__" }));
app.MapPlatformApplicationEndpoints();
app.Run();
