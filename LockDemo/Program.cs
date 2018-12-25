using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LockDemo
{
    class Program
    {
        static void Main(string[] args)
        {
            //Example();
            Example(7);

            Console.ReadKey();
        }

        static Random r = new Random();

        #region 线程一只能输出1，线程二只能输出2，线程三只能输出3，在控制台中输出顺序为 1 2 3 1 2 3

        static void Example()
        {
            int currentValue = 1;
            //
            var lockObj = new object();
            Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    //Thread.Sleep(r.Next(500, 1001));
                    lock (lockObj)
                    {
                        if (currentValue == 1)
                        {
                            currentValue = 2;
                            Console.WriteLine($"线程 {Thread.CurrentThread.ManagedThreadId} : " + 1);
                        }
                    }
                }
            }, TaskCreationOptions.LongRunning);
            Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    // Thread.Sleep(r.Next(500, 1001));
                    lock (lockObj)
                    {
                        if (currentValue == 2)
                        {
                            currentValue = 3;
                            Console.WriteLine($"线程 {Thread.CurrentThread.ManagedThreadId} : " + 2);
                        }
                    }
                }
            }, TaskCreationOptions.LongRunning);
            Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    //Thread.Sleep(r.Next(500, 1001));
                    lock (lockObj)
                    {
                        if (currentValue == 3)
                        {
                            currentValue = 1;
                            Console.WriteLine($"线程 {Thread.CurrentThread.ManagedThreadId} : " + 3);
                        }
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
            var lockObj = new object();
            Enumerable.Range(1, num).AsParallel().ForAll(i =>
            {
                Thread thread = new Thread(() =>
                {
                    while (true)
                    {
                        Thread.Sleep(r.Next(100, 501));
                        int tempNum = i + 1 > num ? 1 : i + 1;
                        /*
                         * 注：这里只用 Interlocked.CompareExchange 为什么有bug?
                         * 因为 Interlocked.CompareExchange 方法不能保证 if 条件里的代码执行顺序！！！
                         * 只能保证currentValue的值的确从 1 - num 之间增长！！！
                         * 所以用 lock 包裹保证顺序输入（既然用了Lock，也就没必要在lock里用Interlocked了）
                         * 
                        */
                        if (currentValue == i)
                        {
                            lock (lockObj)
                            {
                                //if (Interlocked.CompareExchange(ref currentValue, tempNum, i) == i)
                                //{
                                //    Console.WriteLine($"{Thread.CurrentThread.Name} : {i}");
                                //}
                                if (currentValue == i)
                                {
                                    currentValue = tempNum;
                                    Console.WriteLine($"{Thread.CurrentThread.Name} : {i}");
                                }
                            }
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
