namespace WolManager.Models;

/// 로그 한 줄.
public sealed record LogEntry(DateTime Time, string Message);
