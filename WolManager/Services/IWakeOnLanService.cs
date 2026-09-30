namespace WolManager.Services;

/// <summary>
/// 매직 패킷 전송 결과.
/// </summary>
/// <param name="SentVia">전송에 성공한 NIC 이름</param>
/// <param name="Failures">실패 사유</param>
public sealed record WakeResult(IReadOnlyList<string> SentVia, IReadOnlyList<string> Failures)
{
    /// <summary>하나 이상의 NIC로 전송했으면 true</summary>
    public bool Succeeded => SentVia.Count > 0;
}

/// <summary>
/// Wake-on-LAN 매직 패킷을 보내는 서비스.
/// </summary>
public interface IWakeOnLanService
{
    /// <summary>
    /// 주어진 IP 중 하나라도 마스터 PC의 NIC 대역에 속하는지 확인한다.
    /// </summary>
    /// <returns>속하면 true, 하나도 속하지 않으면 false, 비교할 IP가 없으면 null</returns>
    bool? IsOnSameNetwork(IEnumerable<string> ips);

    /// <summary>
    /// 매직 패킷을 NIC별 서브넷 브로드캐스트 주소로 정해진 횟수만큼 보낸다.
    /// IP가 속한 대역의 NIC가 있으면 그 NIC로만, 없으면 모든 NIC로 보낸다.
    /// </summary>
    Task<WakeResult> WakeAsync(string mac, string ip, CancellationToken cancellationToken = default);
}
