using System.Runtime.InteropServices;
using WolManager.Native;
using WolManager.Validation;

namespace WolManager.Services;

/// \\IP\IPC$에 계정으로 연결한 뒤 InitiateSystemShutdownEx로 종료를 요청하는 구현.
/// 명령 프롬프트의 "shutdown /s /f /m \\IP"와 같은 기능이다.
public sealed class RemoteShutdownService : IShutdownService
{
    // Windows 오류 코드
    private const int ErrorAccessDenied = 5;
    private const int ErrorBadNetPath = 53;
    private const int ErrorNetworkUnreachable = 1231;
    private const int ErrorLogonFailure = 1326;
    private const int ErrorAccountRestriction = 1327;
    private const int ErrorSessionCredentialConflict = 1219;

    /// 대상 PC에 정해진 계정(빈 비밀번호)으로 연결해 종료를 요청한다. IP가 있어야 한다.
    public async Task<ShutdownResult> ShutdownAsync(string ip, CancellationToken cancellationToken = default)
    {
        if (!InputValidator.TryNormalizeIp(ip, out var normalized) || normalized.Length == 0)
        {
            return new ShutdownResult(false, "IP가 없어 끌 수 없습니다. 네트워크 스캔으로 IP를 채운 뒤 다시 시도하세요.");
        }

        // 연결과 종료 요청은 응답을 기다리며 스레드를 막으므로 전용 스레드에서 실행한다.
        var request = Task.Factory.StartNew(
            () => Shutdown(normalized),
            cancellationToken,
            TaskCreationOptions.LongRunning,
            TaskScheduler.Default);

        try
        {
            return await request.WaitAsync(Constants.ShutdownRequestTimeout, cancellationToken);
        }
        catch (TimeoutException)
        {
            return new ShutdownResult(false, "응답이 없어 시간이 초과되었습니다. PC가 켜져 있고 같은 네트워크에 있는지 확인하세요.");
        }
    }

    private static ShutdownResult Shutdown(string ip)
    {
        var machine = $@"\\{ip}";
        var share = $@"{machine}\IPC$";
        var resource = new RemoteApi.NetResource { Type = RemoteApi.ResourceTypeAny, RemoteName = share };

        // 다른 계정으로 남아 있는 연결이 있으면 충돌하므로 먼저 끊는다.
        RemoteApi.WNetCancelConnection2(share, 0, true);

        var connect = RemoteApi.WNetAddConnection2(ref resource, string.Empty, Constants.ShutdownAccountName, 0);
        if (connect != RemoteApi.NoError)
        {
            return new ShutdownResult(false, Describe(connect));
        }

        try
        {
            var ok = RemoteApi.InitiateSystemShutdownEx(
                machine,
                null,
                0,
                Constants.ShutdownForceAppsClosed,
                false,
                RemoteApi.ShutdownReasonPlanned);
            return ok
                ? new ShutdownResult(true, string.Empty)
                : new ShutdownResult(false, Describe(Marshal.GetLastWin32Error()));
        }
        finally
        {
            RemoteApi.WNetCancelConnection2(share, 0, true);
        }
    }

    // Windows 오류 코드를 사용자가 할 일 위주의 안내로 바꾼다.
    private static string Describe(int error) => error switch
    {
        ErrorBadNetPath or ErrorNetworkUnreachable =>
            $"PC에 연결할 수 없습니다. PC가 켜져 있는지, 방화벽에서 파일 및 프린터 공유가 허용되어 있는지 확인하세요. (오류 {error})",
        ErrorLogonFailure =>
            $"{Constants.ShutdownAccountName} 계정으로 로그인하지 못했습니다. 계정 이름과 빈 비밀번호 설정을 확인하세요. (오류 {error})",
        ErrorAccountRestriction =>
            "빈 비밀번호로 네트워크 로그인이 막혀 있습니다. 대상 PC 로컬 보안 정책에서 " +
            $"'콘솔 로그온 시 로컬 계정에서 빈 암호 사용 제한'을 사용 안 함으로 바꾸세요. (오류 {error})",
        ErrorAccessDenied =>
            $"끌 권한이 없습니다. {Constants.ShutdownAccountName} 계정이 관리자 그룹인지, " +
            $"레지스트리 LocalAccountTokenFilterPolicy=1이 설정되어 있는지 확인하세요. (오류 {error})",
        ErrorSessionCredentialConflict =>
            $"이 PC에 다른 계정으로 연결된 상태가 남아 있습니다. 잠시 후 다시 시도하세요. (오류 {error})",
        _ => $"끄지 못했습니다. (Windows 오류 {error})",
    };
}
