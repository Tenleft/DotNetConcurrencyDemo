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
        /// <summary>
        /// 在Interlocked中：
        ///     1、所有方法都是原子性操作
        ///     2、所有方法都建立了完整的内存栅栏（memory fence），即调用Interlocked方法不会出现代码重新排序
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {

            //UnsafeRead();
            //Console.WriteLine("-".PadLeft(20, '-'));
            //SafeRead();
            //--------------------------

            //Increment(); 
            //--------------------------

            //Example();
            Example(9);
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

        static Random r = new Random();

        #region Increment & CompareExchange demo

        static int total = 0;
        static void Increment()
        {
            int criticalNum = 200;
            int compeleteNum = 0;
            Task.Run(() =>
            {
                while (true)
                {
                    //reset total
                    if (Interlocked.CompareExchange(ref total, 0, criticalNum) == criticalNum)
                    {
                        compeleteNum++;
                        Console.WriteLine($"已完成 {compeleteNum} 任务");
                    }
                }
            });

            for (int i = 0; i < 10; i++)
            {
                Task.Run(() =>
                {
                    for (int j = 0; j < 100; j++)
                    {
                        Thread.Sleep(r.Next(10, 51));
                        if (Interlocked.Increment(ref total) == criticalNum)
                        {
                            Console.WriteLine($"TaskId = {Task.CurrentId},ThreadId = {Thread.CurrentThread.ManagedThreadId} - {criticalNum}");
                        }
                    }
                });
            }
        }

        #endregion

        #region 线程一只能输出1，线程二只能输出2，线程三只能输出3，在控制台中输出顺序为 1 2 3 1 2 3

        static void Example()
        {
            int currentValue = 1;
            Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    Thread.Sleep(r.Next(500, 1001));
                    if (Interlocked.CompareExchange(ref currentValue, 2, 1) == 1)
                    {
                        Console.WriteLine($"线程 {Thread.CurrentThread.ManagedThreadId} : " +1);
                    }
                }
            }, TaskCreationOptions.LongRunning);
            Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    Thread.Sleep(r.Next(500, 1001));
                    if (Interlocked.CompareExchange(ref currentValue, 3, 2) == 2)
                    {
                        Console.WriteLine($"线程 {Thread.CurrentThread.ManagedThreadId} : " + 2);
                    }
                }
            }, TaskCreationOptions.LongRunning);
            Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    Thread.Sleep(r.Next(500, 1001));
                    if (Interlocked.CompareExchange(ref currentValue, 1, 3) == 3)
                    {
                        Console.WriteLine($"线程 {Thread.CurrentThread.ManagedThreadId} : " + 3);
                    }
                }
            }, TaskCreationOptions.LongRunning);
        }
        static void Example(int num)
        {
            if (num < 1)
            {
                return;
            }
            int currentValue = 1;

            Enumerable.Range(1, num).AsParallel().ForAll(i =>
            {
                Thread thread = new Thread(() =>
                                  {
                                      while (true)
                                      {
                                          Thread.Sleep(r.Next(100, 501));
                                          int tempNum = i + 1 > num ? 1 : i + 1;
                                          if (Interlocked.CompareExchange(ref currentValue, tempNum, i) == i)
                                          {
                                              Console.WriteLine($"{Thread.CurrentThread.Name} : {i}");
                                          }
                                      }
                                  });
                thread.IsBackground = true;
                thread.Name = "线程 " + i;
                int sleep = 100 * (num - i + 1);
                Thread.Sleep(r.Next(sleep, sleep));
                thread.Start();
                Console.WriteLine(thread.Name + " 已启动！");
            });
        }
        #endregion

    }
}
