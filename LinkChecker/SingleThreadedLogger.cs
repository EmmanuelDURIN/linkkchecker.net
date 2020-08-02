using System;
using System.Threading;
using System.Threading.Tasks;
using SpiderInterface;

namespace LinkChecker
{
    internal class SingleThreadedLogger
    {
        private static ConcurrentExclusiveSchedulerPair schedulerPair = new ConcurrentExclusiveSchedulerPair(TaskScheduler.Default, maxConcurrencyLevel: 1);
        private static TaskScheduler exclusiveScheduler = schedulerPair.ExclusiveScheduler;

        static SingleThreadedLogger()
        {
            exclusiveScheduler = schedulerPair.ExclusiveScheduler;
        }
        internal static void LogException(Exception ex, Uri parentUri, Uri uri)
        {
            Task.Factory.StartNew
                (
                        () =>
                        {
                            Console.WriteLine($"Execution of log on thread {Thread.CurrentThread.ManagedThreadId}");
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine($"Exception {ex.Message} processing {uri} parent is {parentUri}");
                            Console.ForegroundColor = ConsoleColor.White;
                        },
                        CancellationToken.None,
                        TaskCreationOptions.None,
                        exclusiveScheduler
                );
        }
        internal static void Log(string msg, MessageSeverity severity)
        {
            Task.Factory.StartNew
            (
                () =>
                {
                    Console.WriteLine($"Execution of log on thread {Thread.CurrentThread.ManagedThreadId}");
                    switch (severity)
                    {
                        case MessageSeverity.Success:
                            Console.ForegroundColor = ConsoleColor.Green;
                            break;
                        case MessageSeverity.Info:
                            Console.ForegroundColor = ConsoleColor.White;
                            break;
                        case MessageSeverity.Warn:
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            break;
                        case MessageSeverity.Error:
                            Console.ForegroundColor = ConsoleColor.Red;
                            break;
                        default:
                            break;
                    }
                    Console.WriteLine(msg);
                    Console.ForegroundColor = ConsoleColor.White;
                },
                CancellationToken.None,
                TaskCreationOptions.None,
                exclusiveScheduler
            );
        }
    }
}