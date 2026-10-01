using WolManager.Mvvm;

namespace WolManager.ViewModels;

/// 하위 영역 ViewModel을 보관하고 서로 연결하는 메인 ViewModel.
public sealed class MainViewModel : ObservableObject
{
    public MainViewModel(ScanViewModel scan, PcListViewModel pcList, EditorViewModel editor, LogViewModel log)
    {
        Scan = scan;
        PcList = pcList;
        Editor = editor;
        Log = log;

        // 목록에서 고른 PC를 편집 영역에 채운다.
        pcList.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(PcListViewModel.SelectedPc))
            {
                editor.Edit(pcList.SelectedPc);
            }
        };

        // 편집 영역이 새로 입력 상태가 되면 목록 선택도 푼다.
        editor.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(EditorViewModel.EditingPc) && editor.EditingPc is null)
            {
                pcList.SelectedPc = null;
            }
        };
    }

    public ScanViewModel Scan { get; }

    public PcListViewModel PcList { get; }

    public EditorViewModel Editor { get; }

    public LogViewModel Log { get; }
}
