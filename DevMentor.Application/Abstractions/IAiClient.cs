namespace DevMentor.Application.Abstractions;

public record GeneratedOption(string Text, bool IsCorrect);

public record GeneratedQuestion(string Text, List<GeneratedOption> Options);

public record InterviewTurnContext(string Question, string Answer);

public record InterviewEvaluationResult(
    string TechnicalAccuracy,
    string MissingConcepts,
    string Communication,
    int Score,
    string FollowUpQuestion,
    bool IsFinalTurn);

public interface IAiClient
{
    Task<List<GeneratedQuestion>> GenerateQuestionsAsync(TechDomain domain, Level level, int count, CancellationToken ct = default);
    Task<string> SolveBlindAsync(string questionText, List<string> optionTexts, CancellationToken ct = default);
    Task<string> GetFirstInterviewQuestionAsync(TechDomain domain, CancellationToken ct = default);
    Task<InterviewEvaluationResult> EvaluateInterviewTurnAsync(
        TechDomain domain,
        List<InterviewTurnContext> history,
        string currentQuestion,
        string currentAnswer,
        int turnNumber,
        int maxTurns,
        CancellationToken ct = default);
}