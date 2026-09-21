namespace DevMentor.Infrastructure.Persistence.Repositories;

public class QuestionRepository : IQuestionRepository
{
    private readonly AppDbContext _context;

    public QuestionRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<Question?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return _context.Questions.Include(q => q.Options).FirstOrDefaultAsync(q => q.Id == id, ct);
    }

    public async Task<List<Question>> GetApprovedRandomAsync(TechDomain domain, Level level, int count, CancellationToken ct = default)
    {
        return await _context.Questions
            .Include(q => q.Options)
            .Where(q => q.Domain == domain && q.Level == level && q.Status == QuestionStatus.Approved)
            .OrderBy(_ => Guid.NewGuid())
            .Take(count)
            .ToListAsync(ct);
    }

    public Task<List<Question>> GetByStatusAsync(QuestionStatus status, CancellationToken ct = default)
    {
        return _context.Questions.Include(q => q.Options).Where(q => q.Status == status).ToListAsync(ct);
    }

    public async Task AddAsync(Question question, CancellationToken ct = default)
    {
        await _context.Questions.AddAsync(question, ct);
    }

    public async Task AddRangeAsync(IEnumerable<Question> questions, CancellationToken ct = default)
    {
        await _context.Questions.AddRangeAsync(questions, ct);
    }

    public void Update(Question question)
    {
        _context.Questions.Update(question);
    }
}