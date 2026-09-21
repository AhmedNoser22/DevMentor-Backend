namespace DevMentor.Application.Common.Settings;

public class ExamLevelSettings
{
    public int QuestionCount { get; set; }
    public int DurationMinutes { get; set; }
}

public class ExamSettings
{
    public int PassPercentage { get; set; } = 85;
    public Dictionary<string, ExamLevelSettings> Levels { get; set; } = new();

    public ExamLevelSettings ForLevel(Level level)
    {
        return Levels.TryGetValue(level.ToString(), out var value)
            ? value
            : new ExamLevelSettings { QuestionCount = 20, DurationMinutes = 20 };
    }
}