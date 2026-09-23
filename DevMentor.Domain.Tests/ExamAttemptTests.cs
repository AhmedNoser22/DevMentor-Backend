using DevMentor.Domain.Entities;
using DevMentor.Domain.Enums;
using DevMentor.Domain.Exceptions;

namespace DevMentor.Domain.Tests;

public class ExamAttemptTests
{
    private static Question BuildApprovedQuestion()
    {
        var question = new Question(TechDomain.DotNet, Level.Intermediate, "Sample question");
        question.AddOption("Correct", true);
        question.AddOption("Wrong", false);
        question.Approve();
        return question;
    }

    [Fact]
    public void Start_WithNoQuestions_ThrowsDomainRuleException()
    {
        Assert.Throws<DomainRuleException>(() =>
            ExamAttempt.Start(Guid.NewGuid(), TechDomain.DotNet, Level.Intermediate, new List<Question>(), 10));
    }

    [Fact]
    public void Submit_AllCorrect_Returns100AndPassed()
    {
        var question = BuildApprovedQuestion();
        var attempt = ExamAttempt.Start(Guid.NewGuid(), TechDomain.DotNet, Level.Intermediate, new List<Question> { question }, 10);
        attempt.RecordAnswer(question.Id, question.Options.First(o => o.IsCorrect).Id);

        var result = attempt.Submit(passPercentage: 85);

        Assert.Equal(100, result.ScorePercentage);
        Assert.True(result.Passed);
        Assert.False(result.WasExpired);
    }

    [Fact]
    public void Submit_ScoreBelowThreshold_NotPassed()
    {
        var question = BuildApprovedQuestion();
        var attempt = ExamAttempt.Start(Guid.NewGuid(), TechDomain.DotNet, Level.Intermediate, new List<Question> { question }, 10);
        attempt.RecordAnswer(question.Id, question.Options.First(o => !o.IsCorrect).Id);

        var result = attempt.Submit(passPercentage: 85);

        Assert.Equal(0, result.ScorePercentage);
        Assert.False(result.Passed);
    }

    [Fact]
    public void Submit_TwiceOnSameAttempt_ThrowsDomainRuleException()
    {
        var question = BuildApprovedQuestion();
        var attempt = ExamAttempt.Start(Guid.NewGuid(), TechDomain.DotNet, Level.Intermediate, new List<Question> { question }, 10);
        attempt.Submit(85);

        Assert.Throws<DomainRuleException>(() => attempt.Submit(85));
    }

    [Fact]
    public void RecordAnswer_AfterExpiry_ThrowsDomainRuleException()
    {
        var question = BuildApprovedQuestion();
        var attempt = ExamAttempt.Start(Guid.NewGuid(), TechDomain.DotNet, Level.Intermediate, new List<Question> { question }, durationMinutes: 0);

        Assert.Throws<DomainRuleException>(() =>
            attempt.RecordAnswer(question.Id, question.Options.First().Id));
    }
}