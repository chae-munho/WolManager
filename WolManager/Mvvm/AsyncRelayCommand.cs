using System.Windows.Input;

namespace WolManager.Mvvm;

/// <summary>
/// 비동기 동작을 실행하는 커맨드. 실행 중에는 비활성화되고, 예외는 오류 처리기로 넘긴다.
/// </summary>
public sealed class AsyncRelayCommand : ObservableObject, ICommand
{
    private readonly Func<Task> _execute;
    private readonly Func<bool>? _canExecute;
    private readonly Action<Exception>? _onError;
    private bool _isRunning;

    public AsyncRelayCommand(Func<Task> execute, Func<bool>? canExecute = null, Action<Exception>? onError = null)
    {
        _execute = execute;
        _canExecute = canExecute;
        _onError = onError;
    }

    public event EventHandler? CanExecuteChanged;

    /// <summary>
    /// 현재 실행 중인지 여부.
    /// </summary>
    public bool IsRunning
    {
        get => _isRunning;
        private set
        {
            if (SetProperty(ref _isRunning, value))
            {
                RaiseCanExecuteChanged();
            }
        }
    }

    public bool CanExecute(object? parameter) => !IsRunning && (_canExecute?.Invoke() ?? true);

    public void Execute(object? parameter)
    {
        // ExecuteAsync가 모든 예외를 잡으므로 결과를 기다리지 않아도 된다.
        _ = ExecuteAsync();
    }

    /// <summary>
    /// 커맨드를 실행하고 끝날 때까지 기다린다. 예외는 오류 처리기로 넘기고 밖으로 던지지 않는다.
    /// </summary>
    public async Task ExecuteAsync()
    {
        if (!CanExecute(null))
        {
            return;
        }

        IsRunning = true;
        try
        {
            await _execute();
        }
        catch (Exception ex)
        {
            _onError?.Invoke(ex);
        }
        finally
        {
            IsRunning = false;
        }
    }

    /// <summary>
    /// 실행 가능 여부를 다시 평가하도록 알린다.
    /// </summary>
    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}

/// <summary>
/// 커맨드 파라미터를 받는 비동기 커맨드. 실행 중에는 비활성화되고, 예외는 오류 처리기로 넘긴다.
/// </summary>
public sealed class AsyncRelayCommand<T> : ObservableObject, ICommand
{
    private readonly Func<T?, Task> _execute;
    private readonly Func<T?, bool>? _canExecute;
    private readonly Action<Exception>? _onError;
    private bool _isRunning;

    public AsyncRelayCommand(Func<T?, Task> execute, Func<T?, bool>? canExecute = null, Action<Exception>? onError = null)
    {
        _execute = execute;
        _canExecute = canExecute;
        _onError = onError;
    }

    public event EventHandler? CanExecuteChanged;

    /// <summary>
    /// 현재 실행 중인지 여부.
    /// </summary>
    public bool IsRunning
    {
        get => _isRunning;
        private set
        {
            if (SetProperty(ref _isRunning, value))
            {
                RaiseCanExecuteChanged();
            }
        }
    }

    public bool CanExecute(object? parameter) => !IsRunning && (_canExecute?.Invoke(Convert(parameter)) ?? true);

    public void Execute(object? parameter)
    {
        // ExecuteAsync가 모든 예외를 잡으므로 결과를 기다리지 않아도 된다.
        _ = ExecuteAsync(Convert(parameter));
    }

    /// <summary>
    /// 커맨드를 실행하고 끝날 때까지 기다린다. 예외는 오류 처리기로 넘기고 밖으로 던지지 않는다.
    /// </summary>
    public async Task ExecuteAsync(T? parameter)
    {
        if (!CanExecute(parameter))
        {
            return;
        }

        IsRunning = true;
        try
        {
            await _execute(parameter);
        }
        catch (Exception ex)
        {
            _onError?.Invoke(ex);
        }
        finally
        {
            IsRunning = false;
        }
    }

    /// <summary>
    /// 실행 가능 여부를 다시 평가하도록 알린다.
    /// </summary>
    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);

    private static T? Convert(object? parameter) => parameter is T value ? value : default;
}
