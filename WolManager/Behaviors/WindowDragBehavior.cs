using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using WolManager.Native;

namespace WolManager.Behaviors;

/// <summary>
/// 창의 빈 영역을 마우스로 끌어 창을 이동할 수 있게 하는 Attached Behavior.
/// 버튼, 입력칸처럼 클릭을 직접 처리하는 컨트롤 위에서는 동작하지 않는다.
/// </summary>
public static class WindowDragBehavior
{
    public static readonly DependencyProperty IsEnabledProperty = DependencyProperty.RegisterAttached(
        "IsEnabled",
        typeof(bool),
        typeof(WindowDragBehavior),
        new PropertyMetadata(false, OnIsEnabledChanged));

    public static bool GetIsEnabled(DependencyObject element) => (bool)element.GetValue(IsEnabledProperty);

    public static void SetIsEnabled(DependencyObject element, bool value) => element.SetValue(IsEnabledProperty, value);

    private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not Window window)
        {
            return;
        }

        window.MouseLeftButtonDown -= OnMouseLeftButtonDown;
        if ((bool)e.NewValue)
        {
            window.MouseLeftButtonDown += OnMouseLeftButtonDown;
        }
    }

    private static void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState != MouseButtonState.Pressed || sender is not Window window)
        {
            return;
        }

        // Window.DragMove는 최대화 상태에서 동작하지 않으므로 제목 표시줄을 잡은 것처럼 처리한다.
        // 이렇게 하면 최대화 상태에서 끌 때 원래 크기로 돌아오며 따라온다.
        var handle = new WindowInteropHelper(window).Handle;
        User32.ReleaseCapture();
        User32.SendMessage(handle, User32.WmNcLButtonDown, User32.HtCaption, IntPtr.Zero);
    }
}
