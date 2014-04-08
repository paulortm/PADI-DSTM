using PADI_DSTM_CommonLib;
using PADI_DSTM_Library;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PADI_DSTM_Client
{
    class Client
    {
        static void Main(string[] args)
        {
            Console.ReadLine();
            PADI_DSTM.Init();
            PadInt p = PADI_DSTM.CreatePadInt(0);
            Console.ReadLine();
        }
    }
}
