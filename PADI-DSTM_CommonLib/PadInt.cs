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

        public int Read()
        {
            return 0;
        }

        public void Write(int value)
        {
            // incomplete
        }
    }
}
