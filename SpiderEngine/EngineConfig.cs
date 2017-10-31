using SpiderInterface;
using System.Collections.Generic;
using System;
using System.IO;
using System.Reflection;

namespace SpiderEngine
{
  public class EngineConfig
  {
    private string[] args;
    public List<ISpiderExtension> Extensions { get; internal set; } = new List<ISpiderExtension>();
    public List<String> Errors { get; set; } = new List<String>();
    public Uri StartUri { get; internal set; }

    public EngineConfig(string[] args)
    {
      this.args = args;

      LoadExtensions();
    }

    private void LoadExtensions()
    {
      string fileName = "extensions.txt";
      if (File.Exists(fileName))
      {
        try
        {
          var lines = File.ReadAllLines(fileName);
          foreach (var line in lines)
          {
            try
            {
              String[] tokens = line.Split(',');
              String assemblyPath = tokens[1];
              String extensionName = tokens[0];
              if (!Path.IsPathRooted(assemblyPath))
              {
                assemblyPath = Path.Combine(Environment.CurrentDirectory, assemblyPath);
              }
              Assembly extensionAssembly = Assembly.LoadFile(assemblyPath);
              ISpiderExtension extension = extensionAssembly.CreateInstance(extensionName) as ISpiderExtension;
              Extensions.Add(extension);
            }
            catch (Exception ex)
            {
              Errors.Add($"Error {ex.Message} reading extension config file line is {line}");
            }
          }
        }
        catch (Exception ex)
        {
          Errors.Add($"Error {ex.Message} reading file {fileName}");
        }
      }
    }
    public bool EnsureCorrect()
    {
      if (args.Length < 1)
      {
        Errors.Add("No url provided");
        return false;
      }
      if (args[0].StartsWith("http://") || args[0].StartsWith("https://"))
        StartUri = new Uri(args[0]);
      else
        StartUri = new Uri("http://"+ args[0]);
      return true;
    }
  }
}