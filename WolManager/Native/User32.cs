using System.Runtime.InteropServices;

namespace WolManager.Native;

/// <summary>
/// 창 이동에 필요한 user32.dll 함수.
/// </summary>
internal static class User32
{
    public const int WmNcLButtonDown = 0x00A1;
    public const int HtCaption = 2;

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool ReleaseCapture();

    [DllImport("user32.dll", EntryPoint = "SendMessageW")]
    public static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);
}
