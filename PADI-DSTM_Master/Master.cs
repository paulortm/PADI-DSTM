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
            RemotingConfiguration.RegisterWellKnownServiceType(typeof(MasterServer), Constants.REMOTE_MASTER_OBJ_NAME, WellKnownObjectMode.Singleton);

            Console.WriteLine("Press <enter> to exit");
            Console.ReadLine();
        }
    }

    class MasterServer : MarshalByRefObject, IMaster
    {
        int timestampCounter = 0;
        // <data server id, data server url>
        private Dictionary<int, String> dataServers = new Dictionary<int, String>();

        // <data server id, number of padints>
        private Dictionary<int, int> numberOfPadInts = new Dictionary<int, int>();

        // <PadInt uid, servers>
        private Dictionary<int, List<int>> locationOfPadInts = new Dictionary<int, List<int>>();

        // <server id, received?>: true if the i am alive was received from server id
        private Dictionary<int,bool> alives = new Dictionary<int,bool>();

        private int dataServerId = 0; // Data Server ids goes from 0 to n servers - 1
        private int transactionId = 0;

        private static System.Timers.Timer checkServersTimer;

        public MasterServer() {
            Console.WriteLine("asdfadfa");
            checkServersTimer = new System.Timers.Timer(0);

            // Hook up the Elapsed event for the timer.
            checkServersTimer.Elapsed += new ElapsedEventHandler(checkDeadServers);

            // Set the Interval to 2 seconds (2000 milliseconds).
            checkServersTimer.Interval = 2000;
            checkServersTimer.Enabled = true;
        }


        public void imAlive(int serverId)
        {
            Console.WriteLine("server {0} is alive", serverId);
            alives[serverId] = true;
        }

        private void checkDeadServers(object source, ElapsedEventArgs e)
        {
            foreach(KeyValuePair<int,bool> server in alives) {
                if(!server.Value) {
                    recoverFromDeadServer(server.Key);
                }
                alives[server.Key] = false;
            }
        }

        private void recoverFromDeadServer(int id) {
            Console.WriteLine("Server with id = {0} is dead.", id);
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
                Console.WriteLine("DataServer " + i + " url:           " + dataServers[i]);
                Console.WriteLine("DataServer " + i + " total PadInts: " + numberOfPadInts[i]);
            }

            foreach (KeyValuePair<int, String> entry in dataServers)
            {
                IDataServer server = getDataServerFromUrl(entry.Value);
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

        private void addDataServer(int id, String url)
        {
            dataServers.Add(id, url);
            numberOfPadInts.Add(id, 0);
            alives[id] = true;
        }

        private IDataServer getDataServer(int id)
        {
            String url = dataServers[id];
            IDataServer server = getDataServerFromUrl(url);
            return server;
        }

        private IDataServer getDataServerFromUrl(String url)
        {
            return (IDataServer)Activator.GetObject(typeof(IDataServer), url);
        }


        private String getDataServerUrl(int id)
        {
            String url = dataServers[id];
            return url;
        }

        // uid The uid of the PadInt
        // Returns the url of the dataserver where the PadInd is present
        private String getPrimaryDataServerUrl(int uid)
        {
            return this.getDataServerUrl(locationOfPadInts[uid][0]);
        }

        // Registers the 'server' on the system.
        // returns the server id.
        public int registerDataServer(String url)
        {
            int id = generateId();
            addDataServer(id, url);
            Console.WriteLine("DataServer " + id + " registered at " + url);
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
            List<int> servers = new List<int>();
            servers.Add(serverId);
            this.locationOfPadInts.Add(uid, servers);

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
    }
}
