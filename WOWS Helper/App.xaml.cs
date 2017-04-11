using System.Diagnostics;

namespace WOWS_Helper
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App
    {
        public static void Restart()
        {
            if (ResourceAssembly.Location == null) return;
            Process.Start(ResourceAssembly.Location);
            Current.Shutdown();
        }
    }
}
