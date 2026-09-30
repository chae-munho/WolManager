using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using WolManager.Models;

namespace WolManager.Services;

/// <summary>
/// System.Net.NetworkInformation으로 NIC 대역을 읽는 구현.
/// </summary>
public sealed class NetworkInterfaceService : INetworkInterfaceService
{
    private const int MaxPrefixLength = 32;

    /// <summary>
    /// 사용할 수 있는 IPv4 대역 목록을 가져온다.
    /// Up 상태이고 Loopback/Tunnel이 아닌 NIC만 포함하며, APIPA(169.254.x.x)와 /32 주소,
    /// VMware/VirtualBox/Hyper-V 같은 가상 어댑터는 제외한다.
    /// </summary>
    public IReadOnlyList<SubnetInfo> GetSubnets()
    {
        var subnets = new List<SubnetInfo>();

        foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (nic.OperationalStatus != OperationalStatus.Up
                || nic.NetworkInterfaceType is NetworkInterfaceType.Loopback or NetworkInterfaceType.Tunnel
                || IsVirtualAdapter(nic))
            {
                continue;
            }

            var properties = nic.GetIPProperties();
            var gateway = properties.GatewayAddresses
                .Select(g => g.Address)
                .FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork && !a.Equals(IPAddress.Any));

            foreach (var unicast in properties.UnicastAddresses)
            {
                var address = unicast.Address;
                if (address.AddressFamily != AddressFamily.InterNetwork
                    || IsApipa(address)
                    || unicast.PrefixLength >= MaxPrefixLength)
                {
                    continue;
                }

                subnets.Add(new SubnetInfo(nic.Name, address, unicast.PrefixLength, gateway));
            }
        }

        return subnets;
    }

    /// <summary>
    /// 마스터 PC에 설정된 모든 IPv4 주소를 가져온다 (가상 어댑터 포함). 스캔 결과에서 자신을 빼는 데 쓴다.
    /// </summary>
    public IReadOnlySet<IPAddress> GetAllLocalAddresses()
    {
        return NetworkInterface.GetAllNetworkInterfaces()
            .SelectMany(nic => nic.GetIPProperties().UnicastAddresses)
            .Select(unicast => unicast.Address)
            .Where(address => address.AddressFamily == AddressFamily.InterNetwork)
            .ToHashSet();
    }

    private static bool IsVirtualAdapter(NetworkInterface nic)
    {
        return Constants.VirtualAdapterKeywords.Any(keyword =>
            nic.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase)
            || nic.Description.Contains(keyword, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsApipa(IPAddress address)
    {
        var bytes = address.GetAddressBytes();
        return bytes[0] == 169 && bytes[1] == 254;
    }
}
