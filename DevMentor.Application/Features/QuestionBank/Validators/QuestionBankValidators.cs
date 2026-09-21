namespace DevMentor.Application.Features.QuestionBank.Validators;

public class GenerateQuestionsRequestValidator : AbstractValidator<GenerateQuestionsRequest>
{
    public GenerateQuestionsRequestValidator()
    {
        RuleFor(x => x.Domain).IsInEnum();
        RuleFor(x => x.Level).IsInEnum();
        RuleFor(x => x.Count).InclusiveBetween(1, 50);
    }
}

public class ReportQuestionRequestValidator : AbstractValidator<ReportQuestionRequest>
{
    public ReportQuestionRequestValidator()
    {
        RuleFor(x => x.QuestionId).NotEmpty();
    }
}