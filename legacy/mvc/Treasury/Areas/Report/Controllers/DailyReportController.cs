using CSD.Common.Utility;
using CSD.Treasury.UI.Controllers;
using CSD.Treasury.UI.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;

namespace CSD.Treasury.UI.Areas.Report.Controllers
{
    [Authorize(Roles = "Admin,Manager,Report,ReportInApp")]
    public class DailyReportController : BaseController
    {
        public ActionResult SearchViewdailyreport()
        {
            return View();
        }
        public ActionResult chequesLs(string Date)
        {
            //RedirectToAction("List", "ExpiredCheckPayable", new {  Date =Date});
            return Redirect("Report/ExpiredCheckPayable/List?Date="+Date);
        }
      

    }
}