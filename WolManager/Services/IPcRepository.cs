using System.Collections.ObjectModel;
using WolManager.Models;

namespace WolManager.Services;

/// PC 목록을 단독으로 소유하고 저장하는 서비스. 목록 변경은 모두 이 서비스를 거친다.
/// UI 스레드에서만 호출한다.
public interface IPcRepository
{
    /// PC 목록.
    ReadOnlyObservableCollection<PcEntry> Items { get; }

    /// 저장 파일에서 목록을 불러온다. 파일이 없거나 깨져 있으면 빈 목록으로 시작한다.
    void Load();

    /// 현재 목록을 저장 파일에 기록한다.
    /// 반환: 저장에 성공하면 true
    bool Save();

    /// PC를 목록에 추가하고 저장한다.
    void Add(PcEntry entry);

    /// PC의 이름, MAC, IP를 바꾸고 저장한다.
    void Update(PcEntry entry, string name, string mac, string ip);

    /// PC를 목록에서 삭제하고 저장한다.
    void Remove(PcEntry entry);

    /// 스캔 결과를 목록에 병합하고 저장한다.
    /// MAC이 같으면 IP 갱신, MAC은 다르고 IP가 같으면 MAC 갱신, 둘 다 없으면 신규 등록한다.
    /// 응답한 PC는 켜짐으로 표시한다.
    MergeSummary MergeScanResults(IReadOnlyList<ScanResult> results);
}
