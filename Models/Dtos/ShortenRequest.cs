namespace url_shortener.Models.Dtos;

public class ShortenRequest
{
    public string Url { get; set; } = string.Empty;
    public string? CustomCode { get; set; }
    public int? ExpiresInHours { get; set; }
}
