namespace WolManager.Models;

/// <summary>
/// 로그 한 줄.
/// </summary>
public sealed record LogEntry(DateTime Time, string Message);
