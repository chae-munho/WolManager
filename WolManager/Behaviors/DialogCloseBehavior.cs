using System.Windows;

namespace WolManager.Behaviors;

/// ViewModel의 결과 값이 정해지면 대화상자 창의 DialogResult로 넘겨 창을 닫는 Attached Behavior.
public static class DialogCloseBehavior
{
    public static readonly DependencyProperty DialogResultProperty = DependencyProperty.RegisterAttached(
        "DialogResult",
        typeof(bool?),
        typeof(DialogCloseBehavior),
        new PropertyMetadata(null, OnDialogResultChanged));

    public static bool? GetDialogResult(DependencyObject element) => (bool?)element.GetValue(DialogResultProperty);

    public static void SetDialogResult(DependencyObject element, bool? value) => element.SetValue(DialogResultProperty, value);

    private static void OnDialogResultChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is Window window && e.NewValue is bool result && window.IsVisible)
        {
            window.DialogResult = result;
        }
    }
}
