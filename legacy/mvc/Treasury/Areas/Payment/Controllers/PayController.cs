using CSD.Treasury.Common.Enumeration;
using CSD.Treasury.Service.BankService;
using CSD.Treasury.Service.BehalfsService;
using CSD.Treasury.Service.ChequeFormatTypesService;
using CSD.Treasury.Service.Request_Payment_StatusDetailsService;
using CSD.Treasury.Service.Request_PaymentService;
using CSD.Treasury.Service.BeneficiaryService;
using CSD.Treasury.Service.BeneficiariesDetailInfoService;
using CSD.Treasury.Service.PermissiveSignature_CompleteProccessService;
using CSD.Treasury.Service.Request_Payment_BreakPriceService;
using CSD.Treasury.Service.Payment_ReferenceTypes_ValueService;
using CSD.Treasury.Service.TransferFromBackupBankService;
using CSD.Treasury.Service.ProceduresDB;
using CSD.Treasury.Service.PaymentService;
using CSD.Treasury.UI.Areas.Payment.Models;
using CSD.Treasury.UI.Controllers;
using Newtonsoft.Json;
using CSD.Treasury.UI.Models;
using System;
using System.Configuration;
using System.Linq;
using System.Web.Mvc;
using CSD.Treasury.DataModel;
using System.Globalization;



namespace CSD.Treasury.UI.Areas.Payment.Controllers
{
    [Authorize(Roles = "Admin,Manager,Payment")]
    public class PayController : BaseController
    {
        #region CashPayment
        public ActionResult Index(SearchModelRequestPayment obj)
        {
            var Banks = Utility.GetBanks();
            var BankLs = Utility.GetBasicBanks();
            ViewBag.Banks = BankLs.Where(d => Banks.Select(t => t.BasicBankID).Contains(d.BasicBankId)).ToList();
            //نمایش لیستی از انواع پرداخت
            ViewBag.Receipt_Pay_TypeDetailLs = Utility.GetReceipt_Pay_TypeDetailLs().Where(d => d.Receipt_Pay_TypeID == 1).ToList();
            ViewBag.payType = obj.PayType;
            return View(obj);
        }

        public ActionResult List(SearchModelRequestPayment obj)
        {
            int pageNum = obj.PageIndex;
            int pageSize = 10;
            if (obj.Status == "Aggregation")
            {
                pageSize = 200;
            }
            var UserId = User.Id;
            //string whereclouse = " AggrigateOrDuplicationID=NULL and ";
            Icls_SPsClient db = new Icls_SPsClient();
            var dc = new TreasuryEntities();
            string CurrentDate = Utility.ToPersianDate(DateTime.Now);
            var query = dc.GetAllRequestStatus(CurrentDate).Where(d => d.AggrigateOrDuplicationID == null).OrderByDescending(d => d.RequestID).ToList();
            //Icls_BankClient BankClient = new Icls_BankClient();
            //var Date = Utility.ToPersianDate(DateTime.Now);
           
            if (obj.Status == "ChequeReadyToDelivery")
            {
                   //if(ConfigurationManager.AppSettings["PayWhenCheckDelivered"] == "True")
                   //{
                   //    whereclouse = " UnKnown != Null   and Confirmed != NULL and ConfirmPayment != NULL and Paid == NULL and Delivered=NULL and  IsWithdrawType= " + false;
                   //}
                   //else
                   //{
                   //لیستی از چک های بلند مدت و نقدی که مجوز امضا دارند وچک های نقدی پرداخت شده هستند در صورتی که بلند مدت ها پرداخت نشده هستند و می تواند تحویل دهد و درزمان سررسید پرداخت می شود   
                   //whereclouse = whereclouse + "UnKnown != NUll   and Confirmed != NULL and ConfirmPayment != NULL  and Delivered=NULL and mojavezeEmza=" + true + " and  IsWithdrawType= " + false;
                   query = query.Where(d => d.Pay_TypeDetailID_source < 6 && d.Unknown != null && d.Confirmed != null && d.ConfirmPayment != null && d.Delivered == null && d.mojavezeEmza == true && d.IsWithdrawType == false).ToList();
                //}
            }
            else if (obj.Status == "DuringPayment" || obj.Status == "Aggregation")
            {
                //or Delivered = NULL

                //whereclouse = whereclouse + " UnKnown != Null and Confirmed != NULL and (ConfirmPayment = NULL or Paid = NULL or Delivered=NULL )  and !(Pay_TypeDetailID_source = 6 and ConfirmPayment != NULL) and !(Pay_TypeDetailID_source =32 and ConfirmPayment != NULL) and !(Pay_TypeDetailID_source =33 and ConfirmPayment != NULL) and !(Pay_TypeDetailID_source =7 and ConfirmPayment != NULL) and !(Pay_TypeDetailID_source =8 and ConfirmPayment != NULL) and mojavezeEmza=" + false + " and IsWithdrawType= " + false;
                query = query.Where(d => d.Unknown != null && d.Confirmed != null && (d.ConfirmPayment == null || d.Paid == null || d.Delivered == null) && !(d.Pay_TypeDetailID_source == 6 && d.ConfirmPayment != null) && !(d.Pay_TypeDetailID_source == 32 && d.ConfirmPayment != null) && !(d.Pay_TypeDetailID_source == 33 && d.ConfirmPayment != null) && !(d.Pay_TypeDetailID_source == 7 && d.ConfirmPayment != null) && !(d.Pay_TypeDetailID_source == 8 && d.ConfirmPayment != null) && d.mojavezeEmza == false && d.IsWithdrawType == false).ToList();
            }
            else if (obj.Status == "DeliveredPayment")
            {
                //whereclouse = whereclouse + "Cancellation=NUll and UnKnown != Null and Confirmed != NULL and ConfirmPayment != NULL   and Delivered!=NULL  and DeleiverdLasheCheque= null and Pay_TypeDetailID_source!=7 and Pay_TypeDetailID_source!=8 and Pay_TypeDetailID_source!=32 and Pay_TypeDetailID_source!=33  and IsWithdrawType= " + false;
                query = query.Where(d => d.Pay_TypeDetailID_source < 6 && d.Cancellation == null && d.Unknown != null && d.Confirmed != null && d.ConfirmPayment != null && d.Delivered != null && d.DeleiverdLasheCheque == null && d.Pay_TypeDetailID_source != 7 && d.Pay_TypeDetailID_source != 8 && d.Pay_TypeDetailID_source != 32 && d.Pay_TypeDetailID_source != 33 && d.IsWithdrawType == false).ToList();
                //and CovertCheque = null
            }
            else if (obj.Status == "CovertCheque")
            {
                //whereclouse = "Cancellation=NUll and UnKnown != Null and Confirmed != NULL and ConfirmPayment != NULL  and Delivered!=NULL and DeleiverdLasheCheque=NULL and CovertCheque!=null and IsWithdrawType= " + false;
                query = query.Where(d => d.Pay_TypeDetailID_source < 6 && d.Cancellation == null && d.Unknown != null && d.Confirmed != null && d.ConfirmPayment != null && d.DeleiverdLasheCheque == null && d.CovertCheque != null && d.IsWithdrawType == false).ToList();
            }
            else if (obj.Status == "DeleiverdLasheCheque")
            {
                //whereclouse = "Cancellation=NUll and UnKnown != Null and Confirmed != NULL and ConfirmPayment != NULL  and Delivered!=NULL and CovertCheque= NULL and DeleiverdLasheCheque!=null and IsWithdrawType= " + false;
                query = query.Where(d => d.Pay_TypeDetailID_source < 6 && d.Cancellation == null && d.Unknown != null && d.Confirmed != null && d.ConfirmPayment != null && d.Delivered != null && d.CovertCheque == null && d.DeleiverdLasheCheque != null && d.IsWithdrawType == false).ToList();
            }
            else if (obj.Status == "GetCash")
            {

                //whereclouse = " UnKnown != Null and Confirmed != NULL and ConfirmPayment != NULL and  Paid != NULL  and Pay_TypeDetailID_source =7 and IsWithdrawType= " + false;
                query = query.Where(d => d.Unknown != null && d.Confirmed != null && d.ConfirmPayment != null && d.Paid != null && d.Pay_TypeDetailID_source == 7 && d.IsWithdrawType == false).ToList();
            }
            else if (obj.Status == "ATM")
            {
                //whereclouse = " UnKnown != Null and Confirmed != NULL and ConfirmPayment != NULL and Pay_TypeDetailID_source =8 and IsWithdrawType= " + false;
                query = query.Where(d => d.Unknown != null && d.Confirmed != null && d.ConfirmPayment != null && d.Pay_TypeDetailID_source == 8 && d.IsWithdrawType == false).ToList();
            }
            else if (obj.Status == "InternetPay")
            {
                //whereclouse = " UnKnown != Null and Confirmed != NULL and ConfirmPayment != NULL and Pay_TypeDetailID_source =32 and IsWithdrawType= " + false;
                query = query.Where(d => d.Unknown != null && d.Confirmed != null && d.ConfirmPayment != null && d.Pay_TypeDetailID_source == 32 && d.IsWithdrawType == false).ToList();
            }
            else if (obj.Status == "Satna")
            {
                //whereclouse = " UnKnown != Null and Confirmed != NULL and ConfirmPayment != NULL and (  Paid != NULL or Paid = NULL )  and Pay_TypeDetailID_source =33 and IsWithdrawType= " + false;
                query = query.Where(d => d.Unknown != null && d.Confirmed != null && d.ConfirmPayment != null && d.Pay_TypeDetailID_source == 33 && d.IsWithdrawType == false).ToList();
            }
            else if (obj.Status == "safte")
            {
                //whereclouse = " UnKnown != Null and Confirmed != NULL and ConfirmPayment != NULL and (  Paid != NULL or Paid = NULL )  and Pay_TypeDetailID_source = 6 and IsWithdrawType= " + false;
                query = query.Where(d => d.Unknown != null && d.Confirmed != null && d.ConfirmPayment != null && (d.Paid != null || d.Paid == null) && d.Pay_TypeDetailID_source == 6 && d.IsWithdrawType == false).ToList();
            }


            Icls_Request_PaymentClient RP = new Icls_Request_PaymentClient();


            if (!User.IsInRole("Admin") && !User.IsInRole("Manager"))
            {
                //whereclouse = whereclouse + " and UserID=" + UserId;
                query = query.Where(d => d.UserID == UserId).ToList();
            }
            if (obj.ShowConfirmPayment)
            {
                //whereclouse = whereclouse + " and ConfirmPayment != NULL ";
                query = query.Where(d => d.ConfirmPayment != null).ToList();
            }
            if (!string.IsNullOrEmpty(obj.DueDate))
            {
                //whereclouse = whereclouse + " and  DueDate = \"" + obj.DueDate + "\"";
                query = query.Where(d => d.DueDate == obj.DueDate).ToList();
            }
            if (obj.RequsetId != 0)
            {
                //whereclouse = whereclouse + " and RequestID =" + obj.RequsetId;
                query = query.Where(d => d.RequestID == obj.RequsetId).ToList();
            }
            if (obj.ChequeNo != 0)
            {
                //whereclouse = whereclouse + " and ChequeNo =" + obj.ChequeNo;
                query = query.Where(d => d.ChequeNo == obj.ChequeNo).ToList();
            }
            if (!string.IsNullOrEmpty(obj.FiscalYear))
            {
                //whereclouse = whereclouse + " and FiscalYear=\"" + obj.FiscalYear + "\"";
                query = query.Where(d => d.FiscalYear == obj.FiscalYear).ToList();
            }
            if (obj.BankId != 0)
            {
                //whereclouse = whereclouse + " and Source_PaymentValue =\"" + obj.BankId + "\"";
                query = query.Where(d => d.Source_PaymentValue == obj.BankId.ToString()).ToList();
            }
            //if (obj.PayType == 0)
            //{
            //    whereclouse = whereclouse + " and Pay_TypeDetailID_source != 7 ";
            //}
            //if (obj.PayType == 7)
            //{
            //    whereclouse = whereclouse + " and Pay_TypeDetailID_source == 7 ";
            //}
            if (obj.payTypeLs != null)
            {
                query = query.Where(d => obj.payTypeLs.ToList().Contains(d.Pay_TypeDetailID_source.Value)).ToList();
                //wherkeclouse = whereclouse + " and (";
                //foreach (var item in obj.payTypeLs)
                //{
                //whereclouse = whereclouse + "Pay_TypeDetailID_source ==" + item + " or ";
                //}
                //whereclouse = whereclouse + ")";
                //var lenght = whereclouse.Length;
                //whereclouse = whereclouse.Remove(lenght - 4, 2);
            }

          
            //var query = RP.GetAllRequestStatus_DynamicWhereClause(CurrentDate, whereclouse, pageNum + 1, pageSize, "RequestID", "dec", false, out totalRows).ToList();
            if (ConfigurationManager.AppSettings["PayWhenCheckDelivered"] == "True" || (ConfigurationManager.AppSettings["PayWhenCheckPrinted"] == "True" && ((obj.Status == "ATM") || (obj.Status == "InternetPay") || (obj.Status == "Satna"))))
            
                {
                
                foreach (var item in query)
                {
                    //Pay_TypeDetailID_source=7 پرداخت نقدی
                    if (item.Pay_TypeDetailID_source != 7)
                    {
                        //$Sp_GetBankBalance$
                        var qury = db.Sp_GetBankBalance("13000101", CurrentDate, Int32.Parse(item.Source_PaymentValue), 1, false);

                        var Bank = new GetBankBalance_Result();
                        //if (query.Count() == 1)
                        //{
                        Bank = qury.SingleOrDefault();
                        //}
                        //else
                        //{
                        //    Bank = qury.LastOrDefault();
                        //}
                        //var BackupBank = BankClient.SelectBy_DynamicWhereClause("Backup_BankID =" + Bank.BankID, 1, 10, "BankID", "int", false, out totalRows);
                        var BackupBank = dc.Banks.Where(d => d.Backup_BankID == Bank.BankID);
                        var ChequePrice = item.PayAmount;
                        GetBankBalance_Result Backup_Bank = new GetBankBalance_Result();
                        if (BackupBank.Count() != 0)
                        {
                            //$Sp_GetBankBalance$
                            var qury1 = db.Sp_GetBankBalance("13000101", CurrentDate, BackupBank.SingleOrDefault().BankID, 1, false);

                            //if (query.Count() == 1)
                            //{
                            Backup_Bank = qury.FirstOrDefault();
                            //}
                            //else
                            //{
                            //    Backup_Bank = qury.LastOrDefault();
                            //}
                        }

                        if ((ChequePrice <= Bank.balance && (ConfigurationManager.AppSettings["CompletePayBackup"] == "True" && BackupBank.Count() == 0))
                        || (ChequePrice <= Bank.balance && (ConfigurationManager.AppSettings["CompletePayBackup"] == "False")))

                        {
                            item.BankBalanceStatus = "HasPrice";
                        }
                        else if (BackupBank.Count() != 0)
                        {
                            decimal? remain = 0;
                            //برداشت مبلغ به صورت کامل از پشتیبان
                            if (ConfigurationManager.AppSettings["CompletePayBackup"] == "True")
                            {
                                remain = ChequePrice;
                            }
                            else
                            {
                                remain = ChequePrice - Bank.balance;
                            }
                            //اگر موجودی حساب پشتیبان بیشتر یا مساوی مبلغ کمبود حساب جاری است
                            if (Backup_Bank.balance >= remain)
                            {
                                item.BankBalanceStatus = "HasBackUp";
                            }
                            else
                            {
                                item.BankBalanceStatus = "NoPriceNoBackUp";
                            }
                        }
                        else
                        {
                            item.BankBalanceStatus = "NoPrice";
                        }

                    }

                }
            }
            var S = "";

            //var All = RP.GetAllRequestStatus_DynamicWhereClause(CurrentDate, "  AggrigateOrDuplicationID !=NULL ", pageNum + 1, 10000 * 10000, "RequestID", "dec", false, out totalRowsAll).ToList();

            //var qls = from q in query
            //          where All.Select(d => d.AggrigateOrDuplicationID).Contains(q.RequestID)
            //          select q;

            //foreach (var item in qls)
            //{
            //    S = S + "," + item.RequestID;
            //    int totalRows1;
            //    //if (item.AggrigateOrDuplicationStatusID == (int)RequestDescription.Duplication || item.AggrigateOrDuplicationStatusID == (int)RequestDescription.ResultOfIntegration || item.AggrigateOrDuplicationStatusID == (int)RequestDescription.ResultOfIntegrationRequest)
            //    //{
            //    item.ChildList = RP.GetAllRequestStatus_DynamicWhereClause(CurrentDate, "AggrigateOrDuplicationID =" + item.RequestID, 1, 100, "RequestID", "dec", false, out totalRows1).ToList();
            //    //}
            //}
            var totalRows = query.Count();
            obj.TotalPageCount = (int)Math.Ceiling(totalRows / (double)pageSize);
            obj.CurrentPage = pageNum;

            obj.List = query;
            obj.CurrentPage = obj.PageIndex;
            obj.TotalItemCount = totalRows;

            return PartialView(obj);
        }

        public ActionResult ResultOfIntegrationRequest(int requestId)
        {
            var obj = new SearchModelRequestPayment();
            obj.RequsetId = requestId;
            int pageNum = obj.NotAccepted ? 0 : obj.PageIndex;
            int pageSize = obj.NotAccepted ? 3000 : 10;
            string CurrentDate = Utility.ToPersianDate(DateTime.Now);
            int totalRows;
            var db = new TreasuryEntities();
            //Icls_Request_PaymentClient RP = new Icls_Request_PaymentClient();
            //var ls = RP.GetAllRequestStatus_DynamicWhereClause(CurrentDate, "AggrigateOrDuplicationID = " + obj.RequsetId, 1, 100, "RequestID", "dec", false, out totalRows).ToList();
            var ls = db.GetAllRequestStatus(CurrentDate).Where(d => d.AggrigateOrDuplicationID == obj.RequsetId).ToList();
            totalRows = ls.Count();
            obj.TotalPageCount = (int)Math.Ceiling(totalRows / (double)pageSize);
            obj.CurrentPage = pageNum;
            obj.List = ls;
            obj.CurrentPage = obj.PageIndex;
            obj.TotalItemCount = totalRows;
            return PartialView(obj);
        }

        #endregion
        public ActionResult pay(int requestId, int Type)
        {
            ViewBag.type = Type;
            var db = new TreasuryEntities();
            Icls_Request_PaymentClient RP = new Icls_Request_PaymentClient();
            Icls_Request_Payment_StatusDetailsClient RS = new Icls_Request_Payment_StatusDetailsClient();
            //var RequestPayment = RP.SelectRow((int)requestId).SingleOrDefault();
            var RequestPayment = db.Request_Payment.Find(requestId);
            if (RequestPayment != null)
            {
                Icls_BehalfsClient BG = new Icls_BehalfsClient();
                ViewBag.BehalfGroupId = BG.SelectRow(RequestPayment.BehalfID).SingleOrDefault().BehalfGroupID;
            }
            ViewBag.RequestID = requestId;

            //بدست آوردن نوع چک و قطع های چک براساس بانکی که ذخیره شده در درخواست وجه
            var BankId = int.Parse(RequestPayment.Source_PaymentValue);
            //Icls_BankClient B = new Icls_BankClient();
            //var BasicBankId = B.SelectRow(BankId).SingleOrDefault().BasicBankID;
            var BasicBankId = db.Banks.SingleOrDefault(d => d.BankID == BankId).BasicBankID;
            Icls_ChequeFormatTypesClient CFT = new Icls_ChequeFormatTypesClient();

            string where = "BankID =" + BasicBankId;
            int totalRows;
            var ls = CFT.SelectBy_DynamicWhereClause(where, 1, 100, "ChequeFormatID", "int", true, out totalRows).ToList();

            ViewBag.checkFormatType = ls;

            return PartialView(RequestPayment);
        }
        public ActionResult ShowChequeDetails(DataModel.GetAllRequestStatus_Result item)
        {
            var chequeFormatTypeLs = Utility.GetChequeFormatTypeLs(int.Parse(item.Source_PaymentValue), true);
            bool ISEntezami = false;
            ViewBag.hasMain_Unique_Account_No = false;
            ViewBag.IzArzi = false;
            var db = new TreasuryEntities();
            PersianCalendar pc = new PersianCalendar();
            var CurrentDate = DateTime.Now;
            var currentYear = pc.GetYear(CurrentDate);
            string persianCurrentDate = Utility.ToPersianDate(CurrentDate);
            ViewBag.ExchangeType = 0;
            if (item.ExchangeAmount != null)
            {
                ViewBag.IzArzi = true;
                var bankid = int.Parse(item.Source_PaymentValue);
                var bank = db.Banks.SingleOrDefault(d => d.BankID == bankid);
                ViewBag.ExchangeRate = 0;               
                ViewBag.ExchangeType = bank.ExchangeType;

                if (bank.ExchangeType == 1)
                {
                    var Exchangestandard = db.ExchangeStandards.SingleOrDefault(d => d.FiscalYear == currentYear);
                    if (Exchangestandard != null)
                    {
                        ViewBag.ExchangeRate = Exchangestandard.Price;
                    }
                }
                else if (bank.ExchangeType == 2 || bank.ExchangeType == 3)
                {
                    var Exchange = db.ExchangeRates.SingleOrDefault(d => d.ExchangeDateRate == persianCurrentDate.Replace("/", ""));
                    if (Exchange != null)
                    {
                        if (bank.ExchangeType == 2)
                        {
                            ViewBag.ExchangeRate = Exchange.FormalPrice;
                        }
                        if (bank.ExchangeType == 3)
                        {
                            ViewBag.ExchangeRate = Exchange.InformalPrice;
                        }
                    }
                }
            }

            //{
            //    options += "<option value='" + lb.ChequeFormatID + "'>" + Utility.GetChequeDesc(lb.FormatDescriptionID_Main) + "</option>";
            //}
            if (item.Pay_TypeDetailID_source == 2)
            {
                ISEntezami = true;
            }
            if (!string.IsNullOrEmpty(item.Main_Unique_Account_No))
            {
                ViewBag.hasMain_Unique_Account_No = true;
            }
            ViewBag.ISEntezami = ISEntezami;
            ViewBag.RequestId = item.RequestID;
            ViewBag.ChequeFormatTypeLs = chequeFormatTypeLs;
            ViewBag.Pay_Type = item.Pay_TypeDetailID_source;
            ViewBag.DefaultValue = ConfigurationManager.AppSettings["ChequeDefaultValue"];
            return PartialView(new ChequeDetails(item.RequestID));
        }


        #region Delivey
        public ActionResult Deliverd(int RequestId)
        {
            try
            {

                Icls_Request_Payment_StatusDetailsClient PS = new Icls_Request_Payment_StatusDetailsClient();
                string message = "";
                int totalRow;
                //بدست آوردن وضعیت های مربوط به هر درخواست وجه
                var AllStatus = PS.SelectBy_DynamicWhereClause("RequestID =" + RequestId, 1, 10000, "Request_Payment_StatusDetailsID", "int", true, out totalRow).Select(d => d.RequestStatusTypeID);
                var db = new TreasuryEntities();


                if (AllStatus.Contains((decimal)RequestStatusTypes.Confirmed) && !AllStatus.Contains((decimal)RequestStatusTypes.Paid))
                {

                    //Service.Request_Payment_StatusDetailsService.Request_Payment_StatusDetails obj = new Service.Request_Payment_StatusDetailsService.Request_Payment_StatusDetails();
                    //DataModel.Request_Payment_StatusDetails obj = new DataModel.Request_Payment_StatusDetails();
                    var persianDate = Utility.ToPersianDate(DateTime.Now);


                    ////پرداخت
                    //obj.ActionDate = persianDate;
                    //obj.ActionTime = DateTime.Now.TimeOfDay;
                    //obj.RequestID = RequestId;
                    //obj.UserID = User.Id;
                    //obj.HostName = System.Net.Dns.GetHostName();
                    //obj.RequestStatusTypeID = (decimal)RequestStatusTypes.Paid;
                    //var result = PS.Insert(obj, out message);

                    //تحویل
                    //Service.Request_Payment_StatusDetailsService.Request_Payment_StatusDetails obj1 = new Service.Request_Payment_StatusDetailsService.Request_Payment_StatusDetails();
                    DataModel.Request_Payment_StatusDetails obj = new DataModel.Request_Payment_StatusDetails();
                    obj.ActionDate = persianDate;
                    obj.ActionTime = DateTime.Now.TimeOfDay;
                    obj.RequestID = RequestId;
                    obj.UserID = User.Id;
                    obj.HostName = System.Net.Dns.GetHostName();
                    obj.RequestStatusTypeID = (decimal)RequestStatusTypes.Delivered;
                    //var result1 = PS.Insert(obj, out message);
                    db.Request_Payment_StatusDetails.Add(obj);
                    db.SaveChanges();

                    return JsonMessage(true, message);

                }
                return Json(false, JsonRequestBehavior.AllowGet);
            }
            catch (Exception)
            {
                return Json(false, JsonRequestBehavior.AllowGet);
                throw;
            }

        }
        #endregion

        #region shebaLs
        public JsonResult shebaLs(decimal RequestId)
        {
            Icls_Request_PaymentClient RP = new Icls_Request_PaymentClient();
            Icls_BeneficiaryClient beneficiaryclient = new Icls_BeneficiaryClient();
            Icls_BeneficiariesDetailInfoClient bDetailsClient = new Icls_BeneficiariesDetailInfoClient();
            var db = new TreasuryEntities();
            int Totalrow;
            //var request = RP.SelectRow(RequestId).SingleOrDefault();
            var request = db.Request_Payment.Find(RequestId);
            var beneficiary = beneficiaryclient.SelectBy_DynamicWhereClause("Unique_Account_No=" + request.Unique_Account_No.Trim(), 1, 100, "BeneficiarID", "int", false, out Totalrow).FirstOrDefault();
            var shebals = bDetailsClient.SelectBy_DynamicWhereClause("BeneficiarID=" + beneficiary.BeneficiarID, 1, 100, "BeneficiarDetailID", "int", false, out Totalrow).ToList();
            return Json(shebals, JsonRequestBehavior.AllowGet);

        }
        #endregion

        #region AccountNumLs
        public JsonResult AccountNumLs(decimal RequestId)
        {
            Icls_Request_PaymentClient RP = new Icls_Request_PaymentClient();
            Icls_BeneficiaryClient beneficiaryclient = new Icls_BeneficiaryClient();
            Icls_BeneficiariesDetailInfoClient bDetailsClient = new Icls_BeneficiariesDetailInfoClient();
            var db = new TreasuryEntities();

            int Totalrow;
            //var request = RP.SelectRow(RequestId).SingleOrDefault();
            var request = db.Request_Payment.Find(RequestId);
            var beneficiary = beneficiaryclient.SelectBy_DynamicWhereClause("Unique_Account_No = \"" + request.Unique_Account_No + "\" ", 1, 100, "BeneficiarID", "int", false, out Totalrow).FirstOrDefault();

            if (beneficiary != null)
            {
                var AccountNumLs = bDetailsClient.SelectBy_DynamicWhereClause("BeneficiarID=" + beneficiary.BeneficiarID, 1, 100, "BeneficiarDetailID", "int", false, out Totalrow).ToList();
                return Json(AccountNumLs, JsonRequestBehavior.AllowGet);
            }

            return Json(null, JsonRequestBehavior.AllowGet);
        }
        #endregion


        /// <summary>
        /// پیگیری وضعیت پرداخت ها
        /// </summary>
        /// <returns></returns>
        #region FollowUpPayment
        public ActionResult FollowUpPayment(SearchModelPayment obj)
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
            var Month = pc.GetMonth(DateTime.Now);

            if (Month >= 10 && ConfigurationManager.AppSettings["YearPlus"] == "True")
            {
                ViewBag.FiscalYear = (pc.GetYear(DateTime.Now) + 1).ToString();
            }

            return View(obj);
        }
        public ActionResult ShowFollowUpPayment(SearchModelPayment obj)
        {
            int pageNum = obj.PageIndex;
            int pageSize = 12;
            string whereclouse = "";
            var UserId = User.Id;
            var db = new TreasuryEntities();
            if (!User.IsInRole("Admin") && !User.IsInRole("Manager"))
            {
                whereclouse = "and UserID=" + UserId;
            }
            if (!string.IsNullOrEmpty(obj.RequestDateFrom) && !string.IsNullOrEmpty(obj.RequestDateTo))
            {
                whereclouse = whereclouse + " and DueDate >=" + obj.RequestDateFrom + "  and DueDate <= " + obj.RequestDateTo + "";
            }
           

            if (!string.IsNullOrEmpty(obj.FiscalYear))
                {
                whereclouse = whereclouse + " and FiscalYear=" + obj.FiscalYear + "";
            }
            if (obj.PayAmount != 0)
            {
                whereclouse = whereclouse + " and  PayAmount =" + obj.PayAmount + " ";
            }
            if (obj.ExchangeTypeID != 0)
            {
                whereclouse = whereclouse + " and ExchangeTypeID=" + obj.ExchangeTypeID;
            }
            if (obj.BehalfID != 0)
            {
                whereclouse = whereclouse + " and BehalfID=" + obj.BehalfID;
            }
            if (obj.Pay_TypeDetailID_source != 0)
            {
                whereclouse = whereclouse + " and Pay_TypeDetailID_source=" + obj.Pay_TypeDetailID_source;
            }
            if (obj.RequsetId != 0)
            {
                whereclouse = whereclouse + " and rp.RequestID=" + obj.RequsetId;
            }
            if (!string.IsNullOrEmpty(obj.Unique_Account_No))
            {
                whereclouse = whereclouse + " and Unique_Account_No=" + obj.Unique_Account_No + "";
            }


            //Icls_SPsClient spsClient = new Icls_SPsClient();
            var query = db.GetRequestPayment_Payment_WithLastStates("PaymentID is not null and Request_Payment_Status_RequestStatusTypeID=3 " + whereclouse, "PaymentDate").ToList();
            var totalRows = query.Count();



            obj.TotalPageCount = (int)Math.Ceiling(totalRows / (double)pageSize);
            obj.CurrentPage = pageNum;
            obj.List = query.Skip((obj.PageIndex - 1) * pageSize).Take(pageSize).ToList();
            obj.CurrentPage = obj.PageIndex;
            obj.TotalItemCount = totalRows;

            return PartialView(obj);
        }
        #endregion

        /// <summary>
        /// تبدیل چک انتظامی به نقد 
        /// یا تحویل  گرفتن لاشه چک از کاربر
        /// </summary>
        /// <param name="RequestId"></param>
        /// <param name="IsConvert"></param>
        /// <param name="DueDate"></param>
        /// <returns></returns>
        #region  ConvertOrDeleiverdLasheCheque

        public ActionResult ConvertOrDeleiverdLasheCheque(int RequestId, int Pay_TypeDetailID_source, decimal remain = 0, int BackupBankId = 0, string DueDate = "", bool IsConvert = false)
        {
            try
            {
                var db = new TreasuryEntities();
                Icls_SPsClient dc = new Icls_SPsClient();
                DueDate = !string.IsNullOrEmpty(DueDate) ? DueDate.Replace("/", string.Empty) : "";
                //int TotalRow;
                //Icls_PermissiveSignature_CompleteProccessClient PermisiveClient = new Icls_PermissiveSignature_CompleteProccessClient();
                //Icls_Request_PaymentClient RP = new Icls_Request_PaymentClient();
                //Icls_PaymentClient paymentClient = new Icls_PaymentClient();
                //Payment_ReferenceTypes_ValueClient paymentRefrenceValueClient = new Payment_ReferenceTypes_ValueClient();
                //Icls_Request_Payment_BreakPriceClient breackPriceClient = new Icls_Request_Payment_BreakPriceClient();
                //Icls_TransferFromBackupBankClient transferFromBackupBankClient = new Icls_TransferFromBackupBankClient();

                //int totalrow;
                //string message = "", message1 = "", message2 = "", message3 = "";

                //if (IsConvert)
                //{
                var CurrentDate = Utility.ToPersianDate(DateTime.Now);
                var dayDuedate = int.Parse(DueDate.Substring(6, 2));
                var dayCurrent = int.Parse(CurrentDate.Substring(6, 2));
                var MonthDuedate = int.Parse(DueDate.Substring(4, 2));
                var MonthDayCurrent = int.Parse(CurrentDate.Substring(4, 2));
                //درصورتی که چک مدت دار باشد و تاریخ سررسید را کوچکتر از تاریخ روز انتخاب کرده باشد 
                //چون تاریخ درخواست باید کوچکتر از تاریخ سررسید باشد پس تاریخ سررسیدر ا یکی کم می کنیم
                if (Pay_TypeDetailID_source == 4 && (dayDuedate < dayCurrent || MonthDuedate < MonthDayCurrent))
                {
                    dayDuedate = dayDuedate - 1;
                    if (MonthDuedate < 6 && dayDuedate == 0)
                    {
                        dayDuedate = 31;
                    }
                    else if (MonthDuedate > 6 && dayDuedate == 0)
                    {
                        dayDuedate = 30;
                    }

                    CurrentDate = DueDate.Substring(0, 6) + (dayDuedate < 10 ? "0" + dayDuedate.ToString() : dayDuedate.ToString());
                }
                else if (Pay_TypeDetailID_source == 1)
                {
                    CurrentDate = DueDate.Replace("/", string.Empty);
                }
                //var CurrentDate = Utility.ToPersianDate(DateTime.Now);
                //save Convert status
                //Icls_Request_Payment_StatusDetailsClient RPS = new Icls_Request_Payment_StatusDetailsClient();

                var persianDate = Utility.ToPersianDate(DateTime.Now);

                //Service.Request_Payment_StatusDetailsService.Request_Payment_StatusDetails objSts = new Service.Request_Payment_StatusDetailsService.Request_Payment_StatusDetails();
                var objSts = new CSD.Treasury.DataModel.Request_Payment_StatusDetails();
                objSts.ActionDate = persianDate;
                objSts.UserID = User.Id;
                objSts.ActionTime = DateTime.Now.TimeOfDay;
                objSts.RequestStatusTypeID = (decimal)Common.Enumeration.RequestStatusTypes.CovertedCheque;
                objSts.RequestID = RequestId;

                objSts.HostName = System.Net.Dns.GetHostName();
                db.Request_Payment_StatusDetails.Add(objSts);
                //result = RPS.Insert(objSts, out message);

                //درخواست وجه
                //var request = RP.SelectRow(RequestId).SingleOrDefault();
                var request = db.Request_Payment.Find(RequestId);

                //insert new Request that is result of convert Cheque
                CSD.Treasury.DataModel.Request_Payment obj = new CSD.Treasury.DataModel.Request_Payment();
                // var dateNow = DateTime.Now;
                obj.UserID = request.UserID;
                obj.FiscalYear = request.FiscalYear;
                obj.RequestDate = CurrentDate;
                obj.RequestCreatedTime = request.RequestCreatedTime;
                obj.Priority = request.Priority;
                obj.Unique_Account_No = request.Unique_Account_No;
                obj.BehalfID = request.BehalfID;
                obj.ExchangeTypeID = request.ExchangeTypeID;
                obj.PayAmount = request.PayAmount;
                obj.Main_Unique_Account_No = request.Main_Unique_Account_No;
                obj.Destination_PaymentValue = request.Destination_PaymentValue;
                obj.Destination_TypesID = request.Destination_TypesID;
                obj.EquivalencePrice = request.EquivalencePrice;
                obj.ExchangeRate = request.ExchangeRate;
                foreach (var item in request.Payment_ReferenceTypes_Value.ToList())
                {
                    obj.Payment_ReferenceTypes_Value.Add(item);
                }

                obj.RefrenceRequestId = RequestId;
                obj.ChequeElements_PrintLayout = request.ChequeElements_PrintLayout;
                obj.ChequeNo = request.ChequeNo;
                obj.Source_TypeID = request.Source_TypeID;
                obj.BankID = request.BankID;
                obj.Pay_TypeDetailID_source = Pay_TypeDetailID_source;
                //if (CurrentDate == DueDate)
                //{
                //    //تبدیل به چک روز
                //    obj.Pay_TypeDetailID_source = 1;
                //}
                //else
                //{
                //    //تبدیل به چک مدت دار
                //    obj.Pay_TypeDetailID_source = 4;
                //}

                obj.Source_PaymentValue = request.Source_PaymentValue;
                //obj.UniqueAccountType = request.UniqueAccountType;
                obj.Unique_Account_No = request.Unique_Account_No;
                obj.DueDate = DueDate;

                obj.Description = Common.Enumeration.RequestDescription.ResultOfConvertCheque.ToString();
                //var result1 = RP.Insert(obj, out message2);
                db.Request_Payment.Add(obj);



                //if (result1)
                //{
                //var requestBreakPrice = breackPriceClient.SelectBy_DynamicWhereClause("RequestID=" + RequestId, 1, 100, "BreakId", "dec", false, out totalrow);
                var requestBreakPrice = db.Request_Payment_BreakPrice.SingleOrDefault(d => d.RequestID == RequestId);
                if (requestBreakPrice != null)
                {
                    var Request_Payment_BreakPriceOBJ = new CSD.Treasury.DataModel.Request_Payment_BreakPrice()
                    {
                        Price_Rial = requestBreakPrice.Price_Rial,
                        ExchangeTypeID = requestBreakPrice.ExchangeTypeID,
                        Exchange_Rate = requestBreakPrice.Exchange_Rate,
                        Exchange_Amount = requestBreakPrice.Exchange_Amount
                    };

                    obj.Request_Payment_BreakPrice.Add(Request_Payment_BreakPriceOBJ);

                    //Service.Request_Payment_BreakPriceService.Request_Payment_BreakPrice requestbreakpriceOBJ = new Service.Request_Payment_BreakPriceService.Request_Payment_BreakPrice();
                    //requestbreakpriceOBJ.RequestID = decimal.Parse(message2.Split(';')[1]);
                    //requestbreakpriceOBJ.Price_Rial = requestBreakPrice.FirstOrDefault().Price_Rial;
                    //requestbreakpriceOBJ.ExchangeTypeID = requestBreakPrice.FirstOrDefault().ExchangeTypeID;
                    //requestbreakpriceOBJ.Exchange_Amount = requestBreakPrice.FirstOrDefault().Exchange_Amount;
                    //requestbreakpriceOBJ.Exchange_Rate = requestBreakPrice.FirstOrDefault().Exchange_Rate;
                    //breackPriceClient.Insert(requestbreakpriceOBJ, out message3);
                }
                //var paymentRefrenceTypeValue = paymentRefrenceValueClient.SelectBy_DynamicWhereClause("RequestId=" + RequestId, 1, 100, "ReferenceTypeValueID", "int", false, out totalrow);
                var paymentRefrenceTypeValue = db.Payment_ReferenceTypes_Value.SingleOrDefault(d => d.RequestId == RequestId);
                if (paymentRefrenceTypeValue != null)
                {
                    var Payment_ReferenceTypes_ValueOBJ = new DataModel.Payment_ReferenceTypes_Value()
                    {
                        ReferenceID = paymentRefrenceTypeValue.ReferenceID,
                        ReferenceValue = paymentRefrenceTypeValue.ReferenceValue
                    };
                    db.Payment_ReferenceTypes_Value.Add(paymentRefrenceTypeValue);
                    //Service.Payment_ReferenceTypes_ValueService.Payment_ReferenceTypes_Value PaymentRefrenceValueOBJ = new Service.Payment_ReferenceTypes_ValueService.Payment_ReferenceTypes_Value();
                    //foreach (var item in paymentRefrenceTypeValue)
                    //{
                    //    PaymentRefrenceValueOBJ.RequestId = decimal.Parse(message2.Split(';')[1]);
                    //    PaymentRefrenceValueOBJ.ReferenceID = item.ReferenceID;
                    //    PaymentRefrenceValueOBJ.ReferenceValue = item.ReferenceValue;
                    //}
                    //paymentRefrenceValueClient.Insert(PaymentRefrenceValueOBJ, out message3);
                }
                //ثبت نامشخص در جدول وضعیت ها
                //Service.Request_Payment_StatusDetailsService.Request_Payment_StatusDetails objSts1 = new Service.Request_Payment_StatusDetailsService.Request_Payment_StatusDetails();
                var objSts1 = new Treasury.DataModel.Request_Payment_StatusDetails();

                objSts1.ActionDate = CurrentDate;
                objSts1.ActionTime = DateTime.Now.TimeOfDay;
                objSts1.UserID = User.Id;
                objSts1.RequestStatusTypeID = (decimal)Common.Enumeration.RequestStatusTypes.Unknown;
                //objSts1.RequestID = decimal.Parse(message2.Split(';')[1]);
                objSts1.HostName = System.Net.Dns.GetHostName();
                obj.Request_Payment_StatusDetails.Add(objSts1);
                //RPS.Insert(objSts1, out message);


                //ثبت تایید درخواست در جدول وضعیت ها


                var objSts2 = new Treasury.DataModel.Request_Payment_StatusDetails();

                objSts2.ActionDate = CurrentDate;
                objSts2.ActionTime = DateTime.Now.TimeOfDay;
                objSts2.UserID = User.Id;
                objSts2.RequestStatusTypeID = (decimal)Common.Enumeration.RequestStatusTypes.Confirmed;
                objSts2.HostName = System.Net.Dns.GetHostName();
                obj.Request_Payment_StatusDetails.Add(objSts2);

                //RPS.Insert(objSts1, out message);

                //ثبت تایید برای پرداخت در جدول وضعیت ها


                var objSts3 = new Treasury.DataModel.Request_Payment_StatusDetails();

                objSts3.ActionDate = CurrentDate;
                objSts3.ActionTime = DateTime.Now.TimeOfDay;
                objSts3.UserID = User.Id;
                objSts3.RequestStatusTypeID = (decimal)Common.Enumeration.RequestStatusTypes.ConfirmPayment;
                objSts3.HostName = System.Net.Dns.GetHostName();
                obj.Request_Payment_StatusDetails.Add(objSts3);
                //RPS.Insert(objSts1, out message);

                if (remain > 0)
                {
                    CSD.Treasury.DataModel.TransferFromBackupBank transferOBJ = new CSD.Treasury.DataModel.TransferFromBackupBank()
                    {
                        From_BankID = BackupBankId,
                        To_BankID = int.Parse(request.Source_PaymentValue),
                        OkDate = CurrentDate,
                        Price = decimal.ToInt64(remain),
                        IsOk = true,
                        TransferDate = CurrentDate
                    };
                    obj.TransferFromBackupBanks.Add(transferOBJ);
                }

                //RPS.Insert(objSts1, out message);

                //درصورتی که چک روز باشد پرداخت شده حساب می گردد در صورتی که مدت دار باشد در کنسول ابتدای برنامه باید وضعیتش مشخص گردد


                if (Pay_TypeDetailID_source == 1)
                {
                    //ثبت پرداخت در جدول وضعیت ها
                    //objSts1.RequestStatusTypeID = (decimal)Common.Enumeration.RequestStatusTypes.Paid;
                    //obj.Request_Payment_StatusDetails.Add(objSts1);
                    var objSts5 = new Treasury.DataModel.Request_Payment_StatusDetails();
                    objSts5.ActionDate = CurrentDate;
                    objSts5.ActionTime = DateTime.Now.TimeOfDay;
                    objSts5.UserID = User.Id;
                    objSts5.RequestStatusTypeID = (decimal)Common.Enumeration.RequestStatusTypes.Paid;
                    objSts5.HostName = System.Net.Dns.GetHostName();
                    obj.Request_Payment_StatusDetails.Add(objSts5);
                    //RPS.Insert(objSts1, out message);


                    //save payment
                    //Service.PaymentService.Payment payment = new Service.PaymentService.Payment();
                    var payment = new Treasury.DataModel.Payment();
                    //payment.RequestID = decimal.Parse(message2.Split(';')[1]);
                    payment.PaymentDate = Utility.ToPersianDate(DateTime.Now);
                    payment.HostName = System.Net.Dns.GetHostName();
                    payment.UserID = User.Id;
                    payment.Effective = true;
                    obj.Payments.Add(payment);
                    //var Result = paymentClient.Insert(payment, out message1);

                    //objSts1.RequestStatusTypeID = (decimal)Common.Enumeration.RequestStatusTypes.Paid;
                    //obj.Request_Payment_StatusDetails.Add(objSts1);
                    //result = RPS.Insert(objSts1, out message);
                }

                //ثبت تحویل در جدول وضعیت ها    


                //var iFTransferIsExict = transferFromBackupBankClient.SelectBy_DynamicWhereClause("RequestID =" + RequestId, 1, 100, "TransferId", "Int64", false, out totalrow).FirstOrDefault();
                //var iFTransferIsExict = db.TransferFromBackupBanks.SingleOrDefault(d => d.RequestID == RequestId);
                //if (iFTransferIsExict != null)
                //{


                //}
                //var SignitureLs = PermisiveClient.SelectBy_DynamicWhereClause("RequestID =" + RequestId, 1, 100, "PermissiveSignature_CompleteProccessID", "int", false, out TotalRow);
                var SignitureLs = db.PermissiveSignature_CompleteProccess.Where(d => d.RequestID == RequestId);
                foreach (var item in SignitureLs)
                {
                    //Service.PermissiveSignature_CompleteProccessService.PermissiveSignature_CompleteProccess permisiveObj = new Service.PermissiveSignature_CompleteProccessService.PermissiveSignature_CompleteProccess();
                    var permisiveObj = new CSD.Treasury.DataModel.PermissiveSignature_CompleteProccess();
                    //permisiveObj.RequestID = int.Parse(message2.Split(';')[1]);
                    permisiveObj.PermissiveSignatureForEachBankAccountID = item.PermissiveSignatureForEachBankAccountID;
                    obj.PermissiveSignature_CompleteProccess.Add(permisiveObj);
                    //db.PermissiveSignature_CompleteProccess.Add(permisiveObj);
                    //PermisiveClient.Insert(permisiveObj, out message1);
                }

                db.SaveChanges();
                var objSts4 = new Treasury.DataModel.Request_Payment_StatusDetails();

                objSts4.ActionDate = CurrentDate;
                objSts4.ActionTime = DateTime.Now.TimeOfDay;
                objSts4.UserID = User.Id;
                objSts4.RequestStatusTypeID = (decimal)Common.Enumeration.RequestStatusTypes.Delivered;
                objSts4.HostName = System.Net.Dns.GetHostName();
                obj.Request_Payment_StatusDetails.Add(objSts4);
                db.SaveChanges();
                return JsonMessage(true, "چک انتظامی به چک پرداختنی تبدیل شد", null, false);


                //}

                //}
                //else
                //{
                //همان عودت چک شد و  حذف گردید

                //ثبت وضعیت تحویل لاشه چک از کاربران
                //save Convert status
                //Icls_Request_Payment_StatusDetailsClient RPS = new Icls_Request_Payment_StatusDetailsClient();
                //var persianDate = Utility.ToPersianDate(DateTime.Now);

                ////Service.Request_Payment_StatusDetailsService.Request_Payment_StatusDetails objSts = new Service.Request_Payment_StatusDetailsService.Request_Payment_StatusDetails();
                //var objSts = new CSD.Treasury.DataModel.Request_Payment_StatusDetails();
                //objSts.ActionDate = persianDate;
                //objSts.UserID = User.Id;
                //objSts.ActionTime = DateTime.Now.TimeOfDay;
                //objSts.RequestStatusTypeID = (decimal)Common.Enumeration.RequestStatusTypes.DeleiverdLasheCheque;
                //objSts.RequestID = RequestId;
                //objSts.HostName = System.Net.Dns.GetHostName();
                //db.Request_Payment_StatusDetails.Add(objSts);
                //return JsonMessage(true, "ثبت با موفقیت انجام شد", null, false);
                //result = RPS.Insert(objSts, out message);
                //}

            }
            catch (Exception ex)
            {
                var message = "با خطا مواجه شد";
                if (ex.InnerException != null)
                {
                    if (ex.InnerException.InnerException.Message.Contains("\r\n"))
                    {
                        message = ex.InnerException.InnerException.Message.Split(new string[] { "\r\n" }, StringSplitOptions.None)[0];
                    }
                    else
                    {
                        message += "</br>" + ex.InnerException.InnerException.Message;
                    }

                }
                else
                {
                    message += "</br>" + ex.Message;
                }
                return JsonMessage(false, message, null, false);
            }
        }

        public ActionResult ShowModalDatetime(int RequestId)
        {
            var dc = new TreasuryEntities();
            var Request = dc.Request_Payment.Find(RequestId);
            var BankID = Request.Source_PaymentValue;
            var ChequePrice = Request.PayAmount;
            var BackupBankID = 0;
            var Isok = true;
            var Message = "";
            decimal? remain = 0;
            //درصورتیکه دسته چک برگه داشته باشد
            //و کافیگ ست شده باشد که برداشت از حساب زمان صدور چک است
            if (ConfigurationManager.AppSettings["PayWhenCheckPrinted"] == "True")
            {
                // بررسی موجودی بانک و بانک پشتیبان
                var Date = Utility.ToPersianDate(DateTime.Now);
                Icls_SPsClient db = new Icls_SPsClient();
                //$Sp_GetBankBalance$
                var Bank = db.Sp_GetBankBalance("13000101", Date, Int32.Parse(BankID), 1, false).FirstOrDefault();
                //var BackupBank = BankClient.SelectBy_DynamicWhereClause("Backup_BankID =" + Bank.BankID, 1, 10, "BankID", "int", false, out totalRows);
                var BackupBank = dc.Banks.Where(d => d.Backup_BankID == Bank.BankID);
                GetBankBalance_Result Backup_Bank = new GetBankBalance_Result();
                if (BackupBank.Count() != 0)
                {
                    if (BackupBank.Count() != 0)
                    {
                        //$Sp_GetBankBalance$
                        var qury = db.Sp_GetBankBalance("13000101", Date, BackupBank.SingleOrDefault().BankID, 1, false);
                        Backup_Bank = qury.FirstOrDefault();
                        BackupBankID = Backup_Bank.BankID;
                    }
                }
                if ((ChequePrice <= Bank.balance && (ConfigurationManager.AppSettings["CompletePayBackup"] == "True" && BackupBank.Count() == 0))
                    || (ChequePrice <= Bank.balance && (ConfigurationManager.AppSettings["CompletePayBackup"] == "False"))
          )
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
                    //اگر موجودی حساب پشتیبان بیشتر یا مساوی مبلغ کمبود حساب جاری است
                    if (Backup_Bank.balance >= remain)
                    {
                        Message = "موجودی حساب جاری کافی نیست . مبلغ از حساب پشتیبان کسر می گردد";

                        if (Bank.balance + Backup_Bank.balance <= ChequePrice)
                        {
                            Message = "مبلغ موجودی به علاوه مبلغ انتقال از پشتیبان کمتر از مبلغ چک است";
                            Isok = false;
                        }

                    }
                    else
                    {

                        if (ConfigurationManager.AppSettings["CompletePayBackup"] == "True" && ChequePrice <= Bank.balance)
                        {
                            Message = "موجودی حساب پشتیبان کافی نیست";
                            Isok = false;
                        }
                        else
                        {
                            Message = "موجودی حساب جاری و پشتیبان کافی نیست.امکان صدور چک نیست";
                            Isok = false;
                        }
                    }
                }
                else
                {
                    Message = "موجودی حساب جاری کافی نیست ";
                    Isok = false;
                }
                ViewBag.Isok = Isok;
                ViewBag.Message = Message;
                ViewBag.RequestId = RequestId;
                ViewBag.remain = remain;
                ViewBag.BackupBankId = BackupBankID;


            }



            return PartialView();
        }

        #endregion
        /// <summary>
        ///تمدید چک پرداختنی 
        /// </summary>
        #region ExtendDueDate
        public ActionResult ExtendDueDate(decimal RequestId)
        {
            var db = new TreasuryEntities();
            var RequestPayment = db.Request_Payment.Find(RequestId);
            ViewBag.MinDate = Utility.GetGregorianDate(RequestPayment.DueDate);
            ViewBag.requestId = RequestId;
            return PartialView();
        }

        [HttpPost]
        public ActionResult ExtendDueDate(decimal RequestId, string ActionDate, string saresid)
        {
            var message = "";
            try
            {
                var db = new TreasuryEntities();
                var request = db.Request_Payment.Find(RequestId);
                if (request != null)
                {

                    CSD.Treasury.DataModel.Request_Payment_StatusDetails status = new DataModel.Request_Payment_StatusDetails()
                    {
                        RequestID = request.RequestID,
                        RequestStatusTypeID = (decimal)Common.Enumeration.RequestStatusTypes.ExtendDuedate,
                        ActionDate = ActionDate.Replace("/", string.Empty),
                        HostName = System.Net.Dns.GetHostName(),
                        ActionTime = DateTime.Now.TimeOfDay,
                        UserID = User.Id,
                        OldDueDate = request.DueDate

                    };
                    request.DueDate = saresid.Replace("/", string.Empty);
                    db.Request_Payment_StatusDetails.Add(status);
                    db.SaveChanges();

                    return JsonMessage(true, "تمدید با موفقیت انجام شد", null, false);


                }

                return JsonMessage(false, "درخواست وجه یافت نشد", null, false);
            }
            catch (Exception ex)
            {
                message = "با خطا مواجه شد";
                if (ex.InnerException != null)
                {
                    if (ex.InnerException.InnerException.Message.Contains("\r\n"))
                    {
                        message = ex.InnerException.InnerException.Message.Split(new string[] { "\r\n" }, StringSplitOptions.None)[0];
                    }
                    else
                    {
                        message += "</br>" + ex.InnerException.InnerException.Message;
                    }

                }
                else
                {
                    message += "</br>" + ex.Message;
                }
                return JsonMessage(false, message, null, false);
            }

        }
        //public ActionResult ExtendDueDate(int requestId)
        //{
        //    Icls_Request_PaymentClient RP = new Icls_Request_PaymentClient();
        //    var RequestPayment = RP.SelectRow(requestId).SingleOrDefault();
        //    ViewBag.MinDate = Utility.GetGregorianDate(RequestPayment.DueDate);
        //    return PartialView(RequestPayment);
        //}
        //[HttpPost]
        //public ActionResult ExtendDueDate(int RequestId, string DueDate, int Pay_TypeDetailID_source)
        //{
        //    try
        //    {
        //        DueDate = !string.IsNullOrEmpty(DueDate) ? DueDate.Replace("/", string.Empty) : "";
        //        //int TotalRow;
        //        //Icls_PermissiveSignature_CompleteProccessClient PermisiveClient = new Icls_PermissiveSignature_CompleteProccessClient();
        //        //Icls_Request_PaymentClient RP = new Icls_Request_PaymentClient();
        //        //Icls_PaymentClient paymentClient = new Icls_PaymentClient();
        //        //Payment_ReferenceTypes_ValueClient paymentRefrenceValueClient = new Payment_ReferenceTypes_ValueClient();
        //        //Icls_Request_Payment_BreakPriceClient breackPriceClient = new Icls_Request_Payment_BreakPriceClient();
        //        //Icls_TransferFromBackupBankClient transferFromBackupBankClient = new Icls_TransferFromBackupBankClient();
        //        //int totalrow;
        //        //string message = "", message1 = "", message2 = "", message3 = "";
        //        //bool result = false;

        //        var db = new TreasuryEntities();

        //        //درصورتیکه قبلا این درخواست را تغییر تاریخ سررسید نداده باشد
        //        //var IsDueDateChanged= RP.SelectBy_DynamicWhereClause("RefrenceRequestId =" + RequestId, 1, 100, "RequestID", "dec", false, out totalrow).Any();
        //        var IsDueDateChanged = db.Request_Payment.Any(d => d.RefrenceRequestId == RequestId);
        //        if (!IsDueDateChanged)
        //        {
        //            var CurrentDate = Utility.ToPersianDate(DateTime.Now);
        //            var dayDuedate = int.Parse(DueDate.Substring(6, 2));
        //            var dayCurrent = int.Parse(CurrentDate.Substring(6, 2));
        //            var MonthDuedate = int.Parse(DueDate.Substring(4, 2));
        //            var MonthDayCurrent = int.Parse(CurrentDate.Substring(4, 2));
        //            //درصورتی که چک مدت دار باشد و تاریخ سررسید را کوچکتر از تاریخ روز انتخاب کرده باشد 
        //            //چون تاریخ درخواست باید کوچکتر از تاریخ سررسید باشد پس تاریخ سررسیدر ا یکی کم می کنیم
        //            if (Pay_TypeDetailID_source == 4 && (dayDuedate < dayCurrent || MonthDuedate < MonthDayCurrent))
        //            {
        //                dayDuedate = dayDuedate - 1;
        //                if (MonthDuedate < 6 && dayDuedate == 0)
        //                {
        //                    dayDuedate = 31;
        //                }
        //                else if (MonthDuedate > 6 && dayDuedate == 0)
        //                {
        //                    dayDuedate = 30;
        //                }

        //                CurrentDate = DueDate.Substring(0, 6) + (dayDuedate < 10 ? "0" + dayDuedate.ToString() : dayDuedate.ToString());
        //            }
        //            else if (Pay_TypeDetailID_source == 1)
        //            {
        //                CurrentDate = DueDate;
        //            }

        //            //save extend duedate  status
        //            //Icls_Request_Payment_StatusDetailsClient RPS = new Icls_Request_Payment_StatusDetailsClient();

        //            var persianDate = Utility.ToPersianDate(DateTime.Now);
        //            //درخواستی که تغییر تاریخ سررسید داده است باید وضعیت برایش ثبت گردد
        //            //Service.Request_Payment_StatusDetailsService.Request_Payment_StatusDetails objSts = new Service.Request_Payment_StatusDetailsService.Request_Payment_StatusDetails();
        //            Treasury.DataModel.Request_Payment_StatusDetails objSts = new Treasury.DataModel.Request_Payment_StatusDetails();
        //            objSts.ActionDate = persianDate;
        //            objSts.UserID = User.Id;
        //            objSts.ActionTime = DateTime.Now.TimeOfDay;
        //            objSts.RequestStatusTypeID = (decimal)Common.Enumeration.RequestStatusTypes.ExtendDuedate;
        //            objSts.RequestID = RequestId;

        //            objSts.HostName = System.Net.Dns.GetHostName();
        //            db.Request_Payment_StatusDetails.Add(objSts);
        //            //result = RPS.Insert(objSts, out message);

        //            //درخواست وجه
        //            //var request = RP.SelectRow(RequestId).SingleOrDefault();
        //            var request = db.Request_Payment.SingleOrDefault(d => d.RequestID == RequestId);
        //            //insert new Request that is result of extend duedate cheque
        //            //Service.Request_PaymentService.Request_Payment obj = new Service.Request_PaymentService.Request_Payment();
        //            var obj = new Treasury.DataModel.Request_Payment();
        //            //var dateNow = DateTime.Now;
        //            obj.UserID = request.UserID;
        //            obj.FiscalYear = request.FiscalYear;
        //            obj.RequestDate = CurrentDate;
        //            obj.RequestCreatedTime = request.RequestCreatedTime;
        //            obj.Priority = request.Priority;
        //            obj.Unique_Account_No = request.Unique_Account_No;
        //            obj.BehalfID = request.BehalfID;
        //            obj.ExchangeTypeID = request.ExchangeTypeID;
        //            obj.PayAmount = request.PayAmount;
        //            obj.Main_Unique_Account_No = request.Main_Unique_Account_No;
        //            obj.Destination_PaymentValue = request.Destination_PaymentValue;
        //            obj.Destination_TypesID = request.Destination_TypesID;
        //            obj.EquivalencePrice = request.EquivalencePrice;
        //            obj.ExchangeRate = request.ExchangeRate;
        //            //obj.Payment_ReferenceTypes_Value = request.Payment_ReferenceTypes_Value;
        //            obj.RefrenceRequestId = RequestId;
        //            obj.ChequeElements_PrintLayout = request.ChequeElements_PrintLayout;
        //            obj.ChequeNo = request.ChequeNo;
        //            obj.Source_TypeID = request.Source_TypeID;
        //            obj.Pay_TypeDetailID_source = Pay_TypeDetailID_source;
        //            //if (CurrentDate == DueDate)
        //            //{
        //            //    //تبدیل به چک روز
        //            //    obj.Pay_TypeDetailID_source = 1;
        //            //}
        //            //else
        //            //{
        //            //    //تبدیل به چک مدت دار
        //            //    obj.Pay_TypeDetailID_source = 4;
        //            //}

        //            obj.Source_PaymentValue = request.Source_PaymentValue;
        //            //obj.UniqueAccountType = request.UniqueAccountType;
        //            obj.Unique_Account_No = request.Unique_Account_No;
        //            obj.DueDate = DueDate;

        //            obj.Description = Common.Enumeration.RequestDescription.ResultOfExtendDueDate.ToString();
        //            //var result1 = RP.Insert(obj, out message2);
        //            db.Request_Payment.Add(obj);
        //            //if (result1)
        //            //{
        //            //var requestBreakPrice = breackPriceClient.SelectBy_DynamicWhereClause("RequestID=" + RequestId, 1, 100, "BreakId", "dec", false, out totalrow);
        //            var requestBreakPrice = db.Request_Payment_BreakPrice.FirstOrDefault(d => d.RequestID == RequestId);
        //            if (requestBreakPrice != null)
        //            {
        //                //Service.Request_Payment_BreakPriceService.Request_Payment_BreakPrice requestbreakpriceOBJ = new Service.Request_Payment_BreakPriceService.Request_Payment_BreakPrice();
        //                var requestbreakpriceOBJ = new DataModel.Request_Payment_BreakPrice();
        //                requestbreakpriceOBJ.RequestID = obj.RequestID;
        //                requestbreakpriceOBJ.Price_Rial = requestBreakPrice.Price_Rial;
        //                requestbreakpriceOBJ.ExchangeTypeID = requestBreakPrice.ExchangeTypeID;
        //                requestbreakpriceOBJ.Exchange_Amount = requestBreakPrice.Exchange_Amount;
        //                requestbreakpriceOBJ.Exchange_Rate = requestBreakPrice.Exchange_Rate;
        //                obj.Request_Payment_BreakPrice.Add(requestbreakpriceOBJ);
        //                //breackPriceClient.Insert(requestbreakpriceOBJ, out message3);
        //            }
        //            //var paymentRefrenceTypeValue = paymentRefrenceValueClient.SelectBy_DynamicWhereClause("RequestId=" + RequestId, 1, 100, "ReferenceTypeValueID", "int", false, out totalrow);
        //            var paymentRefrenceTypeValue = db.Payment_ReferenceTypes_Value.Where(d => d.RequestId == RequestId);
        //            if (paymentRefrenceTypeValue != null)
        //            {
        //                //Service.Payment_ReferenceTypes_ValueService.Payment_ReferenceTypes_Value PaymentRefrenceValueOBJ = new Service.Payment_ReferenceTypes_ValueService.Payment_ReferenceTypes_Value();
        //                var PaymentRefrenceValueOBJ = new Treasury.DataModel.Payment_ReferenceTypes_Value();
        //                foreach (var item in paymentRefrenceTypeValue)
        //                {
        //                    PaymentRefrenceValueOBJ.RequestId = obj.RequestID;
        //                    PaymentRefrenceValueOBJ.ReferenceID = item.ReferenceID;
        //                    PaymentRefrenceValueOBJ.ReferenceValue = item.ReferenceValue;
        //                    obj.Payment_ReferenceTypes_Value.Add(PaymentRefrenceValueOBJ);
        //                }

        //                //paymentRefrenceValueClient.Insert(PaymentRefrenceValueOBJ, out message3);
        //            }
        //            //ثبت نامشخص در جدول وضعیت ها
        //            //Service.Request_Payment_StatusDetailsService.Request_Payment_StatusDetails objSts1 = new Service.Request_Payment_StatusDetailsService.Request_Payment_StatusDetails();
        //            var objSts1 = new DataModel.Request_Payment_StatusDetails();
        //            objSts1.ActionDate = CurrentDate;
        //            objSts1.ActionTime = DateTime.Now.TimeOfDay;
        //            objSts1.UserID = User.Id;
        //            objSts1.RequestStatusTypeID = (decimal)Common.Enumeration.RequestStatusTypes.Unknown;
        //            objSts1.RequestID = obj.RequestID;
        //            objSts1.HostName = System.Net.Dns.GetHostName();
        //            obj.Request_Payment_StatusDetails.Add(objSts1);
        //            //RPS.Insert(objSts1, out message);

        //            //ثبت تایید درخواست در جدول وضعیت ها
        //            //objSts1.RequestStatusTypeID = (decimal)Common.Enumeration.RequestStatusTypes.Confirmed;
        //            //RPS.Insert(objSts1, out message);
        //            var objSts2 = new DataModel.Request_Payment_StatusDetails();
        //            objSts2.ActionDate = CurrentDate;
        //            objSts2.ActionTime = DateTime.Now.TimeOfDay;
        //            objSts2.UserID = User.Id;
        //            objSts2.RequestStatusTypeID = (decimal)Common.Enumeration.RequestStatusTypes.Confirmed;
        //            objSts2.RequestID= obj.RequestID;
        //            objSts2.HostName = System.Net.Dns.GetHostName();
        //            obj.Request_Payment_StatusDetails.Add(objSts2);

        //            //ثبت تایید برای پرداخت در جدول وضعیت ها
        //            //objSts1.RequestStatusTypeID = (decimal)Common.Enumeration.RequestStatusTypes.ConfirmPayment;
        //            //RPS.Insert(objSts1, out message);
        //            var objSts3 = new DataModel.Request_Payment_StatusDetails();
        //            objSts3.ActionDate = CurrentDate;
        //            objSts3.ActionTime = DateTime.Now.TimeOfDay;
        //            objSts3.UserID = User.Id;
        //            objSts3.RequestStatusTypeID = (decimal)Common.Enumeration.RequestStatusTypes.ConfirmPayment;
        //            objSts2.RequestID = obj.RequestID;
        //            objSts3.HostName = System.Net.Dns.GetHostName();
        //            obj.Request_Payment_StatusDetails.Add(objSts3);
        //            //ثبت تحویل در جدول وضعیت ها
        //            //objSts1.RequestStatusTypeID = (decimal)Common.Enumeration.RequestStatusTypes.Delivered;
        //            //RPS.Insert(objSts1, out message);
        //            var objSts4 = new DataModel.Request_Payment_StatusDetails();
        //            objSts4.ActionDate = CurrentDate;
        //            objSts4.ActionTime = DateTime.Now.TimeOfDay;
        //            objSts4.UserID = User.Id;
        //            objSts4.RequestStatusTypeID = (decimal)Common.Enumeration.RequestStatusTypes.Delivered;
        //            objSts2.RequestID = obj.RequestID;
        //            objSts4.HostName = System.Net.Dns.GetHostName();
        //            obj.Request_Payment_StatusDetails.Add(objSts4);
        //            //درصورتی که چک روز باشد پرداخت شده حساب می گردد در صورتی که مدت دار باشد در کنسول ابتدای برنامه باید وضعیتش مشخص گردد

        //            if (Pay_TypeDetailID_source == 1)
        //            {
        //                //ثبت پرداخت در جدول وضعیت ها
        //                //objSts1.RequestStatusTypeID = (decimal)Common.Enumeration.RequestStatusTypes.Paid;
        //                //RPS.Insert(objSts1, out message);
        //                var objSts5 = new DataModel.Request_Payment_StatusDetails();
        //                objSts5.ActionDate = CurrentDate;
        //                objSts5.ActionTime = DateTime.Now.TimeOfDay;
        //                objSts5.UserID = User.Id;
        //                objSts5.RequestStatusTypeID = (decimal)Common.Enumeration.RequestStatusTypes.ConfirmPayment;
        //                objSts2.RequestID = obj.RequestID;
        //                objSts5.HostName = System.Net.Dns.GetHostName();
        //                obj.Request_Payment_StatusDetails.Add(objSts5);
        //                //save payment
        //                //Service.PaymentService.Payment payment = new Service.PaymentService.Payment();
        //                //payment.RequestID = decimal.Parse(message2.Split(';')[1]);
        //                var payment = new DataModel.Payment();
        //                payment.PaymentDate = DueDate;
        //                payment.RequestID = obj.RequestID;
        //                payment.HostName = System.Net.Dns.GetHostName();
        //                payment.UserID = User.Id;
        //                payment.Effective = true;
        //                //var Result = paymentClient.Insert(payment, out message1);
        //                obj.Payments.Add(payment);

        //            }


        //            //var iFTransferIsExict = transferFromBackupBankClient.SelectBy_DynamicWhereClause("RequestID =" + RequestId, 1, 100, "TransferId", "Int64", false, out totalrow).FirstOrDefault();
        //            //var iFTransferIsExict = db.TransferFromBackupBanks.Any(d => d.RequestID == RequestId);
        //            //if (iFTransferIsExict != null)
        //            //{
        //            //    Service.TransferFromBackupBankService.TransferFromBackupBank transferOBJ = new Service.TransferFromBackupBankService.TransferFromBackupBank()
        //            //    {
        //            //        From_BankID = iFTransferIsExict.From_BankID,
        //            //        To_BankID = iFTransferIsExict.To_BankID,
        //            //        RequestID = obj.RequestID,
        //            //        OkDate = obj.DueDate,
        //            //        Price = iFTransferIsExict.Price,
        //            //        IsOk = iFTransferIsExict.IsOk,
        //            //        TransferDate = obj.DueDate
        //            //    };
        //            //}

        //            //var SignitureLs = PermisiveClient.SelectBy_DynamicWhereClause("RequestID =" + RequestId, 1, 100, "PermissiveSignature_CompleteProccessID", "int", false, out TotalRow);
        //            var SignitureLs = db.PermissiveSignature_CompleteProccess.Where(d => d.RequestID == RequestId);
        //            foreach (var item in SignitureLs)
        //            {
        //                //Service.PermissiveSignature_CompleteProccessService.PermissiveSignature_CompleteProccess permisiveObj = new Service.PermissiveSignature_CompleteProccessService.PermissiveSignature_CompleteProccess();
        //                //permisiveObj.RequestID = int.Parse(message2.Split(';')[1]);
        //                var permisiveObj = new Treasury.DataModel.PermissiveSignature_CompleteProccess();
        //                permisiveObj.PermissiveSignatureForEachBankAccountID = item.PermissiveSignatureForEachBankAccountID;
        //                permisiveObj.RequestID = obj.RequestID;
        //                obj.PermissiveSignature_CompleteProccess.Add(permisiveObj);
        //                //PermisiveClient.Insert(permisiveObj, out message1);
        //            }
        //            db.SaveChanges();
        //            return JsonMessage(true, "تمدید چک پرداختنی انجام شد", null, false);

        //            //}
        //            //return JsonMessage(result, "باخطا مواجه شد", null, false);
        //        }

        //        return JsonMessage(false, "قبلا این چک تمدید شده است", null, false);
        //    }
        //    catch (Exception ex)
        //    {
        //        return JsonMessage(false, "باخطا مواجه شد", null, false);
        //        throw;
        //    }

        //}

        #endregion

        //#region PaymentReferenceTypesValue
        //[HttpPost]
        //public ActionResult AddPaymentReferenceTypesValue(FormCollection frm, int requestId)
        //{
        //    string message = "";
        //    Payment_ReferenceTypes_ValueClient PRV = new Payment_ReferenceTypes_ValueClient();
        //    Icls_Request_Payment_StatusDetailsClient PS = new Icls_Request_Payment_StatusDetailsClient();
        //    Service.Payment_ReferenceTypes_ValueService.Payment_ReferenceTypes_Value obj = new Service.Payment_ReferenceTypes_ValueService.Payment_ReferenceTypes_Value();


        //    if (ModelState.IsValid)
        //    {
        //        foreach (var item in frm.AllKeys)
        //        {
        //            if (item.StartsWith("Reference_"))
        //            {
        //                var array = item.Split('_');
        //                obj.RequestId = requestId;
        //                obj.ReferenceID = int.Parse(array[1]);
        //                obj.ReferenceValue = frm[item];
        //                var result = PRV.Insert(obj, out message);
        //            }
        //        }
        //        var persianDate = Utility.ToPersianDate(DateTime.Now);
        //        string message1 = "";
        //        //ثبت در جدول پرداخت ها 
        //        Icls_PaymentClient p = new Icls_PaymentClient();
        //        Service.PaymentService.Payment payment = new Service.PaymentService.Payment();
        //        payment.RequestID = requestId;
        //        payment.PaymentDate = persianDate;
        //        payment.PaymentTime = DateTime.Now.TimeOfDay;
        //        payment.UserID = User.Id;
        //        payment.HostName = System.Net.Dns.GetHostName();
        //        p.Insert(payment, out message1);

        //        //بدست آوردن وضعیت های مربوط به هر درخواست وجه
        //        string where = "RequestID =" + requestId;
        //        int totalRow;
        //        Request_Payment_StatusDetails objStatus = new Request_Payment_StatusDetails();

        //        objStatus.ActionDate = persianDate;
        //        objStatus.ActionTime = DateTime.Now.TimeOfDay;
        //        objStatus.RequestID = requestId;
        //        objStatus.HostName = System.Net.Dns.GetHostName();
        //        var AllStatus = PS.SelectBy_DynamicWhereClause(where, 1, 10000, "Request_Payment_StatusDetailsID", "int", true, out totalRow).Select(d => d.RequestStatusTypeID);
        //        //درصورتی که تایید درخواست کرده باشد  و قبلا تایید نکرده باشد می تواند تایید پرداخت نماید
        //        if (AllStatus.Contains((decimal)RequestStatusTypes.Confirmed) && AllStatus.Contains((decimal)RequestStatusTypes.Confirmed) && !AllStatus.Contains((decimal)RequestStatusTypes.Paid))
        //        {
        //            objStatus.RequestStatusTypeID = (decimal)RequestStatusTypes.Paid;
        //            PS.Insert(objStatus, out message);
        //            return JsonMessage(true, message);
        //        }
        //        else
        //        {
        //            return JsonMessage(false, "Error");
        //        }

        //    }
        //    return Json(false, JsonRequestBehavior.AllowGet);
        //}
        //#endregion
        //مشاهده سند پرداخت شده ها
        #region ShowVocher
        public ActionResult ShowVocher(int RequestID)
        {
            //Icls_SPsClient SpsClient = new Icls_SPsClient();
            //var ls = SpsClient.Sp_CREATE_VOUCHER_PAYMENTS(true, RequestID, User.Id, false, true).ToList();
            var db = new TreasuryEntities();
            var ls = db.Show_Voucher_Row(RequestID);
            return PartialView(ls.ToList());
        }
        #endregion
    }

}

