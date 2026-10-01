using System.Net.NetworkInformation;
using WolManager.Models;
using WolManager.Validation;

namespace WolManager.Services;

/// Ping으로 PC 상태를 판정하는 구현.
/// UI 스레드에서 시작하므로 await 이후의 상태 변경도 UI 스레드에서 일어난다.
public sealed class StatusMonitorService : IStatusMonitorService
{
    private readonly IPcRepository _repository;
    private readonly ILogService _log;
    private CancellationTokenSource? _cancellation;
    private bool _isChecking;

    public StatusMonitorService(IPcRepository repository, ILogService log)
    {
        _repository = repository;
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

    /// 지금 바로 한 번 확인한다. 이전 확인이 진행 중이면 건너뛴다.
    public async Task RefreshAsync()
    {
        if (_isChecking)
        {
            return;
        }

        _isChecking = true;
        try
        {
            var pcs = _repository.Items.ToList();
            var replies = await Task.WhenAll(pcs.Select(pc => PingAsync(pc.Ip)));
            var now = DateTime.Now;
            for (var i = 0; i < pcs.Count; i++)
            {
                Apply(pcs[i], replies[i], now);
            }
        }
        finally
        {
            _isChecking = false;
        }
    }

    public void Dispose() => Stop();

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(Constants.StatusCheckInterval);
        try
        {
            do
            {
                try
                {
                    await RefreshAsync();
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

    // reply: Ping 응답 여부. IP가 없으면 null
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
            _log.Write($"깨우기 요청 후 {Constants.WakingDuration.TotalMinutes:0}분 동안 Ping 응답 없음: {pc.Name}. " +
                       "대상 PC의 BIOS/NIC Wake-on-LAN 설정을 확인하세요. (방화벽이 Ping을 막고 있을 수도 있습니다)");
        }

        pc.Status = PcStatus.Off;
    }

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
