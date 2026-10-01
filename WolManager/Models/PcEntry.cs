using WolManager.Mvvm;

namespace WolManager.Models;

/// 목록에 표시되는 대상 PC 한 대.
public sealed class PcEntry : ObservableObject
{
    private string _name = string.Empty;
    private string _mac = string.Empty;
    private string _ip = string.Empty;
    private PcStatus _status = PcStatus.Unknown;
    private DateTime? _wakeRequestedAt;
    private DateTime? _shutdownRequestedAt;

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    /// AA-BB-CC-DD-EE-FF 형식의 MAC 주소
    public string Mac
    {
        get => _mac;
        set => SetProperty(ref _mac, value);
    }

    /// IPv4 주소. 모르면 빈 문자열
    public string Ip
    {
        get => _ip;
        set => SetProperty(ref _ip, value);
    }

    /// 현재 상태 (저장하지 않음)
    public PcStatus Status
    {
        get => _status;
        set => SetProperty(ref _status, value);
    }

    /// 마지막 깨우기 요청 시각 (저장하지 않음)
    public DateTime? WakeRequestedAt
    {
        get => _wakeRequestedAt;
        set => SetProperty(ref _wakeRequestedAt, value);
    }

    /// 마지막 끄기 요청 시각 (저장하지 않음)
    public DateTime? ShutdownRequestedAt
    {
        get => _shutdownRequestedAt;
        set => SetProperty(ref _shutdownRequestedAt, value);
    }
}
