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
        ExceptionLogger = BasicLogger.LogException,
        Logger = BasicLogger.Log
      };
      engine.Start();
      Environment.ExitCode = engine.ScanResults.Count(sr => sr.Value.Status.IsSuccess() && sr.Value.Exception == null);
    }
  }
}
