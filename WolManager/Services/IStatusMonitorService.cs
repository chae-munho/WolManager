namespace WolManager.Services;

/// 주기적으로 Ping을 보내 PC 상태를 갱신하는 서비스. IP 없이 MAC만 등록된 PC는 백그라운드 ARP 스캔으로 IP를 찾는다.
/// UI 스레드에서 시작하고 호출한다.
public interface IStatusMonitorService : IDisposable
{
    /// 바로 한 번 확인하고, 이후 정해진 주기마다 확인한다.
    void Start();

    /// 주기적인 확인을 멈춘다.
    void Stop();

    /// 지금 바로 한 번 확인한다. 이미 확인이 진행 중이면 새로 시작하지 않고 그 확인이 끝날 때까지 기다린다.
    Task RefreshAsync();
}
