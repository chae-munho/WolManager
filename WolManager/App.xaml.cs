using System.Windows;
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

        var mainViewModel = new MainViewModel();
        var mainWindow = new MainWindow { DataContext = mainViewModel };

        MainWindow = mainWindow;
        mainWindow.Show();
    }
}
