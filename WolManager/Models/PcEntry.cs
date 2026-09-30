using WolManager.Mvvm;

namespace WolManager.Models;

/// <summary>
/// 목록에 표시되는 대상 PC 한 대.
/// </summary>
public sealed class PcEntry : ObservableObject
{
    private string _name = string.Empty;
    private string _mac = string.Empty;
    private string _ip = string.Empty;
    private bool _isTarget = true;
    private PcStatus _status = PcStatus.Unknown;
    private DateTime? _wakeRequestedAt;

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    /// <summary>AA-BB-CC-DD-EE-FF 형식의 MAC 주소</summary>
    public string Mac
    {
        get => _mac;
        set => SetProperty(ref _mac, value);
    }

    /// <summary>IPv4 주소. 모르면 빈 문자열</summary>
    public string Ip
    {
        get => _ip;
        set => SetProperty(ref _ip, value);
    }

    /// <summary>전체 깨우기 대상 여부</summary>
    public bool IsTarget
    {
        get => _isTarget;
        set => SetProperty(ref _isTarget, value);
    }

    /// <summary>현재 상태 (저장하지 않음)</summary>
    public PcStatus Status
    {
        get => _status;
        set => SetProperty(ref _status, value);
    }

    /// <summary>마지막 깨우기 요청 시각 (저장하지 않음)</summary>
    public DateTime? WakeRequestedAt
    {
        get => _wakeRequestedAt;
        set => SetProperty(ref _wakeRequestedAt, value);
    }
}
