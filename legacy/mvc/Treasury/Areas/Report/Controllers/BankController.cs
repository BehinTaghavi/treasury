using CSD.Treasury.UI.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using CSD.Treasury.Service.ProceduresDB;
using Stimulsoft.Report;
using Stimulsoft.Report.Mvc;
using CSD.Treasury.UI.Models;
using System.Configuration;
using CSD.Treasury.Service.BasicBanksService;
using CSD.Treasury.Service.BankService;
using CSD.Treasury.UI.Areas.Report.Models;
using CSD.Treasury.DataModel;

namespace CSD.Treasury.UI.Areas.Report.Controllers
{
    [Authorize(Roles = "Admin,Manager,Report,ReportInApp")]
    public class BankController : BaseController
    {
    
        // GET: Report/Bank
        public ActionResult SearchView()
        {
            int TotalRow;
            //Icls_BankClient BankCLient = new Icls_BankClient();
            Icls_BasicBanksClient Basikclient = new Icls_BasicBanksClient();
            var db = new TreasuryEntities();
            //var BankLS = BankCLient.SelectBy_DynamicWhereClause("IsActive=" + true, 1, 1000, "BankID", "int", false, out TotalRow);
            var BankLS = db.Banks.Where(d => d.IsActive == true);
            var BasicBankLS = Basikclient.SelectAll(1, 1000, "BasicBankId", "int", false, out TotalRow).Where(d => BankLS.Select(i => i.BasicBankID).Contains(d.BasicBankId));

            ViewBag.BasicBank = BasicBankLS;
            ViewBag.Bankls = BankLS;



            //List<BasicBankAndBank> CustomBankLS = new List<BasicBankAndBank>();

            //foreach (var item in BasicBankLS)
            //{
            //    BasicBankAndBank obj = new BasicBankAndBank();
            //    obj.BasicBankDesc = item.BasicBankDescription;
            //    obj.BankLS = BankCLient.SelectBy_DynamicWhereClause("BasicBankID=" + item.BasicBankId + "and IsActive=" + true, 1, 10 * 10, "BankID", "int", false, out TotalRow).ToList();
            //    CustomBankLS.Add(obj);
            //}
            //ViewBag.CustomBankLS = CustomBankLS;
            return View();
        }
        public ActionResult Index(GardeshHesabSearchModel SearchObj)
        {
           
            return View();
        }
 
        public ActionResult List(GardeshHesabSearchModel SearchObj)
        {
            //Date = Date.Replace("/", string.Empty);
            StiReport report = new StiReport();
            Icls_SPsClient ProceduresDB = new Icls_SPsClient();
          
            var ls = ProceduresDB.Sp_GetBankBalance(SearchObj.FromDate.Replace(@"/", ""), SearchObj.ToDate.Replace(@"/", ""), SearchObj.BankId,1, false);
            string Path = Server.MapPath("~/Report/BankBalanceReport.mrt");
 
            report.Load(Path);
            report.Dictionary.Variables["CompanyName"].Value = ConfigurationManager.AppSettings["CompanyTitle"];
            report.Dictionary.Variables["DateNow"].Value = Utility.GetPersianDate(Utility.ToPersianDate(DateTime.Now));          
            report.Dictionary.Variables["DateFrom"].Value = SearchObj.FromDate;
            report.Dictionary.Variables["DateTO"].Value = SearchObj.ToDate;
            report.RegBusinessObject("BankBalanceLS", ls);
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