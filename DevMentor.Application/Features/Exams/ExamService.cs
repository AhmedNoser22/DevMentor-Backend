namespace DevMentor.Application.Features.Exams;

public class ExamService : IExamService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ExamSettings _examSettings;

    public ExamService(IUnitOfWork unitOfWork, ExamSettings examSettings)
    {
        _unitOfWork = unitOfWork;
        _examSettings = examSettings;
    }

    public async Task<ExamAttemptDto> StartAsync(Guid userId, StartExamRequest request, CancellationToken ct = default)
    {
        var existing = await _unitOfWork.ExamAttempts.GetActiveAttemptAsync(userId, request.Domain, request.Level, ct);
        if (existing is not null && !existing.IsPastDeadline())
        {
            return await MapAttemptAsync(existing, ct);
        }

        var levelSettings = _examSettings.ForLevel(request.Level);
        var questions = await _unitOfWork.Questions.GetApprovedRandomAsync(
            request.Domain, request.Level, levelSettings.QuestionCount, ct);

        if (questions.Count < levelSettings.QuestionCount)
        {
            throw new ConflictException("Not enough approved questions available for this domain and level");
        }

        var attempt = ExamAttempt.Start(userId, request.Domain, request.Level, questions, levelSettings.DurationMinutes);

        await _unitOfWork.ExamAttempts.AddAsync(attempt, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        var reloaded = await _unitOfWork.ExamAttempts.GetByIdAsync(attempt.Id, ct)
            ?? throw new NotFoundException("Attempt not found after creation");

        return await MapAttemptAsync(reloaded, ct);
    }

    public async Task<ExamAttemptDto> GetAsync(Guid userId, Guid attemptId, CancellationToken ct = default)
    {
        var attempt = await _unitOfWork.ExamAttempts.GetByIdAsync(attemptId, ct)
            ?? throw new NotFoundException("Attempt not found");

        EnsureOwnership(attempt, userId);

        attempt.ExpireIfPastDeadline();
        _unitOfWork.ExamAttempts.Update(attempt);
        await _unitOfWork.SaveChangesAsync(ct);

        return await MapAttemptAsync(attempt, ct);
    }

    public async Task SaveAnswerAsync(Guid userId, SaveAnswerRequest request, CancellationToken ct = default)
    {
        var attempt = await _unitOfWork.ExamAttempts.GetByIdAsync(request.AttemptId, ct)
            ?? throw new NotFoundException("Attempt not found");

        EnsureOwnership(attempt, userId);

        attempt.RecordAnswer(request.QuestionId, request.SelectedOptionId);

        _unitOfWork.ExamAttempts.Update(attempt);
        await _unitOfWork.SaveChangesAsync(ct);
    }

    public async Task<ExamResultDto> SubmitAsync(Guid userId, Guid attemptId, CancellationToken ct = default)
    {
        var attempt = await _unitOfWork.ExamAttempts.GetByIdAsync(attemptId, ct)
            ?? throw new NotFoundException("Attempt not found");

        EnsureOwnership(attempt, userId);

        await using var transaction = await _unitOfWork.BeginTransactionAsync(ct);

        var submission = attempt.Submit(_examSettings.PassPercentage);
        _unitOfWork.ExamAttempts.Update(attempt);

        string? certificateCode = null;

        if (submission.Passed)
        {
            var existingCertificate = await _unitOfWork.Certificates.GetByUserDomainLevelAsync(
                userId, attempt.Domain, attempt.Level, ct);

            if (existingCertificate is null)
            {
                var certificate = Certificate.Issue(
                    userId, attempt.Domain, attempt.Level, submission.ScorePercentage, attempt.Id,
                    BuildCertificateCode(attempt.Domain, attempt.Level));

                await _unitOfWork.Certificates.AddAsync(certificate, ct);
                certificateCode = certificate.CertificateCode;
            }
            else
            {
                existingCertificate.UpdateIfHigherScore(submission.ScorePercentage, attempt.Id);
                _unitOfWork.Certificates.Update(existingCertificate);
                certificateCode = existingCertificate.CertificateCode;
            }
        }

        await _unitOfWork.SaveChangesAsync(ct);

        return new ExamResultDto(attempt.Id, submission.ScorePercentage, submission.Passed, certificateCode is not null, certificateCode);
    }

    private static void EnsureOwnership(ExamAttempt attempt, Guid userId)
    {
        if (attempt.UserId != userId)
        {
            throw new UnauthorizedAppException("This attempt does not belong to the current user");
        }
    }

    private static string BuildCertificateCode(TechDomain domain, Level level)
    {
        var domainCode = domain switch
        {
            TechDomain.DotNet => "NET",
            TechDomain.Angular => "NG",
            TechDomain.Sql => "SQL",
            TechDomain.SystemDesign => "SD",
            _ => "GEN"
        };

        var levelCode = level switch
        {
            Level.Beginner => "BEG",
            Level.Intermediate => "INT",
            Level.Advanced => "ADV",
            _ => "GEN"
        };

        return $"DM-{domainCode}-{levelCode}-{Random.Shared.Next(10000, 99999)}";
    }

    private async Task<ExamAttemptDto> MapAttemptAsync(ExamAttempt attempt, CancellationToken ct)
    {
        var questions = new List<ExamQuestionDto>();
        foreach (var answer in attempt.Answers.OrderBy(a => a.DisplayOrder))
        {
            var question = answer.Question ?? await _unitOfWork.Questions.GetByIdAsync(answer.QuestionId, ct);
            if (question is null)
            {
                continue;
            }

            questions.Add(new ExamQuestionDto(
                question.Id,
                question.Text,
                question.Options.Select(o => new ExamOptionDto(o.Id, o.Text)).ToList(),
                answer.SelectedOptionId));
        }

        return new ExamAttemptDto(attempt.Id, attempt.Domain, attempt.Level, attempt.Status, attempt.ExpiresAtUtc, questions);
    }
}