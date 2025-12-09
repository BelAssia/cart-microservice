using CartAPI.Services;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// Configuration du port pour Railway
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

// Configuration Redis
var redisUrl = Environment.GetEnvironmentVariable("REDIS_URL")
    ?? Environment.GetEnvironmentVariable("REDIS_PRIVATE_URL");

string redisConnection;
if (!string.IsNullOrEmpty(redisUrl))
{
    // Railway fournit l'URL au format: redis://default:password@host:port
    var uri = new Uri(redisUrl);
    var password = uri.UserInfo.Contains(':') ? uri.UserInfo.Split(':')[1] : "";
    redisConnection = $"{uri.Host}:{uri.Port},password={password},ssl=False,abortConnect=False";
}
else
{
    // Fallback pour développement local
    redisConnection = "localhost:6379";
}

builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("Connexion à Redis: {Host}", redisConnection.Split(',')[0]);

    var config = ConfigurationOptions.Parse(redisConnection);
    config.AbortOnConnectFail = false;
    config.ConnectRetry = 5;
    config.ConnectTimeout = 10000;

    var connection = ConnectionMultiplexer.Connect(config);
    logger.LogInformation(" Redis connecté");
    return connection;
});

// Enregistrer les services
builder.Services.AddSingleton<ProductService>();
builder.Services.AddScoped<CartService>();

// Configuration
builder.Services.AddControllers();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

var app = builder.Build();

// Test Redis
try
{
    var redis = app.Services.GetRequiredService<IConnectionMultiplexer>();
    var db = redis.GetDatabase();
    db.Ping();
    app.Logger.LogInformation(" Redis ping OK");
}
catch (Exception ex)
{
    app.Logger.LogError(ex, "Erreur Redis");
}

// Middleware
app.UseCors("AllowAll");
app.MapControllers();

app.Logger.LogInformation(" API sur port {Port}", port);
app.Run();