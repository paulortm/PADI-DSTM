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
                for (int i = 0; i < 20; i++)
                {
                    PADI_DSTM.CreatePadInt(i);
                }

                PadInt p1 = PADI_DSTM.AccessPadInt(0);
                PadInt p2 = PADI_DSTM.AccessPadInt(3);
                PadInt p3 = PADI_DSTM.AccessPadInt(8);
                PadInt p4 = PADI_DSTM.AccessPadInt(15);

                PADI_DSTM.TxBegin();
                p1.Write(5);
                p2.Write(6);
                p3.Write(7);
                p4.Write(8);
                PADI_DSTM.TxCommit();

                PADI_DSTM.TxBegin();
                p1.Write(1);
                p2.Write(2);
                p3.Write(3);
                p4.Write(4);
                PADI_DSTM.TxAbort();
                Console.ReadLine();

            }
            catch (ServerException e)
            {
                Console.WriteLine(e.Message);

                PadInt p1 = PADI_DSTM.AccessPadInt(0);
                PadInt p2 = PADI_DSTM.AccessPadInt(3);
                PadInt p3 = PADI_DSTM.AccessPadInt(8);
                PadInt p4 = PADI_DSTM.AccessPadInt(15);
                PADI_DSTM.TxBegin();
                Console.WriteLine(p1.Read());
                Console.WriteLine(p2.Read());
                Console.WriteLine(p3.Read());
                Console.WriteLine(p4.Read());
                PADI_DSTM.TxCommit();
                Console.ReadLine();
            }


            Console.ReadLine();
        }
    }
}
