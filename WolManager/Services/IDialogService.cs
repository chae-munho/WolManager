namespace WolManager.Services;

/// 대화상자 종류. 아이콘 모양과 색이 달라진다.
public enum DialogKind
{
    /// 묻기 (강조색 ?)
    Question,

    /// 주의가 필요한 동작이나 경고 (황색 !)
    Warning,

    /// 알림 (강조색 i)
    Info,
}

/// ViewModel이 View 타입 없이 대화상자를 띄우기 위한 서비스.
public interface IDialogService
{
    /// 확인 버튼 하나짜리 경고 창을 띄운다.
    void ShowWarning(string message, string title = "확인이 필요합니다");

    /// 확인 버튼 하나짜리 알림 창을 띄운다.
    void ShowInfo(string message, string title = "알림");

    /// 확인/취소 창을 띄운다.
    /// confirmText: 확인 버튼 글자 (예: 깨우기, 끄기, 삭제)
    /// 반환: 확인 버튼을 누르면 true
    bool Confirm(string message, string title, string confirmText, DialogKind kind = DialogKind.Question);
}
