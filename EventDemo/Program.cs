using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
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
            ManualResetEventDemo();
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

    }
}
