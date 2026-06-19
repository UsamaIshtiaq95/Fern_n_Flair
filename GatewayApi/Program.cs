using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
// C#
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var connString = builder.Configuration["Redis:ConnectionString"];
    var options = ConfigurationOptions.Parse(connString);
    options.AbortOnConnectFail = false; // allow retries instead of aborting startup
    return ConnectionMultiplexer.Connect(options);
});
// Register Redis connection
//builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
//    ConnectionMultiplexer.Connect(builder.Configuration["Redis:ConnectionString"]));
//builder.Services.AddSwaggerGen();
builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);
try
{
    builder.Services.AddOcelot();
}
catch (Exception ex)
{
    Console.WriteLine("Ocelot setup failed: " + ex.Message);
    throw;
}
//builder.Services.AddAuthorization();
var app = builder.Build();
app.UseHttpsRedirection();
//app.UseMiddleware<RedisCacheMiddleware>();

//app.UseAuthorization();
// ? Intercept /swagger before Ocelot handles it

await app.UseOcelot();

app.Run();

//internal record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
//{
//    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
//}
