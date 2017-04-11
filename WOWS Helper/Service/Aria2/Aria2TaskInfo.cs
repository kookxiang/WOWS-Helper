using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CookComputing.XmlRpc;

namespace WOWS_Helper.Service.Aria2
{
    public class Aria2TaskInfo
    {
        [XmlRpcMissingMapping(MappingAction.Ignore)]
        public string bitfield;
        public string completedLength;
        public string connections;
        public string dir;
        public string downloadSpeed;
        [XmlRpcMissingMapping(MappingAction.Ignore)]
        public object[] files;
        public string gid;
        public string numPieces;
        public string pieceLength;
        public string status;
        public string totalLength;
        public string uploadLength;
        public string uploadSpeed;
    }
}
