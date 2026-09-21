namespace DevMentor.Infrastructure.Identity;

public class UserDisplayNameProvider : IUserDisplayNameProvider, IUserEmailProvider
{
    private readonly AppDbContext _context;

    public UserDisplayNameProvider(AppDbContext context)
    {
        _context = context;
    }

    public async Task<string> GetDisplayNameAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        return user?.FullName ?? "DevMentor User";
    }

    public async Task<string> GetEmailAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        return user?.Email ?? string.Empty;
    }
}