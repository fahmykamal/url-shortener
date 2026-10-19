using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using url_shortener.Models;
using url_shortener.Models.Dtos;
using url_shortener.Services;

namespace url_shortener.Controllers;

[ApiController]
[Route("")]
public class UrlController : ControllerBase
{
    private static readonly Regex CustomCodePattern = new("^[A-Za-z0-9_-]{1,32}$", RegexOptions.Compiled);

    private readonly IUrlShortenerService _service;

    public UrlController(IUrlShortenerService service)
    {
        _service = service;
    }

    [HttpPost("shorten")]
    [EnableRateLimiting("shorten")]
    public async Task<IActionResult> Shorten([FromBody] ShortenRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Url))
            return BadRequest("Url is required.");

        if (request.Url.Length > 2048)
            return BadRequest("Url must be at most 2048 characters.");

        if (!Uri.TryCreate(request.Url, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            return BadRequest("Url must be an absolute http or https URL.");

        if (request.ExpiresInHours is <= 0)
            return BadRequest("expiresInHours must be greater than zero.");

        var customCode = string.IsNullOrWhiteSpace(request.CustomCode) ? null : request.CustomCode.Trim();
        if (customCode != null && !CustomCodePattern.IsMatch(customCode))
            return BadRequest("customCode must be 1-32 characters: letters, digits, - or _.");

        ShortenedUrl shortened;
        try
        {
            shortened = await _service.CreateShortUrlAsync(request.Url, customCode, request.ExpiresInHours);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }

        var response = new ShortenResponse
        {
            ShortUrl = $"{Request.Scheme}://{Request.Host}/{shortened.Code}",
            Code = shortened.Code,
            LongUrl = shortened.LongUrl,
            CreatedAt = shortened.CreatedAt,
            ExpiresAt = shortened.ExpiresAt,
            Message = shortened.Message
        };

        return Ok(response);
    }

    [HttpGet("{code}")]
    public async Task<IActionResult> RedirectToUrl(string code)
    {
        var shortened = await _service.GetUrlByCodeAsync(code);
        if (shortened == null)
            return NotFound();

        return Redirect(shortened.LongUrl);
    }
}
