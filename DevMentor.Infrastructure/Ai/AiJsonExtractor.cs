namespace DevMentor.Infrastructure.Ai;

public static class AiJsonExtractor
{
    public static string ExtractJsonObject(string text)
    {
        var cleaned = StripFences(text);
        var start = cleaned.IndexOf('{');
        var end = cleaned.LastIndexOf('}');
        return start >= 0 && end > start ? cleaned[start..(end + 1)] : cleaned;
    }

    public static string ExtractJsonArray(string text)
    {
        var cleaned = StripFences(text);
        var start = cleaned.IndexOf('[');
        var end = cleaned.LastIndexOf(']');
        return start >= 0 && end > start ? cleaned[start..(end + 1)] : cleaned;
    }

    private static string StripFences(string text)
    {
        return text.Replace("```json", string.Empty).Replace("```", string.Empty).Trim();
    }
}