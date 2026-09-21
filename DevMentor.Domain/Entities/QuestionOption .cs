namespace DevMentor.Domain.Entities;

public class QuestionOption : BaseEntity
{
    private QuestionOption() { }

    internal QuestionOption(string text, bool isCorrect)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new DomainRuleException("Option text cannot be empty");
        }

        Text = text;
        IsCorrect = isCorrect;
    }

    public Guid QuestionId { get; private set; }
    public Question? Question { get; private set; }
    public string Text { get; private set; } = string.Empty;
    public bool IsCorrect { get; private set; }
}