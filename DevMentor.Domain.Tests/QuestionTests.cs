using DevMentor.Domain.Entities;
using DevMentor.Domain.Enums;
using DevMentor.Domain.Exceptions;

namespace DevMentor.Domain.Tests;

public class QuestionTests
{
    [Fact]
    public void NewQuestion_DefaultsToPendingReview()
    {
        var question = new Question(TechDomain.Sql, Level.Beginner, "What is a primary key?");

        Assert.Equal(QuestionStatus.PendingReview, question.Status);
        Assert.Equal(0, question.TimesReported);
    }

    [Fact]
    public void Constructor_EmptyText_ThrowsDomainRuleException()
    {
        Assert.Throws<DomainRuleException>(() => new Question(TechDomain.Sql, Level.Beginner, "   "));
    }

    [Fact]
    public void Approve_WithoutTwoOptions_ThrowsDomainRuleException()
    {
        var question = new Question(TechDomain.Angular, Level.Beginner, "What is a signal?");
        question.AddOption("A reactive primitive", true);

        Assert.Throws<DomainRuleException>(() => question.Approve());
    }

    [Fact]
    public void Approve_WithTwoOptionsAndOneCorrect_Succeeds()
    {
        var question = new Question(TechDomain.Angular, Level.Beginner, "What is a signal?");
        question.AddOption("A reactive primitive", true);
        question.AddOption("A CSS class", false);

        question.Approve();

        Assert.Equal(QuestionStatus.Approved, question.Status);
    }

    [Fact]
    public void Report_ReachesThreshold_DisablesQuestion()
    {
        var question = new Question(TechDomain.DotNet, Level.Beginner, "What is DI?");

        for (var i = 0; i < 5; i++)
        {
            question.Report();
        }

        Assert.Equal(QuestionStatus.Disabled, question.Status);
        Assert.NotNull(question.DisabledAtUtc);
    }
}