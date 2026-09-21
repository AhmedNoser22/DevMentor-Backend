namespace DevMentor.Application.Features.Exams.Validators;

public class StartExamRequestValidator : AbstractValidator<StartExamRequest>
{
    public StartExamRequestValidator()
    {
        RuleFor(x => x.Domain).IsInEnum();
        RuleFor(x => x.Level).IsInEnum();
    }
}

public class SaveAnswerRequestValidator : AbstractValidator<SaveAnswerRequest>
{
    public SaveAnswerRequestValidator()
    {
        RuleFor(x => x.AttemptId).NotEmpty();
        RuleFor(x => x.QuestionId).NotEmpty();
        RuleFor(x => x.SelectedOptionId).NotEmpty();
    }
}