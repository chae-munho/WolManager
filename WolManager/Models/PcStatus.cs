namespace WolManager.Models;

/// 대상 PC의 상태.
public enum PcStatus
{
    /// 아직 확인하지 못함 (IP 없는 PC를 MAC으로 찾아보기 전 등)
    Unknown,

    /// Ping 응답 있음 (IP 없는 PC는 MAC으로 찾음)
    On,

    /// 깨우기 요청 후 응답을 기다리는 중
    Waking,

    /// Ping 응답 없음 또는 MAC으로 찾지 못함 (화면에는 "응답 없음")
    Off,
}
