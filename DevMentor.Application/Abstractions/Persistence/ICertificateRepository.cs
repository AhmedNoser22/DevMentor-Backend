namespace DevMentor.Application.Abstractions.Persistence;

public interface ICertificateRepository
{
    Task<Certificate?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Certificate?> GetByCodeAsync(string code, CancellationToken ct = default);
    Task<Certificate?> GetByUserDomainLevelAsync(Guid userId, TechDomain domain, Level level, CancellationToken ct = default);
    Task<List<Certificate>> GetAllByUserAsync(Guid userId, CancellationToken ct = default);
    Task AddAsync(Certificate certificate, CancellationToken ct = default);
    void Update(Certificate certificate);
}