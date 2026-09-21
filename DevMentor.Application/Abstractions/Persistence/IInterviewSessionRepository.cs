namespace DevMentor.Application.Abstractions.Persistence;

public interface IInterviewSessionRepository
{
    Task<InterviewSession?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<InterviewSession>> GetHistoryAsync(Guid userId, CancellationToken ct = default);
    Task<int> CountTodayAsync(Guid userId, CancellationToken ct = default);
    Task AddAsync(InterviewSession session, CancellationToken ct = default);
    void Update(InterviewSession session);
}