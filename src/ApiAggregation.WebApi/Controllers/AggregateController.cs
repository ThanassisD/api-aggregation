
using ApiAggregation.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiAggregation.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AggregateController : ControllerBase
{
    private readonly IAggregationService _aggregationService;
    public AggregateController(IAggregationService aggregationService)
    {
        _aggregationService = aggregationService;
    }
    
    [HttpGet("healthcheck")]
    public IActionResult HealthCheck()
    {
        return Ok(new { status = "Healthy", timestamp = DateTime.UtcNow });
    }
    
    [HttpGet("getdata")]    
    public async Task<IActionResult> GetAggregatedDataAsync(
        [FromQuery] string countryName,
        [FromQuery] int newsPageSize ,
        [FromQuery] string fromDate)
    {
        var result = await _aggregationService.GetAggregatedDataAsync(countryName, newsPageSize, fromDate);
        return Ok(result);
    }
}