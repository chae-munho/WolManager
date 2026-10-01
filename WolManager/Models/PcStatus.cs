namespace WolManager.Models;

/// 대상 PC의 상태.
public enum PcStatus
{
    /// IP가 없어 확인할 수 없음
    Unknown,

    /// Ping 응답 있음
    On,

    /// 깨우기 요청 후 응답을 기다리는 중
    Waking,

    /// Ping 응답 없음 (실제로 꺼졌다고 단정하지 않는다)
    Off,
}
