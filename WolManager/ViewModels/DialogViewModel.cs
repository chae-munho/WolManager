using WolManager.Mvvm;
using WolManager.Services;

namespace WolManager.ViewModels;

/// 앱 전용 대화상자 ViewModel. 확인/취소를 누르면 Result가 정해지고 창이 닫힌다.
public sealed class DialogViewModel : ObservableObject
{
    private bool? _result;

    public DialogViewModel(DialogKind kind, string title, string message, string confirmText, string? cancelText)
    {
        Kind = kind;
        Title = title;
        Message = message;
        ConfirmText = confirmText;
        CancelText = cancelText;
        ConfirmCommand = new RelayCommand(() => Result = true);
        CancelCommand = new RelayCommand(() => Result = false);
    }

    public DialogKind Kind { get; }

    public string Title { get; }

    public string Message { get; }

    public string ConfirmText { get; }

    /// 취소 버튼 글자. null이면 확인 버튼만 보인다.
    public string? CancelText { get; }

    public bool HasCancel => CancelText is not null;

    /// 아이콘 안의 글자
    public string IconText => Kind switch
    {
        DialogKind.Warning => "!",
        DialogKind.Info => "i",
        _ => "?",
    };

    /// 정해지면 창이 닫힌다 (DialogCloseBehavior)
    public bool? Result
    {
        get => _result;
        private set => SetProperty(ref _result, value);
    }

    public RelayCommand ConfirmCommand { get; }

    public RelayCommand CancelCommand { get; }
}
