namespace Feener;

/// <summary>
/// Centralized constants to decouple hardcoded metadata strings from the codebase.
/// Update PackageName here AND in the .csproj ApplicationId when cloning or renaming the app.
/// </summary>
public static class AppConstants
{
    // The base Android Application ID / package name
    public const string PackageName = "com.fen.loid";

    // Intent Action identifiers (must remain constant expressions for attributes)
    public const string ActionStreakAlarm = PackageName + ".ACTION_STREAK_ALARM";
}
