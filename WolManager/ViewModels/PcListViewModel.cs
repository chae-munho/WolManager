using System.Collections.ObjectModel;
using System.Collections.Specialized;
using WolManager.Models;
using WolManager.Mvvm;
using WolManager.Services;

namespace WolManager.ViewModels;

/// <summary>
/// PC 목록 영역 ViewModel. 개별/전체 깨우기를 맡는다.
/// </summary>
public sealed class PcListViewModel : ObservableObject
{
    private const string DifferentNetworkMessage =
        "같은 네트워크에 연결되어 있지 않습니다. 유선 또는 같은 공유기의 Wi-Fi에 연결했는지 확인하세요.";

    private readonly IWakeOnLanService _wakeOnLan;
    private readonly IDialogService _dialog;
    private readonly ILogService _log;
    private PcEntry? _selectedPc;

    public PcListViewModel(
        IPcRepository repository,
        IWakeOnLanService wakeOnLan,
        IStatusMonitorService statusMonitor,
        IDialogService dialog,
        ILogService log)
    {
        _wakeOnLan = wakeOnLan;
        _dialog = dialog;
        _log = log;
        Items = repository.Items;

        RefreshCommand = new AsyncRelayCommand(
            statusMonitor.RefreshAsync,
            onError: ex => _log.Write($"상태 확인 중 오류가 발생했습니다. ({ex.Message})"));

        WakeCommand = new AsyncRelayCommand<PcEntry>(
            pc => pc is null ? Task.CompletedTask : WakeAsync([pc]),
            pc => pc is not null,
            OnError);
        WakeAllCommand = new AsyncRelayCommand(WakeAllAsync, () => Items.Count > 0, OnError);

        ((INotifyCollectionChanged)Items).CollectionChanged += (_, _) => WakeAllCommand.RaiseCanExecuteChanged();
    }

    public ReadOnlyObservableCollection<PcEntry> Items { get; }

    /// <summary>목록에서 선택한 PC. 메인 ViewModel이 편집 영역과 연결한다.</summary>
    public PcEntry? SelectedPc
    {
        get => _selectedPc;
        set => SetProperty(ref _selectedPc, value);
    }

    /// <summary>상태를 지금 바로 다시 확인한다.</summary>
    public AsyncRelayCommand RefreshCommand { get; }

    /// <summary>행의 깨우기 버튼. 파라미터로 PC를 받는다.</summary>
    public AsyncRelayCommand<PcEntry> WakeCommand { get; }

    /// <summary>대상으로 체크된 PC 중 켜져 있지 않은 PC를 모두 깨운다.</summary>
    public AsyncRelayCommand WakeAllCommand { get; }

    private async Task WakeAllAsync()
    {
        var targets = Items.Where(pc => pc.IsTarget).ToList();
        if (targets.Count == 0)
        {
            _log.Write("전체 깨우기 대상으로 체크된 PC가 없습니다.");
            return;
        }

        var toWake = targets.Where(pc => pc.Status != PcStatus.On).ToList();
        var skipped = targets.Count - toWake.Count;
        if (toWake.Count == 0)
        {
            _log.Write("대상 PC가 모두 켜져 있습니다.");
            return;
        }

        _log.Write(skipped > 0
            ? $"전체 깨우기: {toWake.Count}대에 신호를 보냅니다. (켜져 있는 {skipped}대 제외)"
            : $"전체 깨우기: {toWake.Count}대에 신호를 보냅니다.");
        await WakeAsync(toWake);
    }

    private async Task WakeAsync(IReadOnlyList<PcEntry> pcs)
    {
        // 다른 네트워크에 붙어 있으면 오류 없이 아무것도 켜지지 않으므로 먼저 알려 준다. 전송은 그대로 진행한다.
        if (_wakeOnLan.IsOnSameNetwork(pcs.Select(pc => pc.Ip)) == false)
        {
            _log.Write(DifferentNetworkMessage);
            _dialog.ShowWarning(DifferentNetworkMessage);
        }

        var results = await Task.WhenAll(pcs.Select(async pc => (Pc: pc, Result: await _wakeOnLan.WakeAsync(pc.Mac, pc.Ip))));

        foreach (var (pc, result) in results)
        {
            var failures = string.Join(", ", result.Failures.Distinct());
            if (!result.Succeeded)
            {
                _log.Write($"깨우기 실패: {pc.Name} - {failures}");
                continue;
            }

            pc.Status = PcStatus.Waking;
            pc.WakeRequestedAt = DateTime.Now;
            _log.Write($"깨우기 신호를 보냈습니다: {pc.Name} (연결: {string.Join(", ", result.SentVia)})");
            if (failures.Length > 0)
            {
                _log.Write($"일부 연결로는 보내지 못했습니다: {failures}");
            }
        }
    }

    private void OnError(Exception ex) => _log.Write($"깨우기 중 오류가 발생했습니다. ({ex.Message})");
}
