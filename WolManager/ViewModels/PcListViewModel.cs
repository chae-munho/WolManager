using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using WolManager.Models;
using WolManager.Mvvm;
using WolManager.Services;

namespace WolManager.ViewModels;

/// PC 목록(카드) 영역 ViewModel. 상태 요약, 상태 새로고침, 새 PC 추가 요청, 전체 깨우기를 맡는다.
public sealed class PcListViewModel : ObservableObject
{
    private static readonly (PcStatus Status, string Label)[] StatusLabels =
    [
        (PcStatus.On, "켜짐"),
        (PcStatus.Waking, "깨우는 중"),
        (PcStatus.ShuttingDown, "끄는 중"),
        (PcStatus.Off, "응답 없음"),
        (PcStatus.Unknown, "알 수 없음"),
    ];

    private readonly IPcPowerService _power;
    private readonly IDialogService _dialog;
    private readonly ILogService _log;
    private readonly HashSet<PcEntry> _watched = [];
    private PcEntry? _selectedPc;
    private bool _isAllSelected;
    private string _summary = string.Empty;

    public PcListViewModel(
        IPcRepository repository,
        IPcPowerService power,
        IStatusMonitorService statusMonitor,
        IDialogService dialog,
        ILogService log)
    {
        _power = power;
        _dialog = dialog;
        _log = log;
        Items = repository.Items;

        RefreshCommand = new AsyncRelayCommand(
            async () =>
            {
                await statusMonitor.RefreshAsync();
                _log.Write(Items.Count == 0
                    ? "상태를 다시 확인했습니다. (등록된 PC 없음)"
                    : $"상태를 다시 확인했습니다. ({Summary})");
            },
            onError: ex => _log.Write($"상태 확인 중 오류가 발생했습니다. ({ex.Message})"));
        AddCommand = new RelayCommand(() => AddRequested?.Invoke(this, EventArgs.Empty));
        WakeAllCommand = new AsyncRelayCommand(
            WakeAllAsync,
            () => Items.Count > 0,
            ex => _log.Write($"깨우기 중 오류가 발생했습니다. ({ex.Message})"));

        ((INotifyCollectionChanged)Items).CollectionChanged += (_, _) => OnItemsChanged();
        OnItemsChanged();
    }

    /// 새 PC 추가 버튼을 눌렀을 때. 메인 ViewModel이 추가 창을 연다.
    public event EventHandler? AddRequested;

    public ReadOnlyObservableCollection<PcEntry> Items { get; }

    /// 선택한 PC. 메인 ViewModel이 선택한 PC 패널과 연결한다.
    public PcEntry? SelectedPc
    {
        get => _selectedPc;
        set => SetProperty(ref _selectedPc, value);
    }

    /// 전체 깨우기 확인 중에 모든 카드를 선택해 보여 준다.
    public bool IsAllSelected
    {
        get => _isAllSelected;
        private set => SetProperty(ref _isAllSelected, value);
    }

    /// 카드를 한 줄에 놓는 개수. 대수에 맞춰 가로가 조금 긴 격자가 되도록 정해, 스크롤 없이 한 화면에 모두 보이게 한다.
    public int TileColumns => Math.Max(1, (int)Math.Ceiling(Math.Sqrt(Items.Count * Constants.TileGridAspectRatio)));

    /// 상태별 대수. 목록 위에 색 점과 함께 보여 준다.
    public ObservableCollection<StatusCount> StatusCounts { get; } = [];

    /// 상태별 대수 요약 문장 (예: 켜짐 18대 · 응답 없음 3대). 새로고침 로그에 쓴다.
    public string Summary
    {
        get => _summary;
        private set => SetProperty(ref _summary, value);
    }

    public AsyncRelayCommand RefreshCommand { get; }

    public RelayCommand AddCommand { get; }

    /// 모든 카드를 선택해 보여 주고, 확인을 받은 뒤 켜져 있지 않은 PC를 모두 깨운다.
    public AsyncRelayCommand WakeAllCommand { get; }

    private async Task WakeAllAsync()
    {
        // 켜져 있거나 꺼지는 중인 PC는 빼고 깨운다.
        var toWake = Items.Where(pc => pc.Status is not (PcStatus.On or PcStatus.ShuttingDown)).ToList();
        var skipped = Items.Count - toWake.Count;
        if (toWake.Count == 0)
        {
            const string allOn = "모든 PC가 켜져 있어 깨울 PC가 없습니다.";
            _log.Write(allOn);
            _dialog.ShowInfo(allOn, "전체 깨우기");
            return;
        }

        // 전체 선택을 풀면 원래 선택도 사라지므로, 끝난 뒤 되돌린다.
        var previous = SelectedPc;
        IsAllSelected = true;
        try
        {
            var question = skipped > 0
                ? $"PC {Items.Count}대 중 켜져 있지 않은 {toWake.Count}대를 깨웁니다.\n켜져 있는 {skipped}대는 제외됩니다."
                : $"PC {toWake.Count}대를 모두 깨웁니다.";
            if (!_dialog.Confirm(question, "전체 깨우기", "깨우기"))
            {
                return;
            }

            _log.Write(skipped > 0
                ? $"전체 깨우기: {toWake.Count}대에 신호를 보냅니다. (켜져 있는 {skipped}대 제외)"
                : $"전체 깨우기: {toWake.Count}대에 신호를 보냅니다.");
            await _power.WakeAsync(toWake);
        }
        finally
        {
            IsAllSelected = false;
            if (previous is not null && Items.Contains(previous))
            {
                SelectedPc = previous;
            }
        }
    }

    // 목록이 바뀌면 상태 변경 알림을 다시 구독하고 요약을 갱신한다.
    private void OnItemsChanged()
    {
        foreach (var pc in _watched.Except(Items).ToList())
        {
            pc.PropertyChanged -= OnItemPropertyChanged;
            _watched.Remove(pc);
        }

        foreach (var pc in Items.Where(_watched.Add))
        {
            pc.PropertyChanged += OnItemPropertyChanged;
        }

        WakeAllCommand.RaiseCanExecuteChanged();
        OnPropertyChanged(nameof(TileColumns));
        UpdateSummary();
    }

    private void OnItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(PcEntry.Status))
        {
            UpdateSummary();
        }
    }

    private void UpdateSummary()
    {
        var counts = Items.GroupBy(pc => pc.Status).ToDictionary(g => g.Key, g => g.Count());
        var present = StatusLabels.Where(p => counts.ContainsKey(p.Status)).ToList();
        Summary = string.Join(" · ", present.Select(p => $"{p.Label} {counts[p.Status]}대"));

        StatusCounts.Clear();
        foreach (var (status, _) in present)
        {
            StatusCounts.Add(new StatusCount(status, counts[status]));
        }
    }
}
