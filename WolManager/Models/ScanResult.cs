namespace WolManager.Models;

/// <summary>
/// ARP 스캔에서 응답한 장비 하나.
/// </summary>
/// <param name="Ip">IPv4 주소</param>
/// <param name="Mac">AA-BB-CC-DD-EE-FF 형식의 MAC 주소</param>
/// <param name="HostName">호스트명. 조회하지 못하면 IP</param>
public sealed record ScanResult(string Ip, string Mac, string HostName);

/// <summary>
/// 스캔 진행률.
/// </summary>
/// <param name="Done">완료한 요청 수</param>
/// <param name="Total">전체 요청 수</param>
public sealed record ScanProgress(int Done, int Total);

/// <summary>
/// 스캔 결과 병합 요약.
/// </summary>
/// <param name="Responded">응답한 장비 수</param>
/// <param name="Added">새로 등록한 수</param>
/// <param name="Updated">IP 또는 MAC을 갱신한 수</param>
public sealed record MergeSummary(int Responded, int Added, int Updated);
