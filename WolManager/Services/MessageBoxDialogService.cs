using System.Windows;

namespace WolManager.Services;

/// WPF MessageBox로 대화상자를 띄우는 구현.
public sealed class MessageBoxDialogService : IDialogService
{
    private const string Caption = "PC 원격 켜기";

    /// 경고 메시지를 보여 준다.
    public void ShowWarning(string message)
    {
        Show(message, MessageBoxButton.OK, MessageBoxImage.Warning);
    }

    /// 예/아니오 확인창을 띄운다.
    /// 반환: 예를 누르면 true
    public bool Confirm(string message)
    {
        return Show(message, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;
    }

    private static MessageBoxResult Show(string message, MessageBoxButton button, MessageBoxImage image)
    {
        var owner = Application.Current?.MainWindow;
        return owner is null
            ? MessageBox.Show(message, Caption, button, image)
            : MessageBox.Show(owner, message, Caption, button, image);
    }
}
