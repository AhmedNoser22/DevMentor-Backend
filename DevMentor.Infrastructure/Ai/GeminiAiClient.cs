namespace DevMentor.Infrastructure.Ai;

public class GeminiAiClient
{
    private readonly HttpClient _httpClient;
    private readonly AiSettings _settings;
    private readonly ILogger<GeminiAiClient> _logger;

    public GeminiAiClient(HttpClient httpClient, AiSettings settings, ILogger<GeminiAiClient> logger)
    {
        _httpClient = httpClient;
        _settings = settings;
        _logger = logger;
    }

    public async Task<List<GeneratedQuestion>> GenerateQuestionsAsync(TechDomain domain, Level level, int count, CancellationToken ct)
    {
        var prompt = $"Generate {count} multiple choice interview questions for the {domain} domain at {level} level. " +
                     "Return only a JSON array, each item has fields text and options, options is an array of 4 items " +
                     "each with fields text and isCorrect, exactly one option must have isCorrect true. " +
                     "Return raw JSON only, no explanation, no markdown.";

        var response = await CallGeminiAsync(prompt, ct);
        var json = AiJsonExtractor.ExtractJsonArray(response);

        try
        {
            return JsonSerializer.Deserialize<List<GeneratedQuestion>>(json, AiResponseParser.JsonOptions) ?? new();
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Gemini returned unparsable question-generation JSON: {Raw}", response);
            throw;
        }
    }

    public async Task<string> GetFirstInterviewQuestionAsync(TechDomain domain, CancellationToken ct)
    {
        var prompt = $"You are a friendly senior technical interviewer for {domain}. " +
                     "Ask one opening interview question, conversational tone, no numbering, no preamble, plain text only.";
        var response = await CallGeminiAsync(prompt, ct);
        return response.Trim();
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
                     "score (integer 0 to 100), followUpQuestion (a natural next question — never empty unless isFinalTurn is true), " +
                     "isFinalTurn (boolean, true only if there is nothing more useful to ask). " +
                     "Return raw JSON only, no explanation, no markdown.";

        var response = await CallGeminiAsync(prompt, ct);
        var json = AiJsonExtractor.ExtractJsonObject(response);

        InterviewEvaluationResult? parsed;
        try
        {
            parsed = JsonSerializer.Deserialize<InterviewEvaluationResult>(json, AiResponseParser.JsonOptions);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Gemini returned unparsable evaluation JSON: {Raw}", response);
            throw;
        }

        if (parsed is null || (!parsed.IsFinalTurn && string.IsNullOrWhiteSpace(parsed.FollowUpQuestion)))
        {
            _logger.LogWarning("Gemini evaluation was incomplete. Raw: {Raw}", response);
            throw new JsonException("Evaluation result missing a required follow-up question");
        }

        return parsed;
    }

    public async Task<int> SolveBlindAsync(string questionText, List<string> optionTexts, CancellationToken ct)
    {
        var lettered = AiResponseParser.BuildLetteredOptions(optionTexts);
        var validLetters = AiResponseParser.ValidLettersLabel(optionTexts.Count);
        var prompt = $"Question: {questionText}\nOptions:\n{lettered}\n" +
                     $"Reply with only the single letter of the correct option ({validLetters}), nothing else.";
        var response = await CallGeminiAsync(prompt, ct);
        return AiResponseParser.ParseLetterToIndex(response, optionTexts.Count);
    }

    private async Task<string> CallGeminiAsync(string prompt, CancellationToken ct)
    {
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_settings.GeminiModel}:generateContent?key={_settings.GeminiApiKey}";

        var payload = new { contents = new[] { new { parts = new[] { new { text = prompt } } } } };
        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync(url, content, ct);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync(ct);
        using var document = JsonDocument.Parse(body);
        return document.RootElement
            .GetProperty("candidates")[0]
            .GetProperty("content")
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString() ?? string.Empty;
    }
}