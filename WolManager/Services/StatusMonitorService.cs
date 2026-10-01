using System.Net.NetworkInformation;
using WolManager.Models;
using WolManager.Validation;

namespace WolManager.Services;

/// Ping으로 PC 상태를 판정하는 구현.
/// IP 없이 MAC만 등록된 PC는 주기적으로 백그라운드 ARP 스캔을 해서 MAC으로 IP를 찾는다.
/// 찾으면 IP를 채워 저장하고 켜짐으로, 못 찾으면 꺼진 것으로 판정한다.
/// UI 스레드에서 시작하므로 await 이후의 상태 변경도 UI 스레드에서 일어난다.
public sealed class StatusMonitorService : IStatusMonitorService
{
    private readonly IPcRepository _repository;
    private readonly IArpScanService _arp;
    private readonly INetworkInterfaceService _network;
    private readonly ILogService _log;
    private CancellationTokenSource? _cancellation;
    private Task? _checking;
    private Task? _lookup;
    private DateTime _lastLookup = DateTime.MinValue;

    // 마지막 MAC 찾기에서 발견되지 않은(꺼져 있는) IP 없는 PC
    private HashSet<PcEntry> _notFound = [];

    public StatusMonitorService(
        IPcRepository repository, IArpScanService arp, INetworkInterfaceService network, ILogService log)
    {
        _repository = repository;
        _arp = arp;
        _network = network;
        _log = log;
    }

    /// 바로 한 번 확인하고, 이후 정해진 주기마다 확인한다.
    public void Start()
    {
        if (_cancellation is not null)
        {
            return;
        }

        _cancellation = new CancellationTokenSource();
        _ = RunAsync(_cancellation.Token);
    }

    /// 주기적인 확인을 멈춘다.
    public void Stop()
    {
        _cancellation?.Cancel();
        _cancellation?.Dispose();
        _cancellation = null;
    }

    /// 지금 바로 한 번 확인한다. 이미 확인이 진행 중이면 새로 시작하지 않고 그 확인이 끝날 때까지 기다린다.
    /// IP 없는 PC가 있으면 주기를 기다리지 않고 MAC 찾기도 바로 시작한다 (끝날 때까지 기다리지는 않는다).
    public Task RefreshAsync()
    {
        StartMacLookup(force: true);
        return CheckOrJoinAsync();
    }

    public void Dispose() => Stop();

    private Task CheckOrJoinAsync()
    {
        if (_checking is not { IsCompleted: false })
        {
            _checking = CheckAsync();
        }

        return _checking;
    }

    private async Task CheckAsync()
    {
        var pcs = _repository.Items.Select(pc => (Pc: pc, Ip: pc.Ip)).ToList();
        var replies = await Task.WhenAll(pcs.Select(item => PingAsync(item.Ip)));
        var now = DateTime.Now;
        for (var i = 0; i < pcs.Count; i++)
        {
            var (pc, ip) = pcs[i];

            // 확인하는 동안 IP가 바뀌었으면(MAC 찾기, 수정 등) 이번 결과는 맞지 않으므로 버린다.
            if (pc.Ip != ip)
            {
                continue;
            }

            // IP 없는 PC: MAC 찾기에서 못 찾았거나 깨우기 요청 중이면 응답 없음으로 보고(깨우는 중 유지),
            // 아직 한 번도 찾아보지 않았으면 알 수 없음으로 둔다.
            var reply = ip.Length == 0
                ? (_notFound.Contains(pc) || pc.WakeRequestedAt is not null ? false : null)
                : replies[i];
            Apply(pc, reply, now);
        }
    }

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(Constants.StatusCheckInterval);
        try
        {
            do
            {
                StartMacLookup(force: false);

                // 이전 확인(새로고침 포함)이 아직 진행 중이면 이번 주기는 건너뛴다.
                if (_checking is { IsCompleted: false })
                {
                    continue;
                }

                try
                {
                    await CheckOrJoinAsync();
                }
                catch (Exception ex)
                {
                    _log.Write($"상태 확인 중 오류가 발생했습니다. ({ex.Message})");
                }
            }
            while (await timer.WaitForNextTickAsync(cancellationToken));
        }
        catch (OperationCanceledException)
        {
            // 종료 시 정상적으로 멈춘다.
        }
    }

    // IP 없는 PC가 있으면 백그라운드로 MAC 찾기를 시작한다. 진행 중이면 다시 시작하지 않는다.
    // force가 아니면 마지막 찾기 후 정해진 주기가 지났을 때만 시작한다.
    private void StartMacLookup(bool force)
    {
        if (_lookup is { IsCompleted: false })
        {
            return;
        }

        var targets = _repository.Items
            .Where(pc => pc.Ip.Length == 0 && InputValidator.TryNormalizeMac(pc.Mac, out _))
            .ToList();
        if (targets.Count == 0)
        {
            _notFound = [];
            return;
        }

        if (!force && DateTime.Now - _lastLookup < Constants.MacLookupInterval)
        {
            return;
        }

        _lookup = LookupMacsAsync(targets);
    }

    private async Task LookupMacsAsync(List<PcEntry> targets)
    {
        try
        {
            var macs = targets.Select(pc => pc.Mac).ToHashSet();
            var found = new List<ScanResult>();
            foreach (var subnet in _network.GetSubnets())
            {
                found.AddRange(await _arp.FindMacsAsync(subnet, macs));
            }

            foreach (var pc in targets.Where(pc => pc.Status == PcStatus.Waking && found.Any(r => r.Mac == pc.Mac)))
            {
                _log.Write($"켜짐 확인: {pc.Name}");
            }

            // 찾은 PC는 스캔 병합과 같은 규칙으로 IP를 채우고 켜짐으로 바꾼 뒤 저장한다.
            if (found.Count > 0)
            {
                _repository.MergeScanResults(found);
            }

            // 그사이 IP가 생긴 PC(찾았거나 직접 입력함)는 빼고, 남은 PC는 꺼진 것으로 판정한다.
            _notFound = targets.Where(pc => pc.Ip.Length == 0).ToHashSet();
            var now = DateTime.Now;
            foreach (var pc in _notFound)
            {
                Apply(pc, false, now);
            }
        }
        catch (Exception ex)
        {
            _log.Write($"MAC으로 PC를 찾는 중 오류가 발생했습니다. ({ex.Message})");
        }
        finally
        {
            _lastLookup = DateTime.Now;
        }
    }

    // reply: 응답 여부. 확인할 수 없으면 null
    private void Apply(PcEntry pc, bool? reply, DateTime now)
    {
        if (reply is null)
        {
            pc.Status = PcStatus.Unknown;
            return;
        }

        if (reply == true)
        {
            if (pc.Status == PcStatus.Waking)
            {
                _log.Write($"켜짐 확인: {pc.Name}");
            }

            pc.Status = PcStatus.On;
            pc.WakeRequestedAt = null;
            return;
        }

        if (pc.WakeRequestedAt is { } requestedAt)
        {
            if (now - requestedAt < Constants.WakingDuration)
            {
                pc.Status = PcStatus.Waking;
                return;
            }

            // 안내는 한 번만 남기도록 요청 시각을 지운다.
            pc.WakeRequestedAt = null;
            _log.Write($"깨우기 요청 후 {Constants.WakingDuration.TotalMinutes:0}분 동안 응답 없음: {pc.Name}. " +
                       "대상 PC의 BIOS/NIC Wake-on-LAN 설정을 확인하세요.");
        }

        pc.Status = PcStatus.Off;
    }

    // 반환: Ping 응답 여부. IP가 없거나 형식이 틀리면 null
    private static async Task<bool?> PingAsync(string ip)
    {
        if (!InputValidator.TryNormalizeIp(ip, out var normalized) || normalized.Length == 0)
        {
            return null;
        }

        try
        {
            using var ping = new Ping();
            var reply = await ping.SendPingAsync(normalized, Constants.PingTimeoutMilliseconds);
            return reply.Status == IPStatus.Success;
        }
        catch (PingException)
        {
            return false;
        }
    }
}
