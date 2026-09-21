namespace DevMentor.Domain.Entities;

public class InterviewTurn : BaseEntity
{
    private InterviewTurn() { }

    private InterviewTurn(int order, string question)
    {
        Order = order;
        Question = question;
        AskedAtUtc = DateTime.UtcNow;
    }

    public Guid InterviewSessionId { get; private set; }
    public InterviewSession? InterviewSession { get; private set; }
    public int Order { get; private set; }
    public string Question { get; private set; } = string.Empty;
    public string? Answer { get; private set; }
    public string? TechnicalAccuracyFeedback { get; private set; }
    public string? MissingConceptsFeedback { get; private set; }
    public string? CommunicationFeedback { get; private set; }
    public int? Score { get; private set; }
    public string? FollowUpQuestion { get; private set; }
    public DateTime AskedAtUtc { get; private set; } = DateTime.UtcNow;
    public DateTime? AnsweredAtUtc { get; private set; }

    internal static InterviewTurn Ask(int order, string question)
    {
        return new InterviewTurn(order, question);
    }

    public void RecordAnswer(string answer)
    {
        if (Answer is not null)
        {
            throw new DomainRuleException("This turn has already been answered");
        }

        if (string.IsNullOrWhiteSpace(answer))
        {
            throw new DomainRuleException("An answer cannot be empty");
        }

        Answer = answer;
        AnsweredAtUtc = DateTime.UtcNow;
    }

    public void ApplyEvaluation(string technicalAccuracy, string missingConcepts, string communication, int score, string? followUpQuestion)
    {
        TechnicalAccuracyFeedback = technicalAccuracy;
        MissingConceptsFeedback = missingConcepts;
        CommunicationFeedback = communication;
        Score = score;
        FollowUpQuestion = followUpQuestion;
    }
}