namespace DevMentor.API.Controllers;

[Authorize]
[Route("api/exams")]
public class ExamsController : BaseApiController
{
    private readonly IExamService _examService;

    public ExamsController(IExamService examService)
    {
        _examService = examService;
    }

    [EnableRateLimiting("strict")]
    [HttpPost("start")]
    public async Task<ActionResult<ExamAttemptDto>> Start(StartExamRequest request, CancellationToken ct)
    {
        var attempt = await _examService.StartAsync(CurrentUserId, request, ct);
        return Ok(attempt);
    }

    [HttpGet("{attemptId:guid}")]
    public async Task<ActionResult<ExamAttemptDto>> Get(Guid attemptId, CancellationToken ct)
    {
        var attempt = await _examService.GetAsync(CurrentUserId, attemptId, ct);
        return Ok(attempt);
    }

    [HttpPost("answer")]
    public async Task<IActionResult> SaveAnswer(SaveAnswerRequest request, CancellationToken ct)
    {
        await _examService.SaveAnswerAsync(CurrentUserId, request, ct);
        return NoContent();
    }

    [HttpPost("{attemptId:guid}/submit")]
    public async Task<ActionResult<ExamResultDto>> Submit(Guid attemptId, CancellationToken ct)
    {
        var result = await _examService.SubmitAsync(CurrentUserId, attemptId, ct);
        return Ok(result);
    }
}