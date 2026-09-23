using Microsoft.Extensions.Logging;
namespace DevMentor.Application.Features.QuestionBank;

public class QuestionBankService : IQuestionBankService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAiClient _aiClient;
    private readonly ILogger<QuestionBankService> _logger;

    public QuestionBankService(IUnitOfWork unitOfWork, IAiClient aiClient, ILogger<QuestionBankService> logger)
    {
        _unitOfWork = unitOfWork;
        _aiClient = aiClient;
        _logger = logger;
    }

    public async Task<GenerateQuestionsResultDto> GenerateAndReviewAsync(GenerateQuestionsRequest request, CancellationToken ct = default)
    {
        var generated = await _aiClient.GenerateQuestionsAsync(request.Domain, request.Level, request.Count, ct);
        var approvedCount = 0;
        var toInsert = new List<Question>();

        foreach (var candidate in generated)
        {
            var correctIndex = candidate.Options.FindIndex(o => o.IsCorrect);
            if (correctIndex < 0 || candidate.Options.Count < 2)
            {
                continue;
            }

            var question = new Question(request.Domain, request.Level, candidate.Text);
            foreach (var option in candidate.Options)
            {
                question.AddOption(option.Text, option.IsCorrect);
            }

            try
            {
                var solvedIndex = await _aiClient.SolveBlindAsync(
                    candidate.Text,
                    candidate.Options.Select(o => o.Text).ToList(),
                    ct);

                if (solvedIndex == correctIndex)
                {
                    question.Approve();
                    approvedCount++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not validate a generated question, leaving it pending review: {Text}", candidate.Text);
            }

            toInsert.Add(question);
        }

        await _unitOfWork.Questions.AddRangeAsync(toInsert, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return new GenerateQuestionsResultDto(toInsert.Count, approvedCount);
    }

    public async Task<List<QuestionForReviewDto>> GetByStatusAsync(QuestionStatus status, CancellationToken ct = default)
    {
        var questions = await _unitOfWork.Questions.GetByStatusAsync(status, ct);
        return questions.Select(q => new QuestionForReviewDto(
            q.Id,
            q.Domain,
            q.Level,
            q.Text,
            q.Status,
            q.Options.Select(o => new QuestionOptionDto(o.Id, o.Text)).ToList())).ToList();
    }

    public async Task ReportQuestionAsync(ReportQuestionRequest request, CancellationToken ct = default)
    {
        var question = await _unitOfWork.Questions.GetByIdAsync(request.QuestionId, ct)
            ?? throw new NotFoundException("Question not found");

        question.Report();

        _unitOfWork.Questions.Update(question);
        await _unitOfWork.SaveChangesAsync(ct);
    }
}