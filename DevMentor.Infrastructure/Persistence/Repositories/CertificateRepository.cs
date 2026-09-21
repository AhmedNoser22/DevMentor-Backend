namespace DevMentor.Infrastructure.Persistence.Repositories;

public class CertificateRepository : ICertificateRepository
{
    private readonly AppDbContext _context;

    public CertificateRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<Certificate?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return _context.Certificates.FirstOrDefaultAsync(c => c.Id == id, ct);
    }

    public Task<Certificate?> GetByCodeAsync(string code, CancellationToken ct = default)
    {
        return _context.Certificates.FirstOrDefaultAsync(c => c.CertificateCode == code, ct);
    }

    public Task<Certificate?> GetByUserDomainLevelAsync(Guid userId, TechDomain domain, Level level, CancellationToken ct = default)
    {
        return _context.Certificates.FirstOrDefaultAsync(
            c => c.UserId == userId && c.Domain == domain && c.Level == level, ct);
    }

    public Task<List<Certificate>> GetAllByUserAsync(Guid userId, CancellationToken ct = default)
    {
        return _context.Certificates
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.IssuedAtUtc)
            .ToListAsync(ct);
    }

    public async Task AddAsync(Certificate certificate, CancellationToken ct = default)
    {
        await _context.Certificates.AddAsync(certificate, ct);
    }

    public void Update(Certificate certificate)
    {
        _context.Certificates.Update(certificate);
    }
}