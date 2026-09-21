namespace DevMentor.Application.Features.Exams.Dtos;

public record StartExamRequest(TechDomain Domain, Level Level);

public record ExamQuestionDto(Guid QuestionId, string Text, List<ExamOptionDto> Options, Guid? SelectedOptionId);

public record ExamOptionDto(Guid Id, string Text);

public record ExamAttemptDto(
    Guid AttemptId,
    TechDomain Domain,
    Level Level,
    AttemptStatus Status,
    DateTime ExpiresAtUtc,
    List<ExamQuestionDto> Questions);

public record SaveAnswerRequest(Guid AttemptId, Guid QuestionId, Guid SelectedOptionId);

public record ExamResultDto(
    Guid AttemptId,
    int ScorePercentage,
    bool Passed,
    bool CertificateIssued,
    string? CertificateCode);