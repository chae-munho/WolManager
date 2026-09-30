using System.Collections.ObjectModel;
using WolManager.Models;

namespace WolManager.Services;

/// <summary>
/// PC 목록을 단독으로 소유하고 저장하는 서비스. 목록 변경은 모두 이 서비스를 거친다.
/// 목록 항목의 대상 체크 변경은 서비스가 감지해 바로 저장한다. UI 스레드에서만 호출한다.
/// </summary>
public interface IPcRepository
{
    /// <summary>
    /// PC 목록.
    /// </summary>
    ReadOnlyObservableCollection<PcEntry> Items { get; }

    /// <summary>
    /// 저장 파일에서 목록을 불러온다. 파일이 없거나 깨져 있으면 빈 목록으로 시작한다.
    /// </summary>
    void Load();

    /// <summary>
    /// 현재 목록을 저장 파일에 기록한다.
    /// </summary>
    /// <returns>저장에 성공하면 true</returns>
    bool Save();

    /// <summary>
    /// PC를 목록에 추가하고 저장한다.
    /// </summary>
    void Add(PcEntry entry);

    /// <summary>
    /// PC의 이름, MAC, IP를 바꾸고 저장한다.
    /// </summary>
    void Update(PcEntry entry, string name, string mac, string ip);

    /// <summary>
    /// PC를 목록에서 삭제하고 저장한다.
    /// </summary>
    void Remove(PcEntry entry);
}
