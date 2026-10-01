using WolManager.Mvvm;

namespace WolManager.ViewModels;

/// 하위 영역 ViewModel을 보관하고 서로 연결하는 메인 ViewModel.
public sealed class MainViewModel : ObservableObject
{
    public MainViewModel(
        ScanViewModel scan, PcListViewModel pcList, PcDetailViewModel detail, AddPcViewModel addPc, LogViewModel log)
    {
        Scan = scan;
        PcList = pcList;
        Detail = detail;
        AddPc = addPc;
        Log = log;

        // 목록에서 고른 PC를 선택한 PC 패널에 보여 준다.
        pcList.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(PcListViewModel.SelectedPc))
            {
                detail.Show(pcList.SelectedPc);
            }
        };

        // 패널에서 삭제해 비워지면 목록 선택도 푼다.
        detail.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(PcDetailViewModel.Pc) && detail.Pc is null)
            {
                pcList.SelectedPc = null;
            }
        };

        // 새 PC 추가 버튼은 추가 창을 열고, 추가한 PC는 바로 선택한다.
        pcList.AddRequested += (_, _) => addPc.Open();
        addPc.Added += (_, pc) => pcList.SelectedPc = pc;
    }

    public ScanViewModel Scan { get; }

    public PcListViewModel PcList { get; }

    public PcDetailViewModel Detail { get; }

    public AddPcViewModel AddPc { get; }

    public LogViewModel Log { get; }
}
