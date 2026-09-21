namespace DevMentor.Domain.Entities;

public class Certificate : BaseEntity
{
    private Certificate() { }

    private Certificate(Guid userId, TechDomain domain, Level level, int scorePercentage, Guid examAttemptId, string certificateCode)
    {
        UserId = userId;
        Domain = domain;
        Level = level;
        ScorePercentage = scorePercentage;
        ExamAttemptId = examAttemptId;
        CertificateCode = certificateCode;
        IssuedAtUtc = DateTime.UtcNow;
    }

    public Guid UserId { get; private set; }
    public TechDomain Domain { get; private set; }
    public Level Level { get; private set; }
    public int ScorePercentage { get; private set; }
    public string CertificateCode { get; private set; } = string.Empty;
    public Guid ExamAttemptId { get; private set; }
    public DateTime IssuedAtUtc { get; private set; } = DateTime.UtcNow;

    public static Certificate Issue(Guid userId, TechDomain domain, Level level, int scorePercentage, Guid examAttemptId, string certificateCode)
    {
        return new Certificate(userId, domain, level, scorePercentage, examAttemptId, certificateCode);
    }

    public bool UpdateIfHigherScore(int newScore, Guid examAttemptId)
    {
        if (newScore <= ScorePercentage)
        {
            return false;
        }

        ScorePercentage = newScore;
        ExamAttemptId = examAttemptId;
        IssuedAtUtc = DateTime.UtcNow;
        return true;
    }
}