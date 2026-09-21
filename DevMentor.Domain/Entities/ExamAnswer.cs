namespace DevMentor.Domain.Entities;

public class ExamAnswer : BaseEntity
{
    private ExamAnswer() { }

    internal ExamAnswer(Question question, int displayOrder)
    {
        QuestionId = question.Id;
        Question = question;
        DisplayOrder = displayOrder;
    }

    public Guid ExamAttemptId { get; private set; }
    public ExamAttempt? ExamAttempt { get; private set; }
    public Guid QuestionId { get; private set; }
    public Question? Question { get; private set; }
    public int DisplayOrder { get; private set; }
    public Guid? SelectedOptionId { get; private set; }
    public bool? IsCorrect { get; private set; }

    public void SelectOption(Guid optionId)
    {
        SelectedOptionId = optionId;
    }

    public void Grade()
    {
        if (Question is null)
        {
            throw new DomainRuleException("Cannot grade an answer without its question loaded");
        }

        var correctOption = Question.CorrectOption();
        IsCorrect = correctOption is not null && SelectedOptionId == correctOption.Id;
    }
}