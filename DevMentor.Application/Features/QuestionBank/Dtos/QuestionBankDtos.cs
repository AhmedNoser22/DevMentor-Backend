namespace DevMentor.Application.Features.QuestionBank.Dtos;

public record GenerateQuestionsRequest(TechDomain Domain, Level Level, int Count);

public record QuestionOptionDto(Guid Id, string Text);

public record QuestionForReviewDto(
    Guid Id,
    TechDomain Domain,
    Level Level,
    string Text,
    QuestionStatus Status,
    List<QuestionOptionDto> Options);

public record ReportQuestionRequest(Guid QuestionId);