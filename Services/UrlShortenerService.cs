using System.Text.Json;
using System.Security.Cryptography;
using StackExchange.Redis;
using url_shortener.Models;
using url_shortener.Redis;

namespace url_shortener.Services;

public class UrlShortenerService : IUrlShortenerService
{
    private const string UrlKeyPrefix = "url:";
    private const string StatsKeyPrefix = "stats:";
    private const string ReverseKeyPrefix = "reverse:";

    private readonly IDatabase _db;

    public UrlShortenerService(RedisContext redisContext)
    {
        _db = redisContext.Database;
    }

    public async Task<ShortenedUrl> CreateShortUrlAsync(string longUrl, string? customCode = null, int? expiresInHours = null)
    {
        var hash = ComputeHash(longUrl);

        if (customCode == null)
        {
            var reverseCode = await _db.StringGetAsync(ReverseKeyPrefix + hash);
            if (reverseCode.HasValue)
            {
                var data = await _db.StringGetAsync(UrlKeyPrefix + reverseCode.ToString());
                if (data.HasValue)
                {
                    var existing = JsonSerializer.Deserialize<ShortenedUrl>(data!);
                    if (existing != null)
                    {
                        existing.Message = "URL already exists";
                        return existing;
                    }
                }
            }
        }

        var code = customCode ?? GenerateCode(longUrl);

        var codeExists = await _db.StringGetAsync(UrlKeyPrefix + code);
        if (codeExists.HasValue)
            throw new InvalidOperationException("Code already exists.");

        var shortened = new ShortenedUrl
        {
            Code = code,
            LongUrl = longUrl,
            CreatedAt = DateTime.Now,
            ExpiresAt = expiresInHours.HasValue ? DateTime.Now.AddHours(expiresInHours.Value) : null,
        };

        await _db.StringSetAsync(ReverseKeyPrefix + hash, code);
        await _db.StringSetAsync(UrlKeyPrefix + code, JsonSerializer.Serialize(shortened));
        await _db.StringSetAsync(StatsKeyPrefix + code, 0);

        if (shortened.ExpiresAt.HasValue)
        {
            var expiry = shortened.ExpiresAt.Value - DateTime.Now;
            await _db.KeyExpireAsync(UrlKeyPrefix + code, expiry);
            await _db.KeyExpireAsync(StatsKeyPrefix + code, expiry);
        }

        return shortened;
    }

    public async Task<ShortenedUrl?> GetUrlByCodeAsync(string code)
    {
        var data = await _db.StringGetAsync(UrlKeyPrefix + code);
        if (!data.HasValue) return null;

        var shortened = JsonSerializer.Deserialize<ShortenedUrl>(data!);
        if (shortened == null) return null;

        await _db.StringIncrementAsync(StatsKeyPrefix + code);
        shortened.ClickCount = (long)await _db.StringGetAsync(StatsKeyPrefix + code);

        return shortened;
    }

    public async Task<ShortenedUrl?> GetUrlStatsAsync(string code)
    {
        var data = await _db.StringGetAsync(UrlKeyPrefix + code);
        if (!data.HasValue) return null;

        var shortened = JsonSerializer.Deserialize<ShortenedUrl>(data!);
        if (shortened == null) return null;

        shortened.ClickCount = (long)await _db.StringGetAsync(StatsKeyPrefix + code);

        return shortened;
    }

    private static string GenerateCode(string longUrl)
    {
        var hash = ComputeHash(longUrl);
        return hash[..7];
    }

    private static string ComputeHash(string input)
    {
        var bytes = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
