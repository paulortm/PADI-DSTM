using PADI_DSTM_CommonLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Text;
using System.Threading.Tasks;



namespace PADI_DSTM_DataServer
{
    class DataServerApp
    {
        internal static string dataServerUrl;

        static void Main(string[] args)
        {
            Console.Write("Choose port:");
            int port = Convert.ToInt32(Console.ReadLine());

            dataServerUrl = "tcp://" + Util.getLocalIP() + ":" + port + "/" + Constants.REMOTE_DATASERV_OBJ_NAME;

            TcpChannel channelServ = new TcpChannel(port);
            ChannelServices.RegisterChannel(channelServ, true);
            RemotingConfiguration.RegisterWellKnownServiceType(typeof(DataServer), Constants.REMOTE_DATASERV_OBJ_NAME, WellKnownObjectMode.Singleton);

            IMaster master = (IMaster)Activator.GetObject(typeof(IMaster), Constants.MASTER_SERVER_URL);
            master.registerDataServer(dataServerUrl);

            Console.WriteLine("Press <enter> to exit");
            Console.ReadLine();
        }
    }

    public class DataServer : MarshalByRefObject, IDataServer
    {
        private Dictionary<int, int?> padints = new Dictionary<int, int?>();

        // Provisory stage of the PadInt's before a commit
        // <tid, <uid, padint value>>
        private Dictionary<int, Dictionary<int, int>> uncommitedChanges = new Dictionary<int, Dictionary<int, int>>();

        private bool doFail = false;
        private bool doFreeze = false;

        public PadInt createPadInt(int uid)
        {
            checkFailOrFreeze();

            try {
                padints.Add(uid, null);
                Console.WriteLine("PadInt created: " + uid);
            } catch (ArgumentException) {
                throw new InvalidPadIntIdException(uid);
            }
            
            return new PadInt(uid, DataServerApp.dataServerUrl);
        }

        public int Read(int tid, int uid)
        {
            checkFailOrFreeze();

            if (!padints.ContainsKey(uid))
            {
                throw new InexistentPadIntException(uid);
            }

            if (this.uncommitedChanges.ContainsKey(tid))
            {
                Dictionary<int, int> padIntsInTransaction = this.uncommitedChanges[tid];

                if (padIntsInTransaction.ContainsKey(uid))
                {
                    return padIntsInTransaction[uid];
                }
                else
                {
                    int? value = padints[uid];
                    if (value == null)
                        throw new NullPadIntException(uid);

                    padIntsInTransaction.Add(uid, (int)value);

                    return (int)value;
                }
            }
            else
            {
                int? value = padints[uid];
                if (value == null)
                    throw new NullPadIntException(uid);

                Dictionary<int, int> changedPadInts = new Dictionary<int, int>();
                changedPadInts.Add(uid, (int)value);

                this.uncommitedChanges.Add(tid, changedPadInts);

                return (int)value;
            }        
        }

        public void Write(int tid, int uid, int value)
        {
            checkFailOrFreeze();

            if (!padints.ContainsKey(uid))
            {
                throw new InexistentPadIntException(uid);
            }

            if (this.uncommitedChanges.ContainsKey(tid))
            {
                Dictionary<int, int> padIntsInTransaction = this.uncommitedChanges[tid];

                if (padIntsInTransaction.ContainsKey(uid))
                {
                    padIntsInTransaction[uid] = value;
                }
                else
                {
                    padIntsInTransaction.Add(uid, value);
                }
            }
            else
            {
                Dictionary<int, int> changedPadInts = new Dictionary<int,int>();
                changedPadInts.Add(uid, value);

                this.uncommitedChanges.Add(tid, changedPadInts);
            }
        }

        public bool Fail()
        {
            doFail = true;
            Console.WriteLine("Fail mode activated");

            return true;
        }

        public bool Freeze()
        {
            doFreeze = true;
            Console.WriteLine("Freeze mode activated");

            return true;
        }

        public bool Recover()
        {
            doFail = false;
            doFreeze = false;

            return true;
        }

        private void checkFailOrFreeze()
        {
            if(doFail)
            {
                Console.WriteLine("Fail mode activated");

                throw new ServerFailedException(Util.getLocalIP());
            }

            if(doFreeze)
            {
                Console.WriteLine("Freeze mode activated");

                while(doFreeze)
                {
                    // wait recover
                    if(doFail)
                    {
                        Console.WriteLine("Fail mode activated");

                        throw new ServerFailedException(Util.getLocalIP());
                    }
                }
            }
        }

        public bool Abort(int tid)
        {
            if(!this.uncommitedChanges.ContainsKey(tid))
            {
                throw new TransactionNotFoundException(tid, DataServerApp.dataServerUrl);
            }

            this.uncommitedChanges.Remove(tid);
            // When the synchronization is implemented it should also release 
            // the locks on the padInts used by this thread.
            return true;
        }

        public bool canCommit(int tid)
        {
            // used only to check if this server is running
            return true;
        }

        public bool Commit(int tid)
        {
            if (!this.uncommitedChanges.ContainsKey(tid))
            {
                throw new TransactionNotFoundException(tid, DataServerApp.dataServerUrl);
            }

            // save the values on the padints dictionary
            Dictionary<int, int> transactionValues = this.uncommitedChanges[tid];
            foreach(KeyValuePair<int, int> padintValue in transactionValues) {
                int uid = padintValue.Key;
                int value = padintValue.Value;
                this.padints[uid] = value;
            }

            this.uncommitedChanges.Remove(tid);
            // When the synchronization is implemented it should also release 
            // the locks on the padInts used by this thread.

            return true;
        }
    }
}
