namespace WolManager.Services;

/// <summary>
/// 주기적으로 Ping을 보내 PC 상태를 갱신하는 서비스. UI 스레드에서 시작하고 호출한다.
/// </summary>
public interface IStatusMonitorService : IDisposable
{
    /// <summary>
    /// 바로 한 번 확인하고, 이후 정해진 주기마다 확인한다.
    /// </summary>
    void Start();

    /// <summary>
    /// 주기적인 확인을 멈춘다.
    /// </summary>
    void Stop();

    /// <summary>
    /// 지금 바로 한 번 확인한다. 이전 확인이 진행 중이면 건너뛴다.
    /// </summary>
    Task RefreshAsync();
}
