namespace DevMentor.Application.Features.QuestionBank;

public interface IQuestionBankService
{
    Task<GenerateQuestionsResultDto> GenerateAndReviewAsync(GenerateQuestionsRequest request, CancellationToken ct = default);
    Task<List<QuestionForReviewDto>> GetByStatusAsync(QuestionStatus status, CancellationToken ct = default);
    Task ReportQuestionAsync(ReportQuestionRequest request, CancellationToken ct = default);
}