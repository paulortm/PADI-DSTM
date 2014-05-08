using PADI_DSTM;
using PADI_DSTM_CommonLib;
using System;
using System.Collections;
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

    internal class DSPadint
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

    public class DataServer : MarshalByRefObject, IDataServer
    {
        private int timestampCounter = 0;

        // <uid, padint>
        private Dictionary<int, DSPadint> padints = new Dictionary<int, DSPadint>();

        // Provisory stage of the PadInt's before a commit
        // <tid, <uid, padint>>
        private Dictionary<int, Dictionary<int, DSPadint>> uncommitedChanges = new Dictionary<int, Dictionary<int, DSPadint>>();


        private LinkedList<int> padintsBeingCommited = new LinkedList<int>();


        private Mutex mutex = new Mutex();

        private bool doFail = false;
        private bool doFreeze = false;

        public PadInt createPadInt(int uid)
        {
            checkFailOrFreeze();

            try {
                padints.Add(uid, new DSPadint(0, timestampCounter++));
                Console.WriteLine("PadInt created: " + uid);
            } catch (ArgumentException) {
                throw new InvalidPadIntIdException(uid);
            }
            
            return new PadInt(uid, DataServerApp.dataServerUrl);
        }

        public int Read(int tid, int uid)
        {
            this.checkFailOrFreeze();

            if (!padints.ContainsKey(uid))
            {
                throw new InexistentPadIntException(uid);
            }

            if (this.uncommitedChanges.ContainsKey(tid))
            {
                if (this.uncommitedChanges[tid].ContainsKey(uid))
                {
                    return this.uncommitedChanges[tid][uid].Value;
                }
                else
                {
                    /**********************************************************/
                    DSPadint padint = new DSPadint(padints[uid].Value, padints[uid].Timestamp);

                    this.uncommitedChanges[tid].Add(uid, padint);

                    return padint.Value;
                }
            }
            else
            {
                /**********************************************************/
                DSPadint padint = new DSPadint(padints[uid].Value, padints[uid].Timestamp);

                Dictionary<int, DSPadint> changedPadInts = new Dictionary<int, DSPadint>();
                changedPadInts.Add(uid, padint);

                this.uncommitedChanges.Add(tid, changedPadInts);

                return padint.Value;
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
                if (this.uncommitedChanges[tid].ContainsKey(uid))
                {
                    this.uncommitedChanges[tid][uid].Value = value;
                }
                else
                {
                    /*******************************************/
                    int timestamp = padints[uid].Timestamp;
                    this.uncommitedChanges[tid].Add(uid, new DSPadint(value, timestamp));
                }
            }
            else
            {
                /*******************************************/
                int timestamp = padints[uid].Timestamp;
                Dictionary<int, DSPadint> changedPadInts = new Dictionary<int,DSPadint>();
                changedPadInts.Add(uid, new DSPadint(value, timestamp));

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

            foreach (KeyValuePair<int, DSPadint> entry in padints)
            {
                Console.WriteLine("PadInt " + entry.Key + " value: " + entry.Value.Value);
            }

            Console.WriteLine("");
            Console.WriteLine("Dumping Uncommited Transactions:");

            int currentTxId = -1;
            foreach (KeyValuePair<int, Dictionary<int, DSPadint>> entry in uncommitedChanges)
            {
                Dictionary<int, DSPadint> uncommitedPadInts = entry.Value;

                foreach (KeyValuePair<int, DSPadint> entry2 in uncommitedPadInts)
                {
                    if (currentTxId != entry.Key)
                    {
                        currentTxId = entry.Key;
                        Console.Write("TxId: " + entry.Key + "\t --> PadInt<id,value> = " + "<" + entry2.Key + "," + entry2.Value.Value + ">\n");
                    }
                    else
                    {
                        Console.Write("      \t --> PadInt<id,value> = " + "<" + entry2.Key + "," + entry2.Value.Value + ">\n");
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
            // When the synchronization is implemented it should also release 
            // the locks on the padInts used by this thread.
            return true;
        }

        public bool canCommit(int tid)
        {
            lock(this.padintsBeingCommited) {

                foreach (KeyValuePair<int, DSPadint> entry in this.uncommitedChanges[tid])
                {
                    int uid = entry.Key;
                    if (this.padintsBeingCommited.Contains(uid))
                    {
                        return false;
                    }
                }

                foreach (KeyValuePair<int, DSPadint> entry in this.uncommitedChanges[tid])
                {
                    int uid = entry.Key;
                    this.padintsBeingCommited.AddLast(tid);
                }
            }
            
            Dictionary<int, DSPadint> transactionValues = this.uncommitedChanges[tid];
            foreach (KeyValuePair<int, DSPadint> padintValue in transactionValues)
            {
                if (padints[padintValue.Key].Timestamp > padintValue.Value.Timestamp)
                {
                    return false;
                }
            }

            return true;
        }

        public bool Commit(int tid)
        {
            if (!this.uncommitedChanges.ContainsKey(tid))
            {
                throw new TransactionNotFoundException(tid, DataServerApp.dataServerUrl);
            }

            // save the values on the padints dictionary
            Dictionary<int, DSPadint> transactionValues = this.uncommitedChanges[tid];
            foreach (KeyValuePair<int, DSPadint> padintValue in transactionValues)
            {
                int uid = padintValue.Key;
                int value = padintValue.Value.Value;
                this.padints[uid].Value = value;
            }

            this.uncommitedChanges.Remove(tid);

            lock (this.padintsBeingCommited)
            {
                foreach (KeyValuePair<int, DSPadint> entry in this.uncommitedChanges[tid])
                {
                    int uid = entry.Key;
                    this.padintsBeingCommited.Remove(uid);
                }
            }

            return true;
        }
    }
}
