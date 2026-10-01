namespace WolManager;

/// 앱 전체에서 쓰는 고정 값.
public static class Constants
{
    /// 화면에 남기는 로그 최대 줄 수
    public const int LogMaxLines = 500;

    /// PC 목록 저장 파일 이름 (실행 파일 폴더)
    public const string PcFileName = "pcs.json";

    /// 읽을 수 없는 저장 파일을 옮겨 두는 이름
    public const string BrokenPcFileName = "pcs.broken.json";

    /// 매직 패킷을 보내는 UDP 포트
    public const int MagicPacketPort = 9;

    /// 한 번 깨울 때 매직 패킷을 보내는 횟수
    public const int MagicPacketRepeatCount = 3;

    /// 매직 패킷 반복 전송 간격
    public static readonly TimeSpan MagicPacketInterval = TimeSpan.FromMilliseconds(100);

    /// 매직 패킷 한 번 전송의 타임아웃
    public static readonly TimeSpan MagicPacketSendTimeout = TimeSpan.FromSeconds(1);

    /// 상태 확인 주기
    public static readonly TimeSpan StatusCheckInterval = TimeSpan.FromSeconds(5);

    /// 상태 확인 Ping 타임아웃 (밀리초)
    public const int PingTimeoutMilliseconds = 1000;

    /// 깨우기 요청 후 "깨우는 중"으로 유지하는 시간
    public static readonly TimeSpan WakingDuration = TimeSpan.FromMinutes(3);

    /// ARP 스캔 동시 요청 수
    public const int ArpMaxConcurrency = 32;

    /// ARP 요청 하나의 타임아웃 (SendARP 자체 재시도 시간보다 길게)
    public static readonly TimeSpan ArpTimeout = TimeSpan.FromSeconds(5);

    /// 역방향 DNS 조회 타임아웃
    public static readonly TimeSpan ReverseDnsTimeout = TimeSpan.FromSeconds(1.5);

    /// 이보다 넓은 마스크(작은 접두사)는 마스터 PC IP의 /24만 스캔한다
    public const int ScanMinPrefixLength = 22;

    /// 넓은 대역을 줄여서 스캔할 때 쓰는 접두사 길이
    public const int ScanFallbackPrefixLength = 24;

    /// 가상 어댑터 이름/설명 키워드. 게이트웨이가 없으면 스캔과 깨우기에서 제외한다
    public static readonly string[] VirtualAdapterKeywords = ["VMware", "VirtualBox", "Hyper-V", "vEthernet"];
}
