using System.Windows;

namespace WolManager.Services;

/// <summary>
/// WPF MessageBox로 대화상자를 띄우는 구현.
/// </summary>
public sealed class MessageBoxDialogService : IDialogService
{
    private const string Caption = "PC 원격 켜기";

    /// <summary>
    /// 경고 메시지를 보여 준다.
    /// </summary>
    public void ShowWarning(string message)
    {
        Show(message, MessageBoxButton.OK, MessageBoxImage.Warning);
    }

    /// <summary>
    /// 예/아니오 확인창을 띄운다.
    /// </summary>
    /// <returns>예를 누르면 true</returns>
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
