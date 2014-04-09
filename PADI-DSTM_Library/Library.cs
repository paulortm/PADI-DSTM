using PADI_DSTM_CommonLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Text;
using System.Threading.Tasks;

namespace PADI_DSTM_Library
{
    public class PADI_DSTM
    {
        private static IMaster master;
        private static CurrentTransactionHolder currentTransactionHolder = new CurrentTransactionHolder();

        public static bool Init()
        {
            Console.Write("Choose port:");
            int port = Convert.ToInt32(Console.ReadLine());

            TcpChannel channel = new TcpChannel(port);
            ChannelServices.RegisterChannel(channel, true);

            PADI_DSTM.master = (IMaster)Activator.GetObject(typeof(IMaster), Constants.MASTER_SERVER_URL);
            PADI_DSTM.master.check();
            return true;
        }

        public static PadInt CreatePadInt(int uid)
        {
            return PADI_DSTM.master.createPadIntOnDataServer(uid);
        }

        public static PadInt AccessPadInt(int uid)
        {

            return null;
        }

        public static bool Fail(String url)
        {
            IDataServer server = (IDataServer)Activator.GetObject(typeof(IDataServer), url);

            return server.Fail();
        }


        public static bool Recover(String url)
        {
            IDataServer server = (IDataServer)Activator.GetObject(typeof(IDataServer), url);

            return server.Recover();
        }


    }
}