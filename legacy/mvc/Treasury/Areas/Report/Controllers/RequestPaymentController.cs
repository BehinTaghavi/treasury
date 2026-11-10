using CSD.Common.Utility;
using CSD.Treasury.Service.ProceduresDB;
using CSD.Treasury.Service.PaymentService;
using CSD.Treasury.Service.BankService;
using CSD.Treasury.Service.PermissiveSignature_CompleteProccessService;
using CSD.Treasury.Service.BeneficiaryService;
using CSD.Treasury.UI.Controllers;
using CSD.Treasury.UI.Models;
using Stimulsoft.Report;
using Stimulsoft.Report.Mvc;
using System;
using System.Linq;
using System.Web.Mvc;
using CSD.Treasury.Service.Request_PaymentService;
using CSD.Treasury.Service.Request_Payment_StatusDetailsService;
using CSD.Treasury.Common.Enumeration;
using System.Configuration;
using System.Data.SqlClient;
using System.Data;
using System.Collections.Generic;
using CSD.Treasury.DataModel;

namespace CSD.Treasury.UI.Areas.Report.Controllers
{
    [Authorize(Roles = "Admin,Manager,Report,ReportInApp")]
    public class RequestPaymentController : BaseController
    {
        // GET: Report/RequestPayment
        public ActionResult Index()
        {
            return View();
        }
        #region RequestReports
        [HttpPost]
        public ActionResult ShowPayment(SearchModelRequestPayment obj)
        {
            //var parameter = JsonConvert.DeserializeObject<Dictionary<string, string>>(parameterJson);
            //var obj = JsonConvert.DeserializeObject<ChequeFormatTypeDetail>(parameterJson);
            //ViewBag.SearchModelRequestPayment = obj;
            return View(obj);
        }
        public ActionResult LoadShowPayment(SearchModelRequestPayment obj)
        {
            var db = new TreasuryEntities();
            StiReport report = new StiReport();

            //int pageNum = 1;
            //int pageSize = 5000 * 5000;
            //string orderByFieldName = "RequestID";
            //bool isAscendingOrder = false;
            //string OrderByfeildtype = "dec";
            //string whereclouse = "1=1";
            //int totalRows;

            var UserId = User.Id;
           
            string CurrentDate = Utility.ToPersianDate(DateTime.Now);
            Icls_Request_PaymentClient RP = new Icls_Request_PaymentClient();

            //var query = RP.GetAllRequestStatus_DynamicWhereClause(CurrentDate, whereclouse, pageNum, pageSize, orderByFieldName, OrderByfeildtype, isAscendingOrder, out totalRows).ToList();
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
            if (obj.PayAmount != 0)
            {
                //whereclouse = whereclouse + " and  PayAmount >" + obj.PayAmount + " ";
                query = query.Where(d=>d.PayAmount>obj.PayAmount).ToList();
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
                //whereclouse = whereclouse + " and Pay_TypeDetailID_source=" + obj.Pay_TypeDetailID_source;
                query = query.Where(d => d.Pay_TypeDetailID_source == obj.Pay_TypeDetailID_source).ToList();
            }
            if (!string.IsNullOrEmpty(obj.FiscalYear))
            {
                //whereclouse = whereclouse + " and FiscalYear=\"" + obj.FiscalYear + "\"";
                query = query.Where(d => d.FiscalYear == obj.FiscalYear).ToList();
            }
            if (!string.IsNullOrEmpty(obj.Unique_Account_No))
            {
                //whereclouse = whereclouse + " and Unique_Account_No=\"" + obj.Unique_Account_No + "\"";
                query = query.Where(d => d.Unique_Account_No == obj.Unique_Account_No).ToList();
            }

            //var lst = (from p in query
            //           select new {
            //               RequestId = p.RequestID,
            //               UserID = Utility.GetUserName(p.UserID),
            //               Behalf = Utility.GetBehalf(p.BehalfID),
            //               Receipt_Pay_TypeDetail = p.Pay_TypeDetailID_source,
            //               Unique_Account = Utility.GetUniqueAccount(p.Unique_Account_No),
            //               PayAmount = p.PayAmount, RequestDate = Utility.GetPersianDate(p.RequestDate),
            //               DueDate = (p.DueDate == null ? "" : Utility.GetPersianDate(p.DueDate))
            //           }).ToList();


            string Path = Server.MapPath("~/Report/RequestReport.mrt");
            report.Load(Path);
            report.RegBusinessObject("RequestObj", query);
            report.Dictionary.SynchronizeBusinessObjects();
            report.Dictionary.Variables["CompanyName"].Value = ConfigurationManager.AppSettings["CompanyTitle"];


            return StiMvcViewer.GetReportSnapshotResult(HttpContext, report);
        }

        #endregion

        #region PaymentformReport
        public ActionResult PaymentformReport(int RequestId)
        {
            return View();
        }
        public ActionResult LoadPaymentformReport(int RequestId)
        {
            StiReport report = new StiReport();
            string Path = Server.MapPath("~/Report/paymentForm.mrt");
            report.Load(Path);
            Icls_Request_PaymentClient obj = new Icls_Request_PaymentClient();
            var request = obj.SelectRow(RequestId).SingleOrDefault();

            report.Dictionary.Variables["Date"].Value = Utility.GetPersianDate(request.RequestDate);
            report.Dictionary.Variables["Price"].Value = (string.Format("{0:n0}", (Int64)request.PayAmount)).ToString(); // request.PayAmount.ToString().Replace(".00", "").ToString();
            report.Dictionary.Variables["PriceInWord"].Value = NumberToPersianText.num2str(request.PayAmount.ToString("0.00").Replace(".00", ""));
            report.Dictionary.Variables["Behalf"].Value = Utility.GetBehalf(request.BehalfID);
            report.Dictionary.Variables["BehalfGroup"].Value = Utility.GetBehalfGroupBybehalfId(request.BehalfID).BehalfGroupDescription;
            report.Dictionary.Variables["Unique_Account"].Value = Utility.GetUniqueAccount(request.Unique_Account_No);
            report.Dictionary.Variables["CompanyName"].Value = ConfigurationManager.AppSettings["CompanyTitle"];
            report.Dictionary.Variables["CashierName"].Value = "";

            return StiMvcViewer.GetReportSnapshotResult(HttpContext, report);
        }
        #endregion

        #region Payment-ChequeReport
        public ActionResult PaymentChequeReport(int RequestId, string Status)
        {
            ViewBag.Status = Status;
            return View();
        }
        public ActionResult LoadPaymentChequeReport(int RequestId)
        {
            int TotalRow;
            StiReport report = new StiReport();
            Icls_SPsClient spsClient = new Icls_SPsClient();
            var dbTreasury = new TreasuryEntities();
            string Path = Server.MapPath("~/Report/PaymentCheque.mrt");
            report.Load(Path);

            Icls_PaymentClient p = new Icls_PaymentClient();
            var payment = p.SelectBy_DynamicWhereClause("RequestID=" + RequestId, 1, 10, "PaymentID", "dec", true, out TotalRow);

            Icls_SPsClient db = new Icls_SPsClient();
            var obj = db.Sp_PaymentChequeReport((long)payment.FirstOrDefault().PaymentID);

            //string PermissiveSignature_CompleteTitles = "";
            //foreach (var item in obj.ToList())
            //{
            //    PermissiveSignature_CompleteTitles = PermissiveSignature_CompleteTitles + "," + item.User_FullName;
            //}

            report.Dictionary.Variables["DateNow"].Value = Utility.DateStructure(Utility.ToPersianDate(DateTime.Now));
            report.Dictionary.Variables["Price"].Value = (string.Format("{0:n0}", (Int64)obj.FirstOrDefault().PayAmount)).ToString();  //obj.FirstOrDefault().PayAmount.ToString().Replace(".00", "").ToString(); //obj.FirstOrDefault().PayAmount.ToString().Replace(".00", "").ToString();
            report.Dictionary.Variables["PriceInWord"].Value = NumberToPersianText.num2str(obj.FirstOrDefault().PayAmount.ToString("0.00").Replace(".00", ""));
            report.Dictionary.Variables["Behalf"].Value = obj.FirstOrDefault().BehalfDescription;
            report.Dictionary.Variables["BehalfGroup"].Value = obj.FirstOrDefault().BehalfGroupDescription;
            report.Dictionary.Variables["Unique_Account"].Value = obj.FirstOrDefault().Account_Name;
            report.Dictionary.Variables["ChequeNum"].Value = obj.FirstOrDefault().ChequeNo.ToString();
            report.Dictionary.Variables["DueDate"].Value = Utility.GetPersianDate(obj.FirstOrDefault().DueDate);
            report.Dictionary.Variables["AccountNum"].Value = obj.FirstOrDefault().Bank_AccountNumber;
            report.Dictionary.Variables["Bank"].Value = obj.FirstOrDefault().BasicBankDescription;
            report.Dictionary.Variables["Branch"].Value = obj.FirstOrDefault().BranchName;
            //report.Dictionary.Variables["CurrentUser"].Value = obj.FirstOrDefault().User_FullName;
            report.Dictionary.Variables["PaymentNum"].Value = obj.FirstOrDefault().PaymentID.ToString();
            report.Dictionary.Variables["RequestId"].Value = RequestId.ToString();
            //report.Dictionary.Variables["SigniturePermission"].Value = PermissiveSignature_CompleteTitles;
            report.Dictionary.Variables["CompanyName"].Value = ConfigurationManager.AppSettings["CompanyTitle"];
            //var ls = spsClient.Sp_CREATE_VOUCHER_PAYMENTS(false, RequestId, User.Id, false, true);
            var ls = dbTreasury.Voucher_Row.Where(d => d.ReferenceId == RequestId).ToList();
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



        public ActionResult ChequeReport(int RequestId, string Status)
        {
            ViewBag.Status = Status;
            return View();
        }
        public ActionResult LoadChequeReport(int RequestId)
        {
            int TotalRow;
            StiReport report = new StiReport();
            var dbTreasury = new TreasuryEntities();
            //Icls_SPsClient spsClient = new Icls_SPsClient();
            
            //var RequsetObj = spsClient.Sp_GetRequstByRequestId(RequestId).SingleOrDefault();
            //var obj = spsClient.Sp_GetRequstByRequestId(RequestId).SingleOrDefault(); 
            var obj = dbTreasury.GetRequstByRequestId(RequestId).SingleOrDefault();
            var ReportName = "ChequeReport.mrt";
            if (obj.Pay_TypeDetailID_source == 33)
            {
                ReportName = "ChequeReportSatna.mrt";
            }

            string Path = Server.MapPath("~/Report/" + ReportName);
            report.Load(Path);

            //Icls_Request_PaymentClient r = new Icls_Request_PaymentClient();
            //var payment = r.SelectBy_DynamicWhereClause("RequestID=" + RequestId, 1, 10, "RequestID", "dec", true, out TotalRow);
            var payment = dbTreasury.Payments.SingleOrDefault(d => d.RequestID == RequestId);

            Icls_SPsClient db = new Icls_SPsClient();
            //var obj = db.Sp_PaymentChequeReport((long)payment.FirstOrDefault().PaymentID);
            //var obj = spsClient.Sp_GetRequstByRequestId(RequestId).SingleOrDefault(); 

            //string PermissiveSignature_CompleteTitles = "";
            //foreach (var item in obj.ToList())
            //{
            //    PermissiveSignature_CompleteTitles = PermissiveSignature_CompleteTitles + "," + item.User_FullName;
            //}

            report.Dictionary.Variables["DateNow"].Value = Utility.DateStructure(Utility.ToPersianDate(DateTime.Now));
            report.Dictionary.Variables["Price"].Value = (string.Format("{0:n0}", (Int64)obj.PayAmount)).ToString();  //obj.FirstOrDefault().PayAmount.ToString().Replace(".00", "").ToString(); //obj.FirstOrDefault().PayAmount.ToString().Replace(".00", "").ToString();
            report.Dictionary.Variables["PriceInWord"].Value = NumberToPersianText.num2str(obj.PayAmount.ToString("0.00").Replace(".00", ""));
            report.Dictionary.Variables["Behalf"].Value = obj.BehalfDescription;
            report.Dictionary.Variables["BehalfGroup"].Value = obj.BehalfGroupDescription;
            report.Dictionary.Variables["Unique_Account"].Value = obj.Unique_Account_No + "/" + obj.Account_Name;
            if (obj.ChequeNo != null)
            {
                report.Dictionary.Variables["ChequeNum"].Value = ((int)obj.ChequeNo).ToString();
                
            }
            else
            {
                report.Dictionary.Variables["ChequeNum"].Value = "";
            }
            if(payment!= null)
            {
                report.Dictionary.Variables["PaymentNum"].Value = payment.PaymentID.ToString();
            }
            
            if (!string.IsNullOrEmpty(obj.DueDate) && obj.DueDate != "        ")
            {
                report.Dictionary.Variables["DueDate"].Value = Utility.GetPersianDate(obj.DueDate);
            }            
            report.Dictionary.Variables["AccountNum"].Value = obj.Bank_AccountNumber;
            report.Dictionary.Variables["Bank"].Value = obj.BasicBankDescription;
            report.Dictionary.Variables["Branch"].Value = obj.BranchName;
            //report.Dictionary.Variables["CurrentUser"].Value = obj.FirstOrDefault().User_FullName;
            report.Dictionary.Variables["RequestId"].Value = RequestId.ToString();
            //report.Dictionary.Variables["SigniturePermission"].Value = PermissiveSignature_CompleteTitles;
            report.Dictionary.Variables["CompanyName"].Value = ConfigurationManager.AppSettings["CompanyTitle"];



            
           
            //var RequsetObj = RequestClient.SelectBy_DynamicWhereClause("RequestID=" + RequestId, 1, 10, "RequestID", "dec", true, out TotalRow).SingleOrDefault();

 
            report.Dictionary.Variables["Price"].Value = Math.Truncate(obj.PayAmount).ToString("#,##0"); 
            report.Dictionary.Variables["ChequeNum"].Value = obj.ChequeNo.ToString();
            report.Dictionary.Variables["AccountNum"].Value = obj.Bank_AccountNumber;
            report.Dictionary.Variables["Bank"].Value = obj.BasicBankDescription;

            //var ls = spsClient.Sp_CREATE_VOUCHER_PAYMENTS(false, RequestId, User.Id, false, true);
            var ls = dbTreasury.Voucher_Row.Where(d => d.ReferenceId == RequestId).ToList();
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




        public ActionResult PaymentChequeAgreggatedReport(int RequestId)
        {
            return View();
        }
        [HttpPost]
        public ActionResult LoadPaymentChequeAgreggatedReport(int RequestId)
        {
            int TotalRow;
            var dbTreasury = new TreasuryEntities();
            StiReport report = new StiReport();
            Icls_SPsClient spsClient = new Icls_SPsClient();
            Icls_Request_PaymentClient RequestClient = new Icls_Request_PaymentClient();
            string Path = Server.MapPath("~/Report/PaymentChequeAggregatedReport.mrt");
            report.Load(Path);

            Icls_PaymentClient p = new Icls_PaymentClient();
            var payment = p.SelectBy_DynamicWhereClause("RequestID=" + RequestId, 1, 10, "PaymentID", "dec", true, out TotalRow);

            var RequsetObj = dbTreasury.GetRequstByRequestId(RequestId).SingleOrDefault();
            
            //var RequsetObj = RequestClient.SelectBy_DynamicWhereClause("RequestID=" + RequestId, 1, 10, "RequestID", "dec", true, out TotalRow).SingleOrDefault();

            report.Dictionary.Variables["RequestID"].Value = RequsetObj.RequestID.ToString();
            report.Dictionary.Variables["Refrence"].Value = Utility.GetReferenceTypeValue(RequestId, true);
            report.Dictionary.Variables["DateNow"].Value = Utility.DateStructure(Utility.ToPersianDate(DateTime.Now));
            report.Dictionary.Variables["Price"].Value = Math.Truncate(RequsetObj.PayAmount).ToString("#,##0"); 
            report.Dictionary.Variables["PriceInWord"].Value = NumberToPersianText.num2str(RequsetObj.PayAmount.ToString("0.00").Replace(".00", ""));
            report.Dictionary.Variables["Behalf"].Value = RequsetObj.BehalfDescription;
            report.Dictionary.Variables["BehalfGroup"].Value = RequsetObj.BehalfGroupDescription;
            report.Dictionary.Variables["Unique_Account"].Value = RequsetObj.Unique_Account_No + "/" + RequsetObj.Account_Name;
            report.Dictionary.Variables["ChequeNum"].Value = RequsetObj.ChequeNo.ToString();
            report.Dictionary.Variables["DueDate"].Value = Utility.GetPersianDate(RequsetObj.DueDate);
            report.Dictionary.Variables["AccountNum"].Value = RequsetObj.Bank_AccountNumber;
            report.Dictionary.Variables["Bank"].Value = RequsetObj.BasicBankDescription;
            report.Dictionary.Variables["Branch"].Value = RequsetObj.BranchName;
            report.Dictionary.Variables["CompanyName"].Value = ConfigurationManager.AppSettings["CompanyTitle"];

            var childOfRequest = RequestClient.SelectBy_DynamicWhereClause("AggrigateOrDuplicationID ="+RequestId, 1, 500, "RequestID", "dec", false, out TotalRow);
            List<DataModel.Voucher_Row> AllChildVocher= new List<DataModel.Voucher_Row>();
            foreach (var item1 in childOfRequest)
            {
                //var ls = spsClient.Sp_CREATE_VOUCHER_PAYMENTS(false, (Int64)item1.RequestID, User.Id, false, true);
                var ls = dbTreasury.Voucher_Row.Where(d => d.ReferenceId == RequestId).ToList();
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
                    item.Credit =decimal.Parse( Math.Truncate(item.Credit).ToString("#,##0"));
                    item.Debit = decimal.Parse(Math.Truncate(item.Debit).ToString("#,##0"));
                    AllChildVocher.Add(item);
                }
                
            }


            report.RegBusinessObject("VocherLS", AllChildVocher);
            report.Dictionary.SynchronizeBusinessObjects();

            return StiMvcViewer.GetReportSnapshotResult(HttpContext, report);
        }
        #endregion


        #region ShowRequestDetails

        #region Payment-ChequeReport
        public ActionResult RequestDetails(int RequestId)
        {
            return View();
        }
        public ActionResult LoadRequestDetailsReport(int RequestId)
        {
            int TotalRow;
            StiReport report = new StiReport();
            string Path = Server.MapPath("~/Report/RequestPaymentReport.mrt");
            report.Load(Path);
            var db = new TreasuryEntities();

            Icls_Request_PaymentClient RequestClient = new Icls_Request_PaymentClient();
            Icls_SPsClient spsClient = new Icls_SPsClient();
            //var RequsetObj = spsClient.Sp_GetRequstByRequestId(RequestId).SingleOrDefault();
            var RequsetObj = db.GetRequstByRequestId(RequestId).SingleOrDefault();
          //var RequsetObj = RequestClient.SelectBy_DynamicWhereClause("RequestID=" + RequestId, 1, 10, "RequestID", "dec", true, out TotalRow).SingleOrDefault();


          report.Dictionary.Variables["DateNow"].Value = Utility.DateStructure(Utility.ToPersianDate(DateTime.Now));
            report.Dictionary.Variables["Price"].Value = Math.Truncate(RequsetObj.PayAmount).ToString("#,##0");
            //" " + Utility.GetExchaneName(RequsetObj.ExchangeTypeID.Value
            var exchangeTitle = "ریال";
            if (RequsetObj.ExchangeTypeID!=null)
            {
                exchangeTitle= Utility.GetExchaneName(RequsetObj.ExchangeTypeID.Value);
            }
            report.Dictionary.Variables["PriceInWord"].Value = NumberToPersianText.num2str(RequsetObj.PayAmount.ToString("0.00").Replace(".00", ""))+" "+ exchangeTitle;
            report.Dictionary.Variables["Behalf"].Value = RequsetObj.BehalfDescription;
            report.Dictionary.Variables["BehalfGroup"].Value = RequsetObj.BehalfGroupDescription;
            report.Dictionary.Variables["Unique_Account1"].Value = RequsetObj.Unique_Account_No + "/" + RequsetObj.Account_Name;
            report.Dictionary.Variables["Unique_Account2"].Value = RequsetObj.Main_Unique_Account_No;
            report.Dictionary.Variables["RequestDate"].Value =Utility.GetPersianDate(RequsetObj.RequestDate);
            //var Bank = Utility.GetBankInfoBankId(int.Parse(RequsetObj.Source_PaymentValue));
            //if (Bank != null)
            //{
            report.Dictionary.Variables["AccountNum"].Value = RequsetObj.Bank_AccountNumber;
            report.Dictionary.Variables["Bank"].Value = RequsetObj.BasicBankDescription;
            report.Dictionary.Variables["Branch"].Value = RequsetObj.BranchName;
            //}
            report.Dictionary.Variables["CurrentUser"].Value = RequsetObj.User_FullName;
            report.Dictionary.Variables["RequestID"].Value = RequsetObj.RequestID.ToString();
            report.Dictionary.Variables["Refrence"].Value = Utility.GetReferenceTypeValue(RequestId, true);
            report.Dictionary.Variables["CompanyName"].Value= ConfigurationManager.AppSettings["CompanyTitle"];


            return StiMvcViewer.GetReportSnapshotResult(report);
        }
        #endregion
        #endregion

        #region IntegratedRequest
        public ActionResult IntegratedRequest(int RequestId,bool Bankformat=false )
        {    
            return View();
        }
        public ActionResult LoadIntegratedRequest(int RequestId, bool Bankformat = false)
        {
            //int totalRows;
            //Icls_Request_PaymentClient RP = new Icls_Request_PaymentClient();
            Icls_Request_PaymentClient RequestClient = new Icls_Request_PaymentClient();
            //Icls_BankClient BankClient = new Icls_BankClient();
            var db = new TreasuryEntities();
            //StoredProcedureDB procedureDB = new StoredProcedureDB();

            StiReport report = new StiReport();
            string Path = Server.MapPath("~/Report/IntegratedRequestReport11.mrt");
            if(Bankformat)
            {
                Path = Server.MapPath("~/Report/IntegratedRequestReportBankF.mrt");
            }
            var dateNow = Utility.ToPersianDate(DateTime.Now);
            report.Load(Path);
            Icls_SPsClient spsClient = new Icls_SPsClient();
            //حسابهای بانکی
            var ls = spsClient.GetIntegratedRequest(RequestId, "BeneficiariesDetailInfo.Bank_ShebaNo IS NULL").ToList();
            //شباهاکوچگتر از50 میلیون تومن
            var ls1 = spsClient.GetIntegratedRequest(RequestId, "Request_Payment.PayAmount <= 500000000").Where(d=> !ls.Select(t=>t.RequestID).Contains(d.RequestID)).ToList();
     
            //
             var ls2=spsClient.GetIntegratedRequest(RequestId, "Request_Payment.PayAmount > 500000000").Where(d => !ls.Select(t => t.RequestID).Contains(d.RequestID) ).ToList();

            //var ls = new List<dynamic>();
            //var ls1 = new List<dynamic>();
            //var s = ConfigurationManager.AppSettings["temporaryConnection"];
            //using (SqlConnection sqlConnection = new SqlConnection(s))
            //{
            //    using (SqlCommand cmd = new SqlCommand("GetIntegratedRequest", sqlConnection))
            //    {
            //        SqlDataReader reader;
            //        cmd.CommandType = CommandType.StoredProcedure;
            //        cmd.Parameters.AddWithValue("@RequestId", RequestId);
            //        cmd.Parameters.AddWithValue("@WhereStr", "BeneficiariesDetailInfo.Bank_ShebaNo is NULL");
            //        sqlConnection.Open();
            //        cmd.ExecuteNonQuery();
            //        reader = cmd.ExecuteReader();
            //        // Data is accessible through the DataReader object here.

            //        while (reader.Read())
            //        {
            //            ls.Add(new
            //            {
            //                Unique_Account_No = reader[7],
            //                LegalAccountName = reader[5],
            //                Bank = reader[1],
            //                BankAccountNumber = reader[4],
            //                Bank_Sheba = reader[3],
            //                Price = reader[8],
            //                //Beneficiar_TEL = reader[2],
            //                //Account_Name = reader[7],
            //            });
            //        }

            //        sqlConnection.Close();
            //    }
            //    using (SqlCommand cmd = new SqlCommand("GetIntegratedRequest", sqlConnection))
            //    {
            //        SqlDataReader reader;
            //        cmd.CommandType = CommandType.StoredProcedure;
            //        cmd.Parameters.AddWithValue("@RequestId", RequestId);
            //        cmd.Parameters.AddWithValue("@WhereStr", "BeneficiariesDetailInfo.Bank_ShebaNo IS NOT NULL");
            //        sqlConnection.Open();
            //        cmd.ExecuteNonQuery();
            //        reader = cmd.ExecuteReader();
            //        // Data is accessible through the DataReader object here.

            //        while (reader.Read())
            //        {
            //            ls1.Add(new
            //            {
            //                Unique_Account_No = reader[7],
            //                LegalAccountName = reader[5],
            //                Bank = reader[1],
            //                BankAccountNumber = reader[4],
            //                Bank_Sheba = reader[3],
            //                Price = reader[8],
            //                //Beneficiar_TEL = reader[2],
            //                //Account_Name = reader[7],
            //            });
            //        }

            //        sqlConnection.Close();
            //    }
            //}


            //درخواست حاصل تجمیع درخواست های وجه
            var Request = RequestClient.SelectRow(RequestId).SingleOrDefault();
            if (Request != null)
            {
                //var Bank = BankClient.SelectRow(int.Parse(Request.Source_PaymentValue)).SingleOrDefault();
                var bankid = int.Parse(Request.Source_PaymentValue);
                var Bank = db.Banks.SingleOrDefault(d => d.BankID == bankid);
                if (Bank != null)
                {
                    report.Dictionary.Variables["BankName"].Value = Utility.GetBasicBankInfo(Bank.BasicBankID)+"-"+Bank.BranchName;
                    report.Dictionary.Variables["BankAccountNamber"].Value = Bank.Bank_AccountNumber;
                }
            }


            if (Request.ChequeNo != null)
            {
                report.Dictionary.Variables["ChequeNo"].Value = Request.ChequeNo.Value.ToString();
            }

            report.Dictionary.Variables["DueDate"].Value = Request.DueDate;
            report.Dictionary.Variables["Price1"].Value = (string.Format("{0:n0}", (Int64)Request.PayAmount)).ToString();

            report.Dictionary.Variables["Date"].Value = Utility.GetPersianDate(dateNow);
            report.Dictionary.Variables["CompanyName"].Value = ConfigurationManager.AppSettings["CompanyTitle"];

            report.RegBusinessObject("RequsetLS", ls);
            report.RegBusinessObject("RequestShebaLS", ls1);
            report.RegBusinessObject("RequestShebaLS1", ls2);
            report.Dictionary.SynchronizeBusinessObjects();
            report.ReportName = Utility.GetPersianDate(dateNow);


            return StiMvcViewer.GetReportSnapshotResult( report);
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