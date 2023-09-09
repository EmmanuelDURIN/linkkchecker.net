using Newtonsoft.Json;
using SpiderInterface;
using System.Reflection;

namespace SpiderEngine
{
    public class EngineConfig
    {
        public List<ExtensionInfo> ExtensionList { get; set; } = new();
        public List<ISpiderExtension> Extensions { get; internal set; } = new List<ISpiderExtension>();
        public List<string> Errors { get; set; } = new List<string>();
        public bool OnlyCheckInnerLinks { get; set; }
        public Uri? StartUri { get; set; }
        public static EngineConfig Deserialize(string[] args)
        {
            string json = File.ReadAllText(@"LinkChecker.json");
            var engineConfig = JsonConvert.DeserializeObject<EngineConfig>(json);
            ArgumentNullException.ThrowIfNull(engineConfig);
            engineConfig.LoadExtensions();
            return engineConfig;
        }
        private void LoadExtensions()
        {
            foreach (ExtensionInfo extensionInfo in ExtensionList)
            {
                string assemblyPath = extensionInfo.Assembly;
                string extensionName = extensionInfo.Class;
                try
                {
                    if (!Path.IsPathRooted(assemblyPath))
                    {
                        assemblyPath = Path.Combine(Environment.CurrentDirectory, assemblyPath);
                    }
                    Assembly extensionAssembly = Assembly.LoadFile(assemblyPath);
                    ISpiderExtension? extension = extensionAssembly.CreateInstance(extensionName) as ISpiderExtension;
                    if (extension != null)
                        Extensions.Add(extension);
                    else
                        throw new Exception("Error loading extension");
                }
                catch (Exception ex)
                {
                    Errors.Add($"Error {ex.Message} loading extension {extensionName}");
                }
            }
        }
        public bool EnsureCorrect()
        {
            if (StartUri == null)
            {
                Errors.Add("No url provided");
                return false;
            }
            string startUri = StartUri.ToString();
            if (!startUri.StartsWith("http://") && !startUri.StartsWith("https://"))
                StartUri = new Uri("http://" + startUri);
            return true;
        }
    }
}