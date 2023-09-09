using SpiderInterface;

namespace LinkChecker
{
    internal class SingleThreadedLogger
    {
        private static object logLock = new object();
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
                _ => throw new Exception("Illegal value"),
                //    break;
                //case MessageSeverity.Info:
                //    Console.ForegroundColor = ConsoleColor.White;
                //    break;
                //case MessageSeverity.Warn:
                //    Console.ForegroundColor = ConsoleColor.Yellow;
                //    break;
                //case MessageSeverity.Error:
                //    Console.ForegroundColor = ConsoleColor.Red;
                //    break;
                //default:
                //    break;
            };
            //lock (logLock)
            //{
                Console.ForegroundColor = newColor;
                Console.WriteLine(msg);
                Console.ForegroundColor = ConsoleColor.White;
            //}
        }
    }
}