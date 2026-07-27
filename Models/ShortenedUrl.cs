namespace url_shortener.Models;

public class ShortenedUrl
{
    public string Code { get; set; } = string.Empty;
    public string LongUrl { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public long ClickCount { get; set; }
    public string? Message { get; set; }
}
