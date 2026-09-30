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
}
