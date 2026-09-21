namespace DevMentor.Application.Features.QuestionBank;

public class QuestionBankService : IQuestionBankService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAiClient _aiClient;

    public QuestionBankService(IUnitOfWork unitOfWork, IAiClient aiClient)
    {
        _unitOfWork = unitOfWork;
        _aiClient = aiClient;
    }

    public async Task<int> GenerateAndReviewAsync(GenerateQuestionsRequest request, CancellationToken ct = default)
    {
        var generated = await _aiClient.GenerateQuestionsAsync(request.Domain, request.Level, request.Count, ct);
        var approvedCount = 0;
        var toInsert = new List<Question>();

        foreach (var candidate in generated)
        {
            var correctOption = candidate.Options.FirstOrDefault(o => o.IsCorrect);
            if (correctOption is null || candidate.Options.Count < 2)
            {
                continue;
            }

            var solvedText = await _aiClient.SolveBlindAsync(
                candidate.Text,
                candidate.Options.Select(o => o.Text).ToList(),
                ct);

            var isValidated = string.Equals(
                solvedText.Trim(),
                correctOption.Text.Trim(),
                StringComparison.OrdinalIgnoreCase);

            var question = new Question(request.Domain, request.Level, candidate.Text);
            foreach (var option in candidate.Options)
            {
                question.AddOption(option.Text, option.IsCorrect);
            }

            if (isValidated)
            {
                question.Approve();
                approvedCount++;
            }

            toInsert.Add(question);
        }

        await _unitOfWork.Questions.AddRangeAsync(toInsert, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return approvedCount;
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