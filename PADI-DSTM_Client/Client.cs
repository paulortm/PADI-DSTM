using PADI_DSTM_CommonLib;
using PADI_DSTM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PADI_DSTM_Client
{
    class Client
    {


        static void test1()
        {
            PadiDstm.Init();

            try
            {
                for (int i = 0; i < 20; i++)
                {
                    PadiDstm.CreatePadInt(i);
                }

                PadInt p1 = PadiDstm.AccessPadInt(0);
                PadInt p2 = PadiDstm.AccessPadInt(3);
                PadInt p3 = PadiDstm.AccessPadInt(8);
                PadInt p4 = PadiDstm.AccessPadInt(15);

                PadiDstm.TxBegin();
                p1.Write(5);
                p2.Write(6);
                p3.Write(7);
                p4.Write(8);
                PadiDstm.TxCommit();

                PadiDstm.TxBegin();
                p1.Write(1);
                p2.Write(2);
                p3.Write(3);
                p4.Write(4);
                PadiDstm.TxAbort();
            }
            catch (ServerException e)
            {
                Console.WriteLine(e.Message);

                PadInt p1 = PadiDstm.AccessPadInt(0);
                PadInt p2 = PadiDstm.AccessPadInt(3);
                PadInt p3 = PadiDstm.AccessPadInt(8);
                PadInt p4 = PadiDstm.AccessPadInt(15);
                PadiDstm.TxBegin();
                Console.WriteLine(p1.Read());
                Console.WriteLine(p2.Read());
                Console.WriteLine(p3.Read());
                Console.WriteLine(p4.Read());
                PadiDstm.TxCommit();
            }
        }

        private static void test2()
        {
            try
            {
                PadiDstm.Init();

                for (int i = 0; i < 20; i++)
                {
                    PadiDstm.CreatePadInt(i);
                }

                PadInt p1 = PadiDstm.AccessPadInt(0);
                PadInt p2 = PadiDstm.AccessPadInt(3);
                PadInt p3 = PadiDstm.AccessPadInt(8);
                PadInt p4 = PadiDstm.AccessPadInt(15);

                PadiDstm.TxBegin();
                p1.Write(5);
                p2.Write(6);
                p3.Write(7);
                p4.Write(8);
                PadiDstm.Status();
                PadiDstm.TxCommit();
                
                PadiDstm.Status();
            }
            catch (ServerException e)
            {
                Console.WriteLine(e.Message);
            }

        }

        private static void test3()
        {
            PadiDstm.Init();

            PadInt p1;
            PadInt p2;
            PadInt p3;
            PadInt p4;
            
            try
            {
                for (int i = 0; i < 20; i++)
                {
                    PadiDstm.CreatePadInt(i);
                }


                p1 = PadiDstm.AccessPadInt(0);
                p2 = PadiDstm.AccessPadInt(3);
                p3 = PadiDstm.AccessPadInt(8);
                p4 = PadiDstm.AccessPadInt(15);

                PadiDstm.TxBegin();
                p1.Write(2);
                Console.ReadLine();
                Console.WriteLine(PadiDstm.TxCommit());

            }
            catch (ServerException e)
            {

                p1 = PadiDstm.AccessPadInt(0);
                p2 = PadiDstm.AccessPadInt(3);
                p3 = PadiDstm.AccessPadInt(8);
                p4 = PadiDstm.AccessPadInt(15);

                PadiDstm.TxBegin();
                p1.Write(3);
                Console.ReadLine();
                Console.WriteLine(PadiDstm.TxCommit());
            }

            PadiDstm.TxBegin();
            Console.WriteLine(p1.Read());
            Console.ReadLine();
            PadiDstm.TxCommit();
        }

        private static void testEmptyCommit()
        {
            PadiDstm.Init();
            PadInt p = PadiDstm.CreatePadInt(0);
            PadiDstm.TxBegin();
            Console.WriteLine(p.Read());
            Console.WriteLine(PadiDstm.TxCommit());
        }


            

        public static void Main()
        {
            testEmptyCommit();

            Console.ReadLine();
        }
    }
}
