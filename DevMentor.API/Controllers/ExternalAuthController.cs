namespace DevMentor.API.Controllers;

[ApiController]
[Route("api/auth/external")]
public class ExternalAuthController : ControllerBase
{
    private readonly OAuthSettings _oauthSettings;
    private readonly GoogleOAuthClient _googleClient;
    private readonly GitHubOAuthClient _githubClient;
    private readonly IAuthService _authService;
    private readonly string _clientAppUrl;

    public ExternalAuthController(
        OAuthSettings oauthSettings,
        GoogleOAuthClient googleClient,
        GitHubOAuthClient githubClient,
        IAuthService authService,
        string clientAppUrl)
    {
        _oauthSettings = oauthSettings;
        _googleClient = googleClient;
        _githubClient = githubClient;
        _authService = authService;
        _clientAppUrl = clientAppUrl;
    }

    [HttpGet("google/login")]
    public IActionResult GoogleLogin()
    {
        var redirectUri = BuildCallbackUrl("google");
        var url = "https://accounts.google.com/o/oauth2/v2/auth" +
            $"?client_id={Uri.EscapeDataString(_oauthSettings.Google.ClientId)}" +
            $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
            "&response_type=code&scope=openid%20email%20profile&prompt=select_account";
        return Redirect(url);
    }

    [HttpGet("google/callback")]
    public async Task<IActionResult> GoogleCallback([FromQuery] string? code, [FromQuery] string? error, CancellationToken ct)
    {
        if (!string.IsNullOrEmpty(error) || string.IsNullOrEmpty(code))
        {
            return Redirect($"{_clientAppUrl}/auth/external-callback?error=1");
        }

        var redirectUri = BuildCallbackUrl("google");
        var info = await _googleClient.ExchangeCodeAsync(code, redirectUri, _oauthSettings.Google, ct);
        var result = await _authService.ExternalLoginAsync("Google", info.ProviderKey, info.Email, info.FullName, ct);
        return Redirect(BuildClientRedirect(result));
    }

    [HttpGet("github/login")]
    public IActionResult GitHubLogin()
    {
        var redirectUri = BuildCallbackUrl("github");
        var url = "https://github.com/login/oauth/authorize" +
            $"?client_id={Uri.EscapeDataString(_oauthSettings.GitHub.ClientId)}" +
            $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
            "&scope=read:user%20user:email";
        return Redirect(url);
    }

    [HttpGet("github/callback")]
    public async Task<IActionResult> GitHubCallback([FromQuery] string? code, [FromQuery] string? error, CancellationToken ct)
    {
        if (!string.IsNullOrEmpty(error) || string.IsNullOrEmpty(code))
        {
            return Redirect($"{_clientAppUrl}/auth/external-callback?error=1");
        }

        var redirectUri = BuildCallbackUrl("github");
        var info = await _githubClient.ExchangeCodeAsync(code, redirectUri, _oauthSettings.GitHub, ct);
        var result = await _authService.ExternalLoginAsync("GitHub", info.ProviderKey, info.Email, info.FullName, ct);
        return Redirect(BuildClientRedirect(result));
    }

    private string BuildCallbackUrl(string provider)
    {
        return $"{Request.Scheme}://{Request.Host}/api/auth/external/{provider}/callback";
    }

    private string BuildClientRedirect(AuthResponse result)
    {
        var query = $"accessToken={Uri.EscapeDataString(result.AccessToken)}" +
                    $"&refreshToken={Uri.EscapeDataString(result.RefreshToken)}" +
                    $"&userId={result.UserId}" +
                    $"&email={Uri.EscapeDataString(result.Email)}" +
                    $"&fullName={Uri.EscapeDataString(result.FullName)}";
        return $"{_clientAppUrl}/auth/external-callback?{query}";
    }
}