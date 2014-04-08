using PADI_DSTM_CommonLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Text;
using System.Threading.Tasks;

namespace PADI_DSTM_Master
{
    class App
    {
        static void Main(string[] args)
        {
            TcpChannel channelServ = new TcpChannel(Constants.MASTER_SERV_PORT);
            ChannelServices.RegisterChannel(channelServ, true);
            RemotingConfiguration.RegisterWellKnownServiceType(typeof(MasterServer), Constants.REMOTE_DATASERV_OBJ_NAME, WellKnownObjectMode.Singleton);

            Console.WriteLine("Press <enter> to exit");
            Console.ReadLine();
        }
    }

    class MasterServer : MarshalByRefObject, IMaster
    {
        // <data server id, data server object>
        private Dictionary<int, String> dataServers = new Dictionary<int, String>();

        // <data server id, number of padints>
        private Dictionary<int, int> numberOfPadInts = new Dictionary<int, int>();

        private int dataServerId = 0;

        public void check()
        {
            Console.WriteLine("Checked!!!!!!!!!!!!!!!!!!!!!!");
        }

        // maybe needs to be synched
        private int generateId()
        {
            return dataServerId++;
        }

        private void addDataServer(int id, String url)
        {
            dataServers.Add(id, url);
            numberOfPadInts.Add(id, 0);
        }

        private IDataServer getDataServer(int id)
        {
            String url = dataServers[id];
            IDataServer server = (IDataServer)Activator.GetObject(typeof(IDataServer), url);
            return server;
        }

        // Registers the 'server' on the system.
        // returns the server id.
        int registerDataServer(String url)
        {
            int id = generateId();
            addDataServer(id, url);
            Console.WriteLine("DataServer " + id + " registered at " + url);
            return id;
        }

        // return - PadInt on the server with the given uid
        PadInt  createPadIntOnDataServer(int uid)
        {
            if (dataServers.Count == 0)
            {
                throw new NoDataServerException("choseDataServer");
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

            return padInt;
        }



    }
}
