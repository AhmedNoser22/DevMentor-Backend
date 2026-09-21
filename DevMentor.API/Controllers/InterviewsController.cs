namespace DevMentor.API.Controllers;

[Authorize]
[Route("api/interviews")]
public class InterviewsController : BaseApiController
{
    private readonly IInterviewService _interviewService;

    public InterviewsController(IInterviewService interviewService)
    {
        _interviewService = interviewService;
    }

    [EnableRateLimiting("strict")]
    [HttpPost("start")]
    public async Task<ActionResult<InterviewSessionDto>> Start(StartInterviewRequest request, CancellationToken ct)
    {
        var session = await _interviewService.StartAsync(CurrentUserId, request, ct);
        return Ok(session);
    }

    [EnableRateLimiting("strict")]
    [HttpPost("answer")]
    public async Task<ActionResult<InterviewSessionDto>> Answer(AnswerTurnRequest request, CancellationToken ct)
    {
        var session = await _interviewService.AnswerAsync(CurrentUserId, request, ct);
        return Ok(session);
    }

    [HttpGet("{sessionId:guid}")]
    public async Task<ActionResult<InterviewSessionDto>> Get(Guid sessionId, CancellationToken ct)
    {
        var session = await _interviewService.GetAsync(CurrentUserId, sessionId, ct);
        return Ok(session);
    }
}