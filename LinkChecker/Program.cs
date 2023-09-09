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
                ExceptionLogger = BasicLogger.LogException,
                Logger = BasicLogger.Log
            };
            await engine.StartAsync();
            Environment.ExitCode = engine.ScanResults.Count(sr => sr.Value.Status.IsSuccess() && sr.Value.Exception == null);

            Console.WriteLine("Press any key to continue");
            Console.ReadKey();
        }
    }
}
