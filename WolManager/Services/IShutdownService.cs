namespace WolManager.Services;

/// 원격 끄기 결과.
/// Succeeded: 종료 요청이 받아들여졌으면 true
/// Message: 실패 원인 안내 (성공이면 빈 문자열)
public sealed record ShutdownResult(bool Succeeded, string Message);

/// 원격 PC를 끄는 서비스. 대상 PC에 설치할 프로그램 없이 Windows 기본 원격 종료 기능을 쓴다.
public interface IShutdownService
{
    /// 대상 PC에 정해진 계정(빈 비밀번호)으로 연결해 종료를 요청한다. IP가 있어야 한다.
    Task<ShutdownResult> ShutdownAsync(string ip, CancellationToken cancellationToken = default);
}
