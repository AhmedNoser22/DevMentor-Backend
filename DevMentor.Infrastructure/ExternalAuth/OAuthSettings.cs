namespace DevMentor.Infrastructure.ExternalAuth;

public class OAuthProviderSettings
{
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
}

public class OAuthSettings
{
    public OAuthProviderSettings Google { get; set; } = new();
    public OAuthProviderSettings GitHub { get; set; } = new();
}