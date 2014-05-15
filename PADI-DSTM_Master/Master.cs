using PADI_DSTM;
using PADI_DSTM_CommonLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace PADI_DSTM_Master
{
    class App
    {
        static void Main(string[] args)
        {
            TcpChannel channelServ = new TcpChannel(Constants.MASTER_SERV_PORT);
            ChannelServices.RegisterChannel(channelServ, true);
            MarshalByRefObject master = new MasterServer();
            RemotingServices.Marshal(
                master, 
                Constants.REMOTE_MASTER_OBJ_NAME, 
                typeof(IMaster)
            );

            Console.WriteLine("Press <enter> to exit");
            Console.ReadLine();
        }
    }

    internal class DataServerInfo
    {
        private int id;
        private String url;
        private int? backupServerId;
        private int? backedupServerId;

        public DataServerInfo(int id, String url, int? backupServerId, int? backedupServerId)
        {
            this.id = id;
            this.url = url;
            this.backupServerId = backupServerId;
            this.backedupServerId = backedupServerId;
        }

        public int Id
        {
            set { this.id = value; }
            get { return this.id; }
        }

        public String Url
        {
            set { this.url = value; }
            get { return this.url; }
        }

        public int? BackupServerId
        {
            set { this.backupServerId = value; }
            get { return this.backupServerId; }
        }

        public int? BackedupServerId
        {
            set { this.backedupServerId = value; }
            get { return this.backedupServerId; }
        }
    }

    class MasterServer : MarshalByRefObject, IMaster
    {
        int timestampCounter = 0;
        // <data server id, data server url>
        private Dictionary<int, DataServerInfo> dataServers = new Dictionary<int, DataServerInfo>();

        // <data server id, number of padints>
        private Dictionary<int, int> numberOfPadInts = new Dictionary<int, int>();

        // <PadInt uid, servers>
        private Dictionary<int, DataServerInfo> locationOfPadInts = new Dictionary<int, DataServerInfo>();

        // <server id, received?>: true if the i am alive was received from server id
        private Dictionary<int,bool> alives = new Dictionary<int,bool>();

        private int dataServerId = 0;
        private int transactionId = 0;

        private static System.Timers.Timer checkServersTimer;

        public MasterServer() {
            checkServersTimer = new System.Timers.Timer(1);

            // Hook up the Elapsed event for the timer.
            checkServersTimer.Elapsed += new ElapsedEventHandler(checkDeadServers);

            // Set the Interval to 2 seconds (2000 milliseconds).
            checkServersTimer.Interval = Constants.HEARTBEAT_CHECK_INTERVAL;
            checkServersTimer.Enabled = true;
        }


        public void imAlive(int serverId)
        {
            Console.WriteLine("server {0} is alive", serverId);
            alives[serverId] = true;
        }

        private void checkDeadServers(object source, ElapsedEventArgs e)
        {

            var keys = new List<int>(alives.Keys);
            foreach (int serverId in keys)
            {
                if (!alives[serverId])
                {

                    // remove dead server
                    alives.Remove(serverId);
                    recoverFromDeadServer(serverId);
                }
                else
                {
                    alives[serverId] = false;
                }
            }
            // remove dead servers
            /*foreach (int serverId in deadServers)
            {
                alives.Remove(serverId);
            }*/
        }

        private void recoverFromDeadServer(int id) {
            Console.WriteLine("Server with id = {0} is dead. Performing recover.. Wait!", id);

            // change pointers to DataServersInfo
            int? backupServerId   = dataServers[id].BackupServerId;
            int? backedupServerId = dataServers[id].BackedupServerId;

            dataServers.Remove(id);

            if(dataServers.Count > 1)
            {
                dataServers[(int)backupServerId].BackedupServerId = backedupServerId;
                dataServers[(int)backedupServerId].BackupServerId = backupServerId;
            }
            else
            {
                dataServers[(int)backupServerId].BackedupServerId = null;
                dataServers[(int)backedupServerId].BackupServerId = null;
            }

            // change number of primary padints on backupServerId
            int numberOfPadints = numberOfPadInts[id];

            numberOfPadInts[(int)backupServerId] += numberOfPadints;            

            // update new location of padints
            LinkedList<int> keys =  new LinkedList<int>(locationOfPadInts.Keys);
            foreach (int uid in keys)
            {
                if(locationOfPadInts[uid].Id == id)
                {
                    DataServerInfo newDataServerInfo = dataServers[(int)backupServerId];

                    locationOfPadInts[uid] = newDataServerInfo;
                }
            }

            // make server update data
            IDataServer backupServer = getDataServerFromUrl(dataServers[(int)backupServerId].Url);

            if (dataServers.Count > 1)
            {
                IDataServer backedupServer = getDataServerFromUrl(dataServers[(int)backedupServerId].Url);
                backupServer.transferBackupTo(backedupServer);
                backupServer.setBackupAsPrimary();
                backedupServer.transferPrimarysTo(dataServers[(int)backupServerId].Url);
            }
            else
            {
                backupServer.setBackupAsPrimary();
                backupServer.setAsAlone();
            }

            Console.WriteLine("Recover Done!");
        }

        public int generateTimestamp()
        {
            return timestampCounter++;
        }

        public bool Status()
        {
            Console.WriteLine("");
            Console.WriteLine("-------- DUMPING MASTER SERVER STATUS --------");
            Console.WriteLine("MasterServer url:      " + Constants.MASTER_SERVER_URL);
            Console.WriteLine("Number of DataServers: " + dataServerId);

            Console.WriteLine("");

            for(int i = 0; i < dataServerId; i++)
            {
                if (dataServers.ContainsKey(i))
                {
                    Console.WriteLine("DataServer " + i + " url:           " + dataServers[i]);
                    Console.WriteLine("DataServer " + i + " total PadInts: " + numberOfPadInts[i]);
                }
            }

            foreach (KeyValuePair<int, DataServerInfo> entry in dataServers)
            {
                IDataServer server = getDataServerFromUrl(entry.Value.Url);
                server.Status();
            }

            return true;
        }

        // maybe needs to be synched
        private int generateId()
        {
            return dataServerId++;
        }

        private int generateTransactionId()
        {
            return transactionId++;
        }

        private DataServerInfo getFirstDataServerInfo()
        {
            for (int i = 0; i < dataServerId; i++)
            {
                if (dataServers.ContainsKey(i))
                    return dataServers[i];
            }

            return null;
        }

        private DataServerInfo getLastDataServerInfo()
        {
            for (int i = dataServerId - 1; i >= 0; i++)
            {
                if (dataServers.ContainsKey(i))
                    return dataServers[i];
            }

            return null;
        }

        private void addDataServer(int id, String url)
        {
            if (dataServers.Count == 0)
            {
                dataServers.Add(id, new DataServerInfo(id, url, null, null));
            }
            else if (dataServers.Count == 1)
            {
                DataServerInfo firstServerInfo = getFirstDataServerInfo();
                DataServerInfo newServerInfo = new DataServerInfo(id, url, firstServerInfo.Id, firstServerInfo.Id);
                IDataServer firstServer = getDataServerFromUrl(firstServerInfo.Url);
                IDataServer newServer = getDataServerFromUrl(newServerInfo.Url);

                firstServerInfo.BackupServerId = id;
                firstServerInfo.BackedupServerId = id;

                firstServer.transferPrimarysTo(url);
                newServer.setBackupServer(firstServerInfo.Url);

                dataServers.Add(id, newServerInfo);
            }
            else
            {
                DataServerInfo firstServerInfo = getFirstDataServerInfo();
                DataServerInfo lastServerInfo = getLastDataServerInfo();
                DataServerInfo newServerInfo = new DataServerInfo(id, url, lastServerInfo.Id, firstServerInfo.Id);
                IDataServer firstServer = getDataServerFromUrl(firstServerInfo.Url);
                IDataServer newServer = getDataServerFromUrl(url);

                firstServerInfo.BackupServerId = id;
                lastServerInfo.BackedupServerId = id;

                firstServer.transferPrimarysTo(url);
                newServer.transferPrimarysTo(lastServerInfo.Url);

                dataServers.Add(id, newServerInfo);
            }
            numberOfPadInts.Add(id, 0);
            alives[id] = true;
        }

        private IDataServer getDataServer(int id)
        {
            String url = dataServers[id].Url;
            IDataServer server = getDataServerFromUrl(url);
            return server;
        }

        private IDataServer getDataServerFromUrl(String url)
        {
            return (IDataServer)Activator.GetObject(typeof(IDataServer), url);
        }

        // uid The uid of the PadInt
        // Returns the url of the dataserver where the PadInd is present
        private String getPrimaryDataServerUrl(int uid)
        {
            return locationOfPadInts[uid].Url;
        }

        // Registers the 'server' on the system.
        // returns the server id.
        public int registerDataServer(String url)
        {
            int id = generateId();
            addDataServer(id, url);
            Console.WriteLine("DataServer " + id + " registered at " + url);
            alives[id] = true;
            return id;
        }

        // return - PadInt on the server with the given uid
        public PadInt  createPadIntOnDataServer(int uid)
        {
            if (dataServers.Count == 0)
            {
                throw new NoDataServerException("createPadIntOnDataServer");
            }

            // find which data server is storing less PadInts
            KeyValuePair<int, int> first = numberOfPadInts.First();
            int serverId = first.Key;
            int minimumLoad = first.Value;
            foreach (KeyValuePair<int, int> entry in numberOfPadInts)
            {
                if (entry.Value < minimumLoad)
                {
                    serverId = entry.Key;
                    minimumLoad = entry.Value;
                }
            }

            // get server object and create the padint
            IDataServer server = getDataServer(serverId);
            PadInt padInt = server.createPadInt(uid);
            // update info on master
            numberOfPadInts[serverId]++;
            this.locationOfPadInts.Add(uid, dataServers[serverId]);

            Console.WriteLine("Delegated PadInt to " + dataServers[serverId]);
            return padInt;
        }

        // return - PadInt with the given uid
        public PadInt accessPadIntOnDataServer(int uid)
        {
            if (dataServers.Count == 0)
            {
                throw new NoDataServerException("accessPadIntOnDataServer");
            }

            if (!this.locationOfPadInts.ContainsKey(uid))
            {
                throw new InexistentPadIntException(uid);
            }
            PadInt padInt = new PadInt(uid, this.getPrimaryDataServerUrl(uid));

            Console.WriteLine("Returned PadInt presented into " + this.getPrimaryDataServerUrl(uid));
            return padInt;
        }

        public Transaction createTransaction() {
            Transaction t = new Transaction(generateTransactionId());
            return t;
        }

        public String getLocationOfPadInt(int uid)
        {
            return getPrimaryDataServerUrl(uid);
        }
    }
}
