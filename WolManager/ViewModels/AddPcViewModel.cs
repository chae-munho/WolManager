using WolManager.Models;
using WolManager.Mvvm;
using WolManager.Services;
using WolManager.Validation;

namespace WolManager.ViewModels;

/// 새 PC 추가 창 ViewModel. 이름, MAC, IP를 입력받아 목록에 추가한다.
public sealed class AddPcViewModel : ObservableObject
{
    private readonly IPcRepository _repository;
    private readonly IDialogService _dialog;
    private bool _isOpen;
    private string _name = string.Empty;
    private string _mac = string.Empty;
    private string _ip = string.Empty;

    public AddPcViewModel(IPcRepository repository, IDialogService dialog)
    {
        _repository = repository;
        _dialog = dialog;
        AddCommand = new RelayCommand(Add);
        CancelCommand = new RelayCommand(() => IsOpen = false);
    }

    /// PC를 추가했을 때. 메인 ViewModel이 새 PC를 선택한다.
    public event EventHandler<PcEntry>? Added;

    public bool IsOpen
    {
        get => _isOpen;
        private set => SetProperty(ref _isOpen, value);
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

    public RelayCommand AddCommand { get; }

    public RelayCommand CancelCommand { get; }

    /// 입력칸을 비우고 추가 창을 연다.
    public void Open()
    {
        Name = string.Empty;
        Mac = string.Empty;
        Ip = string.Empty;
        IsOpen = true;
    }

    private void Add()
    {
        var error = PcFormValidator.Validate(_repository.Items, null, Name, Mac, Ip, out var name, out var mac, out var ip);
        if (error is not null)
        {
            _dialog.ShowWarning(error);
            return;
        }

        var pc = new PcEntry { Name = name, Mac = mac, Ip = ip };
        _repository.Add(pc);
        IsOpen = false;
        Added?.Invoke(this, pc);
    }
}
