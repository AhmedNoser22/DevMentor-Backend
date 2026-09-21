namespace DevMentor.API.Controllers;

[Authorize]
[Route("api/question-bank")]
public class QuestionBankController : BaseApiController
{
    private readonly IQuestionBankService _questionBankService;

    public QuestionBankController(IQuestionBankService questionBankService)
    {
        _questionBankService = questionBankService;
    }

    [HttpPost("generate")]
    public async Task<ActionResult<int>> Generate(GenerateQuestionsRequest request, CancellationToken ct)
    {
        var approved = await _questionBankService.GenerateAndReviewAsync(request, ct);
        return Ok(approved);
    }

    [HttpGet("by-status/{status}")]
    public async Task<ActionResult<List<QuestionForReviewDto>>> GetByStatus(QuestionStatus status, CancellationToken ct)
    {
        var questions = await _questionBankService.GetByStatusAsync(status, ct);
        return Ok(questions);
    }

    [HttpPost("report")]
    public async Task<IActionResult> Report(ReportQuestionRequest request, CancellationToken ct)
    {
        await _questionBankService.ReportQuestionAsync(request, ct);
        return NoContent();
    }
}