using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PADI_DSTM_CommonLib
{
    [Serializable]
    public class PadInt
    {
        private int uid;
        private string dataServerUrl;
        private CurrentTransactionHolder currentTransactionHolder;

        public PadInt(int uid, string dataServerUrl)
        {
            this.uid = uid;
            this.dataServerUrl = dataServerUrl;
        }

        public void setTransactionHolder(CurrentTransactionHolder currentTransactionHolder)
        {
            this.currentTransactionHolder = currentTransactionHolder;
        }

        public int Read()
        {
            if (this.currentTransactionHolder == null)
            {

            }

            IDataServer server = (IDataServer)Activator.GetObject(typeof(IDataServer), dataServerUrl);
            return server.Read(this.currentTransactionHolder.get().getId(), this.uid);
        }

        public void Write(int value)
        {
            if (this.currentTransactionHolder == null)
            {

            }

            IDataServer server = (IDataServer)Activator.GetObject(typeof(IDataServer), dataServerUrl);
            server.Write(this.currentTransactionHolder.get().getId(), this.uid, value);
        }
    }
}
