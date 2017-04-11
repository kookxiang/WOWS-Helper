using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using WOWS_Helper.Service;
using WOWS_Helper.ViewModel;

namespace WOWS_Helper
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow
    {
        private bool skipWaiting;
        private bool isReady;

        public MainWindow()
        {
            var args = Environment.GetCommandLineArgs();
            if (args.Count(arg => arg == "start") > 0) skipWaiting = true;
            InitializeComponent();
            checkGameUpdate();
        }

        private async Task startGameWithDelay()
        {
            if (!skipWaiting)
            {
                LauncherViewModel.UpdateStatusText("点击这里启动游戏 (3 秒后自动启动)");
                await Task.Delay(1000);
            }
            if (!skipWaiting)
            {
                LauncherViewModel.UpdateStatusText("点击这里启动游戏 (2 秒后自动启动)");
                await Task.Delay(1000);
            }
            if (!skipWaiting)
            {
                LauncherViewModel.UpdateStatusText("点击这里启动游戏 (1 秒后自动启动)");
                await Task.Delay(1000);
            }
            LauncherViewModel.UpdateStatus(LauncherStatus.STATUS_WORKING);
            LauncherViewModel.UpdateStatusText("正在启动游戏...");
            Process.Start("WorldOfWarships.exe");
            await Task.Delay(1000);
            Close();
        }

        private async void checkGameUpdate()
        {
            isReady = false;
            await Task.Delay(100);
            try
            {
                var updateChecker = new UpdateCheckerService();
                var isUpdated = await updateChecker.Start();
                if (isUpdated)
                {
                    LauncherViewModel.UpdateStatus(LauncherStatus.STATUS_NORMAL);
                    LauncherViewModel.UpdateStatusText("游戏已是最新版本");
                    LauncherViewModel.UpdateProgress(100);
                    isReady = true;
                    await startGameWithDelay();
                }
                else
                {
                    LauncherViewModel.UpdateStatus(LauncherStatus.STATUS_WORKING);
                    LauncherViewModel.UpdateStatusText("正在启动 aria2c 下载器...");
                    LauncherViewModel.UpdateProgress(0);
                    new DownloadService().DownloadGameUpdate();
                }
            }
            catch (Exception e)
            {
                // MessageBox.Show(e.ToString());
                LauncherViewModel.UpdateStatus(LauncherStatus.STATUS_ERROR);
                LauncherViewModel.UpdateStatusText("发生错误: " + e.Message);
                LauncherViewModel.UpdateProgress(100);
            }
        }

        private void OnExitClicked(object sender, RoutedEventArgs e)
        {
            Hide();
            Close();
        }

        private void OnStartGame(object sender, RoutedEventArgs e)
        {
            if (isReady)
            {
                skipWaiting = true;
            }
        }

        private void OnMoveWindow(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                DragMove();
        }
    }
}