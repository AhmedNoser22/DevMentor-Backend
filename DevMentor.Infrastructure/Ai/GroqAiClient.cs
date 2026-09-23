namespace DevMentor.Infrastructure.Ai;

public class GroqAiClient
{
    private readonly HttpClient _httpClient;
    private readonly AiSettings _settings;
    private readonly ILogger<GroqAiClient> _logger;

    public GroqAiClient(HttpClient httpClient, AiSettings settings, ILogger<GroqAiClient> logger)
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
        var raw = await CallGroqRawAsync(prompt, ct);
        var json = AiJsonExtractor.ExtractJsonArray(raw);

        try
        {
            return JsonSerializer.Deserialize<List<GeneratedQuestion>>(json, AiResponseParser.JsonOptions) ?? new();
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Groq returned unparsable question-generation JSON: {Raw}", raw);
            throw;
        }
    }

    public async Task<string> GetFirstInterviewQuestionAsync(TechDomain domain, CancellationToken ct)
    {
        var prompt = $"You are a friendly senior technical interviewer for {domain}. " +
                     "Ask one opening interview question, conversational tone, no numbering, no preamble, plain text only.";
        var response = await CallGroqRawAsync(prompt, ct);
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
                     "Return only JSON with fields technicalAccuracy, missingConcepts, communication, score (0 to 100), " +
                     "followUpQuestion (never empty unless isFinalTurn is true), isFinalTurn (boolean). " +
                     "Return raw JSON only, no explanation, no markdown.";

        var raw = await CallGroqRawAsync(prompt, ct);
        var json = AiJsonExtractor.ExtractJsonObject(raw);

        InterviewEvaluationResult? parsed;
        try
        {
            parsed = JsonSerializer.Deserialize<InterviewEvaluationResult>(json, AiResponseParser.JsonOptions);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Groq returned unparsable evaluation JSON: {Raw}", raw);
            throw;
        }

        if (parsed is null || (!parsed.IsFinalTurn && string.IsNullOrWhiteSpace(parsed.FollowUpQuestion)))
        {
            _logger.LogWarning("Groq evaluation was incomplete. Raw: {Raw}", raw);
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
        var response = await CallGroqRawAsync(prompt, ct);
        return AiResponseParser.ParseLetterToIndex(response, optionTexts.Count);
    }

    private async Task<string> CallGroqRawAsync(string prompt, CancellationToken ct)
    {
        var payload = new { model = _settings.GroqModel, messages = new[] { new { role = "user", content = prompt } } };

        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.groq.com/openai/v1/chat/completions")
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
        };
        request.Headers.Add("Authorization", $"Bearer {_settings.GroqApiKey}");

        var response = await _httpClient.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync(ct);
        using var document = JsonDocument.Parse(body);
        return document.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? string.Empty;
    }
}