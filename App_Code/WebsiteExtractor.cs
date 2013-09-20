using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Xml.Linq;

/// <summary>
/// Summary description for WebsiteExtractor
/// </summary>
public static class WebsiteExtractor
{
    const string siteConfig = @"/App_Code/Site.xml";

    public static void ExtractSite()
    {
        var siteConfig = GetConfigFile();
        var pageElements = siteConfig.Descendants("page");

        if (pageElements.Any())
        {
            //Create export folder
            long folderTimeStamp = DateTime.Now.Ticks;
            string exportFolderPath = HttpContext.Current.Server.MapPath(@"/Export" + "/" + folderTimeStamp + "/");
            Directory.CreateDirectory(exportFolderPath);

            //Create our html pages
            using (WebClient client = new WebClient()) // WebClient class inherits IDisposable
            {
                Uri uri = new Uri(HttpContext.Current.Request.Url.ToString());
                var sitePath = uri.GetLeftPart(UriPartial.Authority) + "/";

                foreach (var page in pageElements)
                {
                    string pagePath = sitePath + page.Value;
                    string newFileName = page.Value.Replace("cshtml", "html");

                    //Load our web page and replace all references to cshtml with html for links
                    string pageHtml = client.DownloadString(pagePath).Replace("cshtml", "html");

                    //Convert our html string to a doc to amend
                    HtmlAgilityPack.HtmlDocument htmlDoc = new HtmlAgilityPack.HtmlDocument();
                    htmlDoc.LoadHtml(pageHtml);
                    var currentPageLinks = htmlDoc.DocumentNode.SelectNodes("//nav/descendant::a[@href='" + newFileName + "']");

                    if (currentPageLinks != null)
                    {
                        foreach (var link in currentPageLinks)
                        {
                            //Add selected class to parent node
                            link.ParentNode.Attributes.Add("class", "selected");
                        }
                    }

                    File.WriteAllText(exportFolderPath + newFileName, htmlDoc.DocumentNode.OuterHtml);
                }
            }

            //Copy all our folders we need
            var folders = siteConfig.Descendants("folder");
            foreach (var folder in folders)
            {
                string folderName = folder.Value;
                string folderPath = HttpContext.Current.Server.MapPath(@"/" + folderName);
                
                //Create folder path
                Directory.CreateDirectory(exportFolderPath + folderName);

                //Create all sub folders
                foreach (string dirPath in Directory.GetDirectories(folderPath, "*",
                    SearchOption.AllDirectories))
                    Directory.CreateDirectory(dirPath.Replace(folderPath, exportFolderPath + folderName));

                //Copy all the files
                foreach (string newPath in Directory.GetFiles(folderPath, "*.*",
                    SearchOption.AllDirectories))
                    File.Copy(newPath, newPath.Replace(folderPath, exportFolderPath + folderName));
            }
        }
    }


    public static Dictionary<string, string> GetPages()
    {
        var file = XDocument.Load(HttpContext.Current.Server.MapPath(siteConfig));
        var pageElements = file.Descendants("page");

        var pages = new Dictionary<string, string>();

        foreach (var page in pageElements)
        {
            pages.Add(page.Attribute("Name").Value, page.Value);
        }

        return pages;
    }

    public static XDocument GetConfigFile()
    {
        return XDocument.Load(HttpContext.Current.Server.MapPath(siteConfig));
    }
}