﻿using PADI_DSTM;
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
using System.Timers;



namespace PADI_DSTM_DataServer
{
    class DataServerApp
    {
        static void Main(string[] args)
        {
            Console.Write("Choose port:");
            int port = Convert.ToInt32(Console.ReadLine());

            string dataServerUrl = "tcp://" + Util.getLocalIP() + ":" + port + "/" + Constants.REMOTE_DATASERV_OBJ_NAME;
            IMaster masterServer = null;

            TcpChannel channelServ = new TcpChannel(port);
            ChannelServices.RegisterChannel(channelServ, true);
            masterServer = (IMaster)Activator.GetObject(typeof(IMaster), Constants.MASTER_SERVER_URL);
            MarshalByRefObject dataServer = new DataServer(masterServer, dataServerUrl);
            RemotingServices.Marshal(
                dataServer,
                Constants.REMOTE_DATASERV_OBJ_NAME, 
                typeof(IDataServer) );

            Console.WriteLine("Press <enter> to exit");
            Console.ReadLine();
        }
    }

    public class DataServer : MarshalByRefObject, IDataServer
    {
        private int id;
        private String serverUrl;
        private IMaster masterServer = null;
        private IDataServer backupServer = null;

        // <uid, padint>
        private Dictionary<int, DSPadint> primaryPadints = new Dictionary<int, DSPadint>();
        // <uid, padint>
        private Dictionary<int, DSPadint> backupPadints = new Dictionary<int, DSPadint>();

        // Provisory stage of the PadInt's before a commit
        // <tid, <uid, padint>>
        private Dictionary<int, Dictionary<int, DSPadint>> uncommitedChanges = new Dictionary<int, Dictionary<int, DSPadint>>();


        private LinkedList<int> padintsBeingCommited = new LinkedList<int>();


        private Mutex mutex = new Mutex();

        private bool doFail = false;
        private bool doFreeze = false;

        private static System.Timers.Timer ImAliveTimer;

        public DataServer(IMaster master, String serverUrl) {

            this.serverUrl = serverUrl;
            this.masterServer = master;
            this.id = this.masterServer.registerDataServer(this.serverUrl);


            ImAliveTimer = new System.Timers.Timer(1);

            // Hook up the Elapsed event for the timer.
            ImAliveTimer.Elapsed += new ElapsedEventHandler(sendImAlive);

            // Set the Interval to 2 seconds (2000 milliseconds).
            ImAliveTimer.Interval = Constants.HEARTBEAT_SEND_INTERVAL;
            ImAliveTimer.Enabled = true;
            Console.WriteLine("server {0}", this.id);
        }

        private void sendImAlive(object source, ElapsedEventArgs e)
        {
            Console.WriteLine("sending i'm alive");
            masterServer.imAlive(this.id);
        }

        public PadInt createPadInt(int uid)
        {
            checkFailOrFreeze();

            try {
                primaryPadints.Add(uid, new DSPadint(0, masterServer.generateTimestamp()));
                Console.WriteLine("PadInt created: " + uid);
            } catch (ArgumentException) {
                throw new InvalidPadIntIdException(uid);
            }
            
            return new PadInt(uid, this.serverUrl);
        }

        public int Read(int tid, int uid)
        {
            this.checkFailOrFreeze();

            if (!primaryPadints.ContainsKey(uid))
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
                    DSPadint padint = null;
                    lock (primaryPadints[uid])
                    {
                        padint = new DSPadint(primaryPadints[uid].Value, primaryPadints[uid].Timestamp);
                    }

                    this.uncommitedChanges[tid].Add(uid, padint);

                    return padint.Value;
                }
            }
            else
            {
                DSPadint padint = null;
                lock (primaryPadints[uid])
                {
                    padint = new DSPadint(primaryPadints[uid].Value, primaryPadints[uid].Timestamp);
                }


                Dictionary<int, DSPadint> changedPadInts = new Dictionary<int, DSPadint>();
                changedPadInts.Add(uid, padint);

                this.uncommitedChanges.Add(tid, changedPadInts);

                return padint.Value;
            }        
        }

        public void Write(int tid, int uid, int value)
        {
            checkFailOrFreeze();

            if (!primaryPadints.ContainsKey(uid))
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
                    int timestamp = primaryPadints[uid].Timestamp;
                    this.uncommitedChanges[tid].Add(uid, new DSPadint(value, timestamp));
                }
            }
            else
            {
                int timestamp = primaryPadints[uid].Timestamp;
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

            Console.WriteLine("Number of PadInts: " + primaryPadints.Count());

            foreach (KeyValuePair<int, DSPadint> entry in primaryPadints)
            {
                Console.WriteLine("PadInt<id, value, timestamp> = " + "<" + entry.Key + ", " + entry.Value.Value + ", " + entry.Value.Timestamp + ">");
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
                        Console.Write("TxId: " + entry.Key + "\t --> PadInt<id, value, timestamp> = " + "<" + entry2.Key + "," + entry2.Value.Value + entry2.Value.Timestamp + ">\n");
                    }
                    else
                    {
                        Console.Write("      \t --> PadInt<id, value, timestamp> = " + "<" + entry2.Key + "," + entry2.Value.Value + entry2.Value.Timestamp + ">\n");
                    }

                }


            }



            return true;
        }

        public bool Abort(int tid)
        {
            if(!this.uncommitedChanges.ContainsKey(tid))
            {
                throw new TransactionNotFoundException(tid, this.serverUrl);
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
                    this.padintsBeingCommited.AddLast(uid);
                }
            }
            
            Dictionary<int, DSPadint> transactionValues = this.uncommitedChanges[tid];
            foreach (KeyValuePair<int, DSPadint> padintValue in transactionValues)
            {
                if (primaryPadints[padintValue.Key].Timestamp > padintValue.Value.Timestamp)
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
                throw new TransactionNotFoundException(tid, this.serverUrl);
            }

            // save the values on the padints dictionary
            Dictionary<int, DSPadint> transactionValues = this.uncommitedChanges[tid];
            foreach (KeyValuePair<int, DSPadint> padintValue in transactionValues)
            {
                int uid = padintValue.Key;
                int value = padintValue.Value.Value;
                lock (this.primaryPadints[uid])
                {
                    this.primaryPadints[uid].Value = value;
                    this.primaryPadints[uid].Timestamp = masterServer.generateTimestamp();
                }

                this.padintsBeingCommited.Remove(uid);
            }

            this.uncommitedChanges.Remove(tid);

            return true;
        }

        public void transferBackupTo(IDataServer server)
        {
            foreach (KeyValuePair<int, DSPadint> padint in backupPadints)
            {
                server.addBackupPadInt(padint.Key, padint.Value);
            }
        }

        public void addBackupPadInt(int uid, DSPadint padint)
        {
            this.backupPadints.Add(uid, padint);
        }

        public void transferPrimarysTo(String serverUrl)
        {
            
            IDataServer server =  getDataServerFromUrl(serverUrl);
            foreach (KeyValuePair<int, DSPadint> padint in primaryPadints)
            {
                server.addBackupPadInt(padint.Key, padint.Value);
            }
            this.backupServer = server;
             
        }

        public void setBackupAsPrimary()
        {
            foreach (KeyValuePair<int, DSPadint> padint in this.backupPadints)
            {
                this.primaryPadints.Add(padint.Key, padint.Value);
            }

            this.backupPadints.Clear();
        }

        private IDataServer getDataServerFromUrl(String url)
        {
            Console.WriteLine(url);
            return (IDataServer)Activator.GetObject(typeof(IDataServer), url);
        }

    }
}
