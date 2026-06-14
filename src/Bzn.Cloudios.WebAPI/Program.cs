using System.Text;
using Bzn.Cloudios.Application.Abstractions;
using Bzn.Cloudios.Application.Events;
using Bzn.Cloudios.Application.Services;
using Bzn.Cloudios.Infrastructure.Persistence;
using Bzn.Cloudios.Infrastructure.Services;
using Bzn.Cloudios.WebAPI.Endpoints;
using Bzn.Cloudios.WebAPI.Serialization;
using Bzn.Cloudios.WebAPI.Services;
using Docker.DotNet;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Yarp.ReverseProxy.Configuration;

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

// --- CORS for React apps ---
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApps", policy =>
    {
        policy.WithOrigins("http://localhost:5173", "http://localhost:5174", "http://localhost")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// --- Authentication (JWT Bearer with symmetric key) ---
var jwtKey = builder.Configuration["Jwt:Key"] ?? "YourSuperSecretKeyForDevelopmentOnly12345678901234567890";
var keyBytes = Encoding.UTF8.GetBytes(jwtKey);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
        ClockSkew = TimeSpan.Zero
    };
});

// --- Authorization Policies ---
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("PlatformAdmin", policy => policy.RequireRole("PlatformAdmin"));
    options.AddPolicy("RealmOwner", policy => policy.RequireRole("RealmOwner"));
    options.AddPolicy("RealmMember", policy => policy.RequireRole("RealmOwner", "RealmMember"));
});

// --- Application Services ---
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ITenantProvider, JwtTenantProvider>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<RealmService>();
builder.Services.AddScoped<UserService>();

// Docker client (singleton for Podman socket connection)
// Linux + Podman: Use user-level Unix socket by default
var userId = Environment.GetEnvironmentVariable("UID") ?? "1000";
var socketPath = builder.Configuration["Docker:SocketPath"] ?? $"/run/user/{userId}/podman/podman.sock";
var engineUri = new Uri($"unix://{socketPath}");
var dockerClient = new DockerClientConfiguration(engineUri).CreateClient();

builder.Services.AddSingleton(dockerClient);

builder.Services.AddSingleton<IDockerNetworkService, DockerNetworkService>();
builder.Services.AddSingleton<DockerNetworkService>();
builder.Services.AddScoped<IContainerService, ContainerService>();
builder.Services.AddScoped<IManagedDatabaseService, ManagedDatabaseService>();
builder.Services.AddScoped<ContainerCrudService>();
builder.Services.AddScoped<MetricsService>();
builder.Services.AddScoped<IBillingService, BillingService>();
builder.Services.AddScoped<ManagedDatabaseCrudService>();
builder.Services.AddScoped<HealthCheckService>();
builder.Services.AddSingleton<IEventBus, InProcessEventBus>();
// Enable MetricsCollectionWorker for container state synchronization
builder.Services.AddHostedService<MetricsCollectionWorker>();
// Temporarily disable other hosted services
// builder.Services.AddHostedService<EventProcessorWorker>();
// builder.Services.AddHostedService<MetricsCleanupWorker>();
builder.Services.AddSingleton<BillingEventHandler>();

// --- YARP Reverse Proxy (InMemoryConfigProvider for dynamic routes) ---
// Temporarily disabled for local testing
// var inMemoryConfig = new InMemoryConfigProvider([], []);
// builder.Services.AddSingleton(inMemoryConfig);
// builder.Services.AddSingleton<IYarpRouteUpdater, YarpRouteUpdater>();
// builder.Services.AddReverseProxy()
//     .LoadFromMemory([], []);

var app = builder.Build();

// --- Database seeding ---
using (var scope = app.Services.CreateScope())
{
    var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
    var adminEmail = builder.Configuration["Admin:Email"] ?? "admin@cloudios.local";
    var adminPassword = builder.Configuration["Admin:Password"] ?? "Admin@123";
    await seeder.SeedAsync(adminEmail, adminPassword);

    // Apply MetricsDb migrations
    var metricsDb = scope.ServiceProvider.GetRequiredService<MetricsDbContext>();
    await metricsDb.Database.MigrateAsync();

    // --- Docker network + state sync ---
    try
    {
        var dockerNetwork = scope.ServiceProvider.GetRequiredService<DockerNetworkService>();
        await dockerNetwork.EnsureNetworkAsync();

        var containerService = scope.ServiceProvider.GetRequiredService<IContainerService>();
        await containerService.SynchronizeStateAsync();
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogWarning(ex, "Docker not available - running without Docker integration");
    }
}

// --- Event Bus subscriptions ---
// Temporarily disabled for local testing
// var eventBus = (InProcessEventBus)app.Services.GetRequiredService<IEventBus>();
// var yarpUpdater = app.Services.GetRequiredService<IYarpRouteUpdater>() as YarpRouteUpdater;
// var billingHandler = app.Services.GetRequiredService<BillingEventHandler>();

// YARP handlers (real route manipulation)
// if (yarpUpdater is not null)
// {
//     eventBus.Subscribe<ContainerStartedEvent>(yarpUpdater.HandleContainerStartedAsync);
//     eventBus.Subscribe<ContainerStoppedEvent>(yarpUpdater.HandleContainerStoppedAsync);
//     eventBus.Subscribe<ContainerDeletedEvent>(yarpUpdater.HandleContainerDeletedAsync);
// }

// Billing handlers
// eventBus.Subscribe<ContainerStartedEvent>(billingHandler.RegisterStartAsync);
// eventBus.Subscribe<ContainerStoppedEvent>(billingHandler.RegisterStopAsync);

// --- Forwarded Headers (Cloudflare Tunnel) ---
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
    KnownIPNetworks = { },
    KnownProxies = { }
});

// --- Middleware pipeline ---
app.UseRouting();
app.UseCors("AllowReactApps");
app.UseAuthentication();
app.UseAuthorization();

// --- API Endpoints ---
RegistrationEndpoints.MapRegistrationEndpoints(app);
AuthEndpoints.MapAuthEndpoints(app);
RealmEndpoints.MapRealmEndpoints(app);
UserEndpoints.MapUserEndpoints(app);
ContainerEndpoints.MapContainerEndpoints(app);
ContainerLogsEndpoints.MapContainerLogsEndpoints(app);
MetricsEndpoints.MapMetricsEndpoints(app);
BillingEndpoints.MapBillingEndpoints(app);
ManagedDatabaseEndpoints.MapManagedDatabaseEndpoints(app);
HealthCheckEndpoints.MapHealthCheckEndpoints(app);

// --- YARP ---
// app.MapReverseProxy();

app.Run();
