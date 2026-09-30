using System.Collections.ObjectModel;
using WolManager.Models;
using WolManager.Mvvm;
using WolManager.Services;

namespace WolManager.ViewModels;

/// <summary>
/// 로그 영역 ViewModel.
/// </summary>
public sealed class LogViewModel : ObservableObject
{
    public LogViewModel(ILogService log)
    {
        Entries = log.Entries;
    }

    public ReadOnlyObservableCollection<LogEntry> Entries { get; }
}
