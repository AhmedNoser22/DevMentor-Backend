namespace DevMentor.Infrastructure.ExternalAuth;

public class GitHubOAuthClient
{
    private readonly HttpClient _httpClient;

    public GitHubOAuthClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("DevMentor");
    }

    public async Task<ExternalUserInfo> ExchangeCodeAsync(string code, string redirectUri, OAuthProviderSettings settings, CancellationToken ct)
    {
        var tokenRequest = new Dictionary<string, string>
        {
            ["client_id"] = settings.ClientId,
            ["client_secret"] = settings.ClientSecret,
            ["code"] = code,
            ["redirect_uri"] = redirectUri
        };

        var tokenHttpRequest = new HttpRequestMessage(HttpMethod.Post, "https://github.com/login/oauth/access_token")
        {
            Content = new FormUrlEncodedContent(tokenRequest)
        };
        tokenHttpRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var tokenResponse = await _httpClient.SendAsync(tokenHttpRequest, ct);
        tokenResponse.EnsureSuccessStatusCode();

        var tokenBody = await tokenResponse.Content.ReadAsStringAsync(ct);
        using var tokenDoc = JsonDocument.Parse(tokenBody);
        var accessToken = tokenDoc.RootElement.GetProperty("access_token").GetString()!;

        var userRequest = new HttpRequestMessage(HttpMethod.Get, "https://api.github.com/user");
        userRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var userResponse = await _httpClient.SendAsync(userRequest, ct);
        userResponse.EnsureSuccessStatusCode();

        var userBody = await userResponse.Content.ReadAsStringAsync(ct);
        using var userDoc = JsonDocument.Parse(userBody);
        var userRoot = userDoc.RootElement;

        var id = userRoot.GetProperty("id").GetInt64().ToString();
        var name = userRoot.TryGetProperty("name", out var nameEl) && nameEl.ValueKind == JsonValueKind.String
            ? nameEl.GetString()!
            : userRoot.GetProperty("login").GetString()!;

        var email = userRoot.TryGetProperty("email", out var emailEl) && emailEl.ValueKind == JsonValueKind.String
            ? emailEl.GetString()
            : null;

        if (string.IsNullOrWhiteSpace(email))
        {
            var emailsRequest = new HttpRequestMessage(HttpMethod.Get, "https://api.github.com/user/emails");
            emailsRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            var emailsResponse = await _httpClient.SendAsync(emailsRequest, ct);
            emailsResponse.EnsureSuccessStatusCode();

            var emailsBody = await emailsResponse.Content.ReadAsStringAsync(ct);
            using var emailsDoc = JsonDocument.Parse(emailsBody);
            email = emailsDoc.RootElement.EnumerateArray()
                .Where(e => e.GetProperty("verified").GetBoolean())
                .OrderByDescending(e => e.GetProperty("primary").GetBoolean())
                .Select(e => e.GetProperty("email").GetString())
                .FirstOrDefault();
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ConflictException("Your GitHub account has no verified email address to sign in with");
        }

        return new ExternalUserInfo(id, email, name);
    }
}