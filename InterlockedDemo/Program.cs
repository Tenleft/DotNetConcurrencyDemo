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

            //SpinLockDemo();

            //GetMaximum();
            GetMaximum2();
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

        #region SimpleSpinLock Demo

        /// <summary>
        /// Console输出顺序不一定是执行顺序，注意观察输出时间！
        /// </summary>
        static void SpinLockDemo()
        {
            SimpleSpinLock ssl = new SimpleSpinLock();
            for (int i = 0; i < 5; i++)
            {
                Task.Run(() =>
                {
                    Console.WriteLine($"Task{Task.CurrentId}：try enter lock. {DateTime.Now.ToString("HH:mm:ss.ffffff")}");
                    ssl.Enter();
                    Console.WriteLine($"{"".PadLeft(10, '-')}Task{Task.CurrentId}：get lock with do work. {DateTime.Now.ToString("HH:mm:ss.ffffff")}");
                    Thread.Sleep(2000);
                    Console.WriteLine($"{"".PadLeft(20, '-')}Task{Task.CurrentId}：leave lock. {DateTime.Now.ToString("HH:mm:ss.ffffff")}");
                    ssl.Leave();
                });
            }
        }

        #endregion

        static void GetMaximum()
        {
            int num = 1;

            Parallel.For(0, 10, (i) =>
            {
                Task.Run(() =>
                {
                    var max = Maxinum(ref num, i);
                    Thread.Sleep(10);
                    Console.WriteLine($"i = {i} ，num = {num} ，max = {max}");
                });
            });

            int Maxinum(ref int target, int value)
            {
                int currentVal = target, startVal, desiredVal;

                do
                {
                    startVal = currentVal;

                    desiredVal = Math.Max(startVal, value);

                    currentVal = Interlocked.CompareExchange(ref target, desiredVal, startVal);
                    /*
                     * Interlocked.CompareExchange 逻辑相当于如下代码：
                     *     int tempVal = target;
                     *     if (target == startVal)
                     *     {
                     *         target = desiredVal;
                     *     }
                     *     return tempVal;
                     */


                } while (startVal != currentVal);//startVal != currentVal时，说明target值被另一个线程（其中target 比 value 小）改了！
                return desiredVal;
            }
        }

        #region 通用 GetMaximun
        static void GetMaximum2()
        {
            int num = 1;

            Parallel.For(0, 10, (i) =>
            {
                Task.Run(() =>
                {
                    string str = "".PadLeft(i, '-');
                    var max = Morph(ref num, str, delegate (int a, string r, out string o)
                    {
                        int temp = Math.Max(r.Length, a);
                        o = "".PadLeft(temp, '-');
                        return temp;
                    });
                    Thread.Sleep(10);
                    Console.WriteLine($"i = {i} ，num = {num} ，max = {max}");
                });
            });
        }

        delegate int Morpher<TResult, TArgument>(int startValue, TArgument argument, out TResult morphResult);
        static TResult Morph<TResult, TArgument>(ref int target, TArgument argument, Morpher<TResult, TArgument> morpher)
        {
            TResult morphResult;
            int currentVaul = target, startVal, desiredVal;

            do
            {
                startVal = currentVaul;
                desiredVal = morpher(startVal, argument, out morphResult);
                currentVaul = Interlocked.CompareExchange(ref target, desiredVal, startVal);
            } while (startVal != currentVaul);
            return morphResult;
        } 
        #endregion

    }
    /// <summary>
    /// 实现简单的自旋锁（自旋，即原地打转，循环轮询是否可以得到锁）
    /// </summary>
    struct SimpleSpinLock
    {
        private int resourceInUse;

        public void Enter()
        {
            //第二个线程没有获得锁时，会不停地调用Exchange进行“自旋”，直到第一个线程调用Leave。
            while (true)
            {
                if (Interlocked.Exchange(ref resourceInUse, 1) == 0)
                {
                    Console.WriteLine($"Task{Task.CurrentId}：enter lock. {DateTime.Now.ToString("HH:mm:ss.ffffff")}");
                    return;
                }
            }
        }

        public void Leave()
        {
            //this.resourceInUse = 0;
            Thread.VolatileWrite(ref this.resourceInUse, 0);
        }
    }
}
