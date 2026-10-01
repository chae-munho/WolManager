using System.Windows;
using System.Windows.Controls;
using WolManager.Models;

namespace WolManager.Controls;

/// 상태 점과 상태 글자를 함께 보여 주는 컨트롤.
public partial class StatusIndicator : UserControl
{
    public static readonly DependencyProperty StatusProperty = DependencyProperty.Register(
        nameof(Status),
        typeof(PcStatus),
        typeof(StatusIndicator),
        new PropertyMetadata(PcStatus.Unknown));

    public StatusIndicator()
    {
        InitializeComponent();
    }

    public PcStatus Status
    {
        get => (PcStatus)GetValue(StatusProperty);
        set => SetValue(StatusProperty, value);
    }
}
