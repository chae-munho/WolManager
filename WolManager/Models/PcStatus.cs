namespace WolManager.Models;

/// <summary>
/// 대상 PC의 상태.
/// </summary>
public enum PcStatus
{
    /// <summary>IP가 없어 확인할 수 없음</summary>
    Unknown,

    /// <summary>Ping 응답 있음</summary>
    On,

    /// <summary>깨우기 요청 후 응답을 기다리는 중</summary>
    Waking,

    /// <summary>Ping 응답 없음 (실제로 꺼졌다고 단정하지 않는다)</summary>
    Off,
}
