using PADI_DSTM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PADI_DSTM_CommonLib
{

    public interface IMaster
    {
        int generateTimestamp();

        // Registers the 'server' on the system.
        // returns the server id.
        int registerDataServer(string url);

        bool Status();

        // called by the data server
        void imAlive(int serverid);

        PadInt createPadIntOnDataServer(int uid);

        PadInt accessPadIntOnDataServer(int uid);

        Transaction createTransaction();
    }

    public interface IDataServer
    {
        PadInt createPadInt(int uid);

        // tid - transaction id
        // uid - padint id
        // returns - value of the padint
        int Read(int tid, int uid);

        // tid - transaction id
        // uid - pading id
        // value - the value to be written on the padint
        void Write(int tid, int uid, int value);

        bool Fail();

        bool Freeze();

        bool Recover();

        bool Status();

        // tid - transaction id
        bool Abort(int tid);

        // tid - transaction id
        bool canCommit(int tid);

        // tid - transaction id
        bool Commit(int tid);

        void transferBackupTo(IDataServer server);

        void addBackupPadInt(int uid, DSPadint padint);

        void transferPrimarysTo(String serverUrl);

        void setBackupAsPrimary();

        void setBackupServer(String serverUrl);

        void commitBackedTransaction(int tid, Dictionary<int, DSPadint> updatedValues);

        void backupUncommitedPadint(int tid, int uid, DSPadint padint);

    }
}
