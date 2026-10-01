using WolManager.Models;

namespace WolManager.Services;

/// PC 깨우기/끄기를 맡는 서비스. 확인창, 다른 네트워크 경고, 로그, 상태 변경까지 처리한다.
/// 목록(전체 깨우기)과 선택한 PC 패널(개별 깨우기/끄기)이 함께 쓴다. UI 스레드에서 호출한다.
public interface IPcPowerService
{
    /// 주어진 PC에 매직 패킷을 보내고, 보낸 PC는 깨우는 중으로 바꾼다. 확인창은 띄우지 않는다.
    Task WakeAsync(IReadOnlyList<PcEntry> pcs);

    /// 확인을 받은 뒤 PC 한 대에 매직 패킷을 보낸다.
    Task WakeOneAsync(PcEntry pc);

    /// 확인을 받은 뒤 PC에 원격 종료를 요청하고, 받아들여지면 끄는 중으로 바꾼다.
    Task ShutdownAsync(PcEntry pc);
}
