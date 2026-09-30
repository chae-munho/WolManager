using System.Collections.ObjectModel;
using WolManager.Models;

namespace WolManager.Services;

/// <summary>
/// 모든 ViewModel과 서비스가 함께 쓰는 로그 서비스.
/// </summary>
public interface ILogService
{
    /// <summary>
    /// 로그 목록. 최신 로그가 맨 앞에 있다.
    /// </summary>
    ReadOnlyObservableCollection<LogEntry> Entries { get; }

    /// <summary>
    /// 현재 시각과 함께 로그를 남긴다. 어느 스레드에서 호출해도 된다.
    /// </summary>
    void Write(string message);
}
