using ExCSS;
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
    private static String[] imageTypes = { "image/jpeg", "image/jpg", "image/png", "image/gif", "image/tiff" };
    private static String[] imageExtensions = { "jpeg", "jpg", "png", "gif", "tiff" };
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
            if (!isConfigured)
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
    public Task ProcessHtml(Uri uri, List<CrawlStep> steps, HttpResponseMessage responseMessage, HtmlDocument doc)
    {
      return Task.FromResult<int>(0);
    }
    public Task Done()
    {
      if (!isConfigured)
        return Task<int>.FromResult(0);

      List<String> filesOnDisk = new List<String>();
      FindImageFiles(config.ImagesBaseDirectory, config.ImagesBaseDirectory, filesOnDisk);
      List<String> filesInSite = null;
        filesInSite = Engine.ScanResults
                                  .Where(sr => IsImageInteresting(uri: sr.Key, scanResult: sr.Value))
                                  .Select(sr => sr.Key.LocalPath)
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

        if (filesNotInProjectCaseInsentitive.Count == 0)
        {
          if (filesNotInProject.Any())
          {
            Engine.Log("No File missing in project with case insensitive comparisons, but ...", MessageSeverity.Error);
            DisplayFiles("Files NOT in project with case sentive comparison", filesNotInProject);
          }
        }
        else
          DisplayFiles("Files NOT in project - case insensitive", filesNotInProjectCaseInsentitive);

        if (filesNotUsedInSiteCaseInsentitive.Count == 0)
        {
          if (filesNotUsedInSite.Any())
          {
            Engine.Log("No File not used in site with case insensitive comparisons, but ...", MessageSeverity.Error);
            DisplayFiles("Files NOT used in site with case sentive comparison", filesNotInProject);
          }
        }
        else
          DisplayFiles("Files NOT in site - case insensitive", filesNotUsedInSiteCaseInsentitive);
      }
      Engine.Log($"*********************************************************\n", MessageSeverity.Success);
      return Task<int>.FromResult(0);
    }
    private bool IsImageInteresting(Uri uri, ScanResult scanResult)
    {
      bool isImage = imageTypes.Contains(scanResult.ContentType?.ToLower());
      bool doesImageBelongToRightSite = config.SitesToScan.Contains(uri.DnsSafeHost);
      return isImage && doesImageBelongToRightSite;
    }
    private void DisplayFiles(string label, List<string> files)
    {
      if (files.Count > 0)
      {
        Engine.Log($"{files.Count} {label}", MessageSeverity.Error);
        foreach (var file in files)
        {
          Engine.Log($"\t{file}", MessageSeverity.Info);
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
    public Task ProcessCss(Uri uri, List<CrawlStep> steps, HttpResponseMessage responseMessage, StyleSheet styleSheet)
    {
      return Task.FromResult<int>(0);
    }
    public Task ProcessOther(Uri uri, List<CrawlStep> steps, HttpResponseMessage responseMessage)
    {
      return Task.FromResult<int>(0);
    }
  }
}