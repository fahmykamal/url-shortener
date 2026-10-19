using Microsoft.AspNetCore.Mvc;
using url_shortener.Models;
using url_shortener.Models.Dtos;
using url_shortener.Services;

namespace url_shortener.Controllers;

[ApiController]
[Route("")]
public class UrlController : ControllerBase
{
    private readonly IUrlShortenerService _service;

    public UrlController(IUrlShortenerService service)
    {
        _service = service;
    }

    [HttpPost("shorten")]
    public async Task<IActionResult> Shorten([FromBody] ShortenRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Url))
            return BadRequest("Url is required.");

        if (request.ExpiresInHours is <= 0)
            return BadRequest("expiresInHours must be greater than zero.");

        ShortenedUrl shortened;
        try
        {
            shortened = await _service.CreateShortUrlAsync(request.Url, request.CustomCode, request.ExpiresInHours);
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
