namespace DevMentor.Infrastructure.Persistence.Repositories;

public class ExamAttemptRepository : IExamAttemptRepository
{
    private readonly AppDbContext _context;

    public ExamAttemptRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<ExamAttempt?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return _context.ExamAttempts
            .Include(a => a.Answers).ThenInclude(x => x.Question).ThenInclude(q => q!.Options)
            .FirstOrDefaultAsync(a => a.Id == id, ct);
    }

    public Task<ExamAttempt?> GetActiveAttemptAsync(Guid userId, TechDomain domain, Level level, CancellationToken ct = default)
    {
        return _context.ExamAttempts
            .Include(a => a.Answers).ThenInclude(x => x.Question).ThenInclude(q => q!.Options)
            .Where(a => a.UserId == userId && a.Domain == domain && a.Level == level && a.Status == AttemptStatus.InProgress)
            .OrderByDescending(a => a.StartedAtUtc)
            .FirstOrDefaultAsync(ct);
    }

    public Task<List<ExamAttempt>> GetHistoryAsync(Guid userId, CancellationToken ct = default)
    {
        return _context.ExamAttempts
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.StartedAtUtc)
            .ToListAsync(ct);
    }

    public async Task AddAsync(ExamAttempt attempt, CancellationToken ct = default)
    {
        await _context.ExamAttempts.AddAsync(attempt, ct);
    }

    public void Update(ExamAttempt attempt)
    {
        _context.ExamAttempts.Update(attempt);
    }
}