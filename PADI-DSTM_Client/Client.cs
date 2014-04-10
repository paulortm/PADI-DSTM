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
            PADI_DSTM.Init();

            try
            {

                PadInt p = PADI_DSTM.CreatePadInt(0);
                PADI_DSTM.TxBegin();
                p.Write(5);
                PADI_DSTM.TxCommit();

                PADI_DSTM.TxBegin();
                p.Write(9);
                PADI_DSTM.TxAbort();
                Console.ReadLine();

            }
            catch (ServerException e)
            {
                Console.WriteLine(e.Message);
                PadInt p = PADI_DSTM.AccessPadInt(0);
                PADI_DSTM.TxBegin();
                Console.WriteLine(p.Read());
                PADI_DSTM.TxCommit();
                Console.ReadLine();
            }


            Console.ReadLine();
        }
    }
}
