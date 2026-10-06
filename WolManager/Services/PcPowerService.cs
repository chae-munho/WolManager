using WolManager.Models;

namespace WolManager.Services;

/// 매직 패킷 전송과 원격 종료를 PC 상태, 로그, 대화상자와 묶어 처리하는 구현.
public sealed class PcPowerService : IPcPowerService
{
    private const string DifferentNetworkMessage =
        "같은 네트워크에 연결되어 있지 않습니다. 유선 또는 같은 공유기의 Wi-Fi에 연결했는지 확인하세요.";

    private readonly IWakeOnLanService _wakeOnLan;
    private readonly IShutdownService _shutdown;
    private readonly IDialogService _dialog;
    private readonly ILogService _log;

    public PcPowerService(IWakeOnLanService wakeOnLan, IShutdownService shutdown, IDialogService dialog, ILogService log)
    {
        _wakeOnLan = wakeOnLan;
        _shutdown = shutdown;
        _dialog = dialog;
        _log = log;
    }

    /// 주어진 PC에 매직 패킷을 보내고, 보낸 PC는 깨우는 중으로 바꾼다. 확인창은 띄우지 않는다.
    public async Task WakeAsync(IReadOnlyList<PcEntry> pcs)
    {
        // 다른 네트워크에 붙어 있으면 오류 없이 아무것도 켜지지 않으므로 먼저 알려 준다. 전송은 그대로 진행한다.
        if (_wakeOnLan.IsOnSameNetwork(pcs.Select(pc => pc.Ip)) == false)
        {
            _log.Write(DifferentNetworkMessage);
            _dialog.ShowWarning(DifferentNetworkMessage, "다른 네트워크에 연결되어 있습니다");
        }

        var results = await Task.WhenAll(pcs.Select(async pc => (Pc: pc, Result: await _wakeOnLan.WakeAsync(pc.Mac, pc.Ip))));

        var failedLines = new List<string>();
        foreach (var (pc, result) in results)
        {
            var failures = string.Join(", ", result.Failures.Distinct());
            if (!result.Succeeded)
            {
                _log.Write($"깨우기 실패: {pc.Name} - {failures}");
                failedLines.Add($"{pc.Name}: {failures}");
                continue;
            }

            pc.Status = PcStatus.Waking;
            pc.WakeRequestedAt = DateTime.Now;
            pc.ShutdownRequestedAt = null;
            _log.Write($"깨우기 신호를 보냈습니다: {pc.Name} ({pc.Mac})");
            if (failures.Length > 0)
            {
                _log.Write($"일부 연결로는 보내지 못했습니다: {failures}");
            }
        }

        if (failedLines.Count > 0)
        {
            _dialog.ShowWarning(
                $"다음 PC에는 깨우기 신호를 보내지 못했습니다.\n{string.Join("\n", failedLines)}",
                "깨우지 못한 PC가 있습니다");
        }
    }

    /// 확인을 받은 뒤 PC 한 대에 매직 패킷을 보낸다.
    public Task WakeOneAsync(PcEntry pc)
    {
        if (!_dialog.Confirm($"'{pc.Name}' PC를 깨울까요?", "PC 깨우기", "깨우기"))
        {
            return Task.CompletedTask;
        }

        return WakeAsync([pc]);
    }

    /// 확인을 받은 뒤 PC에 원격 종료를 요청하고, 받아들여지면 끄는 중으로 바꾼다.
    public async Task ShutdownAsync(PcEntry pc)
    {
        var question = Constants.ShutdownForceAppsClosed
            ? $"'{pc.Name}' PC를 끌까요?\n실행 중인 프로그램은 저장 없이 강제로 닫힙니다."
            : $"'{pc.Name}' PC를 끌까요?";
        if (!_dialog.Confirm(question, "PC 끄기", "끄기", DialogKind.Warning))
        {
            return;
        }

        _log.Write($"끄기 요청: {pc.Name} ({pc.Ip})");
        var result = await _shutdown.ShutdownAsync(pc.Ip);
        if (!result.Succeeded)
        {
            _log.Write($"끄기 실패: {pc.Name} - {result.Message}");
            _dialog.ShowWarning(result.Message, $"'{pc.Name}'을(를) 끄지 못했습니다");
            return;
        }

        pc.WakeRequestedAt = null;
        pc.ShutdownRequestedAt = DateTime.Now;
        pc.Status = PcStatus.ShuttingDown;
        _log.Write($"끄기 명령을 보냈습니다: {pc.Name}");
    }
}
