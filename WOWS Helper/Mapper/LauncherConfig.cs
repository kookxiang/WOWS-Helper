using System.Xml.Linq;

namespace WOWS_Helper.Mapper
{
    class LauncherConfig
    {
        public string ClientVersion = "unknown";
        public string SdContentVersion = "unknown";
        public string LocaleVersion = "unknown";
        public string LauncherVersion = "unknown";
        public string Language = "zh_cn";
        public string UpdateServer = "http://update.worldofwarships.cn";
        private XDocument xmlDocument;

        public LauncherConfig()
        {
            Load();
        }

        public void Load()
        {
            xmlDocument = XDocument.Load("wowslauncher.cfg");
            ClientVersion = xmlDocument.Root?.Element((XName)"client_ver")?.Value;
            SdContentVersion = xmlDocument.Root?.Element((XName)"sdcontent_ver")?.Value;
            LocaleVersion = xmlDocument.Root?.Element((XName)"locale_ver")?.Value;
            LauncherVersion = xmlDocument.Root?.Element((XName)"launcher_ver")?.Value;
            Language = xmlDocument.Root?.Element((XName)"lang")?.Value;
            UpdateServer = xmlDocument.Root?.Element((XName)"patch_info_urls")?.Element((XName)"item")?.Value;
        }
    }
}
