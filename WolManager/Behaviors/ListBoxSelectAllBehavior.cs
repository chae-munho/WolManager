using System.Windows;
using System.Windows.Controls;

namespace WolManager.Behaviors;

/// ViewModel의 bool 값으로 ListBox 전체 선택을 켜고 끄는 Attached Behavior.
/// 평소에는 하나만 선택되도록 두고, 전체 선택하는 동안에만 여러 개 선택을 허용한다.
public static class ListBoxSelectAllBehavior
{
    public static readonly DependencyProperty IsAllSelectedProperty = DependencyProperty.RegisterAttached(
        "IsAllSelected",
        typeof(bool),
        typeof(ListBoxSelectAllBehavior),
        new PropertyMetadata(false, OnIsAllSelectedChanged));

    public static bool GetIsAllSelected(DependencyObject element) => (bool)element.GetValue(IsAllSelectedProperty);

    public static void SetIsAllSelected(DependencyObject element, bool value) => element.SetValue(IsAllSelectedProperty, value);

    private static void OnIsAllSelectedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not ListBox list)
        {
            return;
        }

        if ((bool)e.NewValue)
        {
            list.SelectionMode = SelectionMode.Extended;
            list.SelectAll();
        }
        else
        {
            list.UnselectAll();
            list.SelectionMode = SelectionMode.Single;
        }
    }
}
