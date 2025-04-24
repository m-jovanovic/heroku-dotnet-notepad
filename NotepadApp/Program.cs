using NotepadApp.Hubs;
using Microsoft.EntityFrameworkCore;
using NotepadApp.Data;
using StackExchange.Redis;
using DotNetEnv;
using Microsoft.AspNetCore.HttpOverrides;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

// Load environment variables from .env file if it exists
Env.Load();

var redisUrl = Environment.GetEnvironmentVariable("REDIS_URL");
// Configure Redis for SignalR
if (!string.IsNullOrEmpty(redisUrl))
{
    var uri = new Uri(redisUrl);
    var userInfoParts = uri.UserInfo.Split(':');
    if (userInfoParts.Length != 2)
    {
        throw new InvalidOperationException("REDIS_URL is not in the expected format ('redis://user:password@host:port')");
    }

    var configurationOptions = new ConfigurationOptions
    {
        EndPoints = { { uri.Host, uri.Port } },
        Password = userInfoParts[1],
        Ssl = true,
    };
    configurationOptions.CertificateValidation += (sender, cert, chain, errors) => true;

    builder.Services.AddSignalR().AddStackExchangeRedis(options =>
    {
        options.Configuration = configurationOptions;
        options.Configuration.ChannelPrefix = RedisChannel.Literal("NotepadApp_");
    });
}
else
{
    Console.WriteLine("Warning: REDIS_URL environment variable is not set, falling back to in-memory SignalR.");
    builder.Services.AddSignalR();
}

// Configure PostgreSQL
var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
if (!string.IsNullOrEmpty(databaseUrl))
{

    // Parse Heroku PostgreSQL URL format: postgres://username:password@host:port/database
    var uri = new Uri(databaseUrl);
    var host = uri.Host;
    var dbPort = uri.Port;
    var database = uri.LocalPath.TrimStart('/');
    var credentials = uri.UserInfo.Split(':');
    var username = credentials[0];
    var password = credentials[1];
    var connectionString = $"Host={host};Port={dbPort};Database={database};Username={username};Password={password};SSL Mode=Require;Trust Server Certificate=true;";
    builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(connectionString));
}
else
{
    Console.WriteLine("Warning: DATABASE_URL not found, falling back to SQLite.");
    var fallbackDbPath = Path.Combine(AppContext.BaseDirectory, "local.db");
    var sqliteConnection = $"Data Source={fallbackDbPath}";
    builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(sqliteConnection));
}

builder.Services.AddRazorPages();
var isHeroku = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DYNO"));
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    if (isHeroku)
    {
        options.KnownNetworks.Clear();
        options.KnownProxies.Clear();
    }
});
builder.Services.AddHttpsRedirection(options =>
{
    if (isHeroku)
    {
        options.RedirectStatusCode = StatusCodes.Status308PermanentRedirect;
        options.HttpsPort = 443;
    }
    ;
});

builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService("NotepadApp"))
    .WithTracing(tracing =>
        tracing
            .AddSource("NotepadApp")
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddSource("Microsoft.AspNetCore.SignalR.Client")
            .AddSource("Microsoft.AspNetCore.SignalR.Server")
            .AddNpgsql()
            .AddOtlpExporter(options =>
            {
                options.Endpoint = new Uri("http://localhost:4317");
            }));

var app = builder.Build();

app.UseForwardedHeaders();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

// Additional middleware
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();
app.MapRazorPages();
app.MapHub<NoteHub>("/noteHub");

// Ensure database is created and migrations are applied
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.EnsureCreated();
}

app.Run();
