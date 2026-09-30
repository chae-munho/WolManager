using System.Collections.ObjectModel;
using System.Windows.Threading;
using WolManager.Models;

namespace WolManager.Services;

/// <summary>
/// 로그를 메모리에 최대 줄 수만큼 보관하는 구현. 컬렉션은 UI 스레드에서만 바꾼다.
/// </summary>
public sealed class LogService : ILogService
{
    private readonly Dispatcher _dispatcher;
    private readonly ObservableCollection<LogEntry> _entries = new();

    public LogService(Dispatcher dispatcher)
    {
        _dispatcher = dispatcher;
        Entries = new ReadOnlyObservableCollection<LogEntry>(_entries);
    }

    /// <summary>
    /// 로그 목록. 최신 로그가 맨 앞에 있다.
    /// </summary>
    public ReadOnlyObservableCollection<LogEntry> Entries { get; }

    /// <summary>
    /// 현재 시각과 함께 로그를 남긴다. 어느 스레드에서 호출해도 된다.
    /// </summary>
    public void Write(string message)
    {
        var entry = new LogEntry(DateTime.Now, message);
        if (_dispatcher.CheckAccess())
        {
            Add(entry);
        }
        else
        {
            _dispatcher.BeginInvoke(() => Add(entry));
        }
    }

    private void Add(LogEntry entry)
    {
        _entries.Insert(0, entry);
        while (_entries.Count > Constants.LogMaxLines)
        {
            _entries.RemoveAt(_entries.Count - 1);
        }
    }
}
