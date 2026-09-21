namespace DevMentor.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("Default")));

        services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
        {
            options.Password.RequiredLength = 8;
            options.User.RequireUniqueEmail = true;
            options.SignIn.RequireConfirmedEmail = true;
        })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        var jwtSettings = configuration.GetSection("Jwt").Get<JwtSettings>() ?? new JwtSettings();
        services.AddSingleton(jwtSettings);
        services.AddSingleton<JwtTokenGenerator>();

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSettings.Issuer,
                    ValidAudience = jwtSettings.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SigningKey)),
                    NameClaimType = "sub"
                };
            });

        var examSettings = configuration.GetSection("ExamSettings").Get<ExamSettings>() ?? new ExamSettings();
        services.AddSingleton(examSettings);

        var interviewSettings = configuration.GetSection("InterviewSettings").Get<InterviewSettings>() ?? new InterviewSettings();
        services.AddSingleton(interviewSettings);

        var aiSettings = configuration.GetSection("Ai").Get<AiSettings>() ?? new AiSettings();
        services.AddSingleton(aiSettings);
        services.AddHttpClient<GeminiAiClient>();
        services.AddHttpClient<GroqAiClient>();
        services.AddScoped<IAiClient, CompositeAiClient>();

        var emailSettings = configuration.GetSection("Email").Get<EmailSettings>() ?? new EmailSettings();
        services.AddSingleton(emailSettings);
        services.AddScoped<IEmailSender, SmtpEmailSender>();

        var clientAppSettings = configuration.GetSection("ClientApp").Get<ClientAppSettings>() ?? new ClientAppSettings();
        services.AddSingleton(clientAppSettings);
        services.AddScoped(sp => sp.GetRequiredService<ClientAppSettings>().BaseUrl);

        var oauthSettings = configuration.GetSection("OAuth").Get<OAuthSettings>() ?? new OAuthSettings();
        services.AddSingleton(oauthSettings);
        services.AddHttpClient<GoogleOAuthClient>();
        services.AddHttpClient<GitHubOAuthClient>();

        services.AddScoped<ICertificatePdfGenerator, QuestPdfCertificateGenerator>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<UserDisplayNameProvider>();
        services.AddScoped<IUserDisplayNameProvider>(sp => sp.GetRequiredService<UserDisplayNameProvider>());
        services.AddScoped<IUserEmailProvider>(sp => sp.GetRequiredService<UserDisplayNameProvider>());

        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }
}