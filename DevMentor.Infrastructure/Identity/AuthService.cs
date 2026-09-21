namespace DevMentor.Infrastructure.Identity;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly JwtTokenGenerator _tokenGenerator;
    private readonly JwtSettings _jwtSettings;
    private readonly AppDbContext _context;
    private readonly IEmailSender _emailSender;
    private readonly string _clientAppUrl;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        JwtTokenGenerator tokenGenerator,
        JwtSettings jwtSettings,
        AppDbContext context,
        IEmailSender emailSender,
        string clientAppUrl)
    {
        _userManager = userManager;
        _tokenGenerator = tokenGenerator;
        _jwtSettings = jwtSettings;
        _context = context;
        _emailSender = emailSender;
        _clientAppUrl = clientAppUrl;
    }

    public async Task<RegisterResult> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        var existing = await _userManager.FindByEmailAsync(request.Email);
        if (existing is not null)
        {
            throw new ConflictException("An account with this email already exists");
        }

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            FullName = request.FullName,
            EmailConfirmed = false
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            var errors = result.Errors.ToDictionary(e => e.Code, e => new[] { e.Description });
            throw new ValidationAppException(errors);
        }

        await SendConfirmationEmailAsync(user, ct);

        return new RegisterResult(
            user.Id,
            user.Email!,
            "Account created. Please check your email to confirm your address before logging in.");
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var user = await _userManager.FindByEmailAsync(request.Email)
            ?? throw new UnauthorizedAppException("Invalid email or password");

        var validPassword = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!validPassword)
        {
            throw new UnauthorizedAppException("Invalid email or password");
        }

        if (!user.EmailConfirmed)
        {
            throw new UnauthorizedAppException("Please confirm your email address before logging in");
        }

        return await IssueTokensAsync(user, ct);
    }

    public async Task<AuthResponse> RefreshAsync(RefreshRequest request, CancellationToken ct = default)
    {
        var hashed = JwtTokenGenerator.Hash(request.RefreshToken);
        var stored = await _context.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hashed, ct);

        if (stored is null || !stored.IsActive)
        {
            throw new UnauthorizedAppException("Invalid or expired refresh token");
        }

        var user = await _userManager.FindByIdAsync(stored.UserId.ToString())
            ?? throw new UnauthorizedAppException("User not found");

        stored.RevokedAtUtc = DateTime.UtcNow;
        _context.RefreshTokens.Update(stored);

        return await IssueTokensAsync(user, ct);
    }

    public async Task LogoutAsync(Guid userId, string refreshToken, CancellationToken ct = default)
    {
        var hashed = JwtTokenGenerator.Hash(refreshToken);
        var stored = await _context.RefreshTokens
            .FirstOrDefaultAsync(t => t.UserId == userId && t.TokenHash == hashed, ct);

        if (stored is not null)
        {
            stored.RevokedAtUtc = DateTime.UtcNow;
            _context.RefreshTokens.Update(stored);
            await _context.SaveChangesAsync(ct);
        }
    }

    public async Task ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken ct = default)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null || !user.EmailConfirmed)
        {
            return;
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var encodedToken = Uri.EscapeDataString(token);
        var link = $"{_clientAppUrl}/auth/reset-password?email={Uri.EscapeDataString(user.Email!)}&token={encodedToken}";
        var body = $"Use the link below to reset your DevMentor password:<br/><a href=\"{link}\">{link}</a>";
        await _emailSender.SendAsync(request.Email, "Reset your DevMentor password", body, ct);
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken ct = default)
    {
        var user = await _userManager.FindByEmailAsync(request.Email)
            ?? throw new NotFoundException("User not found");

        var result = await _userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);
        if (!result.Succeeded)
        {
            var errors = result.Errors.ToDictionary(e => e.Code, e => new[] { e.Description });
            throw new ValidationAppException(errors);
        }
    }

    public async Task ConfirmEmailAsync(ConfirmEmailRequest request, CancellationToken ct = default)
    {
        var user = await _userManager.FindByEmailAsync(request.Email)
            ?? throw new NotFoundException("User not found");

        if (user.EmailConfirmed)
        {
            return;
        }

        var result = await _userManager.ConfirmEmailAsync(user, request.Token);
        if (!result.Succeeded)
        {
            var errors = result.Errors.ToDictionary(e => e.Code, e => new[] { e.Description });
            throw new ValidationAppException(errors);
        }
    }

    public async Task ResendConfirmationEmailAsync(ResendConfirmationRequest request, CancellationToken ct = default)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null || user.EmailConfirmed)
        {
            return;
        }

        await SendConfirmationEmailAsync(user, ct);
    }

    public async Task<AuthResponse> ExternalLoginAsync(string provider, string providerKey, string email, string fullName, CancellationToken ct = default)
    {
        var user = await _userManager.FindByLoginAsync(provider, providerKey);

        if (user is null)
        {
            user = await _userManager.FindByEmailAsync(email);

            if (user is null)
            {
                user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    FullName = fullName,
                    EmailConfirmed = true
                };

                var createResult = await _userManager.CreateAsync(user);
                if (!createResult.Succeeded)
                {
                    var errors = createResult.Errors.ToDictionary(e => e.Code, e => new[] { e.Description });
                    throw new ValidationAppException(errors);
                }
            }
            else if (!user.EmailConfirmed)
            {
                user.EmailConfirmed = true;
                await _userManager.UpdateAsync(user);
            }

            var loginResult = await _userManager.AddLoginAsync(user, new UserLoginInfo(provider, providerKey, provider));
            if (!loginResult.Succeeded)
            {
                var errors = loginResult.Errors.ToDictionary(e => e.Code, e => new[] { e.Description });
                throw new ValidationAppException(errors);
            }
        }

        return await IssueTokensAsync(user, ct);
    }

    private async Task SendConfirmationEmailAsync(ApplicationUser user, CancellationToken ct)
    {
        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        var encodedToken = Uri.EscapeDataString(token);
        var link = $"{_clientAppUrl}/auth/confirm-email?email={Uri.EscapeDataString(user.Email!)}&token={encodedToken}";
        var body = $"Welcome to DevMentor. Please confirm your email by clicking the link below:<br/><a href=\"{link}\">{link}</a>";
        await _emailSender.SendAsync(user.Email!, "Confirm your DevMentor account", body, ct);
    }

    private async Task<AuthResponse> IssueTokensAsync(ApplicationUser user, CancellationToken ct)
    {
        var (accessToken, expiresAt) = _tokenGenerator.GenerateAccessToken(user);
        var refreshToken = _tokenGenerator.GenerateRefreshToken();

        _context.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = JwtTokenGenerator.Hash(refreshToken),
            ExpiresAtUtc = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenDays)
        });

        await _context.SaveChangesAsync(ct);

        return new AuthResponse(user.Id, user.FullName, user.Email!, accessToken, refreshToken, expiresAt);
    }
}