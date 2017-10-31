using System;
using SpiderInterface;

namespace LinkChecker
{
  internal class SingleThreadedLogger
  {
    private static Object privateLock = new Object();
    internal static void LogException(Exception ex, Uri parentUri, Uri uri)
    {
      lock (privateLock)
      {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"Exception {ex.Message} processing {uri} parent is {parentUri}");
        Console.ForegroundColor = ConsoleColor.White;
      }
    }

    internal static void Log(string msg, MessageSeverity severity)
    {
      lock (privateLock)
      {
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
      }
    }
  }
}