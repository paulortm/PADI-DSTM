using PADI_DSTM_CommonLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.IO;

namespace PADI_DSTM
{
    [Serializable]
    public class PadInt
    {
        private int MAX_RETRIES = 3;

        private int uid;
        private string dataServerUrl;
        private IMaster master;
        private CurrentTransactionHolder currentTransactionHolder;

        public PadInt(int uid, string dataServerUrl)
        {
            this.uid = uid;
            this.dataServerUrl = dataServerUrl;
        }

        public void setMaster(IMaster m) { this.master = m; }

        public void setTransactionHolder(CurrentTransactionHolder currentTransactionHolder)
        {
            this.currentTransactionHolder = currentTransactionHolder;
        }

        public int Read()
        {
            if (this.currentTransactionHolder.get() == null)
            {
                throw new OutOfTransactionException();
            }

            IDataServer server = null;
            this.currentTransactionHolder.get().addServer(this.dataServerUrl);
            int result = -1;
            int i; 
            for (i = 0; i < MAX_RETRIES; i++)
            {
                try
                {
                    server = (IDataServer)Activator.GetObject(typeof(IDataServer), dataServerUrl);
                    result = server.Read(this.currentTransactionHolder.get().getId(), this.uid);
                    break;
                }
                catch (Exception ex)
                {
                    if (ex is SocketException || ex is IOException)
                    {
                        String oldDataServerUrl = this.dataServerUrl;
                        this.dataServerUrl   = this.master.getLocationOfPadInt(this.uid);
                        this.currentTransactionHolder.get().changeServer(oldDataServerUrl, this.dataServerUrl);
                    }
                    else
                    {
                        throw;
                    }
                }
            }
            if (i >= MAX_RETRIES)
            {
                // the maximum number of tries was reached
                throw new ServerNotFoundException(this.uid);
            }
            return result;
        }

        public void Write(int value)
        {
            if (this.currentTransactionHolder.get() == null)
            {
                throw new OutOfTransactionException();
            }

            IDataServer server = (IDataServer)Activator.GetObject(typeof(IDataServer), dataServerUrl);
            this.currentTransactionHolder.get().addServer(this.dataServerUrl);
            int i;
            for (i = 0; i < MAX_RETRIES; i++)
            {
                try
                {
                    server = (IDataServer)Activator.GetObject(typeof(IDataServer), dataServerUrl);
                    server.Write(this.currentTransactionHolder.get().getId(), this.uid, value);
                    break;
                }
                catch (Exception ex)
                {
                    if (ex is SocketException || ex is IOException)
                    {
                        String oldDataServerUrl = this.dataServerUrl;
                        this.dataServerUrl = this.master.getLocationOfPadInt(this.uid);
                        this.currentTransactionHolder.get().changeServer(oldDataServerUrl, this.dataServerUrl);
                    }
                    else
                    {
                        throw;
                    }
                }
            }
            if (i >= MAX_RETRIES)
            {
                // the maximum number of tries was reached
                throw new ServerNotFoundException(this.uid);
            }
        }
    }
}
