using DevMentor.Domain.Entities;
using DevMentor.Domain.Enums;
using DevMentor.Domain.Exceptions;

namespace DevMentor.Domain.Tests;

public class InterviewSessionTests
{
    [Fact]
    public void Start_EmptyQuestion_ThrowsDomainRuleException()
    {
        Assert.Throws<DomainRuleException>(() =>
            InterviewSession.Start(Guid.NewGuid(), TechDomain.Sql, "   "));
    }

    [Fact]
    public void ApplyEvaluationAndAdvance_WithFollowUp_AddsNewTurn()
    {
        var session = InterviewSession.Start(Guid.NewGuid(), TechDomain.Sql, "First question?");
        var firstTurn = session.Turns[0];
        session.RecordAnswer(firstTurn.Id, "My answer");

        var evaluation = new InterviewTurnEvaluation("Good", "None", "Clear", 80, "Follow-up?", false);
        session.ApplyEvaluationAndAdvance(firstTurn.Id, evaluation, maxTurns: 3);

        Assert.Equal(2, session.Turns.Count);
        Assert.Equal("Follow-up?", session.Turns[1].Question);
        Assert.Null(session.EndedAtUtc);
    }

    [Fact]
    public void ApplyEvaluationAndAdvance_NoFollowUpQuestion_EndsSessionSafely()
    {
        var session = InterviewSession.Start(Guid.NewGuid(), TechDomain.Sql, "First question?");
        var firstTurn = session.Turns[0];
        session.RecordAnswer(firstTurn.Id, "My answer");

        var evaluation = new InterviewTurnEvaluation("Good", "None", "Clear", 80, string.Empty, false);
        session.ApplyEvaluationAndAdvance(firstTurn.Id, evaluation, maxTurns: 5);

        Assert.Single(session.Turns);
        Assert.NotNull(session.EndedAtUtc);
        Assert.Equal(80, session.FinalScore);
    }

    [Fact]
    public void ApplyEvaluationAndAdvance_ReachesMaxTurns_EndsSession()
    {
        var session = InterviewSession.Start(Guid.NewGuid(), TechDomain.Sql, "Only question?");
        var firstTurn = session.Turns[0];
        session.RecordAnswer(firstTurn.Id, "My answer");

        var evaluation = new InterviewTurnEvaluation("Good", "None", "Clear", 90, "Would ask more", false);
        session.ApplyEvaluationAndAdvance(firstTurn.Id, evaluation, maxTurns: 1);

        Assert.Single(session.Turns);
        Assert.NotNull(session.EndedAtUtc);
        Assert.Equal(90, session.FinalScore);
    }

    [Fact]
    public void RecordAnswer_AfterSessionEnded_ThrowsDomainRuleException()
    {
        var session = InterviewSession.Start(Guid.NewGuid(), TechDomain.Sql, "Only question?");
        var firstTurn = session.Turns[0];
        session.RecordAnswer(firstTurn.Id, "My answer");
        session.ApplyEvaluationAndAdvance(firstTurn.Id, new InterviewTurnEvaluation("Good", "None", "Clear", 90, "", false), maxTurns: 1);

        Assert.Throws<DomainRuleException>(() => session.RecordAnswer(firstTurn.Id, "Too late"));
    }
}