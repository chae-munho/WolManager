using System.Collections.ObjectModel;
using System.Net;
using WolManager.Models;
using WolManager.Mvvm;
using WolManager.Services;

namespace WolManager.ViewModels;

/// <summary>
/// 스캔 영역 ViewModel. 대역을 골라 ARP 스캔하고 결과를 목록에 병합한다.
/// </summary>
public sealed class ScanViewModel : ObservableObject
{
    private const string HintMessage = "대상 PC를 모두 켜 둔 상태에서 스캔하세요. 켜져 있는 장비만 찾습니다.";
    private const string NoNetworkMessage = "연결된 네트워크가 없습니다. 유선 또는 Wi-Fi에 연결한 뒤 네트워크 스캔을 누르세요.";
    private const string NetworkChangedMessage = "연결된 네트워크가 바뀌었습니다. 네트워크를 확인하고 다시 스캔하세요.";

    private readonly IPcRepository _repository;
    private readonly IArpScanService _scanner;
    private readonly INetworkInterfaceService _network;
    private readonly ILogService _log;

    private SubnetInfo? _selectedSubnet;
    private bool _isScanning;
    private int _progressDone;
    private int _progressTotal;
    private string _message = HintMessage;

    public ScanViewModel(
        IPcRepository repository, IArpScanService scanner, INetworkInterfaceService network, ILogService log)
    {
        _repository = repository;
        _scanner = scanner;
        _network = network;
        _log = log;

        ScanCommand = new AsyncRelayCommand(ScanAsync, onError: OnError);

        ReloadSubnets();
        if (SelectedSubnet is null)
        {
            Message = NoNetworkMessage;
        }
    }

    /// <summary>스캔할 수 있는 대역 목록</summary>
    public ObservableCollection<SubnetInfo> Subnets { get; } = new();

    public SubnetInfo? SelectedSubnet
    {
        get => _selectedSubnet;
        set => SetProperty(ref _selectedSubnet, value);
    }

    public bool IsScanning
    {
        get => _isScanning;
        private set
        {
            if (SetProperty(ref _isScanning, value))
            {
                OnPropertyChanged(nameof(IsIdle));
            }
        }
    }

    /// <summary>스캔 중이 아니면 true (대역 선택 활성화용)</summary>
    public bool IsIdle => !IsScanning;

    public int ProgressDone
    {
        get => _progressDone;
        private set => SetProperty(ref _progressDone, value);
    }

    public int ProgressTotal
    {
        get => _progressTotal;
        private set => SetProperty(ref _progressTotal, value);
    }

    /// <summary>안내 또는 결과 문구</summary>
    public string Message
    {
        get => _message;
        private set => SetProperty(ref _message, value);
    }

    public AsyncRelayCommand ScanCommand { get; }

    private async Task ScanAsync()
    {
        // 연결 상태가 바뀌었을 수 있으므로 스캔 직전에 대역을 다시 읽는다.
        var previous = SelectedSubnet;
        ReloadSubnets();
        if (SelectedSubnet is not { } subnet)
        {
            Message = NoNetworkMessage;
            _log.Write(NoNetworkMessage);
            return;
        }

        if (previous is not null && previous != subnet)
        {
            Message = NetworkChangedMessage;
            _log.Write(NetworkChangedMessage);
            return;
        }

        IsScanning = true;
        try
        {
            ProgressDone = 0;
            ProgressTotal = 0;
            Message = "스캔 중입니다. 잠시 기다려 주세요.";
            _log.Write($"네트워크 스캔 시작: {subnet.DisplayName}");

            var progress = new Progress<ScanProgress>(p =>
            {
                ProgressDone = p.Done;
                ProgressTotal = p.Total;
            });
            var results = await _scanner.ScanAsync(subnet, progress);
            var summary = _repository.MergeScanResults(results);

            Message = summary.Responded == 0
                ? "응답한 장비가 없습니다. 대상 PC가 켜져 있는지 확인하세요."
                : $"{summary.Responded}대 응답 (신규 {summary.Added}, 갱신 {summary.Updated})";
            _log.Write(Message);
        }
        finally
        {
            IsScanning = false;
        }
    }

    // 대역 목록을 다시 읽고, 이전 선택이 남아 있으면 유지한다. 없으면 기본 대역을 고른다.
    private void ReloadSubnets()
    {
        var previous = SelectedSubnet;
        var subnets = _network.GetSubnets();

        Subnets.Clear();
        foreach (var subnet in subnets)
        {
            Subnets.Add(subnet);
        }

        SelectedSubnet = Subnets.FirstOrDefault(subnet => subnet == previous) ?? SelectDefault();
    }

    // 등록된 PC가 속한 대역 → 게이트웨이가 있는 대역 → 첫 번째 대역
    private SubnetInfo? SelectDefault()
    {
        var registered = _repository.Items
            .Select(pc => IPAddress.TryParse(pc.Ip, out var address) ? address : null)
            .OfType<IPAddress>()
            .ToList();

        return Subnets.FirstOrDefault(subnet => registered.Any(subnet.Contains))
            ?? Subnets.FirstOrDefault(subnet => subnet.Gateway is not null)
            ?? Subnets.FirstOrDefault();
    }

    private void OnError(Exception ex)
    {
        Message = "스캔 중 오류가 발생했습니다. 작업 기록을 확인하세요.";
        _log.Write($"스캔 중 오류가 발생했습니다. ({ex.Message})");
    }
}
