namespace WolManager.Services;

/// <summary>
/// ViewModel이 View 타입 없이 대화상자를 띄우기 위한 서비스.
/// </summary>
public interface IDialogService
{
    /// <summary>
    /// 경고 메시지를 보여 준다.
    /// </summary>
    void ShowWarning(string message);

    /// <summary>
    /// 예/아니오 확인창을 띄운다.
    /// </summary>
    /// <returns>예를 누르면 true</returns>
    bool Confirm(string message);
}
