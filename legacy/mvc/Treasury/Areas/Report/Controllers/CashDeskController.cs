using CSD.Common.Utility;
using CSD.Treasury.Service.ProceduresDB;
using CSD.Treasury.UI.Controllers;
using CSD.Treasury.UI.Models;
using Stimulsoft.Report;
using Stimulsoft.Report.Mvc;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace CSD.Treasury.UI.Areas.Report.Controllers
{
    [Authorize(Roles = "Admin,Manager,Report,ReportInApp")]
    public class CashDeskController : BaseController
    {
        // GET: Report/CashDesk
        public ActionResult SearchView()
        {
            return View();
        }
        public ActionResult Index(string Date)
        {
            ViewBag.PersianDate = Date;
            return View();
        }
        public ActionResult List(string Date)
        {
            Icls_SPsClient ProceduresDB = new Icls_SPsClient();
            try
            {
                StiReport report = new StiReport();
                Icls_SPsClient ProcedureDB = new Icls_SPsClient();
                string DateNow = Utility.ToPersianDate(DateTime.Now);

                var day = DateUtils.Jalali2Gregorian(Date, "/", "");
                var thisWeekStart = day.AddDays(-(int)day.DayOfWeek - 1);
                var thisWeekEnd = thisWeekStart.AddDays(7).AddSeconds(-1);
                var persianDayOfWeek = Utility.ToPersianDate(thisWeekEnd);
                var PersianDayOfmonth = "";
                var a = Date.Substring(5, 2);
                if (Date.Substring(5, 2) == "01" || Date.Substring(5, 2) == "02" || Date.Substring(5, 2) == "03" ||
                    Date.Substring(5, 2) == "04" || Date.Substring(5, 2) == "05" || Date.Substring(5, 2) == "06")
                {
                    PersianDayOfmonth = Date.Substring(0, 7).Replace("/", string.Empty) + "31";
                }
                else
                {
                    PersianDayOfmonth = Date.Substring(0, 7).Replace("/", string.Empty) + "30";
                }
                //آخرین سلکت
                //cast 1 as tinyint
                var lst = ProcedureDB.GetCashDeskBalance(Date.Replace("/", string.Empty));
                string Path = Server.MapPath("~/Report/CashDeskReport.mrt");
                report.Load(Path);
                report.Dictionary.Variables["CompanyName"].Value = ConfigurationManager.AppSettings["CompanyTitle"];
                report.Dictionary.Variables["DateNow"].Value = Utility.GetPersianDate(DateNow);
                report.Dictionary.Variables["ReportDate"].Value = Date;
                report.RegBusinessObject("CashDeskLs", lst);
                report.Dictionary.SynchronizeBusinessObjects();


                return StiMvcViewer.GetReportSnapshotResult(HttpContext, report);
            }
            catch (Exception e)
            {

                throw;
            }
        }
            //ایجاد پرینت
        public ActionResult PrintReport()
        {
            return StiMvcViewer.PrintReportResult();

        }
        //ایجاد خروجی
        public ActionResult ExportReport()
        {
            return StiMvcViewer.ExportReportResult();
        }

        public ActionResult ViewerEvent()
        {
            return StiMvcViewer.ViewerEventResult();
        }

        public ActionResult Interaction()
        {
            return StiMvcViewer.InteractionResult();
        }
    }
}