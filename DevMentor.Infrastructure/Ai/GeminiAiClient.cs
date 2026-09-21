namespace DevMentor.Infrastructure.Ai;

public class GeminiAiClient
{
    private readonly HttpClient _httpClient;
    private readonly AiSettings _settings;

    public GeminiAiClient(HttpClient httpClient, AiSettings settings)
    {
        _httpClient = httpClient;
        _settings = settings;
    }

    public async Task<List<GeneratedQuestion>> GenerateQuestionsAsync(TechDomain domain, Level level, int count, CancellationToken ct)
    {
        var prompt = $"Generate {count} multiple choice interview questions for the {domain} domain at {level} level. " +
                     "Return only a JSON array, each item has fields text and options, options is an array of 4 items " +
                     "each with fields text and isCorrect, exactly one option must have isCorrect true.";

        var response = await CallGeminiAsync(prompt, ct);
        return JsonSerializer.Deserialize<List<GeneratedQuestion>>(response, JsonOptions) ?? new();
    }

    public async Task<string> GetFirstInterviewQuestionAsync(TechDomain domain, CancellationToken ct)
    {
        var prompt = $"You are a friendly senior technical interviewer for {domain}. " +
                     "Ask one opening interview question, conversational tone, no numbering, no preamble, plain text only.";
        return await CallGeminiAsync(prompt, ct);
    }

    public async Task<InterviewEvaluationResult> EvaluateInterviewTurnAsync(
        TechDomain domain,
        List<InterviewTurnContext> history,
        string currentQuestion,
        string currentAnswer,
        int turnNumber,
        int maxTurns,
        CancellationToken ct)
    {
        var historyText = string.Join("\n", history.Select(h => $"Q: {h.Question}\nA: {h.Answer}"));
        var prompt = $"You are grading turn {turnNumber} of {maxTurns} in a {domain} technical interview.\n" +
                     $"Conversation so far:\n{historyText}\n\nCurrent question: {currentQuestion}\nCandidate answer: {currentAnswer}\n\n" +
                     "Return only JSON with fields technicalAccuracy, missingConcepts, communication (each a short sentence), " +
                     "score (integer 0 to 100), followUpQuestion (a natural next question, empty string if this was the last turn), " +
                     "isFinalTurn (boolean, true only if there is nothing more useful to ask).";

        var response = await CallGeminiAsync(prompt, ct);
        return JsonSerializer.Deserialize<InterviewEvaluationResult>(response, JsonOptions)
            ?? new InterviewEvaluationResult("No feedback available", "None identified", "Clear", 50, "Can you elaborate further?", turnNumber >= maxTurns);
    }

    public async Task<string> SolveBlindAsync(string questionText, List<string> optionTexts, CancellationToken ct)
    {
        var prompt = $"Question: {questionText}\nOptions: {string.Join(" | ", optionTexts)}\n" +
                     "Reply with only the exact text of the correct option, nothing else.";
        return await CallGeminiAsync(prompt, ct);
    }

    private async Task<string> CallGeminiAsync(string prompt, CancellationToken ct)
    {
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_settings.GeminiModel}:generateContent?key={_settings.GeminiApiKey}";

        var payload = new
        {
            contents = new[]
            {
                new { parts = new[] { new { text = prompt } } }
            }
        };

        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync(url, content, ct);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync(ct);
        using var document = JsonDocument.Parse(body);
        var text = document.RootElement
            .GetProperty("candidates")[0]
            .GetProperty("content")
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString() ?? string.Empty;

        return CleanJsonFence(text);
    }

    private static string CleanJsonFence(string text)
    {
        return text.Replace("```json", string.Empty).Replace("```", string.Empty).Trim();
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };
}