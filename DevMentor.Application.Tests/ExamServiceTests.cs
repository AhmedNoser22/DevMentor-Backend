using DevMentor.Application.Abstractions.Persistence;
using DevMentor.Application.Common.Exceptions;
using DevMentor.Application.Common.Settings;
using DevMentor.Application.Features.Exams;
using DevMentor.Application.Tests.TestHelpers;
using DevMentor.Domain.Entities;
using DevMentor.Domain.Enums;
using DevMentor.Domain.Exceptions;
using Moq;

namespace DevMentor.Application.Tests;

public class ExamServiceTests
{
    private static ExamAttempt BuildAttempt(int correctCount, int totalCount)
    {
        var questions = new List<Question>();
        var correctIds = new List<Guid>();
        var wrongIds = new List<Guid>();

        for (var i = 0; i < totalCount; i++)
        {
            var q = new Question(TechDomain.DotNet, Level.Intermediate, $"Question {i}");
            q.AddOption("Correct", true);
            q.AddOption("Wrong", false);
            questions.Add(q);
            correctIds.Add(q.Options.First(o => o.IsCorrect).Id);
            wrongIds.Add(q.Options.First(o => !o.IsCorrect).Id);
        }

        var attempt = ExamAttempt.Start(Guid.NewGuid(), TechDomain.DotNet, Level.Intermediate, questions, 10);

        for (var i = 0; i < totalCount; i++)
        {
            attempt.RecordAnswer(questions[i].Id, i < correctCount ? correctIds[i] : wrongIds[i]);
        }

        return attempt;
    }

    private static (ExamService Service, Mock<IExamAttemptRepository> ExamRepo, Mock<ICertificateRepository> CertRepo) BuildService()
    {
        var unitOfWork = new Mock<IUnitOfWork>();
        var examRepo = new Mock<IExamAttemptRepository>();
        var certRepo = new Mock<ICertificateRepository>();

        unitOfWork.Setup(u => u.ExamAttempts).Returns(examRepo.Object);
        unitOfWork.Setup(u => u.Certificates).Returns(certRepo.Object);
        unitOfWork.Setup(u => u.Questions).Returns(Mock.Of<IQuestionRepository>());
        unitOfWork.Setup(u => u.InterviewSessions).Returns(Mock.Of<IInterviewSessionRepository>());
        unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        unitOfWork.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new FakeAsyncDisposable());

        var service = new ExamService(unitOfWork.Object, new ExamSettings { PassPercentage = 85 });
        return (service, examRepo, certRepo);
    }

    [Fact]
    public async Task SubmitAsync_BelowThreshold_NoCertificateIssued()
    {
        var (service, examRepo, certRepo) = BuildService();
        var attempt = BuildAttempt(correctCount: 1, totalCount: 2);
        examRepo.Setup(r => r.GetByIdAsync(attempt.Id, It.IsAny<CancellationToken>())).ReturnsAsync(attempt);

        var result = await service.SubmitAsync(attempt.UserId, attempt.Id, CancellationToken.None);

        Assert.Equal(50, result.ScorePercentage);
        Assert.False(result.CertificateIssued);
        certRepo.Verify(c => c.AddAsync(It.IsAny<Certificate>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SubmitAsync_AboveThreshold_IssuesCertificate()
    {
        var (service, examRepo, certRepo) = BuildService();
        var attempt = BuildAttempt(correctCount: 2, totalCount: 2);
        examRepo.Setup(r => r.GetByIdAsync(attempt.Id, It.IsAny<CancellationToken>())).ReturnsAsync(attempt);
        certRepo.Setup(c => c.GetByUserDomainLevelAsync(attempt.UserId, attempt.Domain, attempt.Level, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Certificate?)null);

        var result = await service.SubmitAsync(attempt.UserId, attempt.Id, CancellationToken.None);

        Assert.True(result.Passed);
        Assert.True(result.CertificateIssued);
        certRepo.Verify(c => c.AddAsync(It.IsAny<Certificate>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SubmitAsync_WrongUser_ThrowsUnauthorized()
    {
        var (service, examRepo, _) = BuildService();
        var attempt = BuildAttempt(1, 1);
        examRepo.Setup(r => r.GetByIdAsync(attempt.Id, It.IsAny<CancellationToken>())).ReturnsAsync(attempt);

        await Assert.ThrowsAsync<UnauthorizedAppException>(() =>
            service.SubmitAsync(Guid.NewGuid(), attempt.Id, CancellationToken.None));
    }

    [Fact]
    public async Task SubmitAsync_AlreadySubmitted_ThrowsDomainRuleException()
    {
        var (service, examRepo, _) = BuildService();
        var attempt = BuildAttempt(1, 1);
        attempt.Submit(85);
        examRepo.Setup(r => r.GetByIdAsync(attempt.Id, It.IsAny<CancellationToken>())).ReturnsAsync(attempt);

        await Assert.ThrowsAsync<DomainRuleException>(() =>
            service.SubmitAsync(attempt.UserId, attempt.Id, CancellationToken.None));
    }
}