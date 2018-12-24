using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace InterlockedDemo
{
    class Program
    {
        static void Main(string[] args)
        {

            UnsafeRead();
            Console.WriteLine("-".PadLeft(20, '-'));
            //SafeRead();

            Console.ReadKey();
        }

        #region TornRead
        static long a;
        /// <summary>
        /// torn read:一次读取被撕成两半，或者说在机器级别上，要分成两个MOV指令才能读完。
        /// long.MaxValue 转换成16进制为 ‭7FFF_FFFF_FFFF_FFFF‬
        /// torn read 会造成读取的值为 FFFF_FFFF(4294967295)‭ 或者 7FFF_FFFF_0000_0000‬(9223372032559808512) 
        /// </summary>
        private static void TornRead(Func<long> read)
        {
            Task.Run(() =>
            {
                while (true)
                {
                    var num = read();
                    if (num != 0 && num != long.MaxValue)
                    {
                        Console.WriteLine("1-" + num);
                    }
                }
            });
            Task.Run(() =>
            {
                while (true)
                {
                    var num = read();
                    if (num != 0 && num != long.MaxValue)
                    {
                        Console.WriteLine("2-" + num);
                    }
                }
            });
            Console.ReadKey();
            a = long.MaxValue;
            Console.WriteLine("end");
        }

        static void UnsafeRead()
        {
            while (true)
            {
                TornRead(() => a);
                a = 0;
            }
        }
        static void SafeRead()
        {
            while (true)
            {
                TornRead(() => Interlocked.CompareExchange(ref a, 0, 0));
                a = 0;
            }
        }
        #endregion
    }
}
