using SpiderInterface;
using System;

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
            // C#7 switch expression
            ConsoleColor newColor = severity switch
            {
                MessageSeverity.Success => ConsoleColor.Green,
                MessageSeverity.Info => ConsoleColor.White,
                MessageSeverity.Warn => ConsoleColor.Yellow,
                MessageSeverity.Error => ConsoleColor.Red,
                _ => throw new Exception("Illegal value"),
            };
            lock (logLock)
            {
                // E/S => lent
                Console.ForegroundColor = newColor;
                Console.WriteLine(msg);
                Console.ForegroundColor = ConsoleColor.White;
            }
        }
    }
}