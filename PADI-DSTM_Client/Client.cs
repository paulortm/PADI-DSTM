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
            PADI_DSTM.Fail("tcp://localhost:8888/DataServer");
            try
            {
                PadInt p2 = PADI_DSTM.CreatePadInt(1);
            }
            catch (ServerException ex)
            {
                Console.WriteLine(ex.getMessage());
            }

            Console.ReadLine();
        }
    }
}
