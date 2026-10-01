using System.Windows;
using WolManager.ViewModels;
using WolManager.Views;

namespace WolManager.Services;

/// 앱 전용 디자인의 대화상자(DialogWindow)를 띄우는 구현.
public sealed class DialogService : IDialogService
{
    private const string OkText = "확인";
    private const string CancelText = "취소";

    /// 확인 버튼 하나짜리 경고 창을 띄운다.
    public void ShowWarning(string message, string title = "확인이 필요합니다")
    {
        Show(new DialogViewModel(DialogKind.Warning, title, message, OkText, null));
    }

    /// 확인 버튼 하나짜리 알림 창을 띄운다.
    public void ShowInfo(string message, string title = "알림")
    {
        Show(new DialogViewModel(DialogKind.Info, title, message, OkText, null));
    }

    /// 확인/취소 창을 띄운다.
    /// 반환: 확인 버튼을 누르면 true
    public bool Confirm(string message, string title, string confirmText, DialogKind kind = DialogKind.Question)
    {
        return Show(new DialogViewModel(kind, title, message, confirmText, CancelText));
    }

    private static bool Show(DialogViewModel viewModel)
    {
        var window = new DialogWindow { DataContext = viewModel };
        var owner = Application.Current?.MainWindow;
        if (owner is not null && owner.IsVisible && !ReferenceEquals(owner, window))
        {
            window.Owner = owner;
        }
        else
        {
            window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }

        return window.ShowDialog() == true;
    }
}
