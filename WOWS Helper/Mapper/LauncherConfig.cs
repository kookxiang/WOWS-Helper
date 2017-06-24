using System.Xml.Linq;

namespace WOWS_Helper.Mapper
{
    class LauncherConfig
    {
        public string ClientVersion = "unknown";
        public string SdContentVersion = "unknown";
        public string LocaleVersion = "unknown";
        public string LauncherVersion = "unknown";
        public string UdSoundVersion = "unknown";
        public string SelectedTarget = "high";
        public string Language = "zh_cn";
        public string UpdateServer = "http://update.worldofwarships.cn";
        private XDocument xmlDocument;

        public LauncherConfig()
        {
            Load();
        }

        private void Load()
        {
            xmlDocument = XDocument.Load("wowslauncher.cfg");
            ClientVersion = xmlDocument.Root?.Element("client_ver")?.Value;
            SdContentVersion = xmlDocument.Root?.Element("sdcontent_ver")?.Value;
            LocaleVersion = xmlDocument.Root?.Element("locale_ver")?.Value;
            LauncherVersion = xmlDocument.Root?.Element("launcher_ver")?.Value;
            UdSoundVersion = xmlDocument.Root?.Element("udsound_ver")?.Value;
            SelectedTarget = xmlDocument.Root?.Element("selected_target")?.Value;
            Language = xmlDocument.Root?.Element("lang")?.Value;
            UpdateServer = xmlDocument.Root?.Element("patch_info_urls")?.Element("item")?.Value;
        }

        public string GetUpdateUrl()
        {
            var requestUrl = UpdateServer + "/?protocol_ver=4";
            if (SelectedTarget == "ultra")
            {
                requestUrl += "&target=launcher,client,locale,sdcontent,udsound";
            }
            else
            {
                requestUrl += "&target=launcher,client,locale,sdcontent";
            }
            requestUrl += "&launcher_ver=" + LauncherVersion;
            requestUrl += "&client_ver=" + ClientVersion;
            requestUrl += "&locale_ver=" + LocaleVersion;
            requestUrl += "&sdcontent_ver=" + SdContentVersion;
            requestUrl += "&udsound_ver=" + UdSoundVersion;
            requestUrl += "&lang=" + Language;
            return requestUrl;
        }
    }
}
