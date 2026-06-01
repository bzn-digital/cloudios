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

// --- Static files for Blazor WASM ---
app.UseStaticFiles();

// --- YARP ---
app.MapReverseProxy();

// --- Health check endpoint ---
app.MapGet("/health", () =>
{
    return Results.Ok(new { status = "Healthy", version = "0.1.0" });
});

// --- Fallback to Blazor WASM index.html ---
app.MapFallbackToFile("index.html");

app.Run();
