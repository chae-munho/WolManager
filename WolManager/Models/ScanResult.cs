namespace WolManager.Models;

/// ARP 스캔에서 응답한 장비 하나.
/// Ip: IPv4 주소
/// Mac: AA-BB-CC-DD-EE-FF 형식의 MAC 주소
/// HostName: 호스트명. 조회하지 못하면 IP
public sealed record ScanResult(string Ip, string Mac, string HostName);

/// 스캔 진행률.
/// Done: 완료한 요청 수
/// Total: 전체 요청 수
public sealed record ScanProgress(int Done, int Total);

/// 스캔 결과 병합 요약.
/// Responded: 응답한 장비 수
/// Added: 새로 등록한 수
/// Updated: IP 또는 MAC을 갱신한 수
public sealed record MergeSummary(int Responded, int Added, int Updated);
