using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Drawing;
using System.Drawing.Printing;
using System.Configuration;

namespace CSD.Treasury.UI.Areas.Management.Controllers
{
    public class PrintController : Controller
    {

        // GET: Management/Print
        public ActionResult Index()
        {
            var doc = new PrintDocument();
            doc.PrinterSettings.PrinterName = "\\\\" + ConfigurationManager.AppSettings["ClientName"] + "\\" + ConfigurationManager.AppSettings["PrinterName"];
            doc.PrintPage += new PrintPageEventHandler(ProvideContent);
            doc.Print();
            return View();
        }

        public void ProvideContent(object sender, PrintPageEventArgs e)
        {
            e.Graphics.DrawString(
              "Hello world",
              new Font("Arial", 12),
              Brushes.Black,
              e.MarginBounds.Left,
              e.MarginBounds.Top);
        }

    }
}