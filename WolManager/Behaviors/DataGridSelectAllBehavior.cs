using System.Windows;
using System.Windows.Controls;

namespace WolManager.Behaviors;

/// ViewModel의 bool 값으로 DataGrid 전체 행 선택을 켜고 끄는 Attached Behavior.
/// 평소에는 한 행만 선택되도록 두고, 전체 선택하는 동안에만 여러 행 선택을 허용한다.
public static class DataGridSelectAllBehavior
{
    public static readonly DependencyProperty IsAllSelectedProperty = DependencyProperty.RegisterAttached(
        "IsAllSelected",
        typeof(bool),
        typeof(DataGridSelectAllBehavior),
        new PropertyMetadata(false, OnIsAllSelectedChanged));

    public static bool GetIsAllSelected(DependencyObject element) => (bool)element.GetValue(IsAllSelectedProperty);

    public static void SetIsAllSelected(DependencyObject element, bool value) => element.SetValue(IsAllSelectedProperty, value);

    private static void OnIsAllSelectedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not DataGrid grid)
        {
            return;
        }

        if ((bool)e.NewValue)
        {
            grid.SelectionMode = DataGridSelectionMode.Extended;
            grid.SelectAll();
        }
        else
        {
            grid.UnselectAll();
            grid.SelectionMode = DataGridSelectionMode.Single;
        }
    }
}
