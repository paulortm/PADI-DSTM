using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace PADI_DSTM_CommonLib
{
    public static class Util
    {
        public static string getLocalIP()
        {
            IPHostEntry host;
            string localIP = "?";
            host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (IPAddress ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    localIP = ip.ToString();
                }
            }
            return localIP;
        }
    }

    public class DSPadint
    {
        private int value;
        private int timestamp;

        public DSPadint(int value, int timestamp)
        {
            this.value = value;
            this.timestamp = timestamp;
        }

        public int Value
        {
            set { this.value = value; }
            get { return this.value; }
        }

        public int Timestamp
        {
            set { this.timestamp = value; }
            get { return this.timestamp; }
        }
    }
}
