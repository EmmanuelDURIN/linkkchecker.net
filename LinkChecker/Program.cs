using SpiderEngine;


namespace LinkChecker
{
    class Program
    {
        static async Task Main(string[] args)
        {
            EngineConfig config = EngineConfig.Deserialize(args);
            if (!config.EnsureCorrect())
            {
                foreach (var error in config.Errors)
                {
                    Console.WriteLine(error);
                }
                return;
            }
            Engine engine = new Engine
            {
                Config = config,
                ExceptionLogger = SingleThreadedLogger.LogException,
                Logger = SingleThreadedLogger.Log
            };
            CancellationTokenSource cts = new CancellationTokenSource();
            Task task = engine.StartAsync(cts.Token);
            Console.WriteLine("Press Ctr+C to Stop");
            Console.CancelKeyPress += (sender, e) =>
            {
                SingleThreadedLogger.Log("Cancelling", SpiderInterface.MessageSeverity.Cancel);
                cts.Cancel();
                SingleThreadedLogger.Log("Cancelled", SpiderInterface.MessageSeverity.Cancel);
            };
            await task;
            Environment.ExitCode = engine.ScanResultCollection.Count(sr => sr.Value.Status.IsSuccess() && sr.Value.Exception == null);
            Console.WriteLine("Press any key to continue");
            Console.ReadKey();
        }
    }
}
