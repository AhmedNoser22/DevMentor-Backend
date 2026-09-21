namespace DevMentor.Domain.Entities;

public record ExamSubmissionResult(int ScorePercentage, bool Passed, bool WasExpired);

public class ExamAttempt : BaseEntity
{
    private readonly List<ExamAnswer> _answers = new();

    private ExamAttempt() { }

    private ExamAttempt(Guid userId, TechDomain domain, Level level, int durationMinutes)
    {
        UserId = userId;
        Domain = domain;
        Level = level;
        Status = AttemptStatus.InProgress;
        StartedAtUtc = DateTime.UtcNow;
        ExpiresAtUtc = StartedAtUtc.AddMinutes(durationMinutes);
    }

    public Guid UserId { get; private set; }
    public TechDomain Domain { get; private set; }
    public Level Level { get; private set; }
    public AttemptStatus Status { get; private set; } = AttemptStatus.InProgress;
    public DateTime StartedAtUtc { get; private set; } = DateTime.UtcNow;
    public DateTime ExpiresAtUtc { get; private set; }
    public DateTime? SubmittedAtUtc { get; private set; }
    public int? ScorePercentage { get; private set; }
    public IReadOnlyList<ExamAnswer> Answers => _answers;

    public static ExamAttempt Start(Guid userId, TechDomain domain, Level level, IReadOnlyList<Question> questions, int durationMinutes)
    {
        if (questions.Count == 0)
        {
            throw new DomainRuleException("Cannot start an exam with no questions");
        }

        var attempt = new ExamAttempt(userId, domain, level, durationMinutes);
        for (var i = 0; i < questions.Count; i++)
        {
            attempt._answers.Add(new ExamAnswer(questions[i], i));
        }

        return attempt;
    }

    public bool IsPastDeadline() => ExpiresAtUtc <= DateTime.UtcNow;

    public void ExpireIfPastDeadline()
    {
        if (Status == AttemptStatus.InProgress && IsPastDeadline())
        {
            Status = AttemptStatus.Expired;
        }
    }

    public void RecordAnswer(Guid questionId, Guid selectedOptionId)
    {
        if (Status != AttemptStatus.InProgress || IsPastDeadline())
        {
            throw new DomainRuleException("This attempt is no longer in progress");
        }

        var answer = _answers.FirstOrDefault(a => a.QuestionId == questionId)
            ?? throw new DomainRuleException("This question is not part of the current attempt");

        answer.SelectOption(selectedOptionId);
    }

    public ExamSubmissionResult Submit(int passPercentage)
    {
        if (Status != AttemptStatus.InProgress)
        {
            throw new DomainRuleException("This attempt has already been finalized");
        }

        var wasExpired = IsPastDeadline();

        foreach (var answer in _answers)
        {
            answer.Grade();
        }

        var correctCount = _answers.Count(a => a.IsCorrect == true);
        var score = _answers.Count == 0 ? 0 : (int)Math.Round(correctCount * 100.0 / _answers.Count);

        ScorePercentage = score;
        SubmittedAtUtc = DateTime.UtcNow;
        Status = wasExpired ? AttemptStatus.Expired : AttemptStatus.Submitted;

        var passed = !wasExpired && score >= passPercentage;
        return new ExamSubmissionResult(score, passed, wasExpired);
    }
}