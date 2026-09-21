namespace DevMentor.Application.Features.Exams;

public interface IExamService
{
    Task<ExamAttemptDto> StartAsync(Guid userId, StartExamRequest request, CancellationToken ct = default);
    Task<ExamAttemptDto> GetAsync(Guid userId, Guid attemptId, CancellationToken ct = default);
    Task SaveAnswerAsync(Guid userId, SaveAnswerRequest request, CancellationToken ct = default);
    Task<ExamResultDto> SubmitAsync(Guid userId, Guid attemptId, CancellationToken ct = default);
}