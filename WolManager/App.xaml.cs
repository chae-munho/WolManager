using System.IO;
using System.Windows;
using WolManager.Services;
using WolManager.ViewModels;

namespace WolManager;

/// 앱 시작 시 서비스와 ViewModel을 직접 만들어 창에 연결한다.
public partial class App : Application
{
    private IStatusMonitorService? _statusMonitor;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // 서비스
        ILogService logService = new LogService(Dispatcher);
        IPcRepository pcRepository = new JsonPcRepository(
            logService,
            Path.Combine(AppContext.BaseDirectory, Constants.PcFileName));
        IDialogService dialogService = new DialogService();
        INetworkInterfaceService networkService = new NetworkInterfaceService();
        IWakeOnLanService wakeOnLanService = new WakeOnLanService(networkService);
        IArpScanService arpScanService = new ArpScanService(networkService);
        IShutdownService shutdownService = new RemoteShutdownService();
        IPcPowerService powerService = new PcPowerService(wakeOnLanService, shutdownService, dialogService, logService);
        _statusMonitor = new StatusMonitorService(pcRepository, arpScanService, networkService, logService);

        // 스캔 영역이 등록된 PC로 기본 대역을 고를 수 있도록 ViewModel보다 먼저 불러온다.
        pcRepository.Load();

        // ViewModel
        var mainViewModel = new MainViewModel(
            new ScanViewModel(pcRepository, arpScanService, networkService, logService),
            new PcListViewModel(pcRepository, powerService, _statusMonitor, dialogService, logService),
            new PcDetailViewModel(pcRepository, powerService, dialogService, logService),
            new AddPcViewModel(pcRepository, dialogService),
            new LogViewModel(logService));

        var mainWindow = new MainWindow { DataContext = mainViewModel };
        MainWindow = mainWindow;
        mainWindow.Show();

        _statusMonitor.Start();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _statusMonitor?.Dispose();
        base.OnExit(e);
    }
}
