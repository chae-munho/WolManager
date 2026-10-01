using WolManager.Models;

namespace WolManager.Services;

/// ARP 요청으로 대역 안의 켜져 있는 장비를 찾는 서비스.
public interface IArpScanService
{
    /// 대역 전체에 ARP 요청을 보내 응답한 장비의 IP, MAC, 호스트명을 모은다.
    /// 마스크가 /22보다 넓으면 마스터 PC IP가 속한 /24만 스캔하고, 마스터 PC 자신과 게이트웨이는 제외한다.
    Task<IReadOnlyList<ScanResult>> ScanAsync(
        SubnetInfo subnet, IProgress<ScanProgress>? progress, CancellationToken cancellationToken = default);
}
