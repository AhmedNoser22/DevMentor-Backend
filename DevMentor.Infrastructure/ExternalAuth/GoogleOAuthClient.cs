namespace DevMentor.Infrastructure.ExternalAuth;

public class GoogleOAuthClient
{
    private readonly HttpClient _httpClient;

    public GoogleOAuthClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<ExternalUserInfo> ExchangeCodeAsync(string code, string redirectUri, OAuthProviderSettings settings, CancellationToken ct)
    {
        var tokenRequest = new Dictionary<string, string>
        {
            ["client_id"] = settings.ClientId,
            ["client_secret"] = settings.ClientSecret,
            ["code"] = code,
            ["grant_type"] = "authorization_code",
            ["redirect_uri"] = redirectUri
        };

        var tokenResponse = await _httpClient.PostAsync(
            "https://oauth2.googleapis.com/token",
            new FormUrlEncodedContent(tokenRequest),
            ct);
        tokenResponse.EnsureSuccessStatusCode();

        var tokenBody = await tokenResponse.Content.ReadAsStringAsync(ct);
        using var tokenDoc = JsonDocument.Parse(tokenBody);
        var accessToken = tokenDoc.RootElement.GetProperty("access_token").GetString()!;

        var userRequest = new HttpRequestMessage(HttpMethod.Get, "https://www.googleapis.com/oauth2/v3/userinfo");
        userRequest.Headers.Add("Authorization", $"Bearer {accessToken}");
        var userResponse = await _httpClient.SendAsync(userRequest, ct);
        userResponse.EnsureSuccessStatusCode();

        var userBody = await userResponse.Content.ReadAsStringAsync(ct);
        using var userDoc = JsonDocument.Parse(userBody);
        var root = userDoc.RootElement;

        var email = root.GetProperty("email").GetString()!;
        var name = root.TryGetProperty("name", out var nameEl) ? nameEl.GetString() ?? email : email;
        var sub = root.GetProperty("sub").GetString()!;

        return new ExternalUserInfo(sub, email, name);
    }
}