using CartAPI.Services;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// Configuration du port pour Railway
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

// Configuration Redis
var redisUrl = Environment.GetEnvironmentVariable("REDIS_URL")
    ?? Environment.GetEnvironmentVariable("REDIS_PRIVATE_URL")
    ?? builder.Configuration.GetConnectionString("Redis")
    ?? "localhost:6379";

// Parser Redis URL si format redis://
if (redisUrl.StartsWith("redis://"))
{
    var uri = new Uri(redisUrl);
    var password = uri.UserInfo.Contains(':') ? uri.UserInfo.Split(':')[1] : "";
    redisUrl = $"{uri.Host}:{uri.Port},password={password},ssl=False,abortConnect=False";
}

builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var config = ConfigurationOptions.Parse(redisUrl);
    config.AbortOnConnectFail = false;
    config.ConnectRetry = 5;
    config.ConnectTimeout = 10000;
    return ConnectionMultiplexer.Connect(config);
});

// Enregistrer les services
builder.Services.AddSingleton<ProductService>();
builder.Services.AddScoped<CartService>();

// Configuration de base
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Cart API", Version = "v1" });
});

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
    app.Logger.LogInformation("✅ Redis connecté");
}
catch (Exception ex)
{
    app.Logger.LogError(ex, "❌ Erreur Redis");
}

// Middleware
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Cart API");
    c.RoutePrefix = string.Empty;
});

app.UseCors("AllowAll");
app.MapControllers();

app.Logger.LogInformation("🚀 API démarrée sur le port {Port}", port);
app.Run();




//var builder = WebApplication.CreateBuilder(args);

//// Add services to the container.
//// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
//builder.Services.AddOpenApi();

//var app = builder.Build();

//// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
//{
//    app.MapOpenApi();
//}

//app.UseHttpsRedirection();

//var summaries = new[]
//{
//    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
//};

//app.MapGet("/weatherforecast", () =>
//{
//    var forecast =  Enumerable.Range(1, 5).Select(index =>
//        new WeatherForecast
//        (
//            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
//            Random.Shared.Next(-20, 55),
//            summaries[Random.Shared.Next(summaries.Length)]
//        ))
//        .ToArray();
//    return forecast;
//})
//.WithName("GetWeatherForecast");

//app.Run();

//record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
//{
//    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
//}
