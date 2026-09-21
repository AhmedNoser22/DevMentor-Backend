namespace DevMentor.Application.Features.Interviews;

public interface IInterviewService
{
    Task<InterviewSessionDto> StartAsync(Guid userId, StartInterviewRequest request, CancellationToken ct = default);
    Task<InterviewSessionDto> AnswerAsync(Guid userId, AnswerTurnRequest request, CancellationToken ct = default);
    Task<InterviewSessionDto> GetAsync(Guid userId, Guid sessionId, CancellationToken ct = default);
}