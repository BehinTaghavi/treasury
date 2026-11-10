using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Xml.Linq;

namespace CSD.MVC.Helpers
{
    public static class RSSReader
    {
        public static HtmlString GetHtml(string url, int numberOfItems = 5,
                bool showDescription = true, bool showDateCreated = true, int width = 300)
        {
            TagBuilder divTag = new TagBuilder("div");
            TagBuilder pTag = new TagBuilder("p");
            TagBuilder anchor = new TagBuilder("a");

            var doc = XDocument.Load(url);
            XNamespace a10 = doc.Root.GetNamespaceOfPrefix("a10");
            var items = doc.Descendants("item").Select(feed => new
            {
                Title = feed.Element("title"),
                Link = a10 != null ? feed.Element(a10 + "link").Attribute("href").Value : feed.Element("link").Value,
                Date = feed.Element("pubDate"),
                Description = feed.Element("description")
            }).Take(numberOfItems);

            divTag.MergeAttribute("id", "rssDiv");
            divTag.MergeAttribute("style", "width:" + width + "px");
            foreach (var item in items)
            {
                anchor.InnerHtml = item.Title.Value;
                anchor.MergeAttribute("href", item.Link);
                pTag.InnerHtml = anchor.ToString();
                divTag.InnerHtml += pTag.ToString();
                if (showDateCreated)
                {
                    TagBuilder pTag2 = new TagBuilder("p");
                    DateTime dateValue;
                    if (DateTime.TryParse(item.Date.Value, out dateValue))
                    {
                        pTag2.InnerHtml = dateValue.ToString("G");
                    }
                    divTag.InnerHtml += pTag2.ToString();
                }
                if (showDescription)
                {
                    TagBuilder pTag3 = new TagBuilder("p");
                    pTag3.InnerHtml = item.Description.Value;
                    divTag.InnerHtml += pTag3.ToString();
                }
            }
            return new HtmlString(divTag.ToString());
        }
    }
}