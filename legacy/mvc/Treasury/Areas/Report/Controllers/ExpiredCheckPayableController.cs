using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using CSD.Treasury.Service.ProceduresDB;
using Stimulsoft.Report;
using Stimulsoft.Report.Mvc;
using CSD.Treasury.UI.Models;
using CSD.Treasury.UI.Areas.Report.Models;
using CSD.Treasury.UI.Controllers;
using CSD.Common.Utility;
using System.Configuration;
using System.Text;
using CSD.Treasury.DataModel;
using System.Globalization;
using CSD.Treasury.Service.BankService;
using CSD.Treasury.Service.BasicBanksService;

namespace CSD.Treasury.UI.Areas.Report.Controllers
{
    [Authorize(Roles = "Admin,Manager,Report,ReportInApp")]
    public class ExpiredCheckPayableController : BaseController
    {
        #region checkReports
        /// <summary>
        /// چکهای پرداختنی
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
     
        public ActionResult ExpiredCheckPayableReport(SearchModelExpiredCheque obj)
        {
            return View(obj);
        }
        public ActionResult LoadExpiredCheckPayable(SearchModelExpiredCheque obj)
        {
            string CurrentDate = Utility.ToPersianDate(DateTime.Now);
            StiReport report = new StiReport();
            Icls_SPsClient spsClient = new Icls_SPsClient();
            var query = spsClient.Get_Maturity_Cheques_Report("1").ToList();

            if (obj.RequsetId != 0)
            {
                query = query.Where(d => d.RequestID == obj.RequsetId).ToList();
            }
            if (!string.IsNullOrEmpty(obj.DueDateFrom) && !string.IsNullOrEmpty(obj.DueDateTo))
            {
                query = query.Where(d => d.DueDate.CompareTo(obj.DueDateFrom) >= 0 && d.DueDate.CompareTo(obj.DueDateTo) <= 0).ToList();
            }
            if (!string.IsNullOrEmpty(obj.RequestDateFrom) && !string.IsNullOrEmpty(obj.RequestDateTo))
            {
                query = query.Where(d => d.RequestDate.CompareTo(obj.RequestDateFrom.Replace("/", String.Empty)) >= 0 && d.RequestDate.CompareTo(obj.RequestDateTo.Replace("/", String.Empty)) <= 0).ToList();
            }

            if (!string.IsNullOrEmpty(obj.Unique_Account_No))
            {
                query = query.Where(d => d.Unique_Account_No == obj.Unique_Account_No).ToList();
            }
            if (obj.ChequeNo != 0)
            {
                query = query.Where(d => d.ChequeNo == obj.ChequeNo).ToList();
            }
            //if (obj.ExchangeTypeID != 0)
            //{
            //    query = query.Where(d => d.ExchangeTypeID == obj.ExchangeTypeID).ToList();
            //}
            if (obj.BehalfGroupID != 0)
            {
                query = query.Where(d => d.BehalfGroupID == obj.BehalfGroupID).ToList();
            }
            if (obj.PayAmount != 0)
            {
                query = query.Where(d => d.PayAmount == obj.PayAmount).ToList();
            }
            if (obj.BankId != 0)
            {
               // query = query.Where(d => d.Bank_AccountNumber == obj.BankId).ToList();
            }
            string Path = Server.MapPath("~/Report/AllExpiredCheckPayable.mrt");
            report.Load(Path);
            report.RegBusinessObject("ListExpiredCheckPayable", query.ToList());
            report.Dictionary.SynchronizeBusinessObjects();
            report.Dictionary.Variables["CompanyName"].Value = ConfigurationManager.AppSettings["CompanyTitle"];
            report.Dictionary.Variables["DateNow"].Value = CurrentDate;

            return StiMvcViewer.GetReportSnapshotResult(HttpContext, report);
        }

        #endregion
        #region Search panel
        public ActionResult Search()
        {
            //لیستی از انواع پرداخت ها
            ViewBag.Receipt_Pay_TypeDetailLs = Utility.GetReceipt_Pay_TypeDetailLs().Where(d => d.Receipt_Pay_TypeID == int.Parse(ConfigurationManager.AppSettings["Payment_Type_ID"])).ToList();
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
            ViewBag.BehalfsGroupLs = Utility.GetBehalfsGroupLs().Where(d => d.Receipt_Pay_TypeID == int.Parse(ConfigurationManager.AppSettings["Payment_Type_ID"]));


            //بدست آوردن سال مالی جاری
            PersianCalendar pc = new PersianCalendar();
            ViewBag.FiscalYear = pc.GetYear(DateTime.Now);

            //لیستی از بانک و شماره حساب
            int TotalRow;
            //Icls_BankClient BankCLient = new Icls_BankClient();
            Icls_BasicBanksClient Basikclient = new Icls_BasicBanksClient();
            //var BankLS = BankCLient.SelectBy_DynamicWhereClause("IsActive=" + true, 1, 1000, "BankID", "int", false, out TotalRow).ToList();

            ViewBag.BankLS = Utility.GetBanks();
            return PartialView();
        }
        #endregion
        public ActionResult ALLExpiredCheckPayable(SearchModelExpiredCheque obj)
        {
            var Banks = Utility.GetBanks();
            var BankLs = Utility.GetBasicBanks();
            ViewBag.Banks = BankLs.Where(d => Banks.Select(t => t.BasicBankID).Contains(d.BasicBankId)).ToList();
            //لیستی از نوع ذینفع 
            var dc = new TreasuryEntities();
            var unique_account_type = dc.v_unique_account_type.ToList();
            unique_account_type.Add(new v_unique_account_type { Code = 0, Description = Resourse.Resources.Others });
            ViewBag.unique_account_type = unique_account_type;
            return View(obj);
        }
        public ActionResult ShowALLExpired(SearchModelExpiredCheque obj)
        {
            //if (true)
            //{
            //     return   RedirectToAction("ExpiredCheckPayableReport","ExpiredCheckPayable",obj);
            //}

            int pageNum = obj.PageIndex;
            var UserId = User.Id;

            Icls_SPsClient spsClient = new Icls_SPsClient();
            var query = spsClient.Get_Maturity_Cheques_Report("1").ToList();
           
            if (obj.RequsetId != 0)
            {
                query = query.Where(d => d.RequestID == obj.RequsetId).ToList();
            }
            if (!string.IsNullOrEmpty(obj.DueDateFrom) && !string.IsNullOrEmpty(obj.DueDateTo))
            {
                query = query.Where(d => d.DueDate.CompareTo(obj.DueDateFrom) >= 0 && d.DueDate.CompareTo(obj.DueDateTo) <= 0).ToList();
            }
            if (!string.IsNullOrEmpty(obj.RequestDateFrom) && !string.IsNullOrEmpty(obj.RequestDateTo))
            {
                query = query.Where(d => d.RequestDate.CompareTo(obj.RequestDateFrom.Replace("/", String.Empty)) >= 0 && d.RequestDate.CompareTo(obj.RequestDateTo.Replace("/", String.Empty)) <= 0).ToList();
            }
            
            if (!string.IsNullOrEmpty(obj.Unique_Account_No))
            {
                query = query.Where(d => d.Unique_Account_No == obj.Unique_Account_No).ToList();
            }
            if (obj.ChequeNo!=0)
            {
                query = query.Where(d => d.ChequeNo == obj.ChequeNo).ToList();
            }
            //if (obj.ExchangeTypeID != 0)
            //{
            //    query = query.Where(d => d.ExchangeTypeID == obj.ExchangeTypeID).ToList();
            //}
            if (obj.BehalfGroupID!=0)
            {
                query = query.Where(d => d.BehalfGroupID == obj.BehalfGroupID).ToList();
            }
            var totalRows = query.Count();

            obj.TotalPageCount = (int)Math.Ceiling(totalRows / (double)obj.PageSize);
            obj.CurrentPage = pageNum;
            obj.List = query.Skip((obj.PageIndex) * obj.PageSize).Take(obj.PageSize).ToList();
            obj.CurrentPage = obj.PageIndex;
            obj.TotalItemCount = totalRows;
            return PartialView(obj);
        }
        // GET: Report/ExpiredCheckPayable
        public ActionResult SearchView()
        {
            return View();
        }
        public ActionResult Index(string Date)
        {
            ViewBag.PersianDate = Date;
            return View();
        }
        public string GetDates(string Date)
        {
            var PersianDayOfmonth = "";
            var day = DateUtils.Jalali2Gregorian(Date, "/", "");
            var thisWeekStart = day.AddDays(-(int)day.DayOfWeek== -6 ?0 : - (int)day.DayOfWeek - 1);
            var thisWeekEnd = thisWeekStart.AddDays(7).AddSeconds(-1);
            var persianDayOfWeek = Utility.ToPersianDate(thisWeekEnd);
            if (Date.Substring(5, 2) == "01" || Date.Substring(5, 2) == "02" || Date.Substring(5, 2) == "03" ||
                    Date.Substring(5, 2) == "04" || Date.Substring(5, 2) == "05" || Date.Substring(5, 2) == "06")
            {
                PersianDayOfmonth = Date.Substring(0, 7).Replace("/", string.Empty) + "31";
            }
            else
            {
                PersianDayOfmonth = Date.Substring(0, 7).Replace("/", string.Empty) + "30";
            }
            persianDayOfWeek = persianDayOfWeek.Substring(0, 4) + "/" + persianDayOfWeek.Substring(4, 2) + "/" + persianDayOfWeek.Substring(6, 2);
            PersianDayOfmonth = PersianDayOfmonth.Substring(0, 4) + "/" + PersianDayOfmonth.Substring(4, 2) + "/" + PersianDayOfmonth.Substring(6, 2);
            var res = new StringBuilder();
            res.Append("<label class='control-label col-sm-2' >تاپایان هفته</label><label  class='control-label col-sm-2'>" + persianDayOfWeek + "</label>");
            res.Append("<label class='control-label col-sm-2'>تاپایان ماه</label><label  class='control-label col-sm-2'>" + PersianDayOfmonth + "</label>");
            return res.ToString();
        }
        public ActionResult List(string Date)
        {
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
                var lst = ProcedureDB.sp_GetStatusOfPaybleCheques(Date.Replace("/", string.Empty), persianDayOfWeek, PersianDayOfmonth);
                string Path = Server.MapPath("~/Report/ExpiredCheckPayableReport.mrt");
                report.Load(Path);
                report.Dictionary.Variables["CompanyName"].Value = ConfigurationManager.AppSettings["CompanyTitle"];
                report.Dictionary.Variables["DateNow"].Value = Utility.GetPersianDate(DateNow);
                report.RegBusinessObject("RecieptCheque", lst);
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