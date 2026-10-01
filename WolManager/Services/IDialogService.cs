namespace WolManager.Services;

/// ViewModel이 View 타입 없이 대화상자를 띄우기 위한 서비스.
public interface IDialogService
{
    /// 경고 메시지를 보여 준다.
    void ShowWarning(string message);

    /// 예/아니오 확인창을 띄운다.
    /// 반환: 예를 누르면 true
    bool Confirm(string message);
}
