using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Xml.Linq;
using WOWS_Helper.Mapper;
using WOWS_Helper.ViewModel;

namespace WOWS_Helper.Service
{
    public class UpdateCheckerService
    {
        private LauncherConfig config;

        public async Task<bool> Start()
        {
            var hasUpdate = false;
            LauncherViewModel.UpdateStatusText("正在检查更新...");
            config = new LauncherConfig();

            var request = WebRequest.Create(config.GetUpdateUrl());
            var response = await request.GetResponseAsync();
            var outStream = response.GetResponseStream();
            var responseText = new StreamReader(outStream).ReadToEnd();
            var result = XDocument.Parse(responseText);
            result.Root.Element("parts").Elements("part").ToList().ForEach(node =>
            {
                if (node.Attribute("latest").Value != "true")
                {
                    hasUpdate = true;
                }
            });
            return !hasUpdate;
        }
    }
}
