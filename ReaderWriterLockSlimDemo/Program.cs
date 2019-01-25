using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ReaderWriterLockSlimDemo
{
    class Program
    {
        static void Main(string[] args)
        {
            ReadTest();
            //UpgradeTest();
            //EnterWriteReadTest();
        }
        /*
         * 
         * 
         * ReaderWriterLockSlim (基于用户模式和内核模式的混合构造)
         *     第一列表示当前要进入的模式（读、升级、写），第一行表示当前锁处于什么模式（无锁、读、升级、写）
         *     Y表示进入成功，N表示阻塞，等待锁释放
         *     
         *  |      | 无锁 | 读                                           | 升级                                       | 写  |
         *  | ---- | ---  | --------------------------------------------| ------------------------------------------ | --- |
         *  | 读   | Y    | 如果已有线程等待进入写模式，则阻塞，其他进入     | 如果有线程等待进入写模式，则阻塞，其他进入      | N   |
         *  | 升级 | Y    | 如果有线程等待进入写或读模式，则阻塞，其他进入    | N                                          | N   |
         *  | 写   | Y    | N                                            | N                                          | N   |
         *  
         */


        /// <summary>
        /// 0、测试一个线程进入读模式后，其他线程是否可以进入读模式或写模式（读可以，写不行）
        /// 1、测试一个线程进入写模式后，其他线程是否可以进入读模式（不行）
        /// </summary>
        static void ReadTest()
        {
            ReaderWriterLockSlim slim = new ReaderWriterLockSlim();

            slim.EnterReadLock();
            Console.WriteLine($"{ThreadId()} enter read lock.");
            for (int i = 0; i < 3; i++)
            {
                Task.Run(() =>
                {
                    Thread.Sleep(1000);
                    slim.EnterReadLock();
                    Console.WriteLine($"{ThreadId()} enter read lock.");
                    Thread.Sleep(1000);
                    slim.ExitReadLock();
                    Console.WriteLine($"{ThreadId()} exit read lock.");
                });
            }

            Task.Run(() =>
            {
                Thread.Sleep(999);
                Console.WriteLine($"{ThreadId()} try enter write lock.");
                slim.EnterWriteLock();
                Console.WriteLine($"{ThreadId()} enter write lock.");
                Thread.Sleep(3000);
                slim.ExitWriteLock();
                Console.WriteLine($"{ThreadId()} exit write lock.");
            });

            Thread.Sleep(1000);
            slim.ExitReadLock();
            Console.WriteLine($"{ThreadId()} exit read lock.");
            Console.ReadKey();
        }
        /// <summary>
        /// 0、测试一个线程进入读模式后，其他线程是否可以进入升级锁模式（是）
        /// 1、测试一个线程进入升级锁模式后，其他线程能否进入读模式和升级模式（读能进入，其他不能）
        /// 2、测试一个线程进入升级锁模式且升级为写模式后，其他线程能否进入读模式（读不能进入，线程退出写模式后就能进入读）
        /// </summary>
        static void UpgradeTest()
        {
            ReaderWriterLockSlim slim = new ReaderWriterLockSlim();
            AutoResetEvent autoReset = new AutoResetEvent(false);

            slim.EnterReadLock();
            Console.WriteLine($"{ThreadId()} enter read lock.");

            Task.Run(() =>
            {
                slim.EnterUpgradeableReadLock();
                Console.WriteLine($"{ThreadId()}+ EnterUpgradeableReadLock.");
                autoReset.Set();
                Thread.Sleep(1000);
                slim.ExitUpgradeableReadLock();
                Console.WriteLine($"{ThreadId()}+ ExitUpgradeableReadLock.");
            });

            Task.Run(() =>
            {
                slim.EnterUpgradeableReadLock();
                Console.WriteLine($"{ThreadId()} EnterUpgradeableReadLock.");
                slim.EnterWriteLock();
                Console.WriteLine($"{ThreadId()} EnterUpgradeableReadLock with WriteLock.");
                autoReset.Set(); //升级锁进入写模式时，是否可读测试
                Thread.Sleep(1000);
                slim.ExitWriteLock();//退出写模式后，其他线程可获取读模式
                Console.WriteLine($"{ThreadId()} ExitWriteLock.");
                Thread.Sleep(1000);
                slim.ExitUpgradeableReadLock();
                Console.WriteLine($"{ThreadId()} ExitUpgradeableReadLock with WriteLock.");
            });

            for (int i = 0; i < 2; i++)
            {
                Task.Run(() =>
                {
                    autoReset.WaitOne();
                    Console.WriteLine($"{ThreadId()} try enter read lock.");
                    slim.EnterReadLock();
                    Console.WriteLine($"{ThreadId()} enter read lock.");
                    Thread.Sleep(1000);
                    slim.ExitReadLock();
                    Console.WriteLine($"{ThreadId()} exit read lock.");
                });
            }

            Thread.Sleep(1000);
            slim.ExitReadLock();
            Console.WriteLine($"{ThreadId()} exit read lock.");
            Console.ReadKey();
        }

        /// <summary>
        /// 随便 测试
        /// </summary>
        static void EnterWriteReadTest()
        {
            ReaderWriterLockSlim slim = new ReaderWriterLockSlim();

            slim.EnterWriteLock();
            Console.WriteLine($"{ThreadId()} enter write lock.");
            Task.Run(() =>
            {
                Console.WriteLine($"{ThreadId()} try enter read lock.");
                slim.EnterReadLock();
                Console.WriteLine($"{ThreadId()} enter read lock.");
                Thread.Sleep(3000);
                slim.ExitReadLock();
                Console.WriteLine($"{ThreadId()} exit read lock.");
            });
            Task.Run(() =>
            {
                Console.WriteLine($"{ThreadId()} try enter read lock.");
                slim.EnterReadLock();
                Console.WriteLine($"{ThreadId()} enter read lock.");
                Thread.Sleep(3000);
                slim.ExitReadLock();//注释此句代码，第三个Task.Run 无法进入 写锁
                Console.WriteLine($"{ThreadId()} exit read lock.");
            });
            Task.Run(() =>
            {
                Thread.Sleep(3100);
                Console.WriteLine($"{ThreadId()} try enter write lock.");
                slim.EnterWriteLock();
                Thread.Sleep(100);
                Console.WriteLine($"{ThreadId()} enter write lock.");
                Thread.Sleep(3000);
                slim.ExitWriteLock();
                Console.WriteLine($"{ThreadId()} exit write lock.");
            });
            Task.Run(() =>
            {
                Thread.Sleep(3100);
                Console.WriteLine($"{ThreadId()} try EnterUpgradeableReadLock .");
                slim.EnterUpgradeableReadLock();
                Thread.Sleep(100);
                Console.WriteLine($"{ThreadId()} EnterUpgradeableReadLock.");
                Thread.Sleep(3000);
                slim.ExitUpgradeableReadLock();
                Console.WriteLine($"{ThreadId()} ExitUpgradeableReadLock.");
            });

            Thread.Sleep(3000);
            slim.ExitWriteLock();
            Console.WriteLine($"{ThreadId()} exit write lock.");
            Console.ReadKey();
        }
        
        static string ThreadId()
        {
            return $"{DateTime.Now.ToString("HH:mm:ss.ffff")}{"".PadLeft(4)}ThreadId = {Thread.CurrentThread.ManagedThreadId}{"".PadLeft(4)}";
        }

    }
}
