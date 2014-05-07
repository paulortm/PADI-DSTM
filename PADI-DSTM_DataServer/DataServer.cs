using PADI_DSTM;
using PADI_DSTM_CommonLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Text;
using System.Threading;
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

        // <uid, tid>
        private Dictionary<int, int> lockedPadints = new Dictionary<int,int>();

        // <uid, pending transactions> 
        private Dictionary<int, LinkedList<Thread>> pendingTransactions = new Dictionary<int, LinkedList<Thread>>();

        // Provisory stage of the PadInt's before a commit
        // <tid, <uid, padint value>>
        private Dictionary<int, Dictionary<int, int>> uncommitedChanges = new Dictionary<int, Dictionary<int, int>>();

        private Mutex mutex = new Mutex();

        private bool doFail = false;
        private bool doFreeze = false;

        // if the padint is used by a transaction then lock the thread
        private void lockPadInt(int uid, int tid)
        {
            this.mutex.WaitOne();

            // verify if the padint is locked
            if (lockedPadints.ContainsKey(uid) && lockedPadints[uid] != tid)
            {
                // lock the thread
                pendingTransactions[uid].AddLast(Thread.CurrentThread);
                this.mutex.ReleaseMutex();
                while (true)
                    Thread.Sleep(1000);
            }

            this.mutex.ReleaseMutex();

            lockedPadints.Add(uid, tid);
            if (!pendingTransactions.ContainsKey(uid))
                pendingTransactions.Add(uid, new LinkedList<Thread>());

            
        }

        private void unlockPadInt(int uid, int tid)
        {
            if (pendingTransactions[uid].Count() > 0)
            {
                Thread nextThread = pendingTransactions[uid].First.Value;
                pendingTransactions[uid].RemoveFirst();
                nextThread.Resume();
            }
            lockedPadints.Remove(uid);
        }

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

        private void aquireLockOnPadInt(int tid, int uid)
        {
            // verify if the padint is locked
            if (lockedPadints.ContainsKey(uid) && lockedPadints[uid] != tid) {
                // lock the thread
                pendingTransactions[uid].AddLast(Thread.CurrentThread);
                while (true)
                    Thread.Sleep(1000);
            }

            lockedPadints.Add(uid, tid);
            if (!pendingTransactions.ContainsKey(uid))
                pendingTransactions.Add(uid, new LinkedList<Thread>());

        }

        private void releaseLockOnPadInt(int tid, int uid) {
            if (pendingTransactions[uid].Count() > 0)
            {
                Thread nextThread = pendingTransactions[uid].First.Value;
                pendingTransactions[uid].RemoveFirst();
                nextThread.Resume();
            }
            lockedPadints.Remove(uid);
       }

        public int Read(int tid, int uid)
        {
            this.checkFailOrFreeze();
            this.lockPadInt(uid, tid);

            if (!padints.ContainsKey(uid))
            {
                throw new InexistentPadIntException(uid);
            }

            if (this.uncommitedChanges.ContainsKey(tid))
            {
                if (this.uncommitedChanges[tid].ContainsKey(uid))
                {
                    return this.uncommitedChanges[tid][uid];
                }
                else
                {
                    int? value = padints[uid];
                    if (value == null)
                        throw new NullPadIntException(uid);

                    this.uncommitedChanges[tid].Add(uid, (int)value);

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
            this.lockPadInt(uid, tid);

            if (!padints.ContainsKey(uid))
            {
                throw new InexistentPadIntException(uid);
            }

            if (this.uncommitedChanges.ContainsKey(tid))
            {
                if (this.uncommitedChanges[tid].ContainsKey(uid))
                {
                    this.uncommitedChanges[tid][uid] = value;
                }
                else
                {
                    this.uncommitedChanges[tid].Add(uid, value);
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

        public bool Status()
        {
            Console.WriteLine("");
            Console.WriteLine("------- DUMPING DATASERVER STATUS --------");
            Console.WriteLine("");

            Console.WriteLine("Number of PadInts: " + padints.Count());

            foreach (KeyValuePair<int, int?> entry in padints)
            {
                Console.WriteLine("PadInt " + entry.Key + " value: " + entry.Value);
            }

            Console.WriteLine("");
            Console.WriteLine("Dumping Uncommited Transactions:");

            int currentTxId = -1;
            foreach (KeyValuePair<int, Dictionary<int, int>> entry in uncommitedChanges)
            {
                Dictionary<int, int> uncommitedPadInts = entry.Value;

                foreach (KeyValuePair<int, int> entry2 in uncommitedPadInts)
                {
                    if (currentTxId != entry.Key)
                    {
                        currentTxId = entry.Key;
                        Console.Write("TxId: " + entry.Key + "\t --> PadInt<id,value> = " + "<" + entry2.Key + "," + entry2.Value + ">\n");
                    }
                    else
                    {
                        Console.Write("      \t --> PadInt<id,value> = " + "<" + entry2.Key + "," + entry2.Value + ">\n");
                    }

                }


            }



            return true;
        }

        public bool Abort(int tid)
        {
            if(!this.uncommitedChanges.ContainsKey(tid))
            {
                throw new TransactionNotFoundException(tid, DataServerApp.dataServerUrl);
            }

            this.uncommitedChanges.Remove(tid);
            foreach (var uidValue in uncommitedChanges[tid])
            {
                this.unlockPadInt(tid, uidValue.Key);
            }
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
                this.unlockPadInt(uid, tid);
            }

            this.uncommitedChanges.Remove(tid);

            // When the synchronization is implemented it should also release 
            // the locks on the padInts used by this thread.

            return true;
        }
    }
}
