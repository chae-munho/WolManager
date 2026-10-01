namespace WolManager.Models;

/// 저장 파일에 기록하는 PC 한 대의 정보. 상태와 깨우기 요청 시각은 저장하지 않는다.
public sealed class PcRecord
{
    public string? Name { get; set; }

    public string? Mac { get; set; }

    public string? Ip { get; set; }

    /// 값이 없으면 대상이 아닌 것으로 본다
    public bool? IsTarget { get; set; }
}
