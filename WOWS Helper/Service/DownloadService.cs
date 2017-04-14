using CookComputing.XmlRpc;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Xml.Linq;
using WOWS_Helper.Mapper;
using WOWS_Helper.Properties;
using WOWS_Helper.Service.Aria2;
using WOWS_Helper.ViewModel;

namespace WOWS_Helper.Service
{
    [XmlRpcUrl("http://127.0.0.1:40031/rpc")]
    public interface IAria2 : IXmlRpcProxy
    {
        [XmlRpcMethod("aria2.addUri")]
        void AddUrl(string[] files);

        [XmlRpcMethod("aria2.getGlobalStat")]
        Aria2Statistics GetGlobalStatistics();

        [XmlRpcMethod("aria2.tellActive")]
        Aria2TaskInfo[] GetAliveTasks();

        [XmlRpcMethod("aria2.tellStopped")]
        Aria2TaskInfo[] GetStoppedTasks(int offset, int num);
    }

    class DownloadService
    {
        private string tempDirectory;
        private Process launcherProcess;
        public Process UpdateProcess;
        public long FinishSize;
        public long TotalSize;
        private List<DownloadTask> taskList;
        private IAria2 aria2Proxy = XmlRpcProxyGen.Create<IAria2>();

        ~DownloadService()
        {
            StopAria2();
        }

        private void StartAria2()
        {
            // 新建临时文件夹
            tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            var aria2Archive = Path.Combine(tempDirectory, "aria2c.7z");
            Directory.CreateDirectory(tempDirectory);
            File.WriteAllBytes(aria2Archive, Resources.aria2c);

            // 释放 7-Zip
            var sevenZipBin = Path.GetTempFileName();
            File.WriteAllBytes(sevenZipBin, Resources.sevenZipDec);

            // 解压 aria2c
            var process = Process.Start(new ProcessStartInfo
            {
                FileName = sevenZipBin,
                Arguments = "e " + aria2Archive,
                WorkingDirectory = tempDirectory,
                WindowStyle = ProcessWindowStyle.Hidden,
                UseShellExecute = false,
                CreateNoWindow = true
            });
            process.WaitForExit();

            var id = Process.GetCurrentProcess().Id;
            UpdateProcess = Process.Start(new ProcessStartInfo
            {
                FileName = Path.Combine(tempDirectory, "aria2c.exe"),
                Arguments = "--no-conf --continue=true --force-save=true --enable-mmap=true --file-allocation=falloc --max-concurrent-downloads=2 --max-connection-per-server=15 --min-split-size=2M --stop-with-process=" + id + " --dir=Updates --enable-rpc=true --rpc-allow-origin-all=true --rpc-listen-port=40031",
                WindowStyle = ProcessWindowStyle.Hidden,
                UseShellExecute = false,
                CreateNoWindow = true
            });
        }

        private void StopAria2()
        {
            if (UpdateProcess != null && !UpdateProcess.HasExited)
            {
                UpdateProcess.Kill();
                UpdateProcess.WaitForExit();
            }
            if (!Directory.Exists(tempDirectory))
                return;
            Directory.Delete(tempDirectory, true);
        }

        private Task updateDownloadListAsync()
        {
            taskList = new List<DownloadTask>();
            var config = new LauncherConfig();
            var targetList = new List<string>
            {
                "launcher&lang=" + config.Language,
                "client",
                "locale&locale_lang=" + config.Language,
                "sdcontent"
            };
            var tasks = targetList.Select(async target =>
            {
                var request = WebRequest.Create(config.UpdateServer + "/?protocol_ver=4&target=" + target + "&launcher_ver=" + config.LauncherVersion + "&client_ver=" + config.ClientVersion + "&locale_ver=" + config.LocaleVersion + "&sdcontent_ver=" + config.SdContentVersion + "&lang=" + config.Language);
                var response = await request.GetResponseAsync();
                var outStream = response.GetResponseStream();
                Debug.Assert(outStream != null, "outStream != null");
                var responseText = new StreamReader(outStream).ReadToEnd();
                var result = XDocument.Parse(responseText);
                var needUpdate = false;
                result.Root.Element("parts").Elements("part").ToList().ForEach(node =>
                {
                    if (node.Attribute("latest").Value != "true") needUpdate = true;
                });
                if (!needUpdate) return;
                result.Root.Element("content").Elements("file").ToList().ForEach(file =>
                {
                    var fileSize = long.Parse(file.Element("size").Value.Trim());
                    TotalSize += fileSize;

                    // 删除已存在但大小不对的文件
                    var savePath = "Updates/" + file.Element("name").Value;
                    if (File.Exists(savePath) && new FileInfo(savePath).Length != fileSize)
                        File.Delete(savePath);

                    // 添加到 Aria2 下载器
                    aria2Proxy.AddUrl(new[] { file.Element("http").Value.Trim() });
                    taskList.Add(new DownloadTask() { SavePath = savePath, FileSize = fileSize });
                });
            });
            return Task.WhenAll(tasks);
        }

        public async void DownloadGameUpdate()
        {
        START_DOWNLOAD:
            StartAria2();
            LauncherViewModel.UpdateStatusText("正在下载游戏更新...");
            await updateDownloadListAsync();
            LauncherViewModel.UpdateStatusText("正在下载游戏更新 (" + Math.Round(TotalSize / 10485.76) / 100 + "MB)...");
            await Task.Delay(1000);
            Aria2Statistics statistics;
            do
            {
                await Task.Delay(500);
                statistics = aria2Proxy.GetGlobalStatistics();
                FinishSize = 0;
                aria2Proxy.GetStoppedTasks(0, 50).ToList().ForEach(task =>
                {
                    FinishSize += long.Parse(task.totalLength);
                });
                aria2Proxy.GetAliveTasks().ToList().ForEach(task =>
                {
                    FinishSize += long.Parse(task.completedLength);
                });
                LauncherViewModel.UpdateProgress(Math.Round((double)1000 * FinishSize / TotalSize) / 10);
                LauncherViewModel.UpdateStatusText("正在以 " + Math.Round(double.Parse(statistics.downloadSpeed) / 1024) + " kb/s 速度下载更新 (" + Math.Round(FinishSize / 10485.76) / 100 + "MB / " + Math.Round(TotalSize / 10485.76) / 100 + "MB)...");
            } while (statistics.numActive != "0" || statistics.numWaiting != "0");
            LauncherViewModel.UpdateProgress(0);
            LauncherViewModel.UpdateStatusText("正在校验更新文件, 请稍候...");
            double index = 0;
            var isAllValid = taskList.All(task =>
            {
                LauncherViewModel.UpdateProgress(100 * (index++) / taskList.Count);
                if (!task.SavePath.EndsWith(".wgpkg")) return true;
                var isValid = task.IsFileValid();
                if (!isValid) File.Delete(task.SavePath);
                return isValid;
            });
            if (!isAllValid)
            {
                LauncherViewModel.UpdateStatusText("部分更新文件损坏, 5 秒后自动重新下载...");
                StopAria2();
                await Task.Delay(5000);
                goto START_DOWNLOAD;
            }
            LauncherViewModel.UpdateProgress(100);
            StopAria2();
            await Task.Delay(1000);
            LauncherViewModel.UpdateStatusText("更新下载完毕, 使用官方启动器更新");
            LauncherViewModel.UpdateStatus(LauncherStatus.STATUS_NORMAL);
            try
            {
                var logWatcher = new LauncherLogService();
                logWatcher.OnFileChanged += log =>
                {
                    if (log.Contains("Update complete") || log.Contains("Update complete"))
                    {
                        logWatcher.Stop();
                        if (!launcherProcess.HasExited) launcherProcess?.Kill();
                        LauncherViewModel.UpdateStatusText("更新完成, 3 秒后自动重新启动...");
                        LauncherViewModel.UpdateStatus(LauncherStatus.STATUS_NORMAL);
                        App.Restart();
                    }
                    else if (log.Contains("ERROR:"))
                    {
                        logWatcher.Stop();
                        if (!launcherProcess.HasExited) launcherProcess?.Kill();
                        LauncherViewModel.UpdateStatusText("更新失败! 官方更新器报错啦");
                        LauncherViewModel.UpdateStatus(LauncherStatus.STATUS_ERROR);
                    }
                };
                logWatcher.StartWatch();
                launcherProcess = Process.Start(new ProcessStartInfo
                {
                    FileName = "wowslauncher.exe",
                    WindowStyle = ProcessWindowStyle.Hidden,
                    UseShellExecute = false,
                    CreateNoWindow = true
                });
            }
            catch (Exception)
            {
                LauncherViewModel.UpdateStatus(LauncherStatus.STATUS_ERROR);
                LauncherViewModel.UpdateProgress(100);
                LauncherViewModel.UpdateStatusText("启动官方更新器失败?!");
            }
        }

        private class SevenZipHelper
        {
            public string ExecPath;

            public SevenZipHelper()
            {
                ExecPath = Path.GetTempFileName();
                File.WriteAllBytes(ExecPath, Resources.sevenZipDec);
            }

            public bool TestFile(string filePath)
            {
                var process = Process.Start(new ProcessStartInfo
                {
                    FileName = ExecPath,
                    Arguments = "t " + filePath,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    UseShellExecute = false,
                    CreateNoWindow = true
                });
                process.WaitForExit();
                return process.ExitCode == 0;
            }
        }

        private class DownloadTask
        {
            public string SavePath;
            public long FileSize;
            private readonly SevenZipHelper _sevenZipHelper = new SevenZipHelper();

            public bool IsFileValid()
            {
                if (new FileInfo(SavePath).Length != FileSize) return false;
                if (!_sevenZipHelper.TestFile(SavePath)) return false;
                return true;
            }
        }
    }
}
