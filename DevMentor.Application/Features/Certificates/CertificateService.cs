namespace DevMentor.Application.Features.Certificates;

public class CertificateService : ICertificateService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICertificatePdfGenerator _pdfGenerator;
    private readonly IUserDisplayNameProvider _userDisplayNameProvider;

    public CertificateService(
        IUnitOfWork unitOfWork,
        ICertificatePdfGenerator pdfGenerator,
        IUserDisplayNameProvider userDisplayNameProvider)
    {
        _unitOfWork = unitOfWork;
        _pdfGenerator = pdfGenerator;
        _userDisplayNameProvider = userDisplayNameProvider;
    }

    public async Task<List<CertificateDto>> GetForUserAsync(Guid userId, CancellationToken ct = default)
    {
        var certificates = await _unitOfWork.Certificates.GetAllByUserAsync(userId, ct);
        return certificates.Select(Map).ToList();
    }

    public async Task<byte[]> GeneratePdfAsync(Guid userId, Guid certificateId, CancellationToken ct = default)
    {
        var certificate = await _unitOfWork.Certificates.GetByIdAsync(certificateId, ct)
            ?? throw new NotFoundException("Certificate not found");

        if (certificate.UserId != userId)
        {
            throw new UnauthorizedAppException("This certificate does not belong to the current user");
        }

        var name = await _userDisplayNameProvider.GetDisplayNameAsync(userId, ct);
        return _pdfGenerator.Generate(certificate, name);
    }

    public async Task<VerifyCertificateResultDto> VerifyAsync(string code, CancellationToken ct = default)
    {
        var certificate = await _unitOfWork.Certificates.GetByCodeAsync(code, ct);
        if (certificate is null)
        {
            return new VerifyCertificateResultDto(false, null, null);
        }

        var name = await _userDisplayNameProvider.GetDisplayNameAsync(certificate.UserId, ct);
        return new VerifyCertificateResultDto(true, Map(certificate), name);
    }

    private static CertificateDto Map(Domain.Entities.Certificate certificate)
    {
        return new CertificateDto(
            certificate.Id, certificate.Domain, certificate.Level,
            certificate.ScorePercentage, certificate.CertificateCode, certificate.IssuedAtUtc);
    }
}