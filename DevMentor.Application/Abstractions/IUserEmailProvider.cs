namespace DevMentor.Application.Abstractions;

public interface IUserEmailProvider
{
    Task<string> GetEmailAsync(Guid userId, CancellationToken ct = default);
}