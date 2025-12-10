using Application.Analysis;
using Application.Analysis.DTOs;
using Application.Analysis.DTOs.Response;
using Application.Analysis.Interfaces;
using Application.Risks;
using Application.Risks.Interfaces;
using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using NSubstitute;
using System.Reflection;
using WebApi.Controllers;
using Xunit;

namespace ApplicationTests;

public class TradesControllerTests
{
    private static TradesController CreateController(
        IAnalysisIngestionService? ingestion = null,
        IAnalysisJobStore? store = null,
        IRiskClassifier? classifier = null,
        IRiskSummaryService? summary = null,
        ILogger<TradesController>? logger = null,
        IUrlHelper? urlHelper = null)
    {
        ingestion ??= Substitute.For<IAnalysisIngestionService>();
        store ??= Substitute.For<IAnalysisJobStore>();
        classifier ??= Substitute.For<IRiskClassifier>();
        summary ??= Substitute.For<IRiskSummaryService>();
        logger ??= Substitute.For<ILogger<TradesController>>();
        urlHelper ??= Substitute.For<IUrlHelper>();

        var controller = new TradesController(ingestion, store, classifier, summary, logger);

        if (urlHelper is not null)
        {
            controller.Url = urlHelper;

            urlHelper.Action(Arg.Any<UrlActionContext>()).Returns("http://test/status/1");
        }

        return controller;
    }

    [Fact]
    public void Classify_ReturnsBadRequest_WhenNoTrades()
    {
        // Arrange
        var controller = CreateController();
        var validator = Substitute.For<IValidator<TradeDto>>();

        // Act
        var result = controller.Classify(trades: null!, validator);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public void Classify_ReturnsBadRequest_WhenValidatorFails()
    {
        // Arrange
        var dto = new TradeDto { Value = 100m, ClientSector = "Public" };
        var validator = Substitute.For<IValidator<TradeDto>>();
        var failure = new ValidationFailure("Value", "Invalid value");
        validator.Validate(Arg.Any<TradeDto>()).Returns(new ValidationResult([failure]));

        var controller = CreateController();

        // Act
        var result = controller.Classify([dto], validator);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var bad = (BadRequestObjectResult)result;
        bad.Value.Should().NotBeNull();
    }

    [Fact]
    public void Classify_ReturnsOk_WithCategories_WhenValid()
    {
        // Arrange
        var dto = new TradeDto { Value = 100m, ClientSector = "Public" };
        var validator = Substitute.For<IValidator<TradeDto>>();
        validator.Validate(Arg.Any<TradeDto>()).Returns(new ValidationResult());

        var classifier = Substitute.For<IRiskClassifier>();
        classifier.ClassifyMany(Arg.Any<IEnumerable<Trade>>())
                  .Returns([RiskCategory.LOWRISK, RiskCategory.MEDIUMRISK]);

        var controller = CreateController(classifier: classifier);

        // Act
        var result = controller.Classify([dto], validator);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var ok = (OkObjectResult)result;
        ok.Value.Should().BeOfType<ClassificationDtoResponse>();

        var payload = (ClassificationDtoResponse)ok.Value!;
        payload.Categories.Should().Contain(["LOWRISK", "MEDIUMRISK"]);
    }


    [Fact]
    public async Task AnalyzeQueue_ReturnsAccepted_WhenIngestionSucceeds()
    {
        // Arrange
        var ingestion = Substitute.For<IAnalysisIngestionService>();
        var jobId = Guid.NewGuid().ToString();

        ingestion
            .IngestAsync(Arg.Any<List<AnalysisDto>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new AnalyzeAcceptedResult(jobId, 3)));

        var http = new DefaultHttpContext();
        http.Request.Scheme = "http";
        http.Request.Host = new HostString("test");

        var controller = CreateController(ingestion: ingestion);
        controller.ControllerContext = new ControllerContext { HttpContext = http };

        var urlHelper = Substitute.For<IUrlHelper>();
        
        var actionContext = new ActionContext(http, new RouteData(), new ActionDescriptor());
        
        urlHelper.ActionContext.Returns(actionContext);
        urlHelper.Action(Arg.Any<UrlActionContext>()).Returns($"/status/{jobId}");

        controller.Url = urlHelper;

        var trades = new List<AnalysisDto>
        {
            new() { Value = 1m, ClientSector = "Public", ClientId = "C1" }
        };

        // Act
        var result = await controller.Analyze(trades, CancellationToken.None);

        // Assert

        result.Should().BeOfType<AcceptedResult>();
        var accepted = (AcceptedResult)result;
        accepted.StatusCode.Should().Be(202);

        accepted.Value.Should().NotBeNull();
        var jobProp = accepted.Value!.GetType().GetProperty("jobId", BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
        jobProp.Should().NotBeNull();
        jobProp!.GetValue(accepted.Value)!.Should().Be(jobId);

        await ingestion.Received(1).IngestAsync(Arg.Any<List<AnalysisDto>>(), Arg.Any<CancellationToken>());
    }


    [Fact]
    public async Task AnalyzeQueue_ReturnsBadRequest_WhenIngestionThrowsArgumentException()
    {
        var ingestion = Substitute.For<IAnalysisIngestionService>();
        var ex = new ArgumentException("Nenhum dado foi informado.");
        ingestion.IngestAsync(Arg.Any<List<AnalysisDto>>(), Arg.Any<CancellationToken>())
                 .Returns(Task.FromException<AnalyzeAcceptedResult>(ex));

        var controller = CreateController(ingestion: ingestion);

        var result = await controller.Analyze(new List<AnalysisDto> { new() }, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
        var bad = (BadRequestObjectResult)result;
        bad.Value.Should().NotBeNull();

        // assert error message present
        var prop = bad.Value!.GetType().GetProperty("error", BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
        prop.Should().NotBeNull();
        prop!.GetValue(bad.Value)!.Should().Be("Nenhum dado foi informado.");
    }

    [Fact]
    public void GetAnalyzeStatus_ReturnsStatusPayload_FromStore()
    {
        var store = Substitute.For<IAnalysisJobStore>();
        var payload = new { message = "not found" } as object;
        store.GetStatusPayload("x").Returns((false, payload, 404));

        var controller = CreateController(store: store);

        var result = controller.GetAnalyzeStatus("x");

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var obj = (ObjectResult)result;
        obj.StatusCode.Should().Be(404);
        obj.Value.Should().BeSameAs(payload);
    }

    [Fact]
    public void Analyze_ReturnsBadRequest_WhenNoTradesProvided()
    {
        var controller = CreateController();
        var validator = Substitute.For<IValidator<AnalysisDto>>();

        var result = controller.Analyze(trades: null!, validator);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public void Analyze_ReturnsOk_WithSummary_WhenValid()
    {
        var dto = new AnalysisDto { Value = 500m, ClientSector = "Public", ClientId = "CLI1" };
        var validator = Substitute.For<IValidator<AnalysisDto>>();
        validator.Validate(Arg.Any<AnalysisDto>()).Returns(new ValidationResult());

        var summaryService = Substitute.For<IRiskSummaryService>();
        var summary = new RiskSummary
        {
            ProcessingTimeMs = 42,
            Summary = new Dictionary<RiskCategory, RiskSummary.CategorySummary>
            {
                [RiskCategory.LOWRISK] = new RiskSummary.CategorySummary { Count = 1, TotalValue = 500m, TopClient = "CLI1" }
            }
        };
        summaryService.Analyze(Arg.Any<IEnumerable<Trade>>())
                      .Returns((new[] { RiskCategory.LOWRISK }, summary));

        var controller = CreateController(summary: summaryService);

        var result = controller.Analyze(new List<AnalysisDto> { dto }, validator);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var ok = (OkObjectResult)result;
        ok.Value.Should().BeOfType<AnalysisDtoResponse>();

        var payload = (AnalysisDtoResponse)ok.Value!;
        payload.Categories.Should().ContainSingle("LOWRISK");
        payload.ProcessingTimeMs.Should().Be(42);
        payload.Summary.Should().ContainKey("LOWRISK");
        var cat = payload.Summary["LOWRISK"];
        cat.Count.Should().Be(1);
        cat.TotalValue.Should().Be(500m);
        cat.TopClient.Should().Be("CLI1");
    }
}