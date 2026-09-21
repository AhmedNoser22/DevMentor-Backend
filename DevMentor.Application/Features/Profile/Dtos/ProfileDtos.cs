namespace DevMentor.Application.Features.Profile.Dtos;

public record RecentActivityDto(string Type, string Title, DateTime DateUtc, int? Score, bool? Passed);

public record ProfileDto(
    Guid UserId,
    string FullName,
    string Email,
    int CertificatesCount,
    List<RecentActivityDto> RecentActivity,
    List<CertificateSummaryDto> Certificates);

public record CertificateSummaryDto(TechDomain Domain, Level Level, string CertificateCode, DateTime IssuedAtUtc);