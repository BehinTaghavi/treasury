using CSD.Treasury.UI.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using CSD.Treasury.Service.Request_PaymentService;
using CSD.Treasury.Service.TransferFromBackupBankService;
using CSD.Treasury.Service.PaymentService;
using CSD.Treasury.Service.BankService;
using CSD.Treasury.Service.Request_Payment_StatusDetailsService;
using CSD.Treasury.UI.Models;
using CSD.Treasury.Service.ProceduresDB;
using CSD.Treasury.Common.Enumeration;
using CSD.Treasury.DataModel;
using System.Configuration;

namespace CSD.Treasury.UI.Areas.Management.Controllers
{
    [Authorize(Roles = "Admin,Manager")]
    public class DashBoardController : BaseController
    {
        // GET: Management/DashBoard
        public ActionResult Index()
        {
            return View();
        }
        public ActionResult ChequeDueDateList()
        {
            string CurrentDate = Utility.ToPersianDate(DateTime.Now);
            Icls_SPsClient db = new Icls_SPsClient();
            var query = db.Get_Maturity_Cheques(CurrentDate).ToList();
            return PartialView(query);
        }
        /// <summary>
        /// لیست چک های دریافتنی سررسید شده
        /// چک های دریافتنی با وضعیت های بلند مدت سررسید شده
        /// ممکنه این چک های سررسید شده نزد صندوق باشند یا نزد بانک باشند
        /// </summary>
        /// <returns></returns>
        #region ReceiveChequeDueDateList
        public ActionResult ReceiveChequeDueDateList()
        {
            Icls_SPsClient spsClient = new Icls_SPsClient();
            string DateNow = Utility.ToPersianDate(DateTime.Now);
            var ReceiptLs = spsClient.GetRecieptHeaderAndDetail_ByRecieptTypeId(19).Where(d => d.DueDate.CompareTo(DateNow) <= 0 && (d.PlaningTypeID == 1 || d.PlaningTypeID == 2)).OrderByDescending(d => d.DueDate).ToList();
            return PartialView(ReceiptLs);
        }
        #endregion
        #region FuturePaymentStatus
        public ActionResult FuturePaymentStatus()
        {
            //int TotalRow;
            //Icls_BankClient BankClient = new Icls_BankClient();
            //var Query = BankClient.SelectAll(1, 50, "BankID", "int", false, out TotalRow);
            var db = new TreasuryEntities();
            var Query = db.Banks;
            return View(Query);
        }
        #endregion

        #region PayDay
        public ActionResult PayDay(int BankId)
        {
            var db = new TreasuryEntities();
            Icls_Request_PaymentClient RP = new Icls_Request_PaymentClient();
            //int totalRows;
            //string whereclouse = " UnKnown != Null and Confirmed != NULL " +
            //    "and (ConfirmPayment = NULL or Paid = NULL or Delivered=NULL )   " +
            //    "and IsWithdrawType= " + false + " " +
            //    "and  DueDate <= \"" + Utility.ToPersianDate(DateTime.Now) + "\" " +
            //    "and Pay_TypeDetailID_source in (1,2,3) " +
            //    "and Source_PaymentValue =" + BankId;
            //if (!User.IsInRole("Admin") && !User.IsInRole("Manager"))
            //{
            //    whereclouse = whereclouse + " and UserID=" + User.Id;
            //}
            string CurrentDate = Utility.ToPersianDate(DateTime.Now);
            //var query = RP.GetAllRequestStatus_DynamicWhereClause(CurrentDate, whereclouse, 1, 20, "RequestID", "dec", false, out totalRows).ToList();
            var query = db.GetAllRequestStatus(CurrentDate).Where(d => d.Unknown != null && d.Confirmed != null
                && (d.ConfirmPayment == null || d.Paid == null || d.Delivered == null)
                && d.IsWithdrawType == false && d.Source_PaymentValue == BankId.ToString()
                && d.DueDate.CompareTo(Utility.ToPersianDate(DateTime.Now)) <= 0               
                && (d.Pay_TypeDetailID_source==1 || d.Pay_TypeDetailID_source == 2 || d.Pay_TypeDetailID_source == 3));

            if (!User.IsInRole("Admin") && !User.IsInRole("Manager"))
            {
                query= query.Where(d=>d.UserID ==User.Id);
            }
            return PartialView(query.ToList());
        }
        #endregion

        #region TransferFromBackupBank
        public ActionResult TransferFromBackupBankList()
        {

            //int TotalRow;
            var db = new TreasuryEntities();
            //Icls_TransferFromBackupBankClient TransferClient = new Icls_TransferFromBackupBankClient();
            //var ls = TransferClient.SelectAll(1, 100, "TransferId", "int64", false, out TotalRow).Where(d => d.IsOk == false).ToList();
            //چک تقدی که چاپ نشده
            //چک مدت داری که پرداخت نشده
            //چک نقدی =1
            //چک مدت دار=4
            var ls = (from a in db.TransferFromBackupBanks
                     join b in db.Request_Payment
                     on a.RequestID equals b.RequestID
                     join d in db.GetLastRequestStatus()
                     on a.RequestID equals d.Request_Payment_Status_RequestID
                      where ((b.Pay_TypeDetailID_source != 4) && (b.ChequeNo == null || b.ChequeNo == 0) && d.Request_Payment_Status_RequestStatusTypeID != 4) || (b.Pay_TypeDetailID_source == 4 && d.Request_Payment_Status_RequestStatusTypeID == 4)
                      select a
                     ).ToList();
            
            return PartialView(ls);
        }
        public ActionResult EditTransfer(int TransferId)
        {
            Icls_TransferFromBackupBankClient TransferClient = new Icls_TransferFromBackupBankClient();
            var transferObj = TransferClient.SelectRow(TransferId).SingleOrDefault();
            Icls_SPsClient db = new Icls_SPsClient();
            var Date = Utility.ToPersianDate(DateTime.Now);
            var dc = new TreasuryEntities();
            //$Sp_GetBankBalance$
            var Bank = db.Sp_GetBankBalance("13000101", Date, transferObj.To_BankID, 1, false).FirstOrDefault();

            var BackupBank = dc.Banks.Where(d => d.Backup_BankID == Bank.BankID);
            GetBankBalance_Result Backup_Bank = new GetBankBalance_Result();
            if (BackupBank.Count() != 0)
            {
                if (BackupBank.Count() != 0)
                {
                    //Backup
                    //$Sp_GetBankBalance$
                    var qury = db.Sp_GetBankBalance("13000101", Date, BackupBank.SingleOrDefault().BankID, 1, false);
                    Backup_Bank = qury.FirstOrDefault();
                }
            }
            decimal? remain = 0;
            //chequeType == 2 چک انتظامی
            // chequeType == 4 چک بلندمدت
            var request = dc.Request_Payment.SingleOrDefault(d => d.RequestID == transferObj.RequestID);
            var ChequePrice = request.PayAmount;
            var chequeType = request.Pay_TypeDetailID_source;
            if ((ChequePrice <= Bank.balance && (ConfigurationManager.AppSettings["CompletePayBackup"] == "True" && BackupBank.Count() == 0))
                || (ChequePrice <= Bank.balance && (ConfigurationManager.AppSettings["CompletePayBackup"] == "False"))
                || chequeType == 2 || (ConfigurationManager.AppSettings["Long-termCheckWithoutCash"] == "True" && chequeType == 4))
            {
                
            }
            else if (BackupBank.Count() != 0)
            {
                //اگر بانک حساب پشتیبان داشته باشد
                
                //برداشت مبلغ به صورت کامل از پشتیبان
                if (ConfigurationManager.AppSettings["CompletePayBackup"] == "True")
                {
                    remain = ChequePrice;
                }
                else
                {
                    remain = ChequePrice - Bank.balance;
                }
                ViewBag.remain = remain;
            }
            return PartialView(transferObj);
        }

        [HttpPost]
        public ActionResult EditTransfer(Service.TransferFromBackupBankService.TransferFromBackupBank obj)
        {
            var Message = "";
            bool result = false;
            Icls_TransferFromBackupBankClient TransferClient = new Icls_TransferFromBackupBankClient();
            var transferObj = TransferClient.SelectRow(obj.TransferId).SingleOrDefault();
            transferObj.Price = obj.Price;
            result = TransferClient.Update(transferObj, out Message);
            return JsonMessage(result, Message);
        }
        [HttpPost]
        public ActionResult ConfirmTransfer(int TransferId)
        {
            string Message = "";
            bool result = false;
            string CurrentDate = Utility.ToPersianDate(DateTime.Now);
            // بررسی موجودی بانک و بانک پشتیبان
            var Date = Utility.ToPersianDate(DateTime.Now);
            Icls_SPsClient db = new Icls_SPsClient();
            var dc = new TreasuryEntities();
            Icls_TransferFromBackupBankClient TransferClient = new Icls_TransferFromBackupBankClient();
            var transferObj = TransferClient.SelectRow(TransferId).SingleOrDefault();
           
            if (transferObj.IsOk)
            {
                transferObj.IsOk = false;
            }
            else
            {
                transferObj.IsOk = true;
            }
                transferObj.OkDate = Utility.ToPersianDate(DateTime.Now);
                result = TransferClient.Update(transferObj, out Message);
           
                if (result)
                    return JsonMessage(true, "ثبت با موفقیت انجام شد", null, false);
                else
                    return JsonMessage(result, Message);
      
        }
        #endregion
    }
}