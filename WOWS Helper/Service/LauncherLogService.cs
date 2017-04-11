using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace WOWS_Helper.Service
{
    public class LauncherLogService
    {
        private const string LogFilename = "wowslauncher.log";
        private long _lastSize;
        private string _lastContent = "";
        public delegate void FileChangedListener(string newLines);
        public event FileChangedListener OnFileChanged;
        public bool Stopped { get; private set; }

        public void StartWatch()
        {
            if (!File.Exists(LogFilename)) File.Create(LogFilename).Close();
            _lastSize = new FileInfo(LogFilename).Length;
            new Thread(() =>
            {
                while (!Stopped)
                {
                    var currentSize = new FileInfo(LogFilename).Length;
                    if (currentSize == _lastSize) continue;
                    if (currentSize < _lastSize)
                    {
                        _lastSize = currentSize;
                        continue;
                    }
                    HandleFileChange();
                    Thread.Sleep(200);
                }
            }).Start();
        }

        private void HandleFileChange()
        {
            lock (this)
            {
                var fileStream = new FileStream(LogFilename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                fileStream.Seek(_lastSize, SeekOrigin.Begin);
                var streamReader = new StreamReader(fileStream);
                var newContents = streamReader.ReadToEnd().Trim();
                streamReader.Close();
                fileStream.Close();

                (_lastContent + newContents).Replace("\0", "").Split(new[] { Environment.NewLine }, StringSplitOptions.None).ToList().ForEach(line => OnFileChanged?.Invoke(line.Trim()));
                _lastContent = newContents;

                _lastSize = new FileInfo(LogFilename).Length;
            }
        }

        public void Stop()
        {
            if (Stopped) return;
            Stopped = true;
        }
    }
}
