using System.Threading.RateLimiting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using url_shortener.Redis;
using url_shortener.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (ctx, token) =>
    {
        ctx.HttpContext.Response.ContentType = "application/json";
        await ctx.HttpContext.Response.WriteAsync("{\"error\":\"too many requests\"}", token);
    };
    options.AddPolicy("shorten", context =>
    {
        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(ip, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 10,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0
        });
    });
});

var redisConnection = builder.Configuration.GetConnectionString("Redis")
    ?? "localhost:6379";
builder.Services.AddSingleton(new RedisContext(redisConnection));
builder.Services.AddScoped<IUrlShortenerService, UrlShortenerService>();

var app = builder.Build();

app.UseForwardedHeaders();
app.UseRateLimiter();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseHttpsRedirection();
}
app.UseDefaultFiles();
app.UseStaticFiles();
app.MapControllers();

app.MapGet("/health", (RedisContext redis) =>
    redis.IsConnected
        ? Results.Ok(new { status = "ok" })
        : Results.Json(new { status = "degraded" }, statusCode: StatusCodes.Status503ServiceUnavailable));

app.Run();
