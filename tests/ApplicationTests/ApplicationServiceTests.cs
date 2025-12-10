using Application.Analysis;
using Application.Analysis.Interfaces;
using Application.Analysis.Jobs;
using Application.Analysis.Services;
using Application.Response;
using Application.Risks;
using Application.Risks.Interfaces;
using Application.Risks.Services;
using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using NSubstitute;
using Xunit;

namespace ApplicationTests;

public class ApplicationServiceTests
{
    [Fact]
    public void RiskClassifier_ReturnsFirstMatchingCategory()
    {
        var rule1 = Substitute.For<Domain.Interfaces.IRiskRule>();
        rule1.IsMatch(Arg.Any<Trade>()).Returns(false);
        rule1.Category.Returns(RiskCategory.LOWRISK);

        var rule2 = Substitute.For<Domain.Interfaces.IRiskRule>();
        rule2.IsMatch(Arg.Any<Trade>()).Returns(true);
        rule2.Category.Returns(RiskCategory.HIGHRISK);

        var classifier = new RiskClassifier([rule1, rule2]);

        var res = classifier.Classify(new Trade(2_000_000m, "Private"));

        res.Should().Be(RiskCategory.HIGHRISK);
    }

    [Fact]
    public void RiskClassifier_Throws_WhenNoRuleMatches()
    {
        var rule = Substitute.For<Domain.Interfaces.IRiskRule>();
        rule.IsMatch(Arg.Any<Trade>()).Returns(false);

        var classifier = new RiskClassifier([rule]);

        Action act = () => classifier.Classify(new Trade(1m, "Any"));

        act.Should().Throw<InvalidOperationException>().WithMessage("Nenhuma categoria encontrada.");
    }

    [Fact]
    public void RiskSummaryService_Analyze_AggregatesSummaryCorrectly()
    {
        var classifier = Substitute.For<IRiskClassifier>();
        classifier.Classify(Arg.Any<Trade>())
                  .Returns(ci =>
                  {
                      var t = ci.Arg<Trade>();
                      return t.Value >= 1_000_000m ? RiskCategory.HIGHRISK : RiskCategory.LOWRISK;
                  });

        var svc = new RiskSummaryService(classifier);

        var trades = new[]
        {
            new Trade(500m, "Public", "C1"),
            new Trade(500m, "Public", "C1"),
            new Trade(2_000_000m, "Private", "C2")
        };

        var (categories, summary) = svc.Analyze(trades);

        categories.Should().HaveCount(3);
        summary.ProcessingTimeMs.Should().BeGreaterThanOrEqualTo(0);
        summary.Summary.Should().ContainKey(RiskCategory.LOWRISK);
        summary.Summary.Should().ContainKey(RiskCategory.HIGHRISK);

        var low = summary.Summary[RiskCategory.LOWRISK];
        low.Count.Should().Be(2);
        low.TotalValue.Should().Be(1000m);
        low.TopClient.Should().Be("C1");

        var high = summary.Summary[RiskCategory.HIGHRISK];
        high.Count.Should().Be(1);
        high.TotalValue.Should().Be(2_000_000m);
        high.TopClient.Should().Be("C2");
    }

    [Fact]
    public void AnalysisInputValidator_ReturnsPayloadError_OnNullOrEmpty()
    {
        var validator = Substitute.For<IValidator<Application.Analysis.DTOs.AnalysisDto>>();
        var sut = new AnalysisInputValidator(validator);

        var (errors, trades) = sut.Validate(dtos: null!);

        errors.Should().ContainSingle().Which.Index.Should().Be(-1);
        errors[0].Field.Should().Be("payload");
        trades.Should().BeEmpty();
    }

    [Fact]
    public void AnalysisInputValidator_CollectsValidationErrors()
    {
        var validator = Substitute.For<IValidator<Application.Analysis.DTOs.AnalysisDto>>();
        var failure = new ValidationFailure("Value", "invalid");
        validator.Validate(Arg.Any<Application.Analysis.DTOs.AnalysisDto>())
                 .Returns(new ValidationResult([failure]));

        var sut = new AnalysisInputValidator(validator);

        var dtos = new List<Application.Analysis.DTOs.AnalysisDto> { new() };
        var (errors, trades) = sut.Validate(dtos);

        errors.Should().NotBeEmpty();
        errors[0].Index.Should().Be(0);
        errors[0].Field.Should().Be("Value");
        trades.Should().BeEmpty();
    }

    [Fact]
    public async Task AnalysisIngestionService_Ingests_Successfully()
    {
        var inputValidator = Substitute.For<IAnalysisInputValidator>();
        var validTrades = new List<Trade> { new(1m, "Public", "C1"), new(2m, "Private", "C2") };
        inputValidator.Validate(Arg.Any<List<Application.Analysis.DTOs.AnalysisDto>>())
                      .Returns(([], validTrades));

        var idGen = Substitute.For<IJobIdGenerator>();
        idGen.NewId().Returns("job-123");

        var store = Substitute.For<IAnalysisJobStore>();
        var queue = Substitute.For<IAnalysisQueue>();
        queue.EnqueueManyAsync(Arg.Any<string>(), Arg.Any<IEnumerable<Trade>>(), Arg.Any<CancellationToken>())
             .Returns(Task.CompletedTask);

        var sut = new AnalysisIngestionService(inputValidator, idGen, store, queue);

        var res = await sut.IngestAsync([new()], CancellationToken.None);

        res.JobId.Should().Be("job-123");
        res.EnqueuedCount.Should().Be(validTrades.Count);
        idGen.Received(1).NewId();
        store.Received(1).InitJob("job-123", validTrades.Count);
        await queue.Received(1).EnqueueManyAsync("job-123", validTrades, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AnalysisIngestionService_ThrowsArgumentException_OnPayloadError()
    {
        var inputValidator = Substitute.For<IAnalysisInputValidator>();
        inputValidator.Validate(Arg.Any<List<Application.Analysis.DTOs.AnalysisDto>>())
                      .Returns((new List<Error> { new(-1, "payload", "Nenhum dado foi informado.") }, []));

        var idGen = Substitute.For<IJobIdGenerator>();
        var store = Substitute.For<IAnalysisJobStore>();
        var queue = Substitute.For<IAnalysisQueue>();

        var sut = new AnalysisIngestionService(inputValidator, idGen, store, queue);

        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await sut.IngestAsync([new()], CancellationToken.None));
    }

    [Fact]
    public async Task AnalysisIngestionService_ThrowsValidationException_OnValidationErrors()
    {
        var inputValidator = Substitute.For<IAnalysisInputValidator>();
        inputValidator.Validate(Arg.Any<List<Application.Analysis.DTOs.AnalysisDto>>())
                      .Returns(([new(0, "Value", "invalid")], []));

        var idGen = Substitute.For<IJobIdGenerator>();
        var store = Substitute.For<IAnalysisJobStore>();
        var queue = Substitute.For<IAnalysisQueue>();

        var sut = new AnalysisIngestionService(inputValidator, idGen, store, queue);

        await Assert.ThrowsAsync<Application.Analysis.Exceptions.ValidationException>(async () =>
            await sut.IngestAsync([new()], CancellationToken.None));
    }

    [Fact]
    public void AnalysisJobStore_GetStatusPayload_Workflow()
    {
        var store = new AnalysisJobStore();

        // init job with total 2
        var state = store.InitJob("job-x", total: 2);
        var (found1, payload1, status1) = store.GetStatusPayload("job-x");
        found1.Should().BeTrue();
        status1.Should().Be(202);
        payload1.Should().NotBeNull();

        // register one trade -> still processing
        state.Register(RiskCategory.LOWRISK, new Trade(100m, "Public", "C1"));
        var (found2, payload2, status2) = store.GetStatusPayload("job-x");
        found2.Should().BeTrue();
        status2.Should().Be(202);

        // register second -> completed
        state.Register(RiskCategory.MEDIUMRISK, new Trade(1_000_000m, "Public", "C2"));
        var (found3, payload3, status3) = store.GetStatusPayload("job-x");
        found3.Should().BeTrue();
        status3.Should().Be(200);
        payload3.Should().BeOfType<Application.Analysis.DTOs.Response.AnalysisDtoResponse>();
        var resp = (Application.Analysis.DTOs.Response.AnalysisDtoResponse)payload3;
        resp.Categories.Should().HaveCount(2);
        resp.Summary.Should().ContainKey("LOWRISK");
        resp.Summary.Should().ContainKey("MEDIUMRISK");
    }
}