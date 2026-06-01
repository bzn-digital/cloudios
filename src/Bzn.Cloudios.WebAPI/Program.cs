using Bzn.Cloudios.WebAPI.Serialization;

var builder = WebApplication.CreateBuilder(args);

// --- JSON Serialization (AOT-safe, no reflection) ---
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolver = CloudiosJsonSerializerContext.Default;
});

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
