using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WolManager.Mvvm;

/// 프로퍼티 변경 알림을 제공하는 ViewModel/모델 기반 클래스.
public abstract class ObservableObject : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    /// 값이 바뀌었을 때만 필드를 갱신하고 변경 알림을 보낸다.
    /// 반환: 값이 바뀌었으면 true
    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
