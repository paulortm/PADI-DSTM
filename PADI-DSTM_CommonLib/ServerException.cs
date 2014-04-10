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
    public class ServerException : ApplicationException
    {
        private String message;

        public ServerException()
        {
            this.message = String.Empty;
        }

        public ServerException(String message)
        {
            this.message = message;
        }

        public ServerException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
            : base(info, context)
        {
            message = (String)info.GetValue("message", typeof(String));
	    }

        public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue("message", message);
        }

        // Returns the exception information. 
        public override string Message
        {
            get
            {
                return message;
            }
        }
    }

    [Serializable]
    public class InvalidPadIntIdException : ServerException
    {
        public InvalidPadIntIdException(int uid)
            : base("There is already a PadInt with the uid " + uid)
        {
            //empty
        }

        public InvalidPadIntIdException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
            : base(info, context)
        {
            //empty
	    }

        public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
        {
            base.GetObjectData(info, context);
        }
    }

    [Serializable]
    public class NullPadIntException : ServerException
    {
        public NullPadIntException(int uid)
            : base("The PadInt with uid " + uid + "is null")
        {
            //empty
        }

        public NullPadIntException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
            : base(info, context)
        {
            //empty
	    }

        public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
        {
            base.GetObjectData(info, context);
        }
    }

    [Serializable]
    public class InexistentPadIntException : ServerException
    {
        public InexistentPadIntException(int uid)
            : base("The PadInt with uid " + uid + "does not exist")
        {
            //empty
        }

        public InexistentPadIntException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
            : base(info, context)
        {
            //empty
	    }

        public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
        {
            base.GetObjectData(info, context);
        }
    }

    [Serializable]
    public class NoDataServerException : ServerException
    {
        public NoDataServerException(String methodName)
            : base("The operation \'" + methodName + "\' is invalid when the master doesn't have data servers.")
        {
            //empty
        }

        public NoDataServerException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
            : base(info, context)
        {
            //empty
	    }

        public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
        {
            base.GetObjectData(info, context);
        }

    }

    [Serializable]
    public class ServerFailedException : ServerException
    {
        public ServerFailedException(String serverUrl)
            : base("Server on: " + serverUrl + "\' is in fail mode.")
        {
            //empty
        }

        public ServerFailedException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
            : base(info, context)
        {
            //empty
	    }

        public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
        {
            base.GetObjectData(info, context);
        }
    }

    [Serializable]
    public class TransactionNotFoundException : ServerException
    {
        public TransactionNotFoundException(int transactionId, String serverUrl)
            : base("The transaction " + transactionId + " was not found at " + serverUrl);
        {
            //empty
        }

        public TransactionNotFoundException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
            : base(info, context)
        {
            //empty
        }

        public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
        {
            base.GetObjectData(info, context);
        }
    }
}
