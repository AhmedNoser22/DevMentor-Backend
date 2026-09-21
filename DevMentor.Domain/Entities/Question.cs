namespace DevMentor.Domain.Entities;

public class Question : BaseEntity
{
    private const int DisableAfterReports = 5;
    private readonly List<QuestionOption> _options = new();

    private Question() { }

    public Question(TechDomain domain, Level level, string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new DomainRuleException("Question text cannot be empty");
        }

        Domain = domain;
        Level = level;
        Text = text;
        Status = QuestionStatus.PendingReview;
    }

    public TechDomain Domain { get; private set; }
    public Level Level { get; private set; }
    public string Text { get; private set; } = string.Empty;
    public QuestionStatus Status { get; private set; } = QuestionStatus.PendingReview;
    public int TimesReported { get; private set; }
    public DateTime? DisabledAtUtc { get; private set; }
    public IReadOnlyList<QuestionOption> Options => _options;

    public void AddOption(string text, bool isCorrect)
    {
        _options.Add(new QuestionOption(text, isCorrect));
    }

    public bool IsValidForApproval()
    {
        return _options.Count >= 2 && _options.Count(o => o.IsCorrect) == 1;
    }

    public void Approve()
    {
        if (!IsValidForApproval())
        {
            throw new DomainRuleException("A question needs at least two options with exactly one correct answer to be approved");
        }

        Status = QuestionStatus.Approved;
    }

    public void Reject()
    {
        Status = QuestionStatus.Rejected;
    }

    public void Report()
    {
        if (Status == QuestionStatus.Disabled)
        {
            return;
        }

        TimesReported++;
        if (TimesReported >= DisableAfterReports)
        {
            Status = QuestionStatus.Disabled;
            DisabledAtUtc = DateTime.UtcNow;
        }
    }

    public QuestionOption? CorrectOption()
    {
        return _options.FirstOrDefault(o => o.IsCorrect);
    }
}