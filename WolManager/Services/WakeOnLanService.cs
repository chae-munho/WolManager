using System.Net;
using System.Net.Sockets;
using WolManager.Models;
using WolManager.Validation;

namespace WolManager.Services;

/// <summary>
/// NIC마다 로컬 IP에 바인딩한 UDP 소켓으로 매직 패킷을 보내는 구현.
/// </summary>
public sealed class WakeOnLanService : IWakeOnLanService
{
    private readonly INetworkInterfaceService _network;

    public WakeOnLanService(INetworkInterfaceService network)
    {
        _network = network;
    }

    /// <summary>
    /// 주어진 IP 중 하나라도 마스터 PC의 NIC 대역에 속하는지 확인한다.
    /// </summary>
    /// <returns>속하면 true, 하나도 속하지 않으면 false, 비교할 IP가 없으면 null</returns>
    public bool? IsOnSameNetwork(IEnumerable<string> ips)
    {
        var addresses = ips.Select(ParseIp).OfType<IPAddress>().ToList();
        if (addresses.Count == 0)
        {
            return null;
        }

        var subnets = _network.GetSubnets();
        return addresses.Any(address => subnets.Any(subnet => subnet.Contains(address)));
    }

    /// <summary>
    /// 매직 패킷을 NIC별 서브넷 브로드캐스트 주소로 정해진 횟수만큼 보낸다.
    /// IP가 속한 대역의 NIC가 있으면 그 NIC로만, 없으면 모든 NIC로 보낸다.
    /// </summary>
    public async Task<WakeResult> WakeAsync(string mac, string ip, CancellationToken cancellationToken = default)
    {
        if (!MagicPacket.TryCreate(mac, out var packet))
        {
            return Fail("MAC 주소 형식이 올바르지 않습니다.");
        }

        var subnets = SelectSubnets(ip);
        if (subnets.Count == 0)
        {
            return Fail("사용할 수 있는 네트워크 연결이 없습니다.");
        }

        var failures = new List<string>();
        var senders = new List<(SubnetInfo Subnet, UdpClient Client)>();
        try
        {
            foreach (var subnet in subnets)
            {
                try
                {
                    var client = new UdpClient(new IPEndPoint(subnet.LocalAddress, 0)) { EnableBroadcast = true };
                    senders.Add((subnet, client));
                }
                catch (SocketException ex)
                {
                    failures.Add($"{subnet.InterfaceName}: {ex.Message}");
                }
            }

            var sentVia = new HashSet<string>();
            for (var i = 0; i < Constants.MagicPacketRepeatCount; i++)
            {
                if (i > 0)
                {
                    await Task.Delay(Constants.MagicPacketInterval, cancellationToken);
                }

                foreach (var (subnet, client) in senders)
                {
                    if (await TrySendAsync(client, packet, subnet, failures, cancellationToken))
                    {
                        sentVia.Add(subnet.InterfaceName);
                    }
                }
            }

            return new WakeResult(sentVia.ToList(), failures);
        }
        finally
        {
            foreach (var (_, client) in senders)
            {
                client.Dispose();
            }
        }
    }

    // 대상 IP와 같은 대역의 NIC만 고르고, 없으면 모든 NIC를 쓴다.
    private List<SubnetInfo> SelectSubnets(string ip)
    {
        var subnets = _network.GetSubnets();
        var address = ParseIp(ip);
        if (address is not null)
        {
            var matched = subnets.Where(subnet => subnet.Contains(address)).ToList();
            if (matched.Count > 0)
            {
                return matched;
            }
        }

        return subnets.ToList();
    }

    private static async Task<bool> TrySendAsync(
        UdpClient client, byte[] packet, SubnetInfo subnet, List<string> failures, CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(Constants.MagicPacketSendTimeout);
        try
        {
            var target = new IPEndPoint(subnet.Broadcast, Constants.MagicPacketPort);
            await client.SendAsync(packet, target, timeout.Token);
            return true;
        }
        catch (SocketException ex)
        {
            failures.Add($"{subnet.InterfaceName}: {ex.Message}");
            return false;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            failures.Add($"{subnet.InterfaceName}: 전송 시간 초과");
            return false;
        }
    }

    private static IPAddress? ParseIp(string ip)
    {
        return InputValidator.TryNormalizeIp(ip, out var normalized) && normalized.Length > 0
            ? IPAddress.Parse(normalized)
            : null;
    }

    private static WakeResult Fail(string reason) => new([], [reason]);
}
