using CSD.Treasury.Service.BankService;
using CSD.Treasury.Service.ChequeFormatTypeDetailsService;
using CSD.Treasury.Service.ChequesService;
using CSD.Treasury.Service.Request_PaymentService;
using CSD.Treasury.Service.ChequeFormatTypesService;
using CSD.Treasury.UI.Controllers;
using CSD.Treasury.UI.Models;
using CSD.Treasury.Service.ProceduresDB;
using Stimulsoft.Report;
using Stimulsoft.Report.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Text;
using System.Configuration;
using CSD.Treasury.DataModel;
using System.Globalization;
using CSD.Treasury.Service.BasicBanksService;

namespace CSD.Treasury.UI.Areas.Report.Controllers
{
    [Authorize(Roles = "Admin,Manager,Report,ReportInApp")]
    public class PaymentController : BaseController
    {
        public ActionResult Index(CheckBookStatusSearch obj)
        {            
            ViewBag.GetChequeFormatTypeLs = Utility.GetFormatTypeDescLs(); 
            return View(obj);
        }

        public ActionResult List(CheckBookStatusSearch SearchObj)
        {
            
            int pageNum = SearchObj.PageIndex;
            //string WhereClause= "(Pay_TypeDetailID_source =1 or Pay_TypeDetailID_source =2 or  Pay_TypeDetailID_source =4 or Pay_TypeDetailID_source =5)";
            Icls_Request_PaymentClient RP = new Icls_Request_PaymentClient();
            var db = new TreasuryEntities();
          
            //if( !string.IsNullOrEmpty(SearchObj.BankAccountNumber))
            //{
            //    WhereClause= WhereClause  + " and Source_PaymentValue=\"" + SearchObj.BankAccountNumber + "\"";
            //}
          
           // var list = RP.SelectBy_DynamicWhereClause(WhereClause, pageNum + 1, SearchObj.PageSize, "RequestID", "dec", false, out TotalRow).ToList();


            string CurrentDate = Utility.ToPersianDate(DateTime.Now);
            //var list = RP.GetAllRequestStatus_DynamicWhereClause(CurrentDate, WhereClause, pageNum + 1, SearchObj.PageSize, "RequestID", "dec", false, out TotalRow).ToList();
            var list = db.GetAllRequestStatus(CurrentDate).Where(d=>d.Pay_TypeDetailID_source == 1 || d.Pay_TypeDetailID_source == 2 ||  d.Pay_TypeDetailID_source == 4 || d.Pay_TypeDetailID_source == 5).ToList();
            if (!string.IsNullOrEmpty(SearchObj.BankAccountNumber))
            {
                //WhereClause = WhereClause + " and Source_PaymentValue=\"" + SearchObj.BankAccountNumber + "\"";
                list = list.Where(d=>d.Source_PaymentValue == SearchObj.BankAccountNumber).ToList();
            }
            var TotalRow = list.Count();
            SearchObj.TotalPageCount = (int)Math.Ceiling(TotalRow / (double)SearchObj.PageSize);
            SearchObj.TotalItemCount = TotalRow;
            SearchObj.CurrentPage = pageNum;
            SearchObj.List = list;
            return PartialView(SearchObj);
        }
        #region Search panel
        public ActionResult Search()
        {
            //لیستی از انواع پرداخت ها
            ViewBag.Receipt_Pay_TypeDetailLs = Utility.GetReceipt_Pay_TypeDetailLs().Where(d => d.Receipt_Pay_TypeID == 1).ToList();
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
        /// <summary>
        /// چک های صادره 
        /// </summary>
        /// <returns></returns>
        public ActionResult IndexChecksIssued(SearchModelRequestPayment searchobj)
        {
            //لیستی از انواع پرداخت ها
            ViewBag.Receipt_Pay_TypeDetailLs = Utility.GetReceipt_Pay_TypeDetailLs().Where(d => d.Receipt_Pay_TypeID == 1).ToList();
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
            return View(searchobj);
        }
        public ActionResult ListChecksIssued(SearchModelRequestPayment searchobj)
        {
            int pageNum = searchobj.PageIndex;
            int pageSize =15;
            var UserId = User.Id;
            var db = new TreasuryEntities();
            string CurrentDate = Utility.ToPersianDate(DateTime.Now);
            var query = db.GetAllRequestStatus(CurrentDate).OrderByDescending(d => d.RequestDate).ToList();
            //string whereclouse = "ChequeNo!=NULL and CancelCheque=NUll and Cancellation=NULL and AggrigateOrDuplicationID=NULL and  IsWithdrawType = " + false;

            var totalRows = query.Count();

            //Icls_Request_PaymentClient RP = new Icls_Request_PaymentClient();

            //درصورتی که مدیر سیستم باشد باید تمام درخواست وجه ها را مشاهده نماید
            if (!User.IsInRole("Admin") && !User.IsInRole("Manager"))
            {
                //whereclouse = " UserID=" + UserId;
                query = query.Where(d => d.UserID == UserId).ToList();
            }
            if (!string.IsNullOrEmpty(searchobj.RequestDateFrom) && !string.IsNullOrEmpty(searchobj.RequestDateTo))
            {
                //whereclouse = whereclouse + " and RequestDate >= \"" + searchobj.RequestDateFrom + "\"  and RequestDate <= \"" + searchobj.RequestDateTo + "\"";
                query = query.Where(d => d.RequestDate.CompareTo(searchobj.RequestDateFrom) >= 0 && d.RequestDate.CompareTo(searchobj.RequestDateTo) <= 0).ToList();
            }
            if (!string.IsNullOrEmpty(searchobj.DueDateF) && !string.IsNullOrEmpty(searchobj.DueDateT))
            {
                //whereclouse = whereclouse + " and DueDate >= \"" + searchobj.DueDateF + "\"  and DueDate <= \"" + searchobj.DueDateT + "\"";
                query = query.Where(d => (d.DueDate!=null? d.DueDate : "").CompareTo(searchobj.DueDateF) >= 0 && (d.DueDate != null ? d.DueDate : "").CompareTo(searchobj.DueDateT) <= 0).ToList();
            }
            if (!string.IsNullOrEmpty(searchobj.FiscalYear))
            {
                //whereclouse = whereclouse + " and FiscalYear=\"" + searchobj.FiscalYear + "\"";
                query = query.Where(d => d.FiscalYear == searchobj.FiscalYear).ToList();
            }
         
            if (searchobj.PayAmount != 0)
            {
                //whereclouse = whereclouse + " and  PayAmount ==" + searchobj.PayAmount + " ";
                query = query.Where(d => d.PayAmount == searchobj.PayAmount).ToList();
            }
            if (searchobj.ExchangeTypeID != 0)
            {
                //whereclouse = whereclouse + " and ExchangeTypeID=" + searchobj.ExchangeTypeID;
                query = query.Where(d => d.ExchangeTypeID == searchobj.ExchangeTypeID).ToList();
            }
            if (searchobj.BehalfID != 0)
            {
                //whereclouse = whereclouse + " and BehalfID=" + searchobj.BehalfID;
                query = query.Where(d => d.BehalfID == searchobj.BehalfID).ToList();
            }
            if (searchobj.Pay_TypeDetailID_source != 0)
            {
                //whereclouse = whereclouse + " and Pay_TypeDetailID_source=" + searchobj.Pay_TypeDetailID_source;
                query = query.Where(d => d.Pay_TypeDetailID_source == searchobj.Pay_TypeDetailID_source).ToList();
            }
            if (searchobj.ChequeNo != 0)
            {
                //whereclouse = whereclouse + " and ChequeNo=" + searchobj.ChequeNo;
                query = query.Where(d => d.ChequeNo == searchobj.ChequeNo).ToList();
            }            
            if (searchobj.Source_PaymentValue != 0)
            {
                //whereclouse = whereclouse + " and Source_PaymentValue=\"" + searchobj.Source_PaymentValue + "\"";
                query = query.Where(d => d.Source_PaymentValue == searchobj.Source_PaymentValue.ToString()).ToList();
            }
            if (searchobj.RequsetId != 0)
            {
                //whereclouse = whereclouse + " and RequestID=" + searchobj.RequsetId;
                query = query.Where(d => d.RequestID == searchobj.RequsetId).ToList();
            }
          
            if (!string.IsNullOrEmpty(searchobj.Unique_Account_No))
            {
                //whereclouse = whereclouse + " and Unique_Account_No=\"" + searchobj.Unique_Account_No + "\"";
                query = query.Where(d=>d.Unique_Account_No == searchobj.Unique_Account_No).ToList();
            }
            if(searchobj.ChequePassed)
            {
                query = query.Where(d => d.Paid == null).ToList();
            }
            else
            {
                //whereclouse = whereclouse + " and Paid=NULL";
                query = query.Where(d => d.Paid != null).ToList();
                
            }
          
            //if(!searchobj.IsPaid)
            //{
            //    whereclouse = whereclouse + " and Paid=NULL";
            //}
            //var query = RP.GetAllRequestStatus_DynamicWhereClause(CurrentDate, whereclouse, pageNum + 1, pageSize, "RequestID", "dec", false, out totalRows).ToList();


            //foreach (var item in query)
            //{              
            //    item.ChildList = RP.GetAllRequestStatus_DynamicWhereClause(CurrentDate, "AggrigateOrDuplicationID = " + item.RequestID, 1, 100, "RequestID", "dec", false, out totalRows1).ToList();
            //    if (searchobj.PayAmount > 0 || searchobj.RequsetId > 0)
            //    {
            //        item.AggrigateOrDuplicationID = null;
            //        item.AggrigateOrDuplicationStatusID = null;
            //    }               
            //}

            searchobj.TotalPageCount = (int)Math.Ceiling(totalRows / (double)pageSize);
            searchobj.CurrentPage = pageNum;
            searchobj.List = query;
            searchobj.CurrentPage = searchobj.PageIndex;
            searchobj.TotalItemCount = totalRows;


            return PartialView(searchobj);
        }
        #region checkReports
        /// <summary>
        /// چک های صادره
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult ShowChecksIssued(SearchModelRequestPayment obj)
         {
            return View(obj);
         }
        public ActionResult LoadShowChecksIssued(SearchModelRequestPayment obj)
        {
            StiReport report = new StiReport();
            //int pageNum = 1;
            //int pageSize = 5000 * 5000;
            //string orderByFieldName = "RequestID";
            //bool isAscendingOrder = false;
            //string OrderByfeildtype = "dec";         
            //int totalRows;
            string whereclouse = "  ChequeNo != NULL and Cancellation=NULL and CancelCheque = NUll and AggrigateOrDuplicationID = NULL and IsWithdrawType = " + false;
            var db = new TreasuryEntities();

            var UserId = User.Id;
            
            string CurrentDate = Utility.ToPersianDate(DateTime.Now);
            Icls_Request_PaymentClient RP = new Icls_Request_PaymentClient();

            //var query = RP.GetAllRequestStatus_DynamicWhereClause(CurrentDate, whereclouse, pageNum, pageSize, orderByFieldName, OrderByfeildtype, isAscendingOrder, out totalRows);
            var query = db.GetAllRequestStatus(CurrentDate).ToList();
            if (!User.IsInRole("Admin") && !User.IsInRole("Manager"))
            {
                //whereclouse = " UserID=" + UserId;
                query = query.Where(d => d.UserID == UserId).ToList();
            }

            if (!string.IsNullOrEmpty(obj.RequestDateFrom) && !string.IsNullOrEmpty(obj.RequestDateTo))
            {
                //whereclouse = whereclouse + " and RequestDate >= \"" + obj.RequestDateFrom + "\"  and RequestDate <= \"" + obj.RequestDateTo + "\"";
                query = query.Where(d=>d.RequestDate.CompareTo(obj.RequestDateFrom) >= 0 && d.RequestDate.CompareTo(obj.RequestDateTo) <= 0).ToList();
            }
            else if (!string.IsNullOrEmpty(obj.DueDateF) && !string.IsNullOrEmpty(obj.DueDateT))
            {
                //whereclouse = whereclouse + " and DueDate >= \"" + obj.DueDateF + "\"  and DueDate <= \"" + obj.DueDateT + "\"";
                query = query.Where(d => d.RequestDate.CompareTo(obj.DueDateF) >= 0 && d.RequestDate.CompareTo(obj.DueDateT) <= 0).ToList();
            }
            else
            {
                //whereclouse = whereclouse + " and FiscalYear=\"" + obj.FiscalYear + "\"";
                query = query.Where(d => d.FiscalYear == obj.FiscalYear).ToList();
            }
            if (obj.PayAmount != 0)
            {
                //whereclouse = whereclouse + " and  PayAmount >" + obj.PayAmount + " ";
                query = query.Where(d => d.PayAmount > obj.PayAmount).ToList();
            }
            if (obj.ExchangeTypeID != 0)
            {
                //whereclouse = whereclouse + " and ExchangeTypeID=" + obj.ExchangeTypeID;
                query = query.Where(d => d.ExchangeTypeID == obj.ExchangeTypeID).ToList();
            }
            if (obj.BehalfID != 0)
            {
                //whereclouse = whereclouse + " and BehalfID=" + obj.BehalfID;
                query = query.Where(d => d.BehalfID == obj.BehalfID).ToList();
            }
            if (obj.Pay_TypeDetailID_source != 0)
            {
                //whereclouse = whereclouse + " and Pay_TypeDetailID_source=\"" + obj.Pay_TypeDetailID_source + "\"";
                query = query.Where(d=>d.Source_PaymentValue == obj.Pay_TypeDetailID_source.ToString()).ToList();
            }
            if (obj.Source_PaymentValue != 0)
            {
                //whereclouse = whereclouse + " and Source_PaymentValue=\"" + obj.Source_PaymentValue + "\"";
                query = query.Where(d=>d.Source_PaymentValue == obj.Source_PaymentValue.ToString()).ToList();
            }
            if (!string.IsNullOrEmpty(obj.Unique_Account_No))
            {
                //whereclouse = whereclouse + " and Unique_Account_No=\"" + obj.Unique_Account_No + "\"";
                query = query.Where(d => d.Unique_Account_No == obj.Unique_Account_No).ToList();
            }
            if (obj.ChequePassed)
            {
                //whereclouse = whereclouse + " and Paid=NULL";
                query = query.Where(d=>d.Paid == null).ToList();
            }

            //var lst = (from p in query
            //           select new {
            //               RequestId = p.RequestID,
            //               paymentStatus =p.Paid,
            //               CancelCheque=p.CancelCheque,
            //               Cancellation=p.Cancellation,
            //               BankAccountNum =Utility.GetBankInfoBankId(int.Parse(p.Source_PaymentValue)).Bank_AccountNumber,
            //               DeliveredStatus =p.Delivered,
            //               ChequeNo = p.ChequeNo,
            //               UserID = Utility.GetUserName(p.UserID),
            //               Behalf = Utility.GetBehalf(p.BehalfID),
            //               Receipt_Pay_TypeDetail = p.Pay_TypeDetailID_source,
            //               Unique_Account = Utility.GetUniqueAccount(p.Unique_Account_No),
            //               PayAmount = p.PayAmount,
            //               RequestDate = Utility.GetPersianDate(p.RequestDate),
            //               DueDate = (p.DueDate == null ? "" : Utility.GetPersianDate(p.DueDate)),

            //           }).ToList();

            string Path = Server.MapPath("~/Report/ListChecksIssued.mrt");
            report.Load(Path);
            report.RegBusinessObject("ListChecksIssued", query.ToList());
            report.Dictionary.SynchronizeBusinessObjects();
            report.Dictionary.Variables["CompanyName"].Value = ConfigurationManager.AppSettings["CompanyTitle"];
            report.Dictionary.Variables["DateNow"].Value =  CurrentDate;

            return StiMvcViewer.GetReportSnapshotResult(HttpContext, report);
        }

        #endregion
        #region Repoert
        // GET: Report/Payment
        public ActionResult CheckBookStatus(CheckBookStatusSearch obj)
        {
            return View(obj);
        }
        public ActionResult LoadCheckBookStatus(CheckBookStatusSearch SearchObj)
        {
            StiReport report = new StiReport();
            var db = new TreasuryEntities();
            int TotalRow;

            //string WhereClause = "(Pay_TypeDetailID_source =1 or Pay_TypeDetailID_source =2 or  Pay_TypeDetailID_source =4 or Pay_TypeDetailID_source =5)";
            Icls_Request_PaymentClient RP = new Icls_Request_PaymentClient();


            var UserId = User.Id;
           
        
            string CurrentDate = Utility.ToPersianDate(DateTime.Now);
            //var list = RP.GetAllRequestStatus_DynamicWhereClause(CurrentDate, WhereClause,  1, 5000*5000, "RequestID", "dec", false, out TotalRow).ToList();
            var list = db.GetAllRequestStatus(CurrentDate).Where(d=>d.Pay_TypeDetailID_source == 1 || d.Pay_TypeDetailID_source == 2 || d.Pay_TypeDetailID_source == 4 ||d.Pay_TypeDetailID_source == 5);
            if (!User.IsInRole("Admin") && !User.IsInRole("Manager"))
            {
                //WhereClause = " UserID=" + UserId;
                list = list.Where(d => d.UserID == UserId);
            }
            if (!string.IsNullOrEmpty(SearchObj.BankAccountNumber))
            {
                //WhereClause = WhereClause + " and Source_PaymentValue=\"" + SearchObj.BankAccountNumber + "\"";
                list = list.Where(d => d.Source_PaymentValue == SearchObj.BankAccountNumber);
            }
            //var lst = (from p in list
            //           select new {
            //               RequestId = p.RequestID,
            //               UserID = Utility.GetUserName(p.UserID),
            //               Behalf = Utility.GetBehalf(p.BehalfID),
            //               Receipt_Pay_TypeDetail = Utility.GetChequeFormatType_Description( p.Pay_TypeDetailID_source.Value),
            //               Unique_Account = Utility.GetUniqueAccount(p.Unique_Account_No),
            //               PayAmount = p.PayAmount,
            //               RequestDate = Utility.GetPersianDate(p.RequestDate),
            //               DueDate = (p.DueDate == null ? "" : Utility.GetPersianDate(p.DueDate)),
            //           }).ToList();

            string Path = Server.MapPath("~/Report/checkBookStatusReport.mrt");
         
            report.Load(Path);
            if (SearchObj.BankAccountNumber != null)
            {
                var Bank = Utility.GetBankInfoBankId(int.Parse(SearchObj.BankAccountNumber));
                var BankInfo = Utility.GetBasicBankInfoWithBankId(Bank.BankID) + "-" + Bank.BranchName;
                var AccountNUmber = Bank.Bank_AccountNumber;
                report.Dictionary.Variables["Bank"].Value = BankInfo;
                report.Dictionary.Variables["AccountNum"].Value = AccountNUmber;
            }
            report.Dictionary.Variables["CompanyName"].Value = ConfigurationManager.AppSettings["CompanyTitle"];
            report.Dictionary.Variables["DateNow"].Value = Utility.GetPersianDate(CurrentDate);
            report.RegBusinessObject("CheckBookStatus", list);
            report.Dictionary.SynchronizeBusinessObjects();


            return StiMvcViewer.GetReportSnapshotResult(HttpContext, report);
        }

        public string GetBanks(int FormatDescriptionID_Main)
        {
            Icls_ChequesClient ChequeClient = new Icls_ChequesClient();
            Icls_ChequeFormatTypeDetailsClient ChFormtTypeClient = new Icls_ChequeFormatTypeDetailsClient();
            var BasicBankLs =Utility.GetBasicBanks();
            var BankLS =Utility.GetBanks();
            var ChequeFormatTypels =Utility.GetChequeFormatTypeLs();
            int totalRows;
            var checkFormatType = ChFormtTypeClient.SelectAll(1, 2000, "FormatID", "int", false, out totalRows).ToList();
            var cheques = ChequeClient.SelectBy_DynamicWhereClause("IsActive =" + true, 1, 1000, "ChequeID", "int", false, out totalRows).Where(q => checkFormatType.Select(d => d.ChequeFormatID).Contains(q.ChequeFormatID.Value) && BankLS.Select(u => u.BankID).Contains(q.BankID)).Select(d=>d.BankID);

            var query = (from chtype in ChequeFormatTypels
                         join bank in BankLS on chtype.BankID equals bank.BasicBankID
                         join basicBank in BasicBankLs on chtype.BankID equals basicBank.BasicBankId
                         select new Service.ChequeFormatTypesService.ChequeFormatType
                         {
                             BankID = bank.BankID,
                             FormatDescription = chtype.FormatDescription,
                             BankAccountNumber = bank.Bank_AccountNumber,
                             FormatDescriptionID_Main = chtype.FormatDescriptionID_Main,
                             BranchName = bank.BranchName,
                             BasicBankDescription = basicBank.BasicBankDescription,
                         }).Where(d => d.FormatDescriptionID_Main == FormatDescriptionID_Main && cheques.Contains(d.BankID)).GroupBy(d => d.BankAccountNumber); 



            var res = new StringBuilder();
            res.Append("<option >"+Resourse.Resources.PleaseSelect+"</option>");

            foreach (var i in query)

            {
                res.Append("<option value='" + i.FirstOrDefault().BankID + "'>" + i.FirstOrDefault().BasicBankDescription +"-"+i.FirstOrDefault().BranchName+"-"+ i.FirstOrDefault().BankAccountNumber+"-"+i.FirstOrDefault().FormatDescription+"</option>");
            }
            return res.ToString();
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
        #endregion
        
    }
}