using System.Text;
using Bzn.Cloudios.Application.Abstractions;
using Bzn.Cloudios.Application.Services;
using Bzn.Cloudios.Infrastructure.Persistence;
using Bzn.Cloudios.Infrastructure.Services;
using Bzn.Cloudios.WebAPI.Endpoints;
using Bzn.Cloudios.WebAPI.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

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

// --- Authentication (JWT Bearer with symmetric key) ---
var jwtKey = builder.Configuration["Jwt:Key"] ?? "SuperSecretKey_ReplaceInProduction_32Chars!";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "cloudios";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "cloudios-api";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

// --- Authorization Policies ---
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("RequirePlatformAdmin", policy =>
        policy.RequireRole("PlatformAdmin"))
    .AddPolicy("RequirePlatformUser", policy =>
        policy.RequireRole("PlatformAdmin", "PlatformUser", "PlatformSre"))
    .AddPolicy("RequireRealmOwner", policy =>
        policy.RequireRole("PlatformAdmin", "RealmOwner"))
    .AddPolicy("RequireRealmMember", policy =>
        policy.RequireRole("PlatformAdmin", "PlatformUser", "PlatformSre",
            "RealmOwner", "RealmAdmin", "RealmUser", "RealmSre"));

// --- Application Services ---
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ITenantProvider, JwtTenantProvider>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<RealmService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddSingleton<DockerNetworkService>();
builder.Services.AddScoped<IContainerService, ContainerService>();
builder.Services.AddScoped<ContainerCrudService>();

// --- YARP Reverse Proxy ---
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

// --- Database seeding ---
using (var scope = app.Services.CreateScope())
{
    var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
    var adminEmail = builder.Configuration["Admin:Email"] ?? "admin@cloudios.local";
    var adminPassword = builder.Configuration["Admin:Password"] ?? "Admin@123";
    await seeder.SeedAsync(adminEmail, adminPassword);

    // --- Docker network + state sync ---
    var dockerNetwork = scope.ServiceProvider.GetRequiredService<DockerNetworkService>();
    await dockerNetwork.EnsureNetworkAsync();

    var containerService = scope.ServiceProvider.GetRequiredService<IContainerService>();
    await containerService.SynchronizeStateAsync();
}

// --- Middleware pipeline ---
app.UseAuthentication();
app.UseAuthorization();

// --- API Endpoints ---
app.MapAuthEndpoints();
app.MapRealmEndpoints();
app.MapUserEndpoints();
app.MapContainerEndpoints();

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
