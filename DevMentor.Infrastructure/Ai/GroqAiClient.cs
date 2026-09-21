namespace DevMentor.Infrastructure.Ai;

public class GroqAiClient
{
    private readonly HttpClient _httpClient;
    private readonly AiSettings _settings;

    public GroqAiClient(HttpClient httpClient, AiSettings settings)
    {
        _httpClient = httpClient;
        _settings = settings;
    }

    public Task<List<GeneratedQuestion>> GenerateQuestionsAsync(TechDomain domain, Level level, int count, CancellationToken ct)
    {
        var prompt = $"Generate {count} multiple choice interview questions for the {domain} domain at {level} level. " +
                     "Return only a JSON array, each item has fields text and options, options is an array of 4 items " +
                     "each with fields text and isCorrect, exactly one option must have isCorrect true.";
        return CallGroqAsync<List<GeneratedQuestion>>(prompt, ct);
    }

    public async Task<string> GetFirstInterviewQuestionAsync(TechDomain domain, CancellationToken ct)
    {
        var prompt = $"You are a friendly senior technical interviewer for {domain}. " +
                     "Ask one opening interview question, conversational tone, no numbering, no preamble, plain text only.";
        return await CallGroqRawAsync(prompt, ct);
    }

    public Task<InterviewEvaluationResult> EvaluateInterviewTurnAsync(
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
                     "Return only JSON with fields technicalAccuracy, missingConcepts, communication, score (0 to 100), " +
                     "followUpQuestion, isFinalTurn (boolean).";
        return CallGroqAsync<InterviewEvaluationResult>(prompt, ct);
    }

    public async Task<string> SolveBlindAsync(string questionText, List<string> optionTexts, CancellationToken ct)
    {
        var prompt = $"Question: {questionText}\nOptions: {string.Join(" | ", optionTexts)}\n" +
                     "Reply with only the exact text of the correct option, nothing else.";
        return await CallGroqRawAsync(prompt, ct);
    }

    private async Task<T> CallGroqAsync<T>(string prompt, CancellationToken ct)
    {
        var raw = await CallGroqRawAsync(prompt, ct);
        return JsonSerializer.Deserialize<T>(raw, JsonOptions)!;
    }

    private async Task<string> CallGroqRawAsync(string prompt, CancellationToken ct)
    {
        var payload = new
        {
            model = _settings.GroqModel,
            messages = new[] { new { role = "user", content = prompt } }
        };

        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.groq.com/openai/v1/chat/completions")
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
        };
        request.Headers.Add("Authorization", $"Bearer {_settings.GroqApiKey}");

        var response = await _httpClient.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync(ct);
        using var document = JsonDocument.Parse(body);
        var text = document.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? string.Empty;

        return text.Replace("```json", string.Empty).Replace("```", string.Empty).Trim();
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };
}