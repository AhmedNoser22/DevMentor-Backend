namespace DevMentor.Application.Features.Profile;

public class ProfileService : IProfileService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserDisplayNameProvider _userDisplayNameProvider;
    private readonly IUserEmailProvider _userEmailProvider;
    private readonly ExamSettings _examSettings;

    public ProfileService(
        IUnitOfWork unitOfWork,
        IUserDisplayNameProvider userDisplayNameProvider,
        IUserEmailProvider userEmailProvider,
        ExamSettings examSettings)
    {
        _unitOfWork = unitOfWork;
        _userDisplayNameProvider = userDisplayNameProvider;
        _userEmailProvider = userEmailProvider;
        _examSettings = examSettings;
    }

    public async Task<ProfileDto> GetAsync(Guid userId, CancellationToken ct = default)
    {
        var name = await _userDisplayNameProvider.GetDisplayNameAsync(userId, ct);
        var email = await _userEmailProvider.GetEmailAsync(userId, ct);
        var certificates = await _unitOfWork.Certificates.GetAllByUserAsync(userId, ct);
        var exams = await _unitOfWork.ExamAttempts.GetHistoryAsync(userId, ct);
        var interviews = await _unitOfWork.InterviewSessions.GetHistoryAsync(userId, ct);

        var activity = new List<RecentActivityDto>();
        activity.AddRange(exams.Where(e => e.SubmittedAtUtc is not null).Select(e => new RecentActivityDto(
            "Exam", $"{e.Domain} — {e.Level}", e.SubmittedAtUtc!.Value, e.ScorePercentage,
            e.ScorePercentage.HasValue && e.ScorePercentage >= _examSettings.PassPercentage)));

        activity.AddRange(interviews.Where(i => i.EndedAtUtc is not null).Select(i => new RecentActivityDto(
            "Interview", $"{i.Domain} — AI Interview", i.EndedAtUtc!.Value, i.FinalScore, null)));

        return new ProfileDto(
            userId,
            name,
            email,
            certificates.Count,
            activity.OrderByDescending(a => a.DateUtc).Take(10).ToList(),
            certificates.Select(c => new CertificateSummaryDto(c.Domain, c.Level, c.CertificateCode, c.IssuedAtUtc)).ToList());
    }
}