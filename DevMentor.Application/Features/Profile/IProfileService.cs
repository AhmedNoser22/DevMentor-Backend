namespace DevMentor.Application.Features.Profile;

public interface IProfileService
{
    Task<ProfileDto> GetAsync(Guid userId, CancellationToken ct = default);
}