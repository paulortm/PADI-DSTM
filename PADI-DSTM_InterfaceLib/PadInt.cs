using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PADI_DSTM_CommonLib
{
    [Serializable]
    class PadInt
    {
        private int uid;
        private IDataServer dataServer;
        private CurrentTransactionHolder currentTransactionHolder;

        public PadInt(int uid, IDataServer dataServer)
        {
            this.uid = uid;
            this.dataServer = dataServer;
        }

        public int Read()
        {
            // incomplete
            return getValue();
        }

        public void Write(int value)
        {
            // incomplete
        }
    }
}
