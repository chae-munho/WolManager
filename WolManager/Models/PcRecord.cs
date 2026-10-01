namespace WolManager.Models;

/// 저장 파일에 기록하는 PC 한 대의 정보. 상태와 깨우기 요청 시각은 저장하지 않는다.
/// 예전 파일의 isTarget 값은 읽을 때 무시하고, 다음 저장 때 빠진다.
public sealed class PcRecord
{
    public string? Name { get; set; }

    public string? Mac { get; set; }

    public string? Ip { get; set; }
}
