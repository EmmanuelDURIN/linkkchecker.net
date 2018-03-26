using SpiderEngine;
using SpiderInterface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace LinkChecker
{
  class Program
  {
    static void Main(string[] args)
    {
      EngineConfig config = new EngineConfig (args);
      if (!config.EnsureCorrect())
      {
        foreach (var error in config.Errors)
        {
          Console.WriteLine(error);
        }
        return;
      }
      Engine engine = new Engine {
        Config = config ,
        ExceptionLogger = SingleThreadedLogger.LogException,
        Log = SingleThreadedLogger.Log
      };
      CancellationTokenSource cts = new CancellationTokenSource();
      Task task = engine.Start(cts.Token);
      Console.WriteLine("Press Ctr+C to Stop");
      Console.CancelKeyPress += (sender,e) =>
      {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("Cancellation");
        Console.ForegroundColor = ConsoleColor.White;
        cts.Cancel();
      };
      task.Wait();
      Environment.ExitCode = engine.ScanResults.FailureCount;
    }
  }
}
