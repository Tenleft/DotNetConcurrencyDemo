using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InterlockedDemo
{
    class Program
    {
        static void Main(string[] args)
        {
        }
        /// <summary>
        /// Release X86 平台下会有这个问题！！！
        /// </summary>
        //private static void TornRead()
        //{
        //    Task.Run(() =>
        //    {
        //        while (true)
        //        {
        //            var num = a;
        //            if (num != 0 && num != long.MaxValue)
        //            {
        //                Console.WriteLine("1-" + num);
        //            }
        //        }
        //    });
        //    Task.Run(() =>
        //    {
        //        while (true)
        //        {
        //            var num = a;
        //            if (num != 0 && num != long.MaxValue)
        //            {
        //                Console.WriteLine("2-" + num);
        //            }
        //        }
        //    });
        //    Console.ReadKey();
        //    a = long.MaxValue;
        //    Console.WriteLine("end");
        //}
    }
}
