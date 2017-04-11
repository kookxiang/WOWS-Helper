using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using WOWS_Helper.Annotations;

namespace WOWS_Helper.ViewModel
{
    public enum LauncherStatus
    {
        STATUS_NORMAL,
        STATUS_WORKING,
        STATUS_ERROR
    }

    public class LauncherViewModel : INotifyPropertyChanged
    {
        public static LauncherViewModel Current;

        public LauncherViewModel()
        {
            Current = this;
        }

        public string ActiveColor
        {
            get => _activeColor;
            set
            {
                _activeColor = value;
                NotifyPropertyChanged();
            }
        }

        public string StatusText
        {
            get => _statusText;
            set
            {
                _statusText = value;
                NotifyPropertyChanged();
            }
        }

        public double ProgressValue
        {
            get => _progressValue;
            set
            {
                _progressValue = value;
                NotifyPropertyChanged();
            }
        }

        private string _activeColor = "#007acc";
        private double _progressValue = 100;
        private string _statusText = "请稍候...";

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        private void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }


        public static void UpdateStatus(LauncherStatus status)
        {
            switch (status)
            {
                case LauncherStatus.STATUS_NORMAL:
                    Current.ActiveColor = "#007acc";
                    break;
                case LauncherStatus.STATUS_WORKING:
                    Current.ActiveColor = "#ca5100";
                    break;
                case LauncherStatus.STATUS_ERROR:
                    Current.ActiveColor = "#ff3333";
                    break;
                default:
                    throw new NotSupportedException();
            }
        }

        public static void UpdateStatusText(string status)
        {
            Current.StatusText = status;
        }

        public static void UpdateProgress(double progress)
        {
            Current.ProgressValue = progress;
        }
    }
}