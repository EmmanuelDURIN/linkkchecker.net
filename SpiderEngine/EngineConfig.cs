using SpiderInterface;
using System.Collections.Generic;
using System;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;

namespace SpiderEngine
{
    public class EngineConfig
    {
        private string[] args;
        public List<ExtensionInfo> ExtensionList { get; set; }
        public List<ISpiderExtension> Extensions { get; internal set; } = new List<ISpiderExtension>();
        public List<String> Errors { get; set; } = new List<String>();
        public bool OnlyCheckInnerLinks { get; set; }
        public Uri StartUri { get; set; }
        public static EngineConfig Deserialize(string[] args)
        {
            string json = File.ReadAllText(@"LinkChecker.json");
            var engineConfig = JsonConvert.DeserializeObject<EngineConfig>(json);
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
                    var untypedExtension = extensionAssembly.CreateInstance(extensionName) as ISpiderExtension;
                    ISpiderExtension extension = untypedExtension as ISpiderExtension;
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