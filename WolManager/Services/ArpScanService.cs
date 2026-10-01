using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using WolManager.Models;
using WolManager.Native;

namespace WolManager.Services;

/// Win32 SendARP로 스캔하는 구현. 방화벽이 Ping을 막아도 ARP에는 응답한다.
public sealed class ArpScanService : IArpScanService
{
    private const int MacLength = 6;

    private readonly INetworkInterfaceService _network;

    // 사용자 스캔과 백그라운드 MAC 찾기가 겹쳐도 Wi-Fi에서 응답을 놓치지 않도록 동시 요청 수를 함께 제한한다.
    private readonly SemaphoreSlim _gate = new(Constants.ArpMaxConcurrency);

    public ArpScanService(INetworkInterfaceService network)
    {
        _network = network;
    }

    /// 대역 전체에 ARP 요청을 보내 응답한 장비의 IP, MAC, 호스트명을 모은다.
    /// 마스크가 /22보다 넓으면 마스터 PC IP가 속한 /24만 스캔하고, 마스터 PC 자신과 게이트웨이는 제외한다.
    public async Task<IReadOnlyList<ScanResult>> ScanAsync(
        SubnetInfo subnet, IProgress<ScanProgress>? progress, CancellationToken cancellationToken = default)
    {
        var found = await SweepAsync(subnet, progress, cancellationToken);
        var names = await Task.WhenAll(found.Select(item => GetHostNameAsync(item.Ip)));
        return found.Select((item, i) => new ScanResult(item.Ip.ToString(), item.Mac, names[i])).ToList();
    }

    /// 대역 전체에 ARP 요청을 보내 주어진 MAC을 가진 장비만 돌려준다. 호스트명은 조회하지 않고 IP를 넣는다.
    public async Task<IReadOnlyList<ScanResult>> FindMacsAsync(
        SubnetInfo subnet, IReadOnlySet<string> macs, CancellationToken cancellationToken = default)
    {
        var found = await SweepAsync(subnet, null, cancellationToken);
        return found
            .Where(item => macs.Contains(item.Mac))
            .Select(item => new ScanResult(item.Ip.ToString(), item.Mac, item.Ip.ToString()))
            .ToList();
    }

    // 대역 전체에 ARP 요청을 보내 응답한 장비의 IP와 MAC을 IP 순서로 모은다.
    private async Task<List<(IPAddress Ip, string Mac)>> SweepAsync(
        SubnetInfo subnet, IProgress<ScanProgress>? progress, CancellationToken cancellationToken)
    {
        var targets = GetScanTargets(subnet);
        var total = targets.Count;
        var done = 0;
        progress?.Report(new ScanProgress(0, total));

        var found = new ConcurrentBag<(IPAddress Ip, string Mac)>();
        await Task.WhenAll(targets.Select(async ip =>
        {
            await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                var mac = await ResolveMacAsync(ip, subnet.LocalAddress, cancellationToken).ConfigureAwait(false);
                if (mac is not null)
                {
                    found.Add((ip, mac));
                }
            }
            finally
            {
                _gate.Release();
                progress?.Report(new ScanProgress(Interlocked.Increment(ref done), total));
            }
        }));

        return found.OrderBy(item => SubnetInfo.ToUInt32(item.Ip)).ToList();
    }

    // 스캔할 IP 목록. 네트워크/브로드캐스트 주소, 마스터 PC 자신, 게이트웨이는 뺀다.
    private List<IPAddress> GetScanTargets(SubnetInfo subnet)
    {
        var prefix = subnet.PrefixLength < Constants.ScanMinPrefixLength
            ? Constants.ScanFallbackPrefixLength
            : subnet.PrefixLength;
        var mask = uint.MaxValue << (32 - prefix);
        var network = SubnetInfo.ToUInt32(subnet.LocalAddress) & mask;
        var broadcast = network | ~mask;

        var excluded = _network.GetAllLocalAddresses().Select(SubnetInfo.ToUInt32).ToHashSet();
        if (subnet.Gateway is not null)
        {
            excluded.Add(SubnetInfo.ToUInt32(subnet.Gateway));
        }

        var targets = new List<IPAddress>();
        for (var value = network + 1; value < broadcast; value++)
        {
            if (!excluded.Contains(value))
            {
                targets.Add(SubnetInfo.FromUInt32(value));
            }
        }

        return targets;
    }

    private static async Task<string?> ResolveMacAsync(IPAddress ip, IPAddress source, CancellationToken cancellationToken)
    {
        var destination = ToNetworkOrder(ip);
        var sourceAddress = ToNetworkOrder(source);

        // SendARP는 응답을 기다리며 스레드를 막으므로 전용 스레드에서 실행한다.
        var request = Task.Factory.StartNew(
            () =>
            {
                var mac = new byte[MacLength];
                var length = (uint)MacLength;
                var result = IpHlpApi.SendARP(destination, sourceAddress, mac, ref length);
                return result == IpHlpApi.NoError && length == MacLength ? mac : null;
            },
            cancellationToken,
            TaskCreationOptions.LongRunning,
            TaskScheduler.Default);

        try
        {
            var bytes = await request.WaitAsync(Constants.ArpTimeout, cancellationToken).ConfigureAwait(false);
            if (bytes is null || bytes.All(b => b == 0x00) || bytes.All(b => b == 0xFF))
            {
                return null;
            }

            return string.Join('-', bytes.Select(b => b.ToString("X2")));
        }
        catch (TimeoutException)
        {
            return null;
        }
    }

    // 역방향 DNS로 호스트명을 찾고 도메인 접미사를 뗀다. 실패하거나 시간이 지나면 IP를 쓴다.
    private static async Task<string> GetHostNameAsync(IPAddress ip)
    {
        var fallback = ip.ToString();
        try
        {
            var entry = await Dns.GetHostEntryAsync(ip).WaitAsync(Constants.ReverseDnsTimeout);
            var name = entry.HostName;
            if (string.IsNullOrWhiteSpace(name) || name == fallback)
            {
                return fallback;
            }

            var dot = name.IndexOf('.');
            return dot > 0 ? name[..dot] : name;
        }
        catch (Exception ex) when (ex is SocketException or TimeoutException or ArgumentException)
        {
            return fallback;
        }
    }

    // SendARP는 메모리상 네트워크 바이트 순서의 IPv4 값을 받는다.
    private static uint ToNetworkOrder(IPAddress address) => BitConverter.ToUInt32(address.GetAddressBytes(), 0);
}
