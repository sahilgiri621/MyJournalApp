namespace MyJournalApp.Models;

public static class MoodCatalog
{
    public static readonly string[] PrimaryMoods = { "Happy", "Neutral", "Sad" };

    public static readonly string[] SecondaryHappy = { "Excited", "Relaxed", "Grateful", "Confident" };
    public static readonly string[] SecondaryNeutral = { "Calm", "Thoughtful", "Curious", "Nostalgic", "Bored" };
    public static readonly string[] SecondarySad = { "Angry", "Stressed", "Lonely", "Anxious" };

    public static IEnumerable<string> AllMoods =>
        PrimaryMoods
            .Concat(SecondaryHappy)
            .Concat(SecondaryNeutral)
            .Concat(SecondarySad)
            .Distinct(StringComparer.OrdinalIgnoreCase);

    public static IEnumerable<string> GetSecondaryMoods(string? primaryMood)
    {
        return primaryMood switch
        {
            "Happy" => SecondaryHappy,
            "Neutral" => SecondaryNeutral,
            "Sad" => SecondarySad,
            _ => Array.Empty<string>()
        };
    }
}

