namespace DevMentor.Domain.Entities;

public record InterviewTurnEvaluation(
    string TechnicalAccuracy,
    string MissingConcepts,
    string Communication,
    int Score,
    string FollowUpQuestion,
    bool IsFinalTurn);

public class InterviewSession : BaseEntity
{
    private readonly List<InterviewTurn> _turns = new();

    private InterviewSession() { }

    private InterviewSession(Guid userId, TechDomain domain, string firstQuestion)
    {
        UserId = userId;
        Domain = domain;
        StartedAtUtc = DateTime.UtcNow;
        _turns.Add(InterviewTurn.Ask(1, firstQuestion));
    }

    public Guid UserId { get; private set; }
    public TechDomain Domain { get; private set; }
    public DateTime StartedAtUtc { get; private set; } = DateTime.UtcNow;
    public DateTime? EndedAtUtc { get; private set; }
    public int? FinalScore { get; private set; }
    public string? SummaryText { get; private set; }
    public IReadOnlyList<InterviewTurn> Turns => _turns;

    public static InterviewSession Start(Guid userId, TechDomain domain, string firstQuestion)
    {
        return new InterviewSession(userId, domain, firstQuestion);
    }

    public void RecordAnswer(Guid turnId, string answer)
    {
        if (EndedAtUtc is not null)
        {
            throw new DomainRuleException("This interview session has already ended");
        }

        var turn = _turns.FirstOrDefault(t => t.Id == turnId)
            ?? throw new DomainRuleException("This turn does not belong to the session");

        turn.RecordAnswer(answer);
    }

    public void ApplyEvaluationAndAdvance(Guid turnId, InterviewTurnEvaluation evaluation, int maxTurns)
    {
        var turn = _turns.FirstOrDefault(t => t.Id == turnId)
            ?? throw new DomainRuleException("This turn does not belong to the session");

        turn.ApplyEvaluation(
            evaluation.TechnicalAccuracy,
            evaluation.MissingConcepts,
            evaluation.Communication,
            evaluation.Score,
            evaluation.IsFinalTurn ? null : evaluation.FollowUpQuestion);

        if (evaluation.IsFinalTurn || turn.Order >= maxTurns)
        {
            Conclude();
        }
        else
        {
            _turns.Add(InterviewTurn.Ask(turn.Order + 1, evaluation.FollowUpQuestion));
        }
    }

    private void Conclude()
    {
        EndedAtUtc = DateTime.UtcNow;
        var scoredTurns = _turns.Where(t => t.Score.HasValue).ToList();
        FinalScore = scoredTurns.Count == 0 ? 0 : (int)Math.Round(scoredTurns.Average(t => t.Score!.Value));
        SummaryText = $"Completed {scoredTurns.Count} questions with an average score of {FinalScore}.";
    }
}