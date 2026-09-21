namespace DevMentor.API.Controllers;

[Authorize]
[Route("api/profile")]
public class ProfileController : BaseApiController
{
    private readonly IProfileService _profileService;

    public ProfileController(IProfileService profileService)
    {
        _profileService = profileService;
    }

    [HttpGet]
    public async Task<ActionResult<ProfileDto>> Get(CancellationToken ct)
    {
        var profile = await _profileService.GetAsync(CurrentUserId, ct);
        return Ok(profile);
    }
}