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
            if(doFail)
            {
                Console.WriteLine("Fail mode activated, PadInt could not be created");

                throw new ServerFailedException(Util.getLocalIP());
            }

            try {
                padints.Add(uid, null);
                Console.WriteLine("PadInt created: " + uid);
            } catch (ArgumentException e) {
                throw new InvalidPadIntIdException(uid);
            }
            
            return new PadInt(uid, DataServerApp.dataServerUrl);
        }

        public int Read(int tid, int uid)
        {
            if (padints.ContainsKey(uid))
            {
                int? value = padints[uid];
                if (value == null)
                    throw new NullPadIntException(uid);
                return (int)value;
            }
            else
            {
                throw new InexistentPadIntException(uid);
            }
            
        }

        public void Write(int tid, int uid, int value)
        {
            if (padints.ContainsKey(uid))
            {
                padints[uid] = value;
            }
            else
            {
                throw new InexistentPadIntException(uid);
            }
        }

        public bool Fail()
        {
            doFail = true;
            Console.WriteLine("Fail mode activated");

            return true;
        }

        public bool Recover()
        {
            if(doFreeze)
            {

            }

            doFail = false;
            doFreeze = false;

            return true;
        }

        public bool Abort(int tid)
        {
            // implement
            return true;
        }

        public bool canCommit(int tid)
        {
            // implement
            return true;
        }

        public bool Commit(int tid)
        {
            // implement
            return true;
        }
    }
}
