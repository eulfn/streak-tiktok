namespace Feener.Services;

/// <summary>
/// Service for managing Burst Chat Mode state and configuration.
/// Burst Chat Mode sends multiple chunked messages per friend with randomized
/// short delays to simulate active chatting and maintain streak vitality.
/// </summary>
[Microsoft.Maui.Controls.Internals.Preserve(AllMembers = true)]
public class BurstChatService
{
    private const string BurstEnabledKey = "burst_chat_enabled";
    private const string BurstCountKey = "burst_chat_count";

    /// <summary>
    /// Minimum number of burst messages per friend
    /// </summary>
    public const int MinBurstCount = 2;

    /// <summary>
    /// Maximum number of burst messages per friend (hard cap for anti-spam)
    /// </summary>
    public const int MaxBurstCount = 5;

    /// <summary>
    /// Default number of burst messages per friend
    /// </summary>
    public const int DefaultBurstCount = 4;

    /// <summary>
    /// Minimum delay between burst messages in milliseconds
    /// </summary>
    public const int MinDelayMs = 3000;

    /// <summary>
    /// Maximum delay between burst messages in milliseconds
    /// </summary>
    public const int MaxDelayMs = 10000;

    /// <summary>
    /// Predefined message chunks used for burst messages.
    /// These are short, natural streak-maintenance phrases.
    /// </summary>
    public static readonly string[] BurstChunks = new[]
    {
        "Streak",
        "🔥",
        "keeping it alive",
        "hey",
        "👋"
    };

    /// <summary>
    /// Get whether Burst Chat Mode is enabled
    /// </summary>
    public bool IsEnabled()
    {
        return Preferences.Get(BurstEnabledKey, false);
    }

    /// <summary>
    /// Set whether Burst Chat Mode is enabled
    /// </summary>
    public void SetEnabled(bool enabled)
    {
        Preferences.Set(BurstEnabledKey, enabled);
    }

    /// <summary>
    /// Get the number of burst messages to send per friend
    /// </summary>
    public int GetBurstCount()
    {
        var count = Preferences.Get(BurstCountKey, DefaultBurstCount);
        return Math.Clamp(count, MinBurstCount, MaxBurstCount);
    }

    /// <summary>
    /// Set the number of burst messages to send per friend
    /// </summary>
    public void SetBurstCount(int count)
    {
        Preferences.Set(BurstCountKey, Math.Clamp(count, MinBurstCount, MaxBurstCount));
    }

    /// <summary>
    /// Generate a list of burst message chunks for one friend.
    /// Uses the primary message as the first chunk, then fills remaining
    /// slots from the predefined BurstChunks pool with randomized selection.
    /// </summary>
    /// <param name="primaryMessage">The user's configured streak message</param>
    /// <returns>List of message strings to send in sequence</returns>
    public List<string> GenerateBurstMessages(string primaryMessage)
    {
        var count = GetBurstCount();
        var messages = new List<string>(count);
        var rng = new Random();

        // First message is always the user's configured message
        messages.Add(primaryMessage);

        // Fill remaining slots from the burst chunks pool
        var availableChunks = BurstChunks
            .Where(c => !c.Equals(primaryMessage, StringComparison.OrdinalIgnoreCase))
            .ToList();

        for (int i = 1; i < count; i++)
        {
            if (availableChunks.Count > 0)
            {
                var idx = rng.Next(availableChunks.Count);
                messages.Add(availableChunks[idx]);
                availableChunks.RemoveAt(idx); // No repeats within one burst
            }
            else
            {
                // Fallback: reuse from the full pool
                messages.Add(BurstChunks[rng.Next(BurstChunks.Length)]);
            }
        }

        return messages;
    }

    /// <summary>
    /// Generate a randomized delay in milliseconds between burst messages.
    /// The delay is uniformly distributed between MinDelayMs and MaxDelayMs.
    /// </summary>
    public int GenerateRandomDelay()
    {
        var rng = new Random();
        return rng.Next(MinDelayMs, MaxDelayMs + 1);
    }
}
