using CSD.Treasury.Service.ProceduresDB;
using CSD.Treasury.UI.Controllers;
using CSD.Treasury.UI.Models;
using Stimulsoft.Report;
using Stimulsoft.Report.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace CSD.Treasury.UI.Areas.Report.Controllers
{
    [Authorize(Roles = "Admin,Manager,Report,ReportInApp")]
    public class VocherController : BaseController
    {
        // GET: Report/Vocher
        public ActionResult Index()
        {
            return View();
        }
        public ActionResult ShowVocher(int RequestId)
        {
            return View();
        }
        public ActionResult LoadVocher(int RequestId)
        {
            StiReport report = new StiReport();
            Icls_SPsClient spsClient = new Icls_SPsClient();
            string Path = Server.MapPath("~/Report/VocherReport.mrt");
            report.Load(Path);

            report.Dictionary.Variables["DateNow"].Value = Utility.DateStructure(Utility.ToPersianDate(DateTime.Now));
            var ls = spsClient.Sp_CREATE_VOUCHER_PAYMENTS(false, RequestId, User.Id, false, true);

            foreach (var item in ls)
            {
                if (!string.IsNullOrEmpty(item.Date_Field))
                {
                    item.Date_Field = Utility.DateStructure(item.Date_Field);
                }
                if (!string.IsNullOrEmpty(item.Unique_Account_No))
                {
                    item.Unique_Account_No_Title = Utility.GetUniqueAccount(item.Unique_Account_No) + "/" + item.Unique_Account_No;
                }
                if (!string.IsNullOrEmpty(item.Unique_Account_No_two))
                {
                    item.Unique_Account_No = item.Unique_Account_No.Trim();
                    item.Unique_Account_No_two_Title = Utility.GetUniqueAccount(item.Unique_Account_No_two) + "/" + item.Unique_Account_No_two;
                }
                if (!string.IsNullOrEmpty(item.Account_No))
                {
                    item.Account_No = item.Account_No.Trim();
                    item.Account_Title = Utility.Getaccounts(item.Account_No) + "/" + item.Account_No;
                }
                item.Credit = decimal.Parse(Math.Truncate(item.Credit).ToString("#,##0"));
                item.Debit = decimal.Parse(Math.Truncate(item.Debit).ToString("#,##0"));
            }
            report.RegBusinessObject("VocherLS", ls);
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