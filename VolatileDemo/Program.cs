using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace VolatileDemo
{
    class Program
    {
        /// <summary>
        /// volatile（C# 参考） https://docs.microsoft.com/zh-cn/dotnet/csharp/language-reference/keywords/volatile
        /// volatile 关键字 ：
        ///         1、对于标记volatile的成员，读取时，禁止后面的读取操作重新排序，写入时，禁止前面的写入操作重新排序（避免代码重新排序）
        /// 
        ///         2、告诉C#和JIT编译器不将字段缓存到CPU的寄存器中，确保字段的所有读写操作都在RAM中进行（降低了性能，避免了脏读）
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            //DirtyRead();
            //Reorder();

            //VolatileRead();
            VolatileReorder();

            Console.WriteLine("end.");
            Console.ReadKey();
        }

        #region bad. 在Release模式下才能看到效果
        static bool s_stopWorker;
        static void DirtyRead()
        {
            Console.WriteLine("Main: letting worker run for 3 seconds.");

            Thread t = new Thread(Worker);
            t.Start();
            Thread.Sleep(3000);
            s_stopWorker = true;
            Console.WriteLine("Main: waiting for worker to stop");
            t.Join();

            void Worker(object o)
            {
                int x = 0;
                //while (!Volatile.Read(ref s_stopWorker))
                //{
                //    x++;
                //}

                //下面代码会导致死循环，编译器优化会导致s_stopWorker只会读取一次，
                while (!s_stopWorker)
                {
                    x++;
                }
                s_stopWorker = true;
                Console.WriteLine($"Worker: stopped wheen x = {x}");
            }
        }


        static int _data = 0;
        static bool _initialized = false;

        /// <summary>
        /// 编译器优化和硬件可能会导致 代码重新排序（也就是后写的代码先执行）
        /// 即 Print()方法 可能输出的结果是 0 ！
        /// ps:这种情况我试了很多次，没试出来。
        /// </summary>
        static void Reorder()
        {
            Task.Run(() => { Init(); });
            Task.Run(() => { Print(); });
            void Init()
            {
                _data = 42;            // Write 1
                _initialized = true;   // Write 2
            }
            void Print()
            {
                if (_initialized)            // Read 1
                    Console.WriteLine(_data);  // Read 2
                else
                    Console.WriteLine("Not initialized");
            }
        }

        #endregion

        #region 使用 volatile 解决上述问题

        volatile static bool volatileStopWorker = false;
        static void VolatileRead()
        {
            Console.WriteLine("Main: letting worker run for 3 seconds.");

            Thread t = new Thread(VolatileWorker);
            t.Start();
            Thread.Sleep(3000);
            volatileStopWorker = true;
            Console.WriteLine("Main: waiting for worker to stop");
            t.Join();

            void VolatileWorker(object o)
            {
                int x = 0;
                while (!volatileStopWorker)
                {
                    x++;
                }
                Console.WriteLine($"Worker: stopped wheen x = {x}");
            }
        }

        static int _data2 = 0;
        static volatile bool _volatileinitialized = false;
        
        static void VolatileReorder()
        {
            Task.Run(() => { Init(); });
            Task.Run(() => { Print(); });
            void Init()
            {
                _data2 = 42;            // Write 1
                _volatileinitialized = true;   // Write 2
            }
            void Print()
            {
                if (_volatileinitialized)            // Read 1
                    Console.WriteLine(_data2);  // Read 2
                else
                    Console.WriteLine("Not initialized");
            }
        }
        #endregion
    }
}
