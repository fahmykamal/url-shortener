using url_shortener.Redis;
using url_shortener.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var redisConnection = builder.Configuration.GetConnectionString("Redis")
    ?? "localhost:6379";
builder.Services.AddSingleton(new RedisContext(redisConnection));
builder.Services.AddScoped<IUrlShortenerService, UrlShortenerService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseHttpsRedirection();
}
app.UseDefaultFiles();
app.UseStaticFiles();
app.MapControllers();

app.Run();
