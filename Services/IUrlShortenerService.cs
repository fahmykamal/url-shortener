using url_shortener.Models;

namespace url_shortener.Services;

public interface IUrlShortenerService
{
    Task<ShortenedUrl> CreateShortUrlAsync(string longUrl, string? customCode = null, int? expiresInHours = null);
    Task<ShortenedUrl?> GetUrlByCodeAsync(string code);
    Task<ShortenedUrl?> GetUrlStatsAsync(string code);
}
