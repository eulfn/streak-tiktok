using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Feener.Models;

public record StatusUpdateMessage(string StatusText, bool IsRunning, string? LogEntry = null, bool IsBurstMode = false, int SessionCount = 0);
