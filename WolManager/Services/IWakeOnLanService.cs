namespace WolManager.Services;

/// 매직 패킷 전송 결과.
/// SentVia: 전송에 성공한 NIC 이름
/// Failures: 실패 사유
public sealed record WakeResult(IReadOnlyList<string> SentVia, IReadOnlyList<string> Failures)
{
    /// 하나 이상의 NIC로 전송했으면 true
    public bool Succeeded => SentVia.Count > 0;
}

/// Wake-on-LAN 매직 패킷을 보내는 서비스.
public interface IWakeOnLanService
{
    /// 주어진 IP 중 하나라도 마스터 PC의 NIC 대역에 속하는지 확인한다.
    /// 반환: 속하면 true, 하나도 속하지 않으면 false, 비교할 IP가 없으면 null
    bool? IsOnSameNetwork(IEnumerable<string> ips);

    /// 매직 패킷을 NIC별 서브넷 브로드캐스트 주소로 정해진 횟수만큼 보낸다.
    /// IP가 속한 대역의 NIC가 있으면 그 NIC로만, 없으면 모든 NIC로 보낸다.
    Task<WakeResult> WakeAsync(string mac, string ip, CancellationToken cancellationToken = default);
}
