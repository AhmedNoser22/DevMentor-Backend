namespace DevMentor.Application.Abstractions.Persistence;

public interface IExamAttemptRepository
{
    Task<ExamAttempt?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<ExamAttempt?> GetActiveAttemptAsync(Guid userId, TechDomain domain, Level level, CancellationToken ct = default);
    Task<List<ExamAttempt>> GetHistoryAsync(Guid userId, CancellationToken ct = default);
    Task AddAsync(ExamAttempt attempt, CancellationToken ct = default);
    void Update(ExamAttempt attempt);
}