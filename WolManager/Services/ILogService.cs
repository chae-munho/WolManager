using System.Collections.ObjectModel;
using WolManager.Models;

namespace WolManager.Services;

/// 모든 ViewModel과 서비스가 함께 쓰는 로그 서비스.
public interface ILogService
{
    /// 로그 목록. 최신 로그가 맨 앞에 있다.
    ReadOnlyObservableCollection<LogEntry> Entries { get; }

    /// 현재 시각과 함께 로그를 남긴다. 어느 스레드에서 호출해도 된다.
    void Write(string message);
}
