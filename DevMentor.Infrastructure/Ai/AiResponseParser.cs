namespace DevMentor.Infrastructure.Ai;

public static class AiResponseParser
{
    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString
    };

    public static string BuildLetteredOptions(List<string> options)
    {
        return string.Join("\n", options.Select((text, i) => $"{(char)('A' + i)}) {text}"));
    }

    public static string ValidLettersLabel(int optionCount)
    {
        return string.Join(", ", Enumerable.Range(0, optionCount).Select(i => ((char)('A' + i)).ToString()));
    }

    public static int ParseLetterToIndex(string response, int optionCount)
    {
        var letter = response.Trim().FirstOrDefault(char.IsLetter);
        var index = char.ToUpperInvariant(letter) - 'A';

        if (index < 0 || index >= optionCount)
        {
            throw new FormatException($"AI did not return a valid option letter. Raw response: {response}");
        }

        return index;
    }
}