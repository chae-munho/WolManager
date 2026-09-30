using System.IO;
using System.Windows;
using WolManager.Services;
using WolManager.ViewModels;

namespace WolManager;

/// <summary>
/// 앱 시작 시 서비스와 ViewModel을 직접 만들어 창에 연결한다.
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // 서비스
        ILogService logService = new LogService(Dispatcher);
        IPcRepository pcRepository = new JsonPcRepository(
            logService,
            Path.Combine(AppContext.BaseDirectory, Constants.PcFileName));
        IDialogService dialogService = new MessageBoxDialogService();

        // ViewModel
        var mainViewModel = new MainViewModel(
            new PcListViewModel(pcRepository),
            new EditorViewModel(pcRepository, dialogService),
            new LogViewModel(logService));

        var mainWindow = new MainWindow { DataContext = mainViewModel };
        MainWindow = mainWindow;
        mainWindow.Show();

        pcRepository.Load();
    }
}
