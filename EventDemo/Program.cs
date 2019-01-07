using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EventDemo
{
    /// <summary>
    /// .net下 AutoResetEvent 默认不允许执行递归锁
    /// </summary>
    class RecursiveAutoResetEvent
    {
        AutoResetEvent autoReset = new AutoResetEvent(true);
        int recursiveCount = 0;
        private int enterThreadId = -1;
        public bool Enter()
        {
            var curThreadId = Thread.CurrentThread.ManagedThreadId;
            if (curThreadId == enterThreadId)
            {
                recursiveCount++;
                return true;
            }
            if (autoReset.WaitOne())
            {
                recursiveCount++;
                enterThreadId = curThreadId;
                return true;
            }
            return false;
        }

        public bool Leave()
        {
            var curThreadId = Thread.CurrentThread.ManagedThreadId;
            if (curThreadId == enterThreadId)
            {
                recursiveCount--;
                if (recursiveCount == 0)
                {
                    autoReset.Set();
                }
                return true;
            }
            return false;
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            //AutoResetEventDemo();

            //RecursiveLockDemo();

            //ManualResetEventDemo();

            PerformanceTest();
        }

        static string Now()
        {
            return $"{DateTime.Now.ToString("HH:mm:ss.ffff")}{"".PadLeft(4)}";
        }

        #region AutoResetEvent 

        static void AutoResetEventDemo()
        {
            AutoResetEvent autoReset = new AutoResetEvent(false);//初始为没有信号

            string str = string.Empty;
            ThreadPool.QueueUserWorkItem(o =>
            {
                using (HttpClient client = new HttpClient())
                {
                    Console.WriteLine($"{Now()}start async....");

                    //异步todo...
                    Thread.Sleep(500);
                    str = client.GetStringAsync("https://www.cnblogs.com/").GetAwaiter().GetResult();

                    Console.WriteLine($"{Now()}complete async....");
                }
                autoReset.Set();//设置信号。在一个线程获取信号后，自动阻塞其他线程
            });
            Console.WriteLine($"{Now()}start sync doing....");

            //同步todo...
            Thread.Sleep(600);
            Console.WriteLine($"{Now()}complete sync and wait....");

            //同步做完后，等待异步完成！
            autoReset.WaitOne();
            Console.WriteLine($"{Now()}done. url content length ={str.Length}");

            Console.ReadKey();
        }

        static void RecursiveLockDemo()
        {
            RecursiveAutoResetEvent recursiveEvent = new RecursiveAutoResetEvent();

            for (int i = 0; i < 3; i++)
            {
                Task.Factory.StartNew(o =>
                {
                    Do("Tag" + o.ToString());
                }, i);
            }
            Console.ReadKey();

            void Do(string tag)
            {
                Thread.Sleep(100);
                Console.WriteLine($"try get lock. -- ThreadId={Thread.CurrentThread.ManagedThreadId}");
                recursiveEvent.Enter();
                Console.WriteLine($"{Now()}{nameof(Do)} -- ThreadId={Thread.CurrentThread.ManagedThreadId} --{tag}");
                Do2(tag);
                Thread.Sleep(1000);
                recursiveEvent.Leave();
            }

            void Do2(string tag)
            {
                recursiveEvent.Enter();
                Thread.Sleep(1000);
                Console.WriteLine($"{"".PadLeft(4)}{Now()}{nameof(Do2)} -- ThreadId={Thread.CurrentThread.ManagedThreadId} --{tag}");
                recursiveEvent.Leave();
            }
        }
        #endregion

        static void ManualResetEventDemo()
        {
            //初始为没有信号（非终止），所有现在等待
            ManualResetEvent manualResetEvent = new ManualResetEvent(false);

            for (int i = 1; i <= 6; i++)
            {
                Task.Factory.StartNew(o =>
                {
                    Console.WriteLine($"{Now()} Id = {o.ToString()} wait for lock.");
                    manualResetEvent.WaitOne();
                    Console.WriteLine($"{Now()} Id = {o.ToString()} get lock.");
                }, i);
            }
            Thread.Sleep(2000);
            manualResetEvent.Set();//所有等待的线程都获取锁
            manualResetEvent.Reset(); //设为没有信号，因为Set 与 Reset不是原子性操作，顾可能会多个线程获取锁
            Console.Read();
        }

        #region 比较性能

        /// <summary>
        /// 空方法
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void None() { }

        static void PerformanceTest()
        {
            int complareCount = 10_000_000;
            int num = 0;//递增数值
            long firstMillisecond = 0;//无锁调用耗时
            Console.WriteLine($"{"-".PadLeft(8,'-')}{complareCount.ToString("N0")}次调用测试{"-".PadLeft(8, '-')}");
            Stopwatch watch = Stopwatch.StartNew();
            for (int i = 0; i < complareCount; i++)
            {
                num++;
            }
            watch.Stop();
            firstMillisecond = watch.ElapsedMilliseconds;
            Console.WriteLine($"无锁 调用耗时：{watch.ElapsedMilliseconds} 毫秒 {(num == complareCount ? "" : "Error")}");

            num = 0;
            watch.Restart();
            for (int i = 0; i < complareCount; i++)
            {
                None();
                num++;
                None();
            }
            watch.Stop();
            Console.WriteLine($"无锁+空方法 调用耗时：{watch.ElapsedMilliseconds} 毫秒 {"".PadLeft(10)} 约慢{Math.Ceiling((decimal)watch.ElapsedMilliseconds/ firstMillisecond)}倍  {(num == complareCount ? "" : "Error")}");
            
            num = 0;
            watch.Restart();
            for (int i = 0; i < complareCount; i++)
            {
                lock (watch)
                {
                    num++;
                }
            }
            watch.Stop();

            Console.WriteLine($"无锁+混合构造模式锁Lock 调用耗时：{watch.ElapsedMilliseconds} 毫秒 {"".PadLeft(10)} 约慢{Math.Ceiling((decimal)watch.ElapsedMilliseconds / firstMillisecond)}倍  {(num == complareCount ? "" : "Error")}");

            InterlockedSpinLock interlockedLock = new InterlockedSpinLock();
            num = 0;
            watch.Restart();
            for (int i = 0; i < complareCount; i++)
            {
                interlockedLock.Enter();
                num++;
                interlockedLock.Leave();
            }
            watch.Stop();

            Console.WriteLine($"无锁+用户模式锁Interlocked 调用耗时：{watch.ElapsedMilliseconds} 毫秒 {"".PadLeft(10)} 约慢{Math.Ceiling((decimal)watch.ElapsedMilliseconds / firstMillisecond)}倍  {(num == complareCount ? "" : "Error")}");

            using (AutoResetEventLock autoEventLock = new AutoResetEventLock())
            {
                num = 0;
                watch.Restart();
                for (int i = 0; i < complareCount; i++)
                {
                    autoEventLock.Enter();
                    num++;
                    autoEventLock.Leave();
                }
                watch.Stop();
            }

            Console.WriteLine($"无锁+内核模式锁AutoResetEvent 调用耗时：{watch.ElapsedMilliseconds} 毫秒 {"".PadLeft(10)} 约慢{Math.Ceiling((decimal)watch.ElapsedMilliseconds / firstMillisecond)}倍  {(num == complareCount ? "" : "Error")}");


            Console.ReadKey();
        }


        #endregion

    }

    /// <summary>
    /// Interlocked 自旋锁
    /// </summary>
    class InterlockedSpinLock
    {
        private int resourceInUse;

        public void Enter()
        {
            //第二个线程没有获得锁时，会不停地调用Exchange进行“自旋”，直到第一个线程调用Leave。
            while (true)
            {
                if (Interlocked.Exchange(ref resourceInUse, 1) == 0)
                {
                    return;
                }
            }
        }
        public void Leave()
        {
            Thread.VolatileWrite(ref this.resourceInUse, 0);
        }
    }
    /// <summary>
    /// AutoResetEvent Lock
    /// </summary>
    class AutoResetEventLock : IDisposable
    {
        /// <summary>
        /// 初始为终止状态（不会阻塞线程）
        /// </summary>
        private AutoResetEvent resetEvent = new AutoResetEvent(true);
        public void Enter()
        {
            resetEvent.WaitOne();
        }

        public void Leave()
        {
            resetEvent.Set();
        }

        public void Dispose()
        {
            resetEvent.Dispose();
        }
    }
}
