using DevMentor.Application.Abstractions;
using DevMentor.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace DevMentor.Infrastructure.Ai;

public class CompositeAiClient : IAiClient
{
    private readonly GeminiAiClient _primary;
    private readonly GroqAiClient _fallback;
    private readonly AiSettings _settings;
    private readonly ILogger<CompositeAiClient> _logger;

    public CompositeAiClient(GeminiAiClient primary, GroqAiClient fallback, AiSettings settings, ILogger<CompositeAiClient> logger)
    {
        _primary = primary;
        _fallback = fallback;
        _settings = settings;
        _logger = logger;
    }

    public Task<List<GeneratedQuestion>> GenerateQuestionsAsync(TechDomain domain, Level level, int count, CancellationToken ct = default)
    {
        return RunWithFallbackAsync(
            () => _primary.GenerateQuestionsAsync(domain, level, count, ct),
            () => _fallback.GenerateQuestionsAsync(domain, level, count, ct));
    }

    public Task<int> SolveBlindAsync(string questionText, List<string> optionTexts, CancellationToken ct = default)
    {
        return RunWithFallbackAsync(
            () => _fallback.SolveBlindAsync(questionText, optionTexts, ct),
            () => _primary.SolveBlindAsync(questionText, optionTexts, ct));
    }

    public Task<string> GetFirstInterviewQuestionAsync(TechDomain domain, CancellationToken ct = default)
    {
        return RunWithFallbackAsync(
            () => _primary.GetFirstInterviewQuestionAsync(domain, ct),
            () => _fallback.GetFirstInterviewQuestionAsync(domain, ct));
    }

    public Task<InterviewEvaluationResult> EvaluateInterviewTurnAsync(
        TechDomain domain,
        List<InterviewTurnContext> history,
        string currentQuestion,
        string currentAnswer,
        int turnNumber,
        int maxTurns,
        CancellationToken ct = default)
    {
        return RunWithFallbackAsync(
            () => _primary.EvaluateInterviewTurnAsync(domain, history, currentQuestion, currentAnswer, turnNumber, maxTurns, ct),
            () => _fallback.EvaluateInterviewTurnAsync(domain, history, currentQuestion, currentAnswer, turnNumber, maxTurns, ct));
    }

    private async Task<T> RunWithFallbackAsync<T>(Func<Task<T>> primaryCall, Func<Task<T>> fallbackCall)
    {
        for (var attempt = 1; attempt <= _settings.MaxRetries; attempt++)
        {
            try
            {
                return await primaryCall();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Primary AI provider failed on attempt {Attempt}", attempt);
            }
        }

        try
        {
            return await fallbackCall();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fallback AI provider also failed");
            throw new InvalidOperationException("The AI assistant is temporarily unavailable, please try again shortly");
        }
    }
}