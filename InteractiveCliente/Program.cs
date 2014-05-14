﻿using PADI_DSTM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InteractiveCliente
{
    class Program
    {
        static void Main(string[] args)
        {
            Dictionary<int,PadInt> accessiblePadInts = new Dictionary<int,PadInt>();
            String input;
            char[] separators = { ' ' };
            PadiDstm.Init();
            while(true) {
                Console.Write(">: ");
                input = Console.ReadLine();
                String[] splitedInput = input.Split(separators);
                String command = splitedInput[0];
                try
                {
                    if (command.Equals("begin"))
                    {
                        Console.WriteLine(PadiDstm.TxBegin());
                    }
                    else if (command.Equals("commit"))
                    {
                        Console.WriteLine(PadiDstm.TxCommit());
                    }
                    else if (command.Equals("abort"))
                    {
                        Console.WriteLine(PadiDstm.TxAbort());
                    }
                    else if (command.Equals("status"))
                    {
                        PadiDstm.Status();
                    }
                    else if (command.Equals("freeze"))
                    {
                        PadiDstm.Freeze(splitedInput[1]);
                    }
                    else if (command.Equals("recover"))
                    {
                        PadiDstm.Recover(splitedInput[1]);
                    }
                    else if (command.Equals("access"))
                    {
                        int uid = Int32.Parse(splitedInput[1]);
                        PadInt p = PadiDstm.AccessPadInt(uid);
                        accessiblePadInts[uid] = p;
                        printAccessiblePadInts(accessiblePadInts);
                    }
                    else if (command.Equals("write"))
                    {
                        int uid = Int32.Parse(splitedInput[1]);
                        int value = Int32.Parse(splitedInput[2]);
                        if (!accessiblePadInts.ContainsKey(uid))
                        {
                            Console.WriteLine("That padint is not accessible");
                            printAccessiblePadInts(accessiblePadInts);
                        }
                        else
                        {
                            accessiblePadInts[uid].Write(value);
                        }
                    }
                    else if (command.Equals("read"))
                    {
                        int uid = Int32.Parse(splitedInput[1]);
                        if (!accessiblePadInts.ContainsKey(uid))
                        {
                            Console.WriteLine("That padint is not accessible");
                            printAccessiblePadInts(accessiblePadInts);
                        }
                        else
                        {
                            Console.WriteLine(accessiblePadInts[uid].Read());
                        }
                    }
                    else if (command.Equals("create"))
                    {
                        int uid = Int32.Parse(splitedInput[1]);
                        PadInt p = PadiDstm.CreatePadInt(uid);
                        accessiblePadInts[uid] = p;
                    }
                }
                catch (TxException e)
                {
                    Console.WriteLine(e);
                }
            }
        }

        private static void printAccessiblePadInts(Dictionary<int, PadInt> padints)
        {
            Console.WriteLine("Accessible padints:");
            foreach (KeyValuePair<int, PadInt> padint in padints)
            {
                Console.WriteLine("\t"+padint.Key);
            }
        }


    }
}
