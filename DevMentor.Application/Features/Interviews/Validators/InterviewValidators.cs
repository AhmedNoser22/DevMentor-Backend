namespace DevMentor.Application.Features.Interviews.Validators;

public class StartInterviewRequestValidator : AbstractValidator<StartInterviewRequest>
{
    public StartInterviewRequestValidator()
    {
        RuleFor(x => x.Domain).IsInEnum();
    }
}

public class AnswerTurnRequestValidator : AbstractValidator<AnswerTurnRequest>
{
    public AnswerTurnRequestValidator()
    {
        RuleFor(x => x.SessionId).NotEmpty();
        RuleFor(x => x.TurnId).NotEmpty();
        RuleFor(x => x.Answer).NotEmpty().MaximumLength(4000);
    }
}