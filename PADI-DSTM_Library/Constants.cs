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
        public const string REMOTE_DATASERV_OBJ_NAME = "DataServer";
        public const string REMOTE_CLIE_OBJ_NAME = "ChatClient";
        public const string MASTER_SERVER_URL = "tcp://localhost:9999/MasterServer";

        public const int HEARTBEAT_CHECK_INTERVAL = 2000; // milliseconds (2 seconds)
        public const int HEARTBEAT_SEND_INTERVAL = HEARTBEAT_CHECK_INTERVAL / 2; // to make sure that a heartbeat is sent within the check interval.
    }
}
