using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SemaphoreDemo
{
    class Program
    {
        /// <summary>
        /// 内核模式构造
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            //SingleProcess();
            //MultipleProcessSync();

            //MultipleThreadSync();
            SemaphoreSlimTest();
        }

        /// <summary>
        /// 确保程序只运行一次 单例
        /// </summary>
        static void SingleProcess()
        {
            using (new Semaphore(0, 1, nameof(SemaphoreDemo), out var createdNew))
            //using (new EventWaitHandle(false, EventResetMode.AutoReset, nameof(SemaphoreDemo), out var createdNew))
            //using (new Mutex(false,nameof(SemaphoreDemo),out var createdNew))
            {
                if (createdNew)
                {
                    Console.WriteLine("created");
                    Console.ReadKey();
                }
                //else
                //{
                //    Console.WriteLine("an other program created");
                //}
            }
        }

        /// <summary>
        /// 多进程同步！
        /// </summary>
        static void MultipleProcessSync()
        {
            var num = new Random().Next(1, 101);
            //initialCount:初始信号量（小于 maximumCount）
            //maximumCount:最大信号量
            //ps：如果程序报错，请将语言版本设置成C#7.2或更高（项目属性->生成->高级->语言版本）
            var semaphore = new Semaphore(initialCount: 1, maximumCount: 2, nameof(SemaphoreDemo));

            if (num % 3 != 0)
            {
                Console.WriteLine("消费者 - " + num);
                Task.Run(() =>
                {
                    while (true)
                    {
                        if (semaphore.WaitOne())
                        {
                            Console.WriteLine("get - " + num);
                        }
                        else
                        {
                            Console.WriteLine("waitOne false!!! - " + num);
                        }

                    }
                });
                Console.ReadKey();
            }
            else
            {
                Console.WriteLine("生产者");
                while (true)
                {
                    Console.WriteLine("按任意键获取Semaphore");
                    Console.ReadKey();
                    try
                    {
                        semaphore.Release(2);//释放信号量超过 maximumCount 时抛异常！
                        string str = "release semaphore";
                        Console.WriteLine(str.PadLeft(str.Length + 5));
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                    }

                }
            }
        }

        /// <summary>
        /// 多线程同步
        /// </summary>
        static void MultipleThreadSync()
        {
            var samephore = new Semaphore(2, 2);

            var total = 5;
            for (int i = 1; i <= total; i++)
            {
                Task.Factory.StartNew(o =>
                {
                    Console.WriteLine($"线程 {o} 等待中. for wc");
                    samephore.WaitOne();
                    Console.WriteLine($"{string.Empty.PadLeft(5)}线程 {o} ：终于有坑了！开始 嘘嘘~");
                    Thread.Sleep(3000);
                    Console.WriteLine($"{string.Empty.PadLeft(10)}线程 {o} 释放一个坑");
                    samephore.Release();
                }, i, TaskCreationOptions.LongRunning);
            }
            Console.ReadKey();
        }

        static void SemaphoreSlimTest()
        {
            using (SemaphoreSlim slim = new SemaphoreSlim(2))
            using (HttpClient client = new HttpClient())
            {
                string[] urls = { "https://docs.microsoft.com", "https://www.cnblogs.com", "https://github.com" };

                for (int j = 0; j < 3; j++)
                {
                    for (int i = 0; i < urls.Length; i++)
                    {
                        GetHttp(client, urls[i], slim).ContinueWith((t, o) =>
                        {
                            Console.WriteLine($"{ThreadId()}{o} - Result.Length = {t.Result.Length}");
                        }, $"{j} - {urls[i]}");
                    }
                }
                Console.ReadKey();
            }
        }

        static async Task<string> GetHttp(HttpClient client, string url, SemaphoreSlim slim)
        {
            await slim.WaitAsync();
            string str = string.Empty;
            try
            {
                str = await client.GetStringAsync(url);
                await Task.Delay(1000);
            }
            finally
            {
                slim.Release();
            }
            return str;
        }
        static string ThreadId()
        {
            return $"{DateTime.Now.ToString("HH:mm:ss.ffff")}{"".PadLeft(4)}ThreadId = {Thread.CurrentThread.ManagedThreadId}{"".PadLeft(4)}";
        }
    }
}
