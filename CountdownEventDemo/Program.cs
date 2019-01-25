using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CountdownEventDemo
{
    class Program
    {
        static void Main(string[] args)
        {
            Test();
        }

        /*
         * 内部使用ManualResetEventSlim，当对象内部计数器变成0时可用。（Semaphore在计数为0时阻塞线程）
         * 当对象的CurrentCount变成0，它就无法更改。AddCount会抛异常，TryAddCount返回false。
         *     
         */

        static void Test()
        {
            CountdownEvent countdown = new CountdownEvent(3);
            string str = string.Empty;
            Task.Run(() =>
            {
                while (true)
                {
                    if (countdown.IsSet)
                    {
                        Console.WriteLine(str);
                        break;
                    }
                }
            });
            //1   1   2   3   5   8   13
            Fib(7);
            Console.WriteLine(str);

            Console.ReadKey();

            void Fib(int n)
            {
                if (n < 3)
                {
                    throw new ArgumentException();
                }
                int prev = 1, prev2 = 1, sum = 1;
                for (int i = 3; i <= n; i++)
                {
                    sum = prev2 + prev;
                    if (i!=n)
                    {
                        prev2 = prev;
                        prev = sum;
                    }
                    str = $"Fib({i}) = {sum}";

                    if (countdown.CurrentCount > 0)
                    {
                        countdown.Signal();//小于0 抛异常。
                    }
                }

                var temp = Enumerable.Range(-1, 4).Select(i => i.ToString())
                       .Aggregate((seed, i) => seed.Trim('-', '1') + $"Fib({n + int.Parse(i) - 2}) = {{{i}}}，").TrimEnd('，');
                str = string.Format(temp, prev2, prev, sum);

            }
        }
    }
}
