namespace WolManager.Models;

/// 상태별 PC 대수 (목록 위 요약에 색 점과 함께 보여 준다).
public sealed record StatusCount(PcStatus Status, int Count);
