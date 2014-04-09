using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PADI_DSTM_CommonLib
{
    [Serializable]
    public class Transaction
    {
        private int id;

        public Transaction(int id)
        {
            this.id = id;
        }

        public int getId()
        {
            return this.id;
        }
    }

    // Passed to the PadInt by the PADI_DSTM on the AccessPadInt.
    // Used by the PadInt to know to which transaction read and
    // write operations belong.
    [Serializable]
    public class CurrentTransactionHolder
    {
        private Transaction currentTransaction = null;

        public void set(Transaction t)
        {
            this.currentTransaction = t;
        }

        public Transaction get()
        {
            /*
            if(this.currentTransaction == null)
                throw new TxException
            else
                return currentTransaction;
            */
            return this.currentTransaction;
        }
    }

    [Serializable]
    public abstract class TxException : Exception
    {
        public TxException(String message) : base(message)
        {
            // empty
        }
    }
}
