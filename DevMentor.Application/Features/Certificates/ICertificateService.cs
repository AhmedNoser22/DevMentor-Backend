namespace DevMentor.Application.Features.Certificates;

public interface ICertificateService
{
    Task<List<CertificateDto>> GetForUserAsync(Guid userId, CancellationToken ct = default);
    Task<byte[]> GeneratePdfAsync(Guid userId, Guid certificateId, CancellationToken ct = default);
    Task<VerifyCertificateResultDto> VerifyAsync(string code, CancellationToken ct = default);
}