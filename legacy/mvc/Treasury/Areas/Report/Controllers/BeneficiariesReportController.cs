using CSD.Treasury.DataModel;
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
    public class BeneficiariesReportController : BaseController
    {
        #region BehalfGroupListprint
        public ActionResult Index()
        {
            return View();
        }
        public ActionResult List()
        {
            var db = new TreasuryEntities();
            StiReport report = new StiReport();        
            string Path = Server.MapPath("~/Report/BeneficiariesLs.mrt");
            report.Load(Path);
            var ls = db.GetBeneficiariesDetails().ToList();
            report.Dictionary.Variables["DateNow"].Value = Utility.GetPersianDate(Utility.ToPersianDate(DateTime.Now));
            report.Dictionary.Variables["CompanyName"].Value = ConfigurationManager.AppSettings["CompanyTitle"];
            report.RegBusinessObject("BeneficiariesList", ls);
            report.Dictionary.Databases.Clear();
            report.Dictionary.SynchronizeBusinessObjects();
            return StiMvcViewer.GetReportSnapshotResult(HttpContext, report);
        }
        #endregion
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