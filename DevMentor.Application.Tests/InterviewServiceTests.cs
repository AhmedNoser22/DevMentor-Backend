using DevMentor.Application.Abstractions;
using DevMentor.Application.Abstractions.Persistence;
using DevMentor.Application.Common.Exceptions;
using DevMentor.Application.Common.Settings;
using DevMentor.Application.Features.Interviews;
using DevMentor.Application.Features.Interviews.Dtos;
using DevMentor.Domain.Entities;
using DevMentor.Domain.Enums;
using Moq;

namespace DevMentor.Application.Tests;

public class InterviewServiceTests
{
    private static (InterviewService Service, Mock<IInterviewSessionRepository> Repo, Mock<IAiClient> AiClient, InterviewSettings Settings)
        BuildService(int maxTurns = 3)
    {
        var unitOfWork = new Mock<IUnitOfWork>();
        var repo = new Mock<IInterviewSessionRepository>();
        var aiClient = new Mock<IAiClient>();
        var settings = new InterviewSettings { MaxTurns = maxTurns, DailyLimitPerUser = 3 };

        InterviewSession? saved = null;
        repo.Setup(r => r.AddAsync(It.IsAny<InterviewSession>(), It.IsAny<CancellationToken>()))
            .Callback<InterviewSession, CancellationToken>((s, _) => saved = s)
            .Returns(Task.CompletedTask);
        repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(() => saved);

        unitOfWork.Setup(u => u.InterviewSessions).Returns(repo.Object);
        unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        return (new InterviewService(unitOfWork.Object, aiClient.Object, settings), repo, aiClient, settings);
    }

    [Fact]
    public async Task StartAsync_DailyLimitReached_ThrowsConflict()
    {
        var (service, repo, _, settings) = BuildService();
        var userId = Guid.NewGuid();
        repo.Setup(r => r.CountTodayAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(settings.DailyLimitPerUser);

        await Assert.ThrowsAsync<ConflictException>(() =>
            service.StartAsync(userId, new StartInterviewRequest(TechDomain.Angular), CancellationToken.None));
    }

    [Fact]
    public async Task AnswerAsync_ReachesMaxTurns_EndsSession()
    {
        var (service, repo, aiClient, _) = BuildService(maxTurns: 1);
        var userId = Guid.NewGuid();

        repo.Setup(r => r.CountTodayAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(0);
        aiClient.Setup(a => a.GetFirstInterviewQuestionAsync(TechDomain.Sql, It.IsAny<CancellationToken>()))
            .ReturnsAsync("Final question?");

        var started = await service.StartAsync(userId, new StartInterviewRequest(TechDomain.Sql), CancellationToken.None);

        aiClient.Setup(a => a.EvaluateInterviewTurnAsync(
                TechDomain.Sql, It.IsAny<List<InterviewTurnContext>>(), "Final question?", "My answer", 1, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new InterviewEvaluationResult("Accurate", "None", "Clear", 90, "", false));

        var result = await service.AnswerAsync(userId, new AnswerTurnRequest(started.SessionId, started.Turns[0].TurnId, "My answer"), CancellationToken.None);

        Assert.True(result.Ended);
        Assert.Equal(90, result.FinalScore);
    }

    [Fact]
    public async Task AnswerAsync_BelowMaxTurns_AddsFollowUp()
    {
        var (service, repo, aiClient, _) = BuildService(maxTurns: 3);
        var userId = Guid.NewGuid();

        repo.Setup(r => r.CountTodayAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(0);
        aiClient.Setup(a => a.GetFirstInterviewQuestionAsync(TechDomain.Sql, It.IsAny<CancellationToken>()))
            .ReturnsAsync("First question?");

        var started = await service.StartAsync(userId, new StartInterviewRequest(TechDomain.Sql), CancellationToken.None);

        aiClient.Setup(a => a.EvaluateInterviewTurnAsync(
                TechDomain.Sql, It.IsAny<List<InterviewTurnContext>>(), "First question?", "My answer", 1, 3, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new InterviewEvaluationResult("Accurate", "None", "Clear", 80, "Follow-up?", false));

        var result = await service.AnswerAsync(userId, new AnswerTurnRequest(started.SessionId, started.Turns[0].TurnId, "My answer"), CancellationToken.None);

        Assert.False(result.Ended);
        Assert.Equal(2, result.Turns.Count);
    }
}