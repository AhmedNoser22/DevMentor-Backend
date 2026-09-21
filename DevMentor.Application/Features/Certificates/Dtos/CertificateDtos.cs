namespace DevMentor.Application.Features.Certificates.Dtos;

public record CertificateDto(
    Guid Id,
    TechDomain Domain,
    Level Level,
    int ScorePercentage,
    string CertificateCode,
    DateTime IssuedAtUtc);

public record VerifyCertificateResultDto(bool IsValid, CertificateDto? Certificate, string? HolderName);