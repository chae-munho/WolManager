using System.Collections.ObjectModel;
using WolManager.Models;
using WolManager.Mvvm;
using WolManager.Services;

namespace WolManager.ViewModels;

/// <summary>
/// PC 목록 영역 ViewModel.
/// </summary>
public sealed class PcListViewModel : ObservableObject
{
    private PcEntry? _selectedPc;

    public PcListViewModel(IPcRepository repository)
    {
        Items = repository.Items;
    }

    public ReadOnlyObservableCollection<PcEntry> Items { get; }

    /// <summary>목록에서 선택한 PC. 메인 ViewModel이 편집 영역과 연결한다.</summary>
    public PcEntry? SelectedPc
    {
        get => _selectedPc;
        set => SetProperty(ref _selectedPc, value);
    }
}
