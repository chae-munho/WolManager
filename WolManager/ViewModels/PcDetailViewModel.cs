using System.ComponentModel;
using WolManager.Models;
using WolManager.Mvvm;
using WolManager.Services;
using WolManager.Validation;

namespace WolManager.ViewModels;

/// 선택한 PC 패널 ViewModel. 상태를 보여 주고 깨우기, 끄기, 수정, 삭제를 맡는다.
public sealed class PcDetailViewModel : ObservableObject
{
    private readonly IPcRepository _repository;
    private readonly IPcPowerService _power;
    private readonly IDialogService _dialog;
    private readonly ILogService _log;
    private readonly RelayCommand _updateCommand;
    private readonly RelayCommand _deleteCommand;
    private readonly AsyncRelayCommand _wakeCommand;
    private readonly AsyncRelayCommand _shutdownCommand;

    private PcEntry? _pc;
    private string _name = string.Empty;
    private string _mac = string.Empty;
    private string _ip = string.Empty;

    // 입력칸에 마지막으로 채운 값. 사용자가 고치지 않은 칸은 스캔 등으로 바뀐 값을 따라간다.
    private string _loadedName = string.Empty;
    private string _loadedMac = string.Empty;
    private string _loadedIp = string.Empty;

    public PcDetailViewModel(IPcRepository repository, IPcPowerService power, IDialogService dialog, ILogService log)
    {
        _repository = repository;
        _power = power;
        _dialog = dialog;
        _log = log;

        _updateCommand = new RelayCommand(Update, () => Pc is not null);
        _deleteCommand = new RelayCommand(Delete, () => Pc is not null);
        _wakeCommand = new AsyncRelayCommand(
            () => Pc is { } pc ? _power.WakeAsync([pc]) : Task.CompletedTask,
            () => Pc is not null,
            ex => _log.Write($"깨우기 중 오류가 발생했습니다. ({ex.Message})"));
        _shutdownCommand = new AsyncRelayCommand(
            () => Pc is { } pc ? _power.ShutdownAsync(pc) : Task.CompletedTask,
            () => Pc is not null,
            ex => _log.Write($"끄기 중 오류가 발생했습니다. ({ex.Message})"));
    }

    /// 보여 주는 PC. 없으면 "PC를 선택하세요" 안내를 보여 준다.
    public PcEntry? Pc
    {
        get => _pc;
        private set
        {
            var previous = _pc;
            if (!SetProperty(ref _pc, value))
            {
                return;
            }

            if (previous is not null)
            {
                previous.PropertyChanged -= OnPcPropertyChanged;
            }

            if (value is not null)
            {
                value.PropertyChanged += OnPcPropertyChanged;
            }

            OnPropertyChanged(nameof(HasPc));
            _updateCommand.RaiseCanExecuteChanged();
            _deleteCommand.RaiseCanExecuteChanged();
            _wakeCommand.RaiseCanExecuteChanged();
            _shutdownCommand.RaiseCanExecuteChanged();
        }
    }

    public bool HasPc => Pc is not null;

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    public string Mac
    {
        get => _mac;
        set => SetProperty(ref _mac, value);
    }

    public string Ip
    {
        get => _ip;
        set => SetProperty(ref _ip, value);
    }

    public RelayCommand UpdateCommand => _updateCommand;

    public RelayCommand DeleteCommand => _deleteCommand;

    public AsyncRelayCommand WakeCommand => _wakeCommand;

    public AsyncRelayCommand ShutdownCommand => _shutdownCommand;

    /// 보여 줄 PC를 정하고 입력칸을 채운다. null이면 선택 안내를 보여 준다.
    public void Show(PcEntry? pc)
    {
        Pc = pc;
        Name = _loadedName = pc?.Name ?? string.Empty;
        Mac = _loadedMac = pc?.Mac ?? string.Empty;
        Ip = _loadedIp = pc?.Ip ?? string.Empty;
    }

    private void Update()
    {
        if (Pc is not { } pc)
        {
            return;
        }

        var error = PcFormValidator.Validate(_repository.Items, pc, Name, Mac, Ip, out var name, out var mac, out var ip);
        if (error is not null)
        {
            _dialog.ShowWarning(error);
            return;
        }

        _repository.Update(pc, name, mac, ip);
        Show(pc);
    }

    private void Delete()
    {
        if (Pc is not { } pc || !_dialog.Confirm($"'{pc.Name}'을(를) 목록에서 삭제할까요?"))
        {
            return;
        }

        _repository.Remove(pc);
        Show(null);
    }

    // 스캔이나 MAC 찾기로 PC 정보가 바뀌면, 사용자가 고치지 않은 입력칸만 새 값으로 바꾼다.
    private void OnPcPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not PcEntry pc)
        {
            return;
        }

        switch (e.PropertyName)
        {
            case nameof(PcEntry.Name) when Name == _loadedName:
                Name = _loadedName = pc.Name;
                break;
            case nameof(PcEntry.Mac) when Mac == _loadedMac:
                Mac = _loadedMac = pc.Mac;
                break;
            case nameof(PcEntry.Ip) when Ip == _loadedIp:
                Ip = _loadedIp = pc.Ip;
                break;
        }
    }
}
