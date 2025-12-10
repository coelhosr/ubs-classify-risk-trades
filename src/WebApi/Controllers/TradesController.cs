using Application.Analysis.DTOs;
using Application.Analysis.DTOs.Response;
using Application.Analysis.Interfaces;
using Application.Response;
using Application.Risks.Interfaces;
using Domain.Entities;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers;

[ApiController]
[Route("api/trades")]
public class TradesController (
    IAnalysisIngestionService ingestion,
    IAnalysisJobStore store,
    IRiskClassifier classifier,
    IRiskSummaryService summaryService,
    ILogger<TradesController> logger) : ControllerBase
{
    [HttpPost("classify")]
    public IActionResult Classify([FromBody] List<TradeDto> trades, [FromServices] IValidator<TradeDto> validator)
    {
        if (trades == null || trades.Count == 0)
            return BadRequest(new { error = "Nenhum dado foi informado." });

        var errors = new List<Error>(capacity: 256);
        var domainTrades = new List<Trade>();

        for (int i = 0; i < trades.Count; i++)
        {
            var dto = trades[i];
            var res = validator.Validate(dto);

            if (!res.IsValid)
                foreach (var e in res.Errors)
                    errors.Add(new Error(i, e.PropertyName, e.ErrorMessage));
            else
                domainTrades.Add(new Trade(dto.Value, dto.ClientSector));
        }

        if (errors.Count > 0)
            return BadRequest(new { errors });

        var categories = classifier
            .ClassifyMany(domainTrades)
            .Select(c => c.ToString().ToUpperInvariant())
            .ToList();

        logger.LogInformation("Summary: {Count} categories.", categories.Count);

        return Ok(new ClassificationDtoResponse { Categories = categories });
    }

    [HttpPost("analyze/queue")]
    public async Task<IActionResult> Analyze([FromBody] List<AnalysisDto> trades, CancellationToken ct)
    {
        try
        {
            var res = await ingestion.IngestAsync(trades, ct);
            var statusUrl = Url.ActionLink(nameof(GetAnalyzeStatus), values: new { jobId = res.JobId });
            logger.LogInformation("Accepted job {JobId} with {Count} trades", res.JobId, res.EnqueuedCount);
            return Accepted(new { jobId = res.JobId, enqueued = res.EnqueuedCount, statusUrl });
        }
        catch (ArgumentException ex) when (ex.Message == "Nenhum dado foi informado.")
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (ValidationException vex)
        {
            return BadRequest(new { errors = vex.Errors });
        }
    }


    [HttpGet("analyze/{jobId}")]
    public IActionResult GetAnalyzeStatus([FromRoute] string jobId)
    {
        var (found, payload, status) = store.GetStatusPayload(jobId);
        
        if (!found) 
            return StatusCode(status, payload);
        
        return StatusCode(status, payload);
    }


    [HttpPost("analyze")]
    public IActionResult Analyze([FromBody] List<AnalysisDto> trades, [FromServices] IValidator<AnalysisDto> validator)
    {
        if (trades == null || trades.Count == 0)
            return BadRequest(new { error = "Nenhum dado foi informado." });

        var errors = new List<Error>(capacity: 256);
        var domainTrades = new List<Trade>();

        for (int i = 0; i < trades.Count; i++)
        {
            var dto = trades[i];
            var res = validator.Validate(dto);

            if (!res.IsValid)
                foreach (var e in res.Errors)
                    errors.Add(new Error(i, e.PropertyName, e.ErrorMessage));
            else
                domainTrades.Add(new Trade(dto.Value, dto.ClientSector, dto.ClientId));
        }

        if (errors.Count > 0)
            return BadRequest(new { errors });

        var (cats, summary) = summaryService.Analyze(domainTrades);
        var response = new AnalysisDtoResponse
        {
            Categories = [.. cats.Select(c => c.ToString().ToUpperInvariant())],
            ProcessingTimeMs = summary.ProcessingTimeMs,
            Summary = summary.Summary.ToDictionary(
                kv => kv.Key.ToString().ToUpperInvariant(),
                kv => new CategorySummaryDto { Count = kv.Value.Count, TotalValue = kv.Value.TotalValue, TopClient = kv.Value.TopClient }
            )
        };
        logger.LogInformation("Summary: {Count} trades in {Ms}ms", response.Categories.Count, response.ProcessingTimeMs);
        return Ok(response);
    }
}
