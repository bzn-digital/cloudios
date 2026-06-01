using Bzn.Cloudios.Infrastructure.Persistence;
using Bzn.Cloudios.Infrastructure.Services;
using Bzn.Cloudios.WebAPI.Serialization;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// --- JSON Serialization (AOT-safe, no reflection) ---
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolver = CloudiosJsonSerializerContext.Default;
});

// --- Database (SQLite with PRAGMA interceptor) ---
var mainDbPath = builder.Configuration["ConnectionStrings:MainDb"] ?? "Data Source=cloudios_main.db;Mode=ReadWriteCreate;Cache=Shared";
var metricsDbPath = builder.Configuration["ConnectionStrings:MetricsDb"] ?? "Data Source=cloudios_metrics.db;Mode=ReadWriteCreate;Cache=Shared";
var pragmaInterceptor = new SqlitePragmaInterceptor();

builder.Services.AddDbContext<CloudiosDbContext>(options =>
    options.UseSqlite(mainDbPath).AddInterceptors(pragmaInterceptor));
builder.Services.AddDbContext<MetricsDbContext>(options =>
    options.UseSqlite(metricsDbPath).AddInterceptors(pragmaInterceptor));
builder.Services.AddScoped<DatabaseSeeder>();

// --- Authentication & Authorization ---
builder.Services.AddAuthentication()
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration["Jwt:Authority"];
        options.Audience = builder.Configuration["Jwt:Audience"];
    });
builder.Services.AddAuthorization();

// --- YARP Reverse Proxy ---
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

// --- Database seeding ---
using (var scope = app.Services.CreateScope())
{
    var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
    var adminEmail = builder.Configuration["Admin:Email"] ?? "admin@cloudios.local";
    var adminPasswordHash = builder.Configuration["Admin:PasswordHash"] ?? string.Empty;
    await seeder.SeedAsync(adminEmail, adminPasswordHash);
}

// --- Middleware pipeline ---
app.UseAuthentication();
app.UseAuthorization();

// --- Static files for Blazor WASM (Client panel) ---
app.UseStaticFiles();

// --- Static files for Admin panel (WebPlatform) ---
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(
        Path.Combine(builder.Environment.ContentRootPath, "wwwroot", "admin")),
    RequestPath = "/admin"
});

// --- YARP ---
app.MapReverseProxy();

// --- Health check endpoint ---
app.MapGet("/health", () =>
{
    return Results.Ok(new { status = "Healthy", version = "0.1.0" });
});

// --- Fallback: Client panel (WebApp) ---
app.MapFallbackToFile("index.html");

// --- Fallback: Admin panel (WebPlatform) ---
app.MapFallbackToFile("/admin/{**path}", "admin/index.html");

app.Run();
