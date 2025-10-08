using SpiderInterface;

namespace LinkChecker
{
    internal class SingleThreadedLogger
    {
        private static readonly ConcurrentExclusiveSchedulerPair schedulerPair = new ConcurrentExclusiveSchedulerPair(TaskScheduler.Default, maxConcurrencyLevel: 1);
        private static readonly TaskFactory logTaskFactory = new TaskFactory(schedulerPair.ExclusiveScheduler);
        internal static void LogException(Exception ex, Uri? parentUri, Uri uri)
        {
            logTaskFactory.StartNew(() =>
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Exception {ex.Message} processing {uri} parent is {parentUri}");
                Console.ForegroundColor = ConsoleColor.White;
            });
        }
        internal static void Log(string msg, MessageSeverity severity)
        {
            ConsoleColor newColor = severity switch
            {
                MessageSeverity.Success => ConsoleColor.Green,
                MessageSeverity.Info => ConsoleColor.White,
                MessageSeverity.Warn => ConsoleColor.Yellow,
                MessageSeverity.Error => ConsoleColor.Red,
                _ => throw new Exception("Illegal value"),
            };
            logTaskFactory.StartNew(() =>
            {
                Console.ForegroundColor = newColor;
                Console.WriteLine(msg);
                Console.ForegroundColor = ConsoleColor.White;
            });
        }
    }
}