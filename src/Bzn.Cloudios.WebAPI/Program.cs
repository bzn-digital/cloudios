using System.Text;
using Bzn.Cloudios.Application.Abstractions;
using Bzn.Cloudios.Application.Events;
using Bzn.Cloudios.Application.Services;
using Bzn.Cloudios.Infrastructure.Persistence;
using Bzn.Cloudios.Infrastructure.Services;
using Bzn.Cloudios.WebAPI.Endpoints;
using Bzn.Cloudios.WebAPI.Serialization;
using Bzn.Cloudios.WebAPI.Services;
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
        policy.WithOrigins("http://localhost:5173", "http://localhost:5174")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// --- Authentication (JWT Bearer with symmetric key) ---
// Temporarily disabled for local testing

// --- Authorization Policies ---
// Temporarily disabled for local testing

// --- Application Services ---
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ITenantProvider, JwtTenantProvider>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<RealmService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddSingleton<IDockerNetworkService, DockerNetworkService>();
builder.Services.AddSingleton<DockerNetworkService>();
builder.Services.AddScoped<IContainerService, ContainerService>();
builder.Services.AddScoped<ContainerCrudService>();
builder.Services.AddScoped<MetricsService>();
builder.Services.AddScoped<IBillingService, BillingService>();
builder.Services.AddScoped<HealthCheckService>();
builder.Services.AddSingleton<IEventBus, InProcessEventBus>();
// Temporarily disable hosted services - Docker not accessible
// builder.Services.AddHostedService<EventProcessorWorker>();
// builder.Services.AddHostedService<MetricsCollectionWorker>();
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
    KnownNetworks = { },
    KnownProxies = { }
});

// --- Middleware pipeline ---
app.UseCors("AllowReactApps");
// app.UseAuthentication(); // Temporarily disabled for local testing
// app.UseAuthorization(); // Temporarily disabled for local testing

// --- API Endpoints ---
app.MapAuthEndpoints();
app.MapRealmEndpoints();
app.MapUserEndpoints();
app.MapContainerEndpoints();
app.MapContainerConfigEndpoints();
app.MapContainerLogsEndpoints();
app.MapMetricsEndpoints();
app.MapBillingEndpoints();
app.MapHealthCheckEndpoints();

// --- YARP ---
// app.MapReverseProxy();

// --- Health check endpoint ---
app.MapGet("/health", () =>
{
    return Results.Ok(new { status = "Healthy", version = "0.1.0" });
});

app.Run();
