using WolManager.Models;
using WolManager.Mvvm;
using WolManager.Services;
using WolManager.Validation;

namespace WolManager.ViewModels;

/// <summary>
/// 편집 영역 ViewModel. PC를 직접 추가, 수정, 삭제한다.
/// </summary>
public sealed class EditorViewModel : ObservableObject
{
    private readonly IPcRepository _repository;
    private readonly IDialogService _dialog;
    private readonly RelayCommand _updateCommand;
    private readonly RelayCommand _deleteCommand;

    private PcEntry? _editingPc;
    private string _name = string.Empty;
    private string _mac = string.Empty;
    private string _ip = string.Empty;

    public EditorViewModel(IPcRepository repository, IDialogService dialog)
    {
        _repository = repository;
        _dialog = dialog;

        NewCommand = new RelayCommand(() => Edit(null));
        AddCommand = new RelayCommand(Add);
        _updateCommand = new RelayCommand(Update, () => EditingPc is not null);
        _deleteCommand = new RelayCommand(Delete, () => EditingPc is not null);
    }

    /// <summary>수정/삭제할 PC. 없으면 새로 입력하는 상태</summary>
    public PcEntry? EditingPc
    {
        get => _editingPc;
        private set
        {
            if (SetProperty(ref _editingPc, value))
            {
                _updateCommand.RaiseCanExecuteChanged();
                _deleteCommand.RaiseCanExecuteChanged();
            }
        }
    }

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

    public RelayCommand NewCommand { get; }

    public RelayCommand AddCommand { get; }

    public RelayCommand UpdateCommand => _updateCommand;

    public RelayCommand DeleteCommand => _deleteCommand;

    /// <summary>
    /// 편집할 PC를 정하고 입력칸을 채운다. null이면 입력칸을 비운다.
    /// </summary>
    public void Edit(PcEntry? pc)
    {
        EditingPc = pc;
        Name = pc?.Name ?? string.Empty;
        Mac = pc?.Mac ?? string.Empty;
        Ip = pc?.Ip ?? string.Empty;
    }

    private void Add()
    {
        if (!TryValidate(null, out var name, out var mac, out var ip))
        {
            return;
        }

        _repository.Add(new PcEntry { Name = name, Mac = mac, Ip = ip, IsTarget = true });
        Edit(null);
    }

    private void Update()
    {
        if (EditingPc is not { } pc || !TryValidate(pc, out var name, out var mac, out var ip))
        {
            return;
        }

        _repository.Update(pc, name, mac, ip);
        Edit(pc);
    }

    private void Delete()
    {
        if (EditingPc is not { } pc)
        {
            return;
        }

        if (!_dialog.Confirm($"'{pc.Name}'을(를) 목록에서 삭제할까요?"))
        {
            return;
        }

        _repository.Remove(pc);
        Edit(null);
    }

    // 입력값을 검사하고 정리한다. 문제가 있으면 경고를 띄우고 false를 돌려준다.
    // except: 수정 중인 PC (중복 검사에서 제외)
    private bool TryValidate(PcEntry? except, out string name, out string mac, out string ip)
    {
        name = Name.Trim();
        mac = string.Empty;
        ip = string.Empty;

        if (name.Length == 0)
        {
            _dialog.ShowWarning("이름을 입력하세요.");
            return false;
        }

        if (!InputValidator.TryNormalizeMac(Mac, out mac))
        {
            _dialog.ShowWarning("MAC 주소 형식이 올바르지 않습니다.\n예: AA-BB-CC-DD-EE-FF");
            return false;
        }

        if (!InputValidator.TryNormalizeIp(Ip, out ip))
        {
            _dialog.ShowWarning("IP 주소 형식이 올바르지 않습니다.\n예: 192.168.0.11 (모르면 비워 두세요)");
            return false;
        }

        var normalizedMac = mac;
        var sameMac = _repository.Items.FirstOrDefault(item => item != except && item.Mac == normalizedMac);
        if (sameMac is not null)
        {
            _dialog.ShowWarning($"이미 등록된 MAC 주소입니다. ({sameMac.Name})");
            return false;
        }

        var normalizedIp = ip;
        var sameIp = normalizedIp.Length == 0
            ? null
            : _repository.Items.FirstOrDefault(item => item != except && item.Ip == normalizedIp);
        if (sameIp is not null)
        {
            _dialog.ShowWarning($"이미 등록된 IP 주소입니다. ({sameIp.Name})");
            return false;
        }

        return true;
    }
}
