using System.Runtime.InteropServices;

namespace WolManager.Native;

/// 원격 PC 연결(mpr.dll)과 원격 종료(advapi32.dll)에 필요한 Windows 함수.
internal static class RemoteApi
{
    public const int NoError = 0;
    public const uint ResourceTypeAny = 0;

    /// 계획된 종료로 기록하는 종료 사유 값
    public const uint ShutdownReasonPlanned = 0x80000000;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct NetResource
    {
        public uint Scope;
        public uint Type;
        public uint DisplayType;
        public uint Usage;
        public string? LocalName;
        public string? RemoteName;
        public string? Comment;
        public string? Provider;
    }

    /// 원격 공유(\\IP\IPC$)에 계정으로 연결한다. 반환: 0이면 성공, 아니면 Windows 오류 코드
    [DllImport("mpr.dll", CharSet = CharSet.Unicode, EntryPoint = "WNetAddConnection2W")]
    public static extern int WNetAddConnection2(ref NetResource resource, string? password, string? userName, uint flags);

    /// 원격 공유 연결을 끊는다.
    [DllImport("mpr.dll", CharSet = CharSet.Unicode, EntryPoint = "WNetCancelConnection2W")]
    public static extern int WNetCancelConnection2(string name, uint flags, [MarshalAs(UnmanagedType.Bool)] bool force);

    /// 원격 PC에 종료를 요청한다. 실패하면 false이고 Marshal.GetLastWin32Error로 오류 코드를 얻는다.
    [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true, EntryPoint = "InitiateSystemShutdownExW")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool InitiateSystemShutdownEx(
        string machineName,
        string? message,
        uint timeoutSeconds,
        [MarshalAs(UnmanagedType.Bool)] bool forceAppsClosed,
        [MarshalAs(UnmanagedType.Bool)] bool rebootAfterShutdown,
        uint reason);
}
