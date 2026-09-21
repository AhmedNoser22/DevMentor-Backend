namespace DevMentor.API.Controllers;

[Route("api/certificates")]
public class CertificatesController : BaseApiController
{
    private readonly ICertificateService _certificateService;

    public CertificatesController(ICertificateService certificateService)
    {
        _certificateService = certificateService;
    }

    [Authorize]
    [HttpGet("mine")]
    public async Task<ActionResult<List<CertificateDto>>> Mine(CancellationToken ct)
    {
        var certificates = await _certificateService.GetForUserAsync(CurrentUserId, ct);
        return Ok(certificates);
    }

    [Authorize]
    [HttpGet("{certificateId:guid}/pdf")]
    public async Task<IActionResult> DownloadPdf(Guid certificateId, CancellationToken ct)
    {
        var bytes = await _certificateService.GeneratePdfAsync(CurrentUserId, certificateId, ct);
        return File(bytes, "application/pdf", $"certificate-{certificateId}.pdf");
    }

    [AllowAnonymous]
    [HttpGet("verify/{code}")]
    public async Task<ActionResult<VerifyCertificateResultDto>> Verify(string code, CancellationToken ct)
    {
        var result = await _certificateService.VerifyAsync(code, ct);
        return Ok(result);
    }
}