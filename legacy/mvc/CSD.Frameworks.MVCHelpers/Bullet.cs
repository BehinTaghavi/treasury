using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.IO;
using System.Collections;
using System.Web.Mvc;

namespace CSD.MVC.Helpers
{
    public static class BulletedListHelper
    {
        public static string BulletedList(this HtmlHelper helper, string name)
        {
            var items = helper.ViewData.Eval(name) as IEnumerable;
            if (items == null)
                throw new NullReferenceException("Cannot find " + name + " in view data");

            var writer = new HtmlTextWriter(new StringWriter());

            // Open UL
            writer.RenderBeginTag(HtmlTextWriterTag.Ul);
            foreach (var item in items)
            {
                writer.RenderBeginTag(HtmlTextWriterTag.Li);
                writer.Write(helper.Encode(item));
                writer.RenderEndTag();
                writer.WriteLine();
            }
            // Close UL
            writer.RenderEndTag();

            // Return the HTML string
            return writer.InnerWriter.ToString();
        }
    }
}