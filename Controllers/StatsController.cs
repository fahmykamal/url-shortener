using Microsoft.AspNetCore.Mvc;
using url_shortener.Models.Dtos;
using url_shortener.Services;

namespace url_shortener.Controllers;

[ApiController]
[Route("stats")]
public class StatsController : ControllerBase
{
    private readonly IUrlShortenerService _service;

    public StatsController(IUrlShortenerService service)
    {
        _service = service;
    }

    [HttpGet("{code}")]
    public async Task<IActionResult> GetStats(string code)
    {
        var shortened = await _service.GetUrlStatsAsync(code);
        if (shortened == null)
            return NotFound();

        return Ok(shortened);
    }
}
