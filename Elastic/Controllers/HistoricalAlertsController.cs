using ElasticTest.Models;
using ElasticTest.Services;
using Microsoft.AspNetCore.Mvc;
using static ElasticTest.Services.AlertElasticService;

[ApiController]
[Route("api/[controller]")]
public class HistoricalAlertsController(AlertElasticService svc) : ControllerBase
{
    private readonly AlertElasticService _svc = svc;

    [HttpGet("search")]
    public async Task<IResult> Search([FromQuery] HistoricalAlertSearchRequest req, CancellationToken ct)
    {
        var res = await _svc.SearchHistoricalAsync(req, ct);
        return Results.Ok(res);
    }


    [HttpPost("seed")]
    public async Task<IResult> Seed(CancellationToken ct)
    {

        //await _svc.UnblockIndexAsync(ct);

        var count = await _svc.BulkIndexAsync();
        return Results.Ok(new { indexed = count });
    }
}