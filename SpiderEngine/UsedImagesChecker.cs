using HtmlAgilityPack;
using Newtonsoft.Json;
using SpiderInterface;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace SpiderEngine
{
  public class UsedImagesChecker : ISpiderExtension
  {
    private class UsedImagesCheckerConfig
    {
      /// <summary>
      /// Directory on disk where to find images to be compared with site content
      /// </summary>
      public String ImagesBaseDirectory { get; set; }
      /// <summary>
      /// Directory of site to remove of url for comparison
      /// </summary>
      public String SitePrefixToRemove { get; set; }
      /// <summary>
      /// All sites containing images to scan
      /// </summary>
      public String[] SitesToScan { get; set; }
    }
    private UsedImagesCheckerConfig config;
    private List<String> requestedImages = new List<String>();
    private static String[] imageTypes = { "image/jpeg", "image/jpg", "image/png", "image/gif", "image/tiff" };
    private static String[] imageExtensions = { "jpeg", "jpg", "png", "gif", "tiff"};
    private bool isConfigured = false;
    public CancellationToken CancellationToken { get; set; }
    public IEngine Engine { get; set; }
    public Task Init()
    {
      string fileName = typeof(UsedImagesChecker) + ".json";
      if (File.Exists(fileName))
      {
        try
        {
          using (StreamReader reader = new StreamReader(fileName))
          {
            string json = reader.ReadToEnd();
            config = JsonConvert.DeserializeObject<UsedImagesCheckerConfig>(json);
            isConfigured = Directory.Exists(config.ImagesBaseDirectory);
            if ( ! isConfigured )
              Engine.Log($"Path for comparing images doesn't exist {config.ImagesBaseDirectory}. can't compare site and project images", MessageSeverity.Error);
          }
        }
        catch (Exception ex)
        {
          Engine.Log($"Error reading config file for extension {nameof(UsedImagesChecker)} : {ex.Message}", MessageSeverity.Error);
        }
      }
      else
      {
        Engine.Log($"No config file for extension {nameof(UsedImagesChecker)}", MessageSeverity.Error);
      }
      return Task<int>.FromResult(0);
    }
    public Task Process(Uri uri, List<CrawlStep> steps, HttpResponseMessage responseMessage, HtmlDocument doc)
    {
      String contentType = responseMessage.Content.Headers.ContentType.MediaType;
      bool isImage = imageTypes.Contains(contentType.ToLower());
      if (isImage && config.SitesToScan.Contains(uri.DnsSafeHost))
        requestedImages.Add(uri.ToString());
      return Task.FromResult<int>(0);
    }
    public Task Done()
    {
      if (!isConfigured)
        return Task<int>.FromResult(0);

      List<String> filesOnDisk = new List<String>();
      FindImageFiles(config.ImagesBaseDirectory, config.ImagesBaseDirectory, filesOnDisk);

      var filesInSite = requestedImages
                                      .Select(n => new Uri(n).LocalPath)
                                      .Where(n => n.StartsWith("/" + config.SitePrefixToRemove))
                                      .Select(n => n.Substring(config.SitePrefixToRemove.Length + 1))
                                      .Select(n => n.Replace(oldChar: '/', newChar: '\\'))
                                      .ToList();

      var filesNotUsedInSite = filesOnDisk.Except(filesInSite).OrderBy(f => f).ToList();

      Engine.Log($"\n*********************************************************", MessageSeverity.Success);
      Engine.Log($"Report of {nameof(UsedImagesChecker)}", MessageSeverity.Success);
      Engine.Log($"{filesInSite.Count} in site", MessageSeverity.Success);
      Engine.Log($"{filesOnDisk.Count} in project", MessageSeverity.Success);
      if (filesNotUsedInSite.Count == 0)
      {
        Engine.Log("All files in project are in site", MessageSeverity.Success);
      }
      else
      {
        var filesNotInProject = filesInSite.Except(filesOnDisk).OrderBy(f => f).ToList();
        var filesNotInProjectCaseInsentitive = filesInSite.Select(n => n.ToLower()).Except(filesOnDisk.Select(n => n.ToLower())).OrderBy(f => f).ToList();
        var filesNotUsedInSiteCaseInsentitive = filesOnDisk.Select(n => n.ToLower()).Except(filesInSite.Select(n => n.ToLower())).OrderBy(f => f).ToList();

        DisplayFiles("Files NOT in project", filesNotInProject);
        if (filesNotInProjectCaseInsentitive.Count==0)
          Engine.Log("\tNo File missing with case insensitive comparisons", MessageSeverity.Error);
        else
          DisplayFiles("Files NOT in project - case insensitive", filesNotInProjectCaseInsentitive);

        DisplayFiles("Files NOT USED in site", filesNotUsedInSite);
        if (filesNotUsedInSiteCaseInsentitive.Count == 0)
          Engine.Log("\tNo File missing with case insensitive comparisons", MessageSeverity.Error);
        DisplayFiles("Files NOT in site - case insensitive", filesNotUsedInSiteCaseInsentitive);
      }
      Engine.Log($"*********************************************************\n", MessageSeverity.Success);
      return Task<int>.FromResult(0);
    }

    private void DisplayFiles(string label, List<string> files)
    {
      if (files.Count > 0)
      {
        Engine.Log($"{label}", MessageSeverity.Error);
        Engine.Log($"{files.Count} files missing", MessageSeverity.Error);

        foreach (var file in files)
        {
          Engine.Log($"\t{file}", MessageSeverity.Error);
        }
      }
    }

    private void FindImageFiles(string imagesBaseDirectory, string directoryToScan, List<string> filesOnDisk)
    {
      filesOnDisk.AddRange(
        imageExtensions.SelectMany(
                            ext => Directory.GetFiles(directoryToScan, "*." + ext)
                                            .Select(file => file.Substring(imagesBaseDirectory.Length))
        )
      );
      foreach (var dir in Directory.EnumerateDirectories(directoryToScan))
      {
        FindImageFiles(imagesBaseDirectory, Path.Combine(directoryToScan, dir), filesOnDisk);
      }
    }
  }
}