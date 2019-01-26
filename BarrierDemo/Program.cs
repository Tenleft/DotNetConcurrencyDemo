using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BarrierDemo
{
    class Program
    {
        static void Main(string[] args)
        {
            BarrierTest();
        }

        /*
         * Barrier：
         *     允许把一个功能分成多个阶段，每个阶段里的参与者可能耗时不同，
         *     所以，必须等待n个参与者都完成时（每个参与者完成后调用 SignalAndWait），才会进行一阶段的任务。
         * 
         *     个人感觉这个任务并行库（Parallel）很相似，主线程同步等待并行任务的完成。
         */

        static void BarrierTest()
        {
            int total = 10_000, sum = 0;
            int participantCount = 3;

            AutoResetEvent autoReset = new AutoResetEvent(false);

            Barrier b = new Barrier(participantCount, o =>
              {
                  //进入这里说明 participantCount 个参与者都已做完自己的工作。
                  //每个阶段完成后，ParticipantsRemaining 会重置为参与者数量
                  Console.WriteLine($"{ThreadId()}In callBack 当前阶段：{o.CurrentPhaseNumber}，参与者：{o.ParticipantCount}，未发出信号个数(ParticipantsRemaining)：{o.ParticipantsRemaining}。");
                  autoReset.Set();
              });

            for (int i = 1; i <= participantCount; i++)
            {
                Task.Factory.StartNew(o =>
                {
                    Thread.Sleep((int)o * 1000);//模拟耗时操作，每个参与者耗时不同
                    Console.WriteLine($"{ThreadId()}{o} 当前阶段：{b.CurrentPhaseNumber}，参与者：{b.ParticipantCount}，未发出信号个数(ParticipantsRemaining)：{b.ParticipantsRemaining}。");
                    b.SignalAndWait();//等待所有参与者完成后才继续，最后一个参与者完成后，将触发回调函数调用
                    Console.WriteLine($"{ThreadId()} end.");
                }, i);
            }

            //等待 第一阶段 3个参与者 完成
            autoReset.WaitOne();
            Console.WriteLine();
            //第二阶段
            for (int i = 1; i <= participantCount; i++)
            {
                Task.Factory.StartNew(o =>
                {
                    for (int j = 1; j <= total; j++)
                    {
                        Interlocked.Add(ref sum, j);
                    }
                    Thread.Sleep((int)o * 1000);//模拟耗时操作，每个参与者耗时不同
                    Console.WriteLine($"{ThreadId()}{o} 当前阶段：{b.CurrentPhaseNumber}，参与者：{b.ParticipantCount}，未发出信号个数(ParticipantsRemaining)：{b.ParticipantsRemaining}。");
                    b.SignalAndWait();//等待所有参与者完成后才继续，最后一个参与者完成后，将触发回调函数调用
                    Console.WriteLine($"{ThreadId()} end.");
                }, i);
            }

            //等待 第二阶段 3个参与者 完成
            autoReset.WaitOne();
            Console.WriteLine($"{ThreadId()}Sum = {sum}，Check result = {sum == (1 + total) * total * participantCount / 2}");
            
            b.Dispose();
            Console.ReadKey();
        }

        static string ThreadId()
        {
            return $"{DateTime.Now.ToString("HH:mm:ss.ffff")}{"".PadLeft(4)}ThreadId = {Thread.CurrentThread.ManagedThreadId}{"".PadLeft(4)}";
        }
    }
}
