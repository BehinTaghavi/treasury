using CSD.Treasury.Service.ProceduresDB;
using CSD.Treasury.UI.Areas.Report.Models;
using CSD.Treasury.UI.Models;
using Stimulsoft.Report;
using Stimulsoft.Report.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using CSD.Common.Utility;
using CSD.Treasury.UI.Controllers;
using System.Configuration;
using CSD.Treasury.DataModel;
using System.Globalization;
using CSD.Treasury.Service.BankService;
using CSD.Treasury.Service.BasicBanksService;

namespace CSD.Treasury.UI.Areas.Report.Controllers
{
    [Authorize(Roles = "Admin,Manager,Report,ReportInApp")]
    public class ExpiredCheckReceivableController : BaseController
    {
        #region Search panel
        public ActionResult Search()
        {
            //لیستی از انواع پرداخت ها
            ViewBag.Receipt_Pay_TypeDetailLs = Utility.GetReceipt_Pay_TypeDetailLs().Where(d => d.Receipt_Pay_TypeID == int.Parse(ConfigurationManager.AppSettings["Receipt_Type_ID"])).ToList();
            //لیستی از نوع ارز ها
            ViewBag.ExchangeLs = Utility.GetExchangeLs();
            //لیستی از سال مالی 
            ViewBag.FiscalYearls = Utility.GetFiscalYear();
            //لیستی از نوع ذینفع 
            var dc = new TreasuryEntities();
            var unique_account_type = dc.v_unique_account_type.ToList();
            unique_account_type.Add(new v_unique_account_type { Code = 0, Description = Resourse.Resources.Others });
            ViewBag.unique_account_type = unique_account_type;
            //گروه بابت ها
            ViewBag.BehalfsGroupLs = Utility.GetBehalfsGroupLs().Where(d => d.Receipt_Pay_TypeID == int.Parse(ConfigurationManager.AppSettings["Receipt_Type_ID"]));


            //بدست آوردن سال مالی جاری
            PersianCalendar pc = new PersianCalendar();
            ViewBag.FiscalYear = pc.GetYear(DateTime.Now);

            //لیستی از بانک و شماره حساب
            //Icls_BankClient BankCLient = new Icls_BankClient();
            //Icls_BasicBanksClient Basikclient = new Icls_BasicBanksClient();
            //var BankLS = BankCLient.SelectBy_DynamicWhereClause("IsActive=" + true, 1, 1000, "BankID", "int", false, out TotalRow).ToList();

            ViewBag.BankLS = Utility.GetBanks();
            return PartialView();
        }
        #endregion
     
        public ActionResult ALLExpiredCheckReceivable(SearchModelExpiredReceivableCheque obj)
        {
            //var Banks = Utility.GetBanks();
            //var BankLs = Utility.GetBasicBanks();
            //ViewBag.Banks = BankLs.Where(d => Banks.Select(t => t.BasicBankID).Contains(d.BasicBankId)).ToList();
            ////لیستی از نوع ذینفع 
            //var dc = new TreasuryEntities();
            //var unique_account_type = dc.v_unique_account_type.ToList();
            //unique_account_type.Add(new v_unique_account_type { Code = 0, Description = Resourse.Resources.Others });
            //ViewBag.unique_account_type = unique_account_type;
            return View(obj);
        }

        public ActionResult ShowALLExpired(SearchModelExpiredReceivableCheque obj)
        {
            int pageNum = obj.PageIndex;
            var UserId = User.Id;
            Icls_SPsClient spsClient = new Icls_SPsClient();
            string DateNow = Utility.ToPersianDate(DateTime.Now);
            var query = spsClient.GetRecieptHeaderAndDetail_ByRecieptTypeId(19).Where(d => d.PlaningTypeID == 1|| d.PlaningTypeID == 2).OrderByDescending(d => d.DueDate).ToList();
            if (obj.ReceiptDetailId != 0)
            {
                query = query.Where(d => d.ReceiptDetailId == obj.ReceiptDetailId).ToList();
            }
            if (!string.IsNullOrEmpty(obj.DueDateFrom) && !string.IsNullOrEmpty(obj.DueDateTo))
            {
                query = query.Where(d => d.DueDate.CompareTo(obj.DueDateFrom) >= 0 && d.DueDate.CompareTo(obj.DueDateTo) <= 0).ToList();
            }
            if (!string.IsNullOrEmpty(obj.ReceiptDateFrom) && !string.IsNullOrEmpty(obj.ReceeiptDateTo))
            {
                query = query.Where(d => d.ReceiptDate.CompareTo(obj.ReceiptDateFrom.Replace("/", String.Empty)) >= 0 && d.ReceiptDate.CompareTo(obj.ReceeiptDateTo.Replace("/", String.Empty)) <= 0).ToList();
            }

            if (!string.IsNullOrEmpty(obj.StatusUnique_Account_No_One))
            {
                query = query.Where(d => d.StatusUnique_Account_No_One == obj.StatusUnique_Account_No_One).ToList();
            }
            if (!string.IsNullOrEmpty(obj.ChequeNo))
            {
                query = query.Where(d => d.Cheque_Number == obj.ChequeNo).ToList();
            }

            if (obj.BehalfGroupID != 0)
            {
                query = query.Where(d => d.BehalfGroupID == obj.BehalfGroupID).ToList();
            }
            if (obj.exchangeTypeId != 0)
            {
                query = query.Where(d => d.ExchangeTypeID == obj.exchangeTypeId).ToList();
            }
            if (obj.PayAmount != 0)
            {
                query = query.Where(d => d.EquivalencePrice == obj.PayAmount).ToList();
            }
            var totalRows = query.Count();

            obj.TotalPageCount = (int)Math.Ceiling(totalRows / (double)obj.PageSize);
            obj.CurrentPage = pageNum;
            obj.List = query.Skip((obj.PageIndex) * obj.PageSize).Take(obj.PageSize).ToList();
            obj.CurrentPage = obj.PageIndex;
            obj.TotalItemCount = totalRows;
            return PartialView(obj);
        }

        [HttpPost]
        public ActionResult ExpiredCheckListReport (SearchModelExpiredReceivableCheque obj)
        {
            return View(obj);
        }
        public ActionResult LoadExpiredCheckReceivable(SearchModelExpiredReceivableCheque obj)
        {
            string CurrentDate = Utility.ToPersianDate(DateTime.Now);
            StiReport report = new StiReport();
            Icls_SPsClient spsClient = new Icls_SPsClient();
            var query = spsClient.GetRecieptHeaderAndDetail_ByRecieptTypeId(19).Where(d => d.PlaningTypeID == 1 || d.PlaningTypeID == 2).OrderByDescending(d => d.DueDate).ToList();
            if (obj.ReceiptDetailId != 0)
            {
                query = query.Where(d => d.ReceiptDetailId == obj.ReceiptDetailId).ToList();
            }
            if (!string.IsNullOrEmpty(obj.DueDateFrom) && !string.IsNullOrEmpty(obj.DueDateTo))
            {
                query = query.Where(d => d.DueDate.CompareTo(obj.DueDateFrom) >= 0 && d.DueDate.CompareTo(obj.DueDateTo) <= 0).ToList();
            }
            if (!string.IsNullOrEmpty(obj.ReceiptDateFrom) && !string.IsNullOrEmpty(obj.ReceeiptDateTo))
            {
                query = query.Where(d => d.ReceiptDate.CompareTo(obj.ReceiptDateFrom.Replace("/", String.Empty)) >= 0 && d.ReceiptDate.CompareTo(obj.ReceeiptDateTo.Replace("/", String.Empty)) <= 0).ToList();
            }

            if (!string.IsNullOrEmpty(obj.StatusUnique_Account_No_One))
            {
                query = query.Where(d => d.StatusUnique_Account_No_One == obj.StatusUnique_Account_No_One).ToList();
            }
            if (!string.IsNullOrEmpty(obj.ChequeNo))
            {
                query = query.Where(d => d.Cheque_Number == obj.ChequeNo).ToList();
            }

            if (obj.BehalfGroupID != 0)
            {
                query = query.Where(d => d.BehalfGroupID == obj.BehalfGroupID).ToList();
            }
            if (obj.exchangeTypeId != 0)
            {
                query = query.Where(d => d.ExchangeTypeID == obj.exchangeTypeId).ToList();
            }
            if (obj.PayAmount != 0)
            {
                query = query.Where(d => d.EquivalencePrice == obj.PayAmount).ToList();
            }
            var totalRows = query.Count();

            string Path = Server.MapPath("~/Report/AllExpiredCheckReceivable.mrt");
            report.Load(Path);
            report.RegBusinessObject("ListExpiredCheckReceivable", query.ToList());
            report.Dictionary.SynchronizeBusinessObjects();
            report.Dictionary.Variables["CompanyName"].Value = ConfigurationManager.AppSettings["CompanyTitle"];
            report.Dictionary.Variables["DateNow"].Value = CurrentDate;

            return StiMvcViewer.GetReportSnapshotResult(HttpContext, report);
        }

        // GET: Report/ExpiredCheckReceivable
        public ActionResult SearchView()
        {
            return View();
        }
        public ActionResult Index(string Date)
        {
            // var DateNow = DateTime.Now;
            //var PersianDateNow = Utility.ToPersianDate(DateNow);        

            ViewBag.PersianDate = Date;
            return View();
        }
        public ActionResult List(string Date)
        {
            StiReport report = new StiReport();
            Icls_SPsClient ProcedureDB = new Icls_SPsClient();
            string DateNow = Utility.ToPersianDate(DateTime.Now);


            var day = DateUtils.Jalali2Gregorian(Date, "/", "");
            

            var thisWeekStart = day.AddDays(-(int)day.DayOfWeek-1);
            var thisWeekEnd = thisWeekStart.AddDays(7).AddSeconds(-1);
            var persianDayOfWeek = Utility.ToPersianDate(thisWeekEnd);
            var PersianDayOfmonth="";
            var a = Date.Substring(5, 2);
            if (Date.Substring(5, 2)=="01"|| Date.Substring(5, 2) == "02" || Date.Substring(5, 2) == "03" ||
                Date.Substring(5, 2) == "04" || Date.Substring(5, 2) == "05"|| Date.Substring(5, 2) == "06") {
                PersianDayOfmonth = Date.Substring(0, 7).Replace("/", string.Empty) + "31";
            }
            else
            {
                PersianDayOfmonth = Date.Substring(0, 7).Replace("/", string.Empty) + "30";
            }
            

            var ReceiptLs = ProcedureDB.Sp_GetStatusOfReceivableCheques("18,19", Date.Replace("/",string.Empty), persianDayOfWeek, PersianDayOfmonth).Where(d => (d.PlaningTypeID == 1 || d.PlaningTypeID == 2)).ToList();

            string Path = Server.MapPath("~/Report/ExpiredCheckReceivableReport.mrt");
            report.Load(Path);
            report.Dictionary.Variables["CompanyName"].Value = ConfigurationManager.AppSettings["CompanyTitle"];
            report.Dictionary.Variables["DateNow"].Value =Utility.GetPersianDate( DateNow);
            report.RegBusinessObject("RecieptCheque", ReceiptLs);
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