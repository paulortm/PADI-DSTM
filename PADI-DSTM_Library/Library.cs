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

            PadInt padint = PADI_DSTM.master.createPadIntOnDataServer(uid);
            padint.setTransactionHolder(currentTransactionHolder);
            return padint;
        }

        public static PadInt AccessPadInt(int uid)
        {
            PadInt padint = PADI_DSTM.master.accessPadIntOnDataServer(uid);
            padint.setTransactionHolder(currentTransactionHolder);
            return padint;
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

        public static bool TxBegin()
        {
            Transaction t = PADI_DSTM.master.createTransaction();
            PADI_DSTM.currentTransactionHolder.set(t);
            return true;
        }

        public static bool TxAbort() {
            Transaction currentTransaction = PADI_DSTM.currentTransactionHolder.get();
            foreach (String serverUrl in currentTransaction.getServers())
            {
                IDataServer dataServer = (IDataServer)Activator.GetObject(typeof(IDataServer), Constants.MASTER_SERVER_URL);
                dataServer.Abort(currentTransaction.getId());
            }
            PADI_DSTM.currentTransactionHolder.set(null);
            return true;
        }

        public static bool TxCommit()
        {
            Transaction currentTransaction = PADI_DSTM.currentTransactionHolder.get();
            bool canCommit = true;
            foreach (String serverUrl in currentTransaction.getServers())
            {
                IDataServer dataServer = (IDataServer)Activator.GetObject(typeof(IDataServer), Constants.MASTER_SERVER_URL);
                if (!dataServer.canCommit(currentTransaction.getId()))
                {
                    canCommit = false;
                    break;
                }
            }
            return true;
        }
    }
}