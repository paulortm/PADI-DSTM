using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PADI_DSTM_CommonLib
{

    public interface IMaster
    {
        void check();

        IDataServer chooseDataServer(int uid);

    }

    public interface IDataServer
    {
        PadInt createPadInt(int uid);
    }
}
