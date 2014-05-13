using PADI_DSTM_CommonLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Text;
using System.Threading.Tasks;

namespace PADI_DSTM
{
    public class PadiDstm
    {
        private static IMaster master;
        private static CurrentTransactionHolder currentTransactionHolder = new CurrentTransactionHolder();
        private static bool inited = false;

        public static bool Init()
        {
            Console.Write("Choose port:");
            int port = Convert.ToInt32(Console.ReadLine());

            TcpChannel channel = new TcpChannel(port);
            ChannelServices.RegisterChannel(channel, true);

            PadiDstm.master = (IMaster)Activator.GetObject(typeof(IMaster), Constants.MASTER_SERVER_URL);
            PadiDstm.master.check();

            PadiDstm.inited = true;

            return true;
        }

        public static PadInt CreatePadInt(int uid)
        {
            PadInt padint = PadiDstm.master.createPadIntOnDataServer(uid);
            padint.setTransactionHolder(currentTransactionHolder);
            return padint;
        }

        public static PadInt AccessPadInt(int uid)
        {
            PadInt padint = PadiDstm.master.accessPadIntOnDataServer(uid);
            padint.setTransactionHolder(currentTransactionHolder);
            return padint;
        }

        public static bool Fail(String url)
        {
            IDataServer server = (IDataServer)Activator.GetObject(typeof(IDataServer), url);

            return server.Fail();
        }

        public static bool Freeze(String url)
        {
            IDataServer server = (IDataServer)Activator.GetObject(typeof(IDataServer), url);

            return server.Freeze();
        }

        public static bool Recover(String url)
        {
            IDataServer server = (IDataServer)Activator.GetObject(typeof(IDataServer), url);

            return server.Recover();
        }

        public static bool Status()
        {
            return master.Status();
        }

        public static bool TxBegin()
        {
            if (!PadiDstm.inited)
            {
                throw new UninitedLibException();
            }
            Transaction t = PadiDstm.master.createTransaction();
            PadiDstm.currentTransactionHolder.set(t);
            return true;
        }

        public static bool TxAbort() {
            Transaction currentTransaction = PadiDstm.currentTransactionHolder.get();
            foreach (String serverUrl in currentTransaction.getServers())
            {
                IDataServer dataServer = (IDataServer)Activator.GetObject(typeof(IDataServer), serverUrl);
                dataServer.Abort(currentTransaction.getId());
            }
            PadiDstm.currentTransactionHolder.set(null);
            return true;
        }

        public static bool TxCommit()
        {
            if (PadiDstm.currentTransactionHolder.get() == null)
            {
                throw new OutOfTransactionException();
            }

            // two phase commit

            // voting
            Transaction currentTransaction = PadiDstm.currentTransactionHolder.get();
            PadiDstm.currentTransactionHolder.set(null);

            // verify if there was any write or read during the transaction
            if (currentTransaction.getServers() != null)
            {
                foreach (String serverUrl in currentTransaction.getServers())
                {
                    IDataServer dataServer = (IDataServer)Activator.GetObject(typeof(IDataServer), serverUrl);
                    if (!dataServer.canCommit(currentTransaction.getId()))
                    {
                        // abort all transactions and return false
                        foreach (String serverUrl2 in currentTransaction.getServers())
                        {
                            dataServer = (IDataServer)Activator.GetObject(typeof(IDataServer), serverUrl2);
                            dataServer.Abort(currentTransaction.getId());
                        }
                        return false;
                    }
                }

                // commit
                foreach (String serverUrl in currentTransaction.getServers())
                {
                    IDataServer dataServer = (IDataServer)Activator.GetObject(typeof(IDataServer), serverUrl);
                    if (!dataServer.Commit(currentTransaction.getId()))
                    {
                        // one of the data servers didn't acknolaged the commit
                        foreach (String serverUrl2 in currentTransaction.getServers())
                        {
                            dataServer = (IDataServer)Activator.GetObject(typeof(IDataServer), serverUrl2);
                            dataServer.Abort(currentTransaction.getId());
                        }
                        return false;
                    }
                }
            }

            return true;
        }
    }
}