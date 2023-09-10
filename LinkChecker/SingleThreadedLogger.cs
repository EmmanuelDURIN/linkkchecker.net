using SpiderInterface;

namespace LinkChecker
{
    internal class SingleThreadedLogger
    {
        private static ConcurrentExclusiveSchedulerPair schedulerPair = new ConcurrentExclusiveSchedulerPair(TaskScheduler.Default,maxConcurrencyLevel:1);
        private static TaskFactory logTaskFactory = new TaskFactory(schedulerPair.ExclusiveScheduler);
        internal static void LogException(Exception ex, Uri? parentUri, Uri uri)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Exception {ex.Message} processing {uri} parent is {parentUri}");
            Console.ForegroundColor = ConsoleColor.White;
        }
        internal static void Log(string msg, MessageSeverity severity)
        {
            ConsoleColor newColor = severity switch
            {
                MessageSeverity.Success => ConsoleColor.Green,
                MessageSeverity.Info => ConsoleColor.White,
                MessageSeverity.Warn => ConsoleColor.Yellow,
                MessageSeverity.Error => ConsoleColor.Red,
                MessageSeverity.Cancel=> ConsoleColor.Cyan,
                _ => throw new Exception("Illegal value"),
            };
            logTaskFactory.StartNew( () =>
            {
                Console.ForegroundColor = newColor;
                Console.WriteLine(msg);
                Console.ForegroundColor = ConsoleColor.White;
            });
        }
    }
}