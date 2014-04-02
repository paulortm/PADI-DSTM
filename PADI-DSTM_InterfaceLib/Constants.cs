using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PADI_DSTM_CommonLib
{
    public static class Constants
    {
        public const int MASTER_SERV_PORT = 9999;
        public const int SERVICE_CLIENT_PORT = 8888;
        public const string REMOTE_MASTER_OBJ_NAME = "MasterServer";
        public const string REMOTE_CLIE_OBJ_NAME = "ChatClient";
        public const string MASTER_SERVER_URL = "tcp://localhost:9999/MasterServer";
    }
}
