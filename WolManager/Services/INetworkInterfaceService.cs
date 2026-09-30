using WolManager.Models;

namespace WolManager.Services;

/// <summary>
/// 마스터 PC의 NIC 대역을 알려 주는 서비스. 유선과 Wi-Fi를 구분하지 않는다.
/// </summary>
public interface INetworkInterfaceService
{
    /// <summary>
    /// 사용할 수 있는 IPv4 대역 목록을 가져온다.
    /// Up 상태이고 Loopback/Tunnel이 아닌 NIC만 포함하며, APIPA(169.254.x.x)와 /32 주소는 제외한다.
    /// </summary>
    IReadOnlyList<SubnetInfo> GetSubnets();
}
