using WolManager.Models;

namespace WolManager.Services;

/// ARP 요청으로 대역 안의 켜져 있는 장비를 찾는 서비스.
public interface IArpScanService
{
    /// 대역 전체에 ARP 요청을 보내 응답한 장비의 IP, MAC, 호스트명을 모은다.
    /// 마스크가 /22보다 넓으면 마스터 PC IP가 속한 /24만 스캔하고, 마스터 PC 자신과 게이트웨이는 제외한다.
    Task<IReadOnlyList<ScanResult>> ScanAsync(
        SubnetInfo subnet, IProgress<ScanProgress>? progress, CancellationToken cancellationToken = default);

    /// 대역 전체에 ARP 요청을 보내 주어진 MAC을 가진 장비만 돌려준다. 호스트명은 조회하지 않고 IP를 넣는다.
    /// IP 없이 MAC만 등록된 PC의 IP를 찾는 데 쓴다.
    Task<IReadOnlyList<ScanResult>> FindMacsAsync(
        SubnetInfo subnet, IReadOnlySet<string> macs, CancellationToken cancellationToken = default);
}
