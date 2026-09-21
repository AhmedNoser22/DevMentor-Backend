namespace DevMentor.Infrastructure.Persistence;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;

    public UnitOfWork(AppDbContext context)
    {
        _context = context;
        Questions = new QuestionRepository(context);
        ExamAttempts = new ExamAttemptRepository(context);
        InterviewSessions = new InterviewSessionRepository(context);
        Certificates = new CertificateRepository(context);
    }

    public IQuestionRepository Questions { get; }
    public IExamAttemptRepository ExamAttempts { get; }
    public IInterviewSessionRepository InterviewSessions { get; }
    public ICertificateRepository Certificates { get; }

    public Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        return _context.SaveChangesAsync(ct);
    }

    public async Task<IAsyncDisposable> BeginTransactionAsync(CancellationToken ct = default)
    {
        var transaction = await _context.Database.BeginTransactionAsync(ct);
        return new TransactionWrapper(transaction);
    }

    private class TransactionWrapper : IAsyncDisposable
    {
        private readonly IDbContextTransaction _transaction;

        public TransactionWrapper(IDbContextTransaction transaction)
        {
            _transaction = transaction;
        }

        public async ValueTask DisposeAsync()
        {
            await _transaction.CommitAsync();
            await _transaction.DisposeAsync();
        }
    }
}