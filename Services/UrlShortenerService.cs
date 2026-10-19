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
    private const int MinCodeLength = 7;

    private readonly IDatabase _db;

    public UrlShortenerService(RedisContext redisContext)
    {
        _db = redisContext.Database;
    }

    public async Task<ShortenedUrl> CreateShortUrlAsync(string longUrl, string? customCode = null, int? expiresInHours = null)
    {
        var hash = ComputeHash(longUrl);
        var now = DateTime.Now;
        var expiresAt = expiresInHours.HasValue ? now.AddHours(expiresInHours.Value) : (DateTime?)null;

        if (string.IsNullOrWhiteSpace(customCode))
        {
            var existing = await GetExistingByLongUrlAsync(hash);
            if (existing != null)
                return existing;
        }

        var shortened = string.IsNullOrWhiteSpace(customCode)
            ? await ReserveGeneratedCodeAsync(longUrl, hash, now, expiresAt)
            : await ReserveCustomCodeAsync(customCode, longUrl, now, expiresAt);

        await _db.StringSetAsync(ReverseKeyPrefix + hash, shortened.Code);
        await _db.StringSetAsync(StatsKeyPrefix + shortened.Code, 0);

        if (expiresAt.HasValue)
        {
            var expiry = expiresAt.Value - DateTime.Now;
            await _db.KeyExpireAsync(UrlKeyPrefix + shortened.Code, expiry);
            await _db.KeyExpireAsync(StatsKeyPrefix + shortened.Code, expiry);
            await _db.KeyExpireAsync(ReverseKeyPrefix + hash, expiry);
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

    private async Task<ShortenedUrl?> GetExistingByLongUrlAsync(string hash)
    {
        var reverseCode = await _db.StringGetAsync(ReverseKeyPrefix + hash);
        if (!reverseCode.HasValue) return null;

        var data = await _db.StringGetAsync(UrlKeyPrefix + reverseCode.ToString());
        if (!data.HasValue) return null;

        var existing = JsonSerializer.Deserialize<ShortenedUrl>(data!);
        if (existing != null)
            existing.Message = "URL already exists";

        return existing;
    }

    private async Task<ShortenedUrl> ReserveGeneratedCodeAsync(string longUrl, string hash, DateTime now, DateTime? expiresAt)
    {
        for (var length = MinCodeLength; length <= hash.Length; length++)
        {
            var shortened = new ShortenedUrl
            {
                Code = hash[..length],
                LongUrl = longUrl,
                CreatedAt = now,
                ExpiresAt = expiresAt,
            };

            var stored = await _db.StringSetAsync(
                UrlKeyPrefix + shortened.Code,
                JsonSerializer.Serialize(shortened),
                when: When.NotExists);

            if (stored)
                return shortened;
        }

        throw new InvalidOperationException("Unable to generate a unique code.");
    }

    private async Task<ShortenedUrl> ReserveCustomCodeAsync(string code, string longUrl, DateTime now, DateTime? expiresAt)
    {
        var shortened = new ShortenedUrl
        {
            Code = code,
            LongUrl = longUrl,
            CreatedAt = now,
            ExpiresAt = expiresAt,
        };

        var stored = await _db.StringSetAsync(
            UrlKeyPrefix + code,
            JsonSerializer.Serialize(shortened),
            when: When.NotExists);

        if (!stored)
            throw new InvalidOperationException("Code already exists.");

        return shortened;
    }

    private static string ComputeHash(string input)
    {
        var bytes = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
