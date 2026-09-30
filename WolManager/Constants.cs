namespace WolManager;

/// <summary>
/// 앱 전체에서 쓰는 고정 값.
/// </summary>
public static class Constants
{
    /// <summary>화면에 남기는 로그 최대 줄 수</summary>
    public const int LogMaxLines = 500;

    /// <summary>PC 목록 저장 파일 이름 (실행 파일 폴더)</summary>
    public const string PcFileName = "pcs.json";

    /// <summary>읽을 수 없는 저장 파일을 옮겨 두는 이름</summary>
    public const string BrokenPcFileName = "pcs.broken.json";

    /// <summary>매직 패킷을 보내는 UDP 포트</summary>
    public const int MagicPacketPort = 9;

    /// <summary>한 번 깨울 때 매직 패킷을 보내는 횟수</summary>
    public const int MagicPacketRepeatCount = 3;

    /// <summary>매직 패킷 반복 전송 간격</summary>
    public static readonly TimeSpan MagicPacketInterval = TimeSpan.FromMilliseconds(100);

    /// <summary>매직 패킷 한 번 전송의 타임아웃</summary>
    public static readonly TimeSpan MagicPacketSendTimeout = TimeSpan.FromSeconds(1);
}
