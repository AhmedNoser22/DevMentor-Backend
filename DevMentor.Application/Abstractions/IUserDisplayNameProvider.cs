namespace DevMentor.Application.Abstractions;

public interface IUserDisplayNameProvider
{
    Task<string> GetDisplayNameAsync(Guid userId, CancellationToken ct = default);
}