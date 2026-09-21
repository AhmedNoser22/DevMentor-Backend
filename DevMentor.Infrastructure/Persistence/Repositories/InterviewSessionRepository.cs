namespace DevMentor.Infrastructure.Persistence.Repositories;

public class InterviewSessionRepository : IInterviewSessionRepository
{
    private readonly AppDbContext _context;

    public InterviewSessionRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<InterviewSession?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return _context.InterviewSessions.Include(s => s.Turns).FirstOrDefaultAsync(s => s.Id == id, ct);
    }

    public Task<List<InterviewSession>> GetHistoryAsync(Guid userId, CancellationToken ct = default)
    {
        return _context.InterviewSessions
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.StartedAtUtc)
            .ToListAsync(ct);
    }

    public Task<int> CountTodayAsync(Guid userId, CancellationToken ct = default)
    {
        var today = DateTime.UtcNow.Date;
        return _context.InterviewSessions
            .CountAsync(s => s.UserId == userId && s.StartedAtUtc >= today, ct);
    }

    public async Task AddAsync(InterviewSession session, CancellationToken ct = default)
    {
        await _context.InterviewSessions.AddAsync(session, ct);
    }

    public void Update(InterviewSession session)
    {
        _context.InterviewSessions.Update(session);
    }
}