namespace DevMentor.Application.Abstractions.Persistence;

public interface IUnitOfWork
{
    IQuestionRepository Questions { get; }
    IExamAttemptRepository ExamAttempts { get; }
    IInterviewSessionRepository InterviewSessions { get; }
    ICertificateRepository Certificates { get; }
    Task<int> SaveChangesAsync(CancellationToken ct = default);
    Task<IAsyncDisposable> BeginTransactionAsync(CancellationToken ct = default);
}