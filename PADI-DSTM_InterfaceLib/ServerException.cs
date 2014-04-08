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

    public class InvalidPadIntIdException : ServerException
    {
        public InvalidPadIntIdException(int uid)
            : base("There is already a PadInt with the uid " + uid)
        {
            //empty
        }
    }

    public class NullPadIntException : ServerException
    {
        public NullPadIntException(int uid)
            : base("The PadInt with uid " + uid + "is null")
        {
            //empty
        }
    }

    public class InexistentPadIntException : ServerException
    {
        public InexistentPadIntException(int uid)
            : base("The PadInt with uid " + uid + "does not exist")
        {
            //empty
        }
    }
}
