using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace PADI_DSTM_CommonLib
{
    [Serializable]
    public abstract class ServerException : RemotingException, ISerializable
    {
        private String message;

        public ServerException(String message)
        {
            this.message = message;
        }

        public ServerException(SerializationInfo info, StreamingContext context)
        {
            this.message = (string)info.GetValue("message", typeof(string));
        }

        public string getMessage()
        {
            return this.message;
        }
    }

    public class NoDataServerException : ServerException
    {
        public NoDataServerException(String method)
            : base("The operation \'" + method + "\' is invalid when the master doesn't have data servers.")
        {
            // empty
        }
    }
}
