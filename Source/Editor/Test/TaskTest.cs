using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace UniqueEditor
{
    public class TaskTest
    {

        static async Task Test(object msg)
        {
            Console.WriteLine("Start " + msg + ", thread : " + Thread.CurrentThread.ManagedThreadId);
            int i = 2_000_000_0;
            while(i-- > 0)
            {

            }

            Console.WriteLine("Finish " + msg);
        }

        static void PrintThreadID(string msg = "")
        {
         //   Console.WriteLine("SyncContext : {0}", SynchronizationContext.Current);
            Console.WriteLine(msg + " Thread : " + Thread.CurrentThread.ManagedThreadId);
        }


        static async void TestAsync()
        {
           // await Test("test 1 ");

            var task = Task.Run(async () =>
                {
                    PrintThreadID();
                    await Test("test 2 ");
            });

            //await task;
            /*
            Task task = new Task(Test, "Task");
            task.Start();
            await task;*/
            
        }

        public static void Go()
        {
            PrintThreadID("Main start");

            TestAsync();

            PrintThreadID("Main continue");

            return;
            List<Task> tasks = new List<Task>();

            long start = Stopwatch.GetTimestamp();
            for(int i = 0; i < 8; i++)
            {
                Task t = Task.Factory.StartNew((id) =>
                {
                    Test("Task " + id);
                }, i);

                tasks.Add(t);
            }

            for(int i = 0; i < tasks.Count; i++)
            {
                tasks[i].Wait();
            }

            PrintThreadID();
            Console.WriteLine("Time cost : " + (Stopwatch.GetTimestamp() - start) / (float)Stopwatch.Frequency);

            start = Stopwatch.GetTimestamp();
            Parallel.For(0, 8, (id) =>
            {
                Test("Task " + id);
            });

            PrintThreadID();
            Console.WriteLine("Time cost : " + (Stopwatch.GetTimestamp() - start) / (float)Stopwatch.Frequency);

            Console.ReadLine();
        }
    }
}
