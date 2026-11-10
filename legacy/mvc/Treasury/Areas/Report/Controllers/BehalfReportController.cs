using CSD.Treasury.Service.ProceduresDB;
using CSD.Treasury.UI.Areas.Report.Models;
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
    public class BehalfReportController : BaseController
    {
        // GET: Report/BehlalfsReport
        public ActionResult SearchView()
        {
            return View();
        }
        public ActionResult Index(BehalfModel SearchObj)
        {
            return View(SearchObj);
        }
        public ActionResult List(BehalfModel SearchObj)
        {
            StiReport report = new StiReport();
            Icls_SPsClient ProceduresDB = new Icls_SPsClient();
            string Path = Server.MapPath("~/Report/GetAllPaymentReceiptByBehalf.mrt");
            report.Load(Path);

            var ls =ProceduresDB.GetAllPaymentReceiptByBehalf(SearchObj.FromDate.Replace(@"/", ""), SearchObj.ToDate.Replace(@"/", ""),SearchObj.ExchangeTypeId).ToList();
            if (SearchObj.ExchangeTypeId == 1)
            {
                ls = ls.Where(d => d.ExchangeTypeID == 1).ToList();
            }
            else
            {
                ls = ls.Where(d => d.ExchangeTypeID != 1).ToList();
            }
            if (!string.IsNullOrEmpty(SearchObj.BehalfDescription))
            {
                if (SearchObj.BehalfDescription.Contains('ی'))
                {
                    SearchObj.BehalfDescription = SearchObj.BehalfDescription.Replace('ی', 'ي');
                }
                if (SearchObj.BehalfDescription.Contains('ک'))
                {
                    SearchObj.BehalfDescription = SearchObj.BehalfDescription.Replace('ک', 'ك');
                }
                ls = ls.Where(d => d.BehalfDescription.Contains(SearchObj.BehalfDescription)).ToList();
            }

            if(SearchObj.type==0)
            {
                report.RegBusinessObject("Receipt", ls.Where(d => d.RecordNo == 2));
                report.RegBusinessObject("Payment", ls.Where(d => d.RecordNo == 3));
            }
            else if(SearchObj.type==1)
            {
                report.RegBusinessObject("Receipt", ls.Where(d => d.RecordNo == 2));
            }
            else if(SearchObj.type==2)
            {
                report.RegBusinessObject("Payment", ls.Where(d => d.RecordNo == 3));
            }
   
            report.Dictionary.Variables["CompanyName"].Value = ConfigurationManager.AppSettings["CompanyTitle"];
            report.Dictionary.Variables["DateNow"].Value = Utility.GetPersianDate(Utility.ToPersianDate(DateTime.Now));
            report.Dictionary.Variables["DateFrom"].Value = SearchObj.FromDate;
            report.Dictionary.Variables["DateTO"].Value = SearchObj.ToDate;
            report.Dictionary.Databases.Clear();
            report.Dictionary.SynchronizeBusinessObjects();
            return StiMvcViewer.GetReportSnapshotResult(HttpContext, report);
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