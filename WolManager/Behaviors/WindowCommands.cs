using System.Windows;
using System.Windows.Input;
using WolManager.Mvvm;

namespace WolManager.Behaviors;

/// 커스텀 제목 표시줄의 창 버튼 커맨드. 커맨드 파라미터로 대상 Window를 받는다.
public static class WindowCommands
{
    public static ICommand Minimize { get; } = new RelayCommand<Window>(window =>
    {
        if (window is not null)
        {
            SystemCommands.MinimizeWindow(window);
        }
    });

    public static ICommand ToggleMaximize { get; } = new RelayCommand<Window>(window =>
    {
        if (window is null)
        {
            return;
        }

        if (window.WindowState == WindowState.Maximized)
        {
            SystemCommands.RestoreWindow(window);
        }
        else
        {
            SystemCommands.MaximizeWindow(window);
        }
    });

    public static ICommand Close { get; } = new RelayCommand<Window>(window => window?.Close());
}
