namespace SpiderEngine
{
    public class ExtensionInfo
    {
        public ExtensionInfo(string assembly, string @class)
        {
            Assembly = assembly;
            Class = @class;
        }
        public string Assembly { get; set; }
        public string Class { get; set; }
    }
}