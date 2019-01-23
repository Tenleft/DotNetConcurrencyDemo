using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MutexDemo
{
    class Program
    {
        static void Main(string[] args)
        {
            //ReleaseMutexAnotherThread();
            //ReleaseMutexError();
            RecursiveMutex();
        }

        /*
         * Mutex本质上是由事件和信号量组成
         *      1、释放锁的线程 和 获取锁的线程 必须一样，否则抛异常
         *      2、获取锁的线程由于异常而未释放锁时，等待的线程会抛出异常
         *      3、Mutex支持递归调用，但不建议用性能不好，建议自己封装。见EventDemo项目中RecursiveAutoResetEvent类
         */

        #region Mutex

        /// <summary>
        /// 在另一个线程上 ReleaseMutex 抛异常
        /// </summary>
        static void ReleaseMutexAnotherThread()
        {
            Mutex m = new Mutex();
            Console.WriteLine($"{nameof(Mutex)} was created.");
            m.WaitOne();//这里记录获取锁的线程Id！

            Console.WriteLine("get lock.");
            Task.Run(() =>
            {
                try
                {
                    Task.Delay(3000).Wait();
                    m.ReleaseMutex();//释放锁的线程 != 获取锁的线程 ，抛出异常！
                    Console.WriteLine("m.ReleaseMutex();");
                }
                catch (Exception e)
                {
                    Console.WriteLine("ReleaseMutex() error. " + e);
                }
            }).Wait();
            Console.WriteLine("end.");
            Console.ReadKey();
        }

        /// <summary>
        /// WaitOne 后抛出异常
        /// </summary>
        static void ReleaseMutexError()
        {
            Mutex m = new Mutex();
            Console.WriteLine($"{Now()}{nameof(Mutex)} was created.");
            Task.Run(() =>
            {
                m.WaitOne();
                int mId = Thread.CurrentThread.ManagedThreadId;
                Console.WriteLine($"{Now()}{mId} get lock.");

                int ms = 1000;
                Console.WriteLine($"{Now()}{ms} 毫秒后线程{mId}抛出异常！");
                Thread.Sleep(ms);
                Console.WriteLine($"{Now()}线程{mId}抛出异常！");
                throw new Exception("test");

            });
            Thread.Sleep(1000);//让异步线程先获取锁
            try
            {
                m.WaitOne();//大概再过20秒WaitOne就会抛出异常 
                Console.WriteLine(Now() + "Main, Thread get lock.");
            }
            catch (Exception e)
            {
                Console.WriteLine(Now() + "Main, WaitOne error. " + e);
            }
            Console.WriteLine(Now() + "end.");
            Console.ReadKey();
        }

        /// <summary>
        /// 递归调用 
        /// </summary>
        static void RecursiveMutex()
        {
            Mutex m = new Mutex();

            //1   1   2   3   5   8   13
            int n = Fib(7);//13
            Console.WriteLine("Fib(7) = " + n);

            Task.Run(() =>
            {
                //测试计数是否归零！
                m.WaitOne();
                Console.WriteLine("true");
                m.ReleaseMutex();
            });
            Console.ReadKey();

            //Mutex对象维护着一个递归计数（recursion count），指出拥有该Mutex的线程调用了多次！
            int Fib(int num)
            {
                try
                {
                    m.WaitOne();
                    if (num <= 2)
                    {
                        //m.WaitOne(); 取消注释这句话 Task.Run不会有输出！！！
                        return 1;
                    }
                    return Fib(num - 1) + Fib(num - 2);
                }
                finally
                {
                    m.ReleaseMutex();
                }
            }
        }

        #endregion

        static string Now()
        {
            return $"{DateTime.Now.ToString("HH:mm:ss.ffff")}{"".PadLeft(4)}";
        }
    }
}
