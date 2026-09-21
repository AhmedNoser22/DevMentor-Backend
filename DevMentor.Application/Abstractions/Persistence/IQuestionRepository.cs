namespace DevMentor.Application.Abstractions.Persistence;

public interface IQuestionRepository
{
    Task<Question?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<Question>> GetApprovedRandomAsync(TechDomain domain, Level level, int count, CancellationToken ct = default);
    Task<List<Question>> GetByStatusAsync(QuestionStatus status, CancellationToken ct = default);
    Task AddAsync(Question question, CancellationToken ct = default);
    Task AddRangeAsync(IEnumerable<Question> questions, CancellationToken ct = default);
    void Update(Question question);
}