using CSD.Treasury.Common.Enumeration;
using CSD.Treasury.DataModel;
using CSD.Treasury.Service.Payment_ReferenceTypes_ValueService;
using CSD.Treasury.Service.PettyCash_TransactionDetailService;
using CSD.Treasury.Service.PettyCash_TransactionHeaderService;
using CSD.Treasury.Service.PettyCashService;
using CSD.Treasury.Service.ReferenceTypes_By_BehalfGroupService;
using CSD.Treasury.Service.Request_Payment_StatusDetailsService;
using CSD.Treasury.Service.BankService;
using CSD.Treasury.Service.Request_PaymentService;
using CSD.Treasury.Service.PaymentService;
using CSD.Treasury.UI.Controllers;
using CSD.Treasury.UI.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web.Mvc;
using CSD.Treasury.Service.BehalfsService;
using System.Configuration;
using CSD.Treasury.Service.ProceduresDB;
using CSD.Treasury.Service.Vocher_RowService;
using Stimulsoft.Report;
using Stimulsoft.Report.Mvc;

namespace CSD.Treasury.UI.Areas.Payment.Controllers
{
    [Authorize(Roles = "Admin,Manager,DirectDebit")]
    public class DirectDebitController : BaseController
    {
        // GET: Payment/DirectDebit
        public ActionResult Index()
        {
            //ViewBag.HeaderId = headerId;
            ViewBag.DateNow = Utility.DateStructure(Utility.ToPersianDate(DateTime.Now));
            return View();
        }


        public JsonResult Show()
        {
            List<CSD.Treasury.DataModel.GetAllRequestStatus_Result> list = new List<CSD.Treasury.DataModel.GetAllRequestStatus_Result>();
            Icls_Request_PaymentClient RPClient = new Icls_Request_PaymentClient();

            //string whereclouse = "AggrigateOrDuplicationID = NULL and IsWithdrawType= " + true;
            //string CurrentDate = Utility.ToPersianDate(DateTime.Now);
            //int totalRows;
            //var RequsetLs = RPClient.GetAllRequestStatus_DynamicWhereClause(CurrentDate, whereclouse, 1, 100, "RequestID", "dec", false, out totalRows).ToList();

            //foreach (var item in RequsetLs)
            //{
            //    item.UniqueAccountTitle = Utility.GetUniqueAccount(item.Unique_Account_No);
            //}
            ////Edit Mode
            //if (HeaderId != 0)
            //{

            //    foreach (var item in RequsetLs)
            //    {
            //        GetAllRequestStatus_Result obj = new GetAllRequestStatus_Result();
            //        obj.PayAmount = item.PayAmount;
            //        obj.RequestDate = item.RequestDate;
            //        obj.BehalfID = item.BehalfID;
            //        obj.BehalfTitle = Utility.GetBehalf(item.BehalfID);
            //        //obj.BehalfList = Utility.GetBehalfLs(item.BehalfGroupID);
            //        //obj.ReferenceTypeLsBYbehalfGroupId = Utility.GetReferenceTypeByBehalfGroupLs(item.BehalfGroupID);
            //        //obj.BehalfGroupTiltle = Utility.GetBehalfGroup(item.BehalfGroupID);
            //        obj.UniqueAccountTitle = Utility.GetUniqueAccount(item.Unique_Account_No);
            //        list.Add(obj);
            //    }

            //    if (list != null)
            //    {
            //        return Json(new
            //        {
            //            BehalfGroupList = Utility.GetBehalfsGroupLs(),
            //            AccountNumLs = Utility.v_accountsLS(),
            //            RefrenceTypeLs = Utility.GetReferenceTypeLs(),
            //            DirectDebit_TransactionLs = list
            //        }, JsonRequestBehavior.AllowGet);
            //    }
            //}
            //Create Mode




            return Json(new
            {
                BehalfGroupList = Utility.GetBehalfsGroupLs().Where(d => d.Receipt_Pay_TypeID == int.Parse(ConfigurationManager.AppSettings["Direct_Type_ID"])),
                AccountNumLs = Utility.v_accountsLS(),
                RefrenceTypeLs = Utility.GetReferenceTypeLs(),
                DirectDebit_TransactionLs = list,
                BasicBanksLs = Utility.GetBasicBanks(),
                BankLS = Utility.GetBanks()


            }, JsonRequestBehavior.AllowGet);

        }
        //json List Of Bank By BasicBankID
        public JsonResult GetBankByBasickId(int BasicBankID)
        {
            int TotalRow;
            //Icls_BankClient BankClient = new Icls_BankClient();
            var db = new TreasuryEntities();
            //var query = BankClient.SelectBy_DynamicWhereClause("BasicBankID =" + BasicBankID, 1, 100, "BankID", "int", false, out TotalRow);
            var query = db.Banks.Where(d => d.BasicBankID == BasicBankID);
            return Json(query, JsonRequestBehavior.AllowGet);
        }


        public ActionResult getDirectDebitUC()
        {
            var dc = new TreasuryEntities();
            //نوع ذینفع  
            var unique_account_type = dc.v_unique_account_type.ToList();
            unique_account_type.Add(new v_unique_account_type { Code = 0, Description = Resourse.Resources.Others });
            ViewBag.unique_account_type = unique_account_type;
            return PartialView();
        }

        public JsonResult ChangeBehalfGroup(int BehalfGroupId)
        {
            //behalf list by  BehalfGroupId
            int totalRows;
            Service.BehalfsService.Icls_BehalfsClient obj = new Service.BehalfsService.Icls_BehalfsClient();
            List<Service.BehalfsService.Behalf> lsBehalf = new List<Service.BehalfsService.Behalf>();
            string whereclouse = "1=1";
            whereclouse = " BehalfGroupId=" + BehalfGroupId.ToString();
            var query = obj.SelectBy_DynamicWhereClause(whereclouse, 1, 100, "BehalfGroupID", "int", true, out totalRows);
            lsBehalf.AddRange(query);

            Service.BehalfGroupService.Icls_BehalfGroupClient behalfGroupClient = new Service.BehalfGroupService.Icls_BehalfGroupClient();
            var behalfGroup = behalfGroupClient.SelectRow(BehalfGroupId).SingleOrDefault();
            var account = Utility.v_accountsLS().Where(q => q.account_code.Trim() == behalfGroup.BehalfGroup_Account.Trim()).SingleOrDefault();
            //.Getaccounts(behalfGroup.BehalfGroup_Account) + "/" + behalfGroup.BehalfGroup_Account;

            //Refrence by BehalfGroupId
            Icls_ReferenceTypes_By_BehalfGroupClient RTBG = new Icls_ReferenceTypes_By_BehalfGroupClient();
            var Ls = RTBG.SelectBy_DynamicWhereClause("BehalfGroupID = " + BehalfGroupId, 1, 10000, "ReferenceID", "int", true, out totalRows).ToList();
            List<Service.ReferenceTypeService.ReferenceType> lsRefrence = new List<Service.ReferenceTypeService.ReferenceType>();
            foreach (var item in Ls)
            {
                Service.ReferenceTypeService.ReferenceType objRefrence = new Service.ReferenceTypeService.ReferenceType();
                objRefrence.referenceTypeID = item.ReferenceID;
                objRefrence.referenceTypeDescription = Utility.GetReferenceTitle(item.referenceTypeID);
                lsRefrence.Add(objRefrence);
            }
            List<Service.ReferenceTypes_By_BehalfGroupService.ReferenceTypes_By_BehalfGroup> refList = new List<Service.ReferenceTypes_By_BehalfGroupService.ReferenceTypes_By_BehalfGroup>();
            return Json(new { BehalfList = lsBehalf, Refrences = lsRefrence, account = account }, JsonRequestBehavior.AllowGet);

        }

        #region Create
        [HttpPost]
        public ActionResult Save()
        {
            var message = "";
            try
            {
                dynamic DaynamicObj = JsonConvert.DeserializeObject(Request.Form[0]);
                foreach (var item in DaynamicObj)
                {
                    var RequestObj = new Treasury.DataModel.Request_Payment();
                    var ReferenceTypes_Value = new DataModel.Payment_ReferenceTypes_Value();
                    var db = new TreasuryEntities();                   
                    RequestObj.UserID = User.Id;
                    PersianCalendar pc = new PersianCalendar();
                    var dateNow = DateTime.Now;
                    var refrences = item.references;
                    string date = item.date;
                    var Month = pc.GetMonth(DateTime.Now);
                    RequestObj.FiscalYear = pc.GetYear(DateTime.Now).ToString();
                    if (Month >= 10 && ConfigurationManager.AppSettings["YearPlus"] == "True")
                    {
                        RequestObj.FiscalYear = (pc.GetYear(DateTime.Now) + 1).ToString();
                    }
                    RequestObj.IsWithdrawType = true;                    
                    RequestObj.DueDate = date.Replace("/", String.Empty);
                    RequestObj.BehalfID = item.behalfId;                    
                    if (item.uniqueAccount1 != null)
                    {
                        RequestObj.Unique_Account_No = ((string)item.uniqueAccount1).Trim();
                    }
                    else
                    {
                        RequestObj.Unique_Account_No = "000000";
                    }
                    if (item.uniqueAccount2 != null)
                    {
                        RequestObj.Main_Unique_Account_No = ((string)item.uniqueAccount2).Trim();
                    }
                    RequestObj.PayAmount = item.price;
                    RequestObj.Priority = (short)Priority.Normal;
                    RequestObj.RequestDate = date.Replace("/", String.Empty);
                    RequestObj.RequestCreatedTime = dateNow.TimeOfDay;
                    RequestObj.UserID = User.Id;              
                    RequestObj.Source_PaymentValue = item.bankId;
                    RequestObj.BankID = item.bankId;
                    RequestObj.Source_TypeID = 3;                    
                    RequestObj.Pay_TypeDetailID_source = 29;                 
                    db.Request_Payment.Add(RequestObj);
                    foreach (var item1 in refrences)
                    {
                        if (item1.value != null)
                        {                            
                            ReferenceTypes_Value.ReferenceID = item1.id;
                            ReferenceTypes_Value.ReferenceValue = item1.value;
                            RequestObj.Payment_ReferenceTypes_Value.Add(ReferenceTypes_Value);                                                      
                        }
                    }
                    //ثبت در جدول پرداخت ها                     
                    var paymentobj = new DataModel.Payment
                    {                        
                        PaymentDate = date.Replace("/", String.Empty),
                        PaymentTime = DateTime.Now.TimeOfDay,
                        UserID = User.Id,
                        HostName = System.Net.Dns.GetHostName(),
                        Effective = true,
                        Canceled = false
                    };
                    RequestObj.Payments.Add(paymentobj);
                    var StatusUnknown = new DataModel.Request_Payment_StatusDetails();
                    StatusUnknown.UserID = User.Id;
                    StatusUnknown.ActionDate = date.Replace("/", String.Empty);
                    StatusUnknown.ActionTime = DateTime.Now.TimeOfDay;
                    StatusUnknown.RequestStatusTypeID = (decimal)RequestStatusTypes.Unknown;                    
                    StatusUnknown.HostName = System.Net.Dns.GetHostName();
                    RequestObj.Request_Payment_StatusDetails.Add(StatusUnknown);
                    //ثبت وضعیت تایید
                    
                    var StatusConfirmed = new DataModel.Request_Payment_StatusDetails();
                    StatusConfirmed.UserID = User.Id;
                    StatusConfirmed.ActionDate = date.Replace("/", String.Empty);
                    StatusConfirmed.ActionTime = DateTime.Now.TimeOfDay;
                    StatusConfirmed.RequestStatusTypeID = (decimal)RequestStatusTypes.Confirmed;                    
                    StatusConfirmed.HostName = System.Net.Dns.GetHostName();
                    
                    RequestObj.Request_Payment_StatusDetails.Add(StatusConfirmed);
                    
                    //ثبت وضعیت تایید پرداخت
                    
                    var StatusConfirmPayment = new DataModel.Request_Payment_StatusDetails();
                    StatusConfirmPayment.UserID = User.Id;
                    StatusConfirmPayment.ActionDate = date.Replace("/", String.Empty);
                    StatusConfirmPayment.ActionTime = DateTime.Now.TimeOfDay;
                    StatusConfirmPayment.RequestStatusTypeID = (decimal)RequestStatusTypes.ConfirmPayment;                    
                    StatusConfirmPayment.HostName = System.Net.Dns.GetHostName();                    
                    RequestObj.Request_Payment_StatusDetails.Add(StatusConfirmPayment);
                                        
                    //ثبت وضعیت  پرداخت
                    
                    var StatusPaid = new DataModel.Request_Payment_StatusDetails();
                    StatusPaid.UserID = User.Id;
                    StatusPaid.ActionDate = date.Replace("/", String.Empty);
                    StatusPaid.ActionTime = DateTime.Now.TimeOfDay;
                    StatusPaid.RequestStatusTypeID = (decimal)RequestStatusTypes.Paid;                    
                    StatusPaid.HostName = System.Net.Dns.GetHostName();
                    db.Request_Payment_StatusDetails.Add(StatusPaid);
                    RequestObj.Request_Payment_StatusDetails.Add(StatusPaid);
                    
                    //ثبت وضعیت  تحویل
                    
                    var StatusDeliever = new DataModel.Request_Payment_StatusDetails();
                    StatusDeliever.UserID = User.Id;
                    StatusDeliever.ActionDate = date.Replace("/", String.Empty);
                    StatusDeliever.ActionTime = DateTime.Now.TimeOfDay;
                    StatusDeliever.RequestStatusTypeID = (decimal)RequestStatusTypes.Delivered;                    
                    StatusDeliever.HostName = System.Net.Dns.GetHostName();
                    db.Request_Payment_StatusDetails.Add(StatusDeliever);
                    RequestObj.Request_Payment_StatusDetails.Add(StatusDeliever);
                   
                    db.SaveChanges();
                }
               

                return JsonMessage(true, message, null, false);
                
            }
            catch (Exception ex)
            {
                message = "باخطا مواجه شد";
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
                throw;
            }
        }
        #endregion

        #region Delete
        [HttpPost]
        public ActionResult Delete(int requestId)
        {
            try
            {
                //string message = "";
                //int TotalRow;
                var db = new TreasuryEntities();
                //Delete RequestStatus
                var StatusLs = db.Request_Payment_StatusDetails.Where(d=>d.RequestID == requestId);
                db.Request_Payment_StatusDetails.RemoveRange(StatusLs);
                

                //Delete Payment
                var Payment = db.Payments.SingleOrDefault(d => d.RequestID == requestId);
                db.Payments.Remove(Payment);

                //Delete Refrences Values
                var RefrenceLs = db.Payment_ReferenceTypes_Value.Where(d => d.RequestId == requestId); 
                if(RefrenceLs != null)
                {
                    db.Payment_ReferenceTypes_Value.RemoveRange(RefrenceLs);
                }

                //Delete Request
                var request = db.Request_Payment.SingleOrDefault(d => d.RequestID == requestId);
                db.Request_Payment.Remove(request);

                //Delete File
                var dc = new TreasuryFilesEntities();
                var file = dc.Files.SingleOrDefault(d => d.relatedID == requestId && d.tableName == "Request_Payment");
                if (file != null)
                {
                    dc.Files.Remove(file);
                }

                db.SaveChanges();
                //Icls_Request_Payment_StatusDetailsClient RPStatusDetailsClient = new Icls_Request_Payment_StatusDetailsClient();
                //var StatusLs = RPStatusDetailsClient.SelectBy_DynamicWhereClause("RequestID = " + requestId, 1, 100, "RequestID", "dec", false, out TotalRow);
                //foreach (var item in StatusLs)
                //{
                //    RPStatusDetailsClient.Delete(item.Request_Payment_StatusDetailsID, out message);
                //}


                //Icls_PaymentClient paymentClient = new Icls_PaymentClient();
                //var Payment = paymentClient.SelectBy_DynamicWhereClause("RequestID = " + requestId, 1, 10, "PaymentID", "dec", false, out TotalRow).SingleOrDefault();
                //paymentClient.Delete(Payment.PaymentID, out message);


                //Payment_ReferenceTypes_ValueClient PRefrenceValueClient = new Payment_ReferenceTypes_ValueClient();
                //var RefrenceLs = PRefrenceValueClient.SelectBy_DynamicWhereClause("RequestId =" + requestId, 1, 20, "RequestId", "dec", false, out TotalRow);
                //if (RefrenceLs != null)
                //{
                //    foreach (var item in RefrenceLs)
                //    {
                //        PRefrenceValueClient.Delete(item.ReferenceTypeValueID, out message);
                //    }
                //}

                //Delete Request
                //Icls_Request_PaymentClient rpClient = new Icls_Request_PaymentClient();
                //var result = rpClient.Delete(requestId, out message);


                return JsonMessage(true, "حذف برداشت مستقیم با موفقیت انجام شد", null, false);
            }
            catch (Exception ex)
            {
                return JsonMessage(false, "حذف برداشت مستقیم با موفقیت انجام شد", null, false);         
            }
        }

        #endregion
        #region PettyCash
        public JsonResult GetDirectDebit(int UserId)
        {
            int TotalRow;
            Icls_PettyCashClient PC = new Icls_PettyCashClient();
            var obj = PC.SelectBy_DynamicWhereClause("UserID=" + UserId, 1, 100, "PettyCashID", "int", true, out TotalRow).SingleOrDefault();
            if (obj != null)
            {
                return Json(obj, JsonRequestBehavior.AllowGet);
            }
            return null;
        }
        #endregion

        public ActionResult _Index(SearchModelRequestPayment obj)
        {
            return View(obj);
        }
        public ActionResult list(SearchModelRequestPayment obj)
        {
            int pageNum = obj.PageIndex;
            int pageSize = 10;
            var UserId = User.Id;
            //string whereclouse = "AggrigateOrDuplicationID = NULL and IsWithdrawType= " + true;
            int totalRows, totalRows1;
            string CurrentDate = Utility.ToPersianDate(DateTime.Now);
            var db = new TreasuryEntities();
            //Icls_Request_PaymentClient RP = new Icls_Request_PaymentClient();
            //درصورتی که مدیر سیستم باشد باید تمام درخواست وجه ها را مشاهده نماید
            var query = db.GetAllRequestStatus(CurrentDate).Where(d => d.AggrigateOrDuplicationID == null && d.IsWithdrawType == true).OrderByDescending(d => d.RequestID).ToList();
            totalRows = query.Count();
            if (!User.IsInRole("Admin") && !User.IsInRole("Manager"))
            {
                //whereclouse = " UserID=" + UserId;
                query = query.Where(d => d.UserID == UserId).ToList();
            }
            if (!string.IsNullOrEmpty(obj.RequestDateFrom) && !string.IsNullOrEmpty(obj.RequestDateTo))
            {
                //whereclouse = whereclouse + " and RequestDate >= \"" + obj.RequestDateFrom + "\"  and RequestDate <= \"" + obj.RequestDateTo + "\"";
                query = query.Where(d => d.RequestDate.CompareTo(obj.RequestDateFrom) >= 0 && d.RequestDate.CompareTo(obj.RequestDateTo) <= 0).ToList(); ;
            }
            else
            {
                //whereclouse = whereclouse + " and FiscalYear=\"" + obj.FiscalYear + "\"";
                query = query.Where(d => d.FiscalYear == obj.FiscalYear).ToList();
            }
            if (obj.PayAmount != 0)
            {
                //whereclouse = whereclouse + " and  PayAmount >" + obj.PayAmount + " ";
                query = query.Where(d => d.PayAmount == obj.PayAmount).ToList();
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
            if (obj.RequsetId != 0)
            {
                //whereclouse = whereclouse + " and RequestID=" + obj.RequsetId;
                query = query.Where(d => d.RequestID == obj.RequsetId).ToList();
            }
            if (!string.IsNullOrEmpty(obj.Unique_Account_No))
            {
                //whereclouse = whereclouse + " and Unique_Account_No=\"" + obj.Unique_Account_No + "\"";
                query = query.Where(d => d.Unique_Account_No == obj.Unique_Account_No).ToList();
            }
            if (obj.Source_PaymentValue != 0)
            {
                //whereclouse = whereclouse + " and Source_PaymentValue=\"" + obj.Source_PaymentValue + "\"";
                query = query.Where(d => d.Source_PaymentValue == obj.Source_PaymentValue.ToString()).ToList();
            }

            //var query = RP.GetAllRequestStatus_DynamicWhereClause(CurrentDate, whereclouse, pageNum + 1, pageSize, "RequestID", "dec", false, out totalRows).ToList();

            obj.TotalPageCount = (int)Math.Ceiling(totalRows / (double)pageSize);
            obj.CurrentPage = pageNum;
            obj.List = query;
            //foreach (var item in query)
            //{
            //    if (item.AggrigateOrDuplicationStatusID == (int)RequestDescription.Duplication || item.AggrigateOrDuplicationStatusID == (int)RequestDescription.ResultOfIntegration)
            //    {
            //        var R = RP.GetAllRequestStatus_DynamicWhereClause(CurrentDate, "AggrigateOrDuplicationID =" + item.RequestID, 1, 100, "RequestID", "dec", false, out totalRows1).ToList();
            //        item.ChildList = RP.GetAllRequestStatus_DynamicWhereClause(CurrentDate, "AggrigateOrDuplicationID =" + item.RequestID, 1, 100, "RequestID", "dec", false, out totalRows1).ToList();
            //    }
            //}

            obj.CurrentPage = obj.PageIndex;
            obj.TotalItemCount = totalRows;


            return PartialView(obj);
        }

        #region MyRegion

        //[HttpGet]
        //public ActionResult Create()
        //{
        //    var dc = new TreasuryEntities();
        //    //نوع ارزها
        //    ViewBag.ExchangeLs = Utility.GetExchangeLs();
        //    //گروه بابت ها
        //    ViewBag.BehalfsGroupLs = Utility.GetBehalfsGroupLs().Where(d => d.UseForWithdraw == true);
        //    //انواع عطف مبانی پرداخت
        //    ViewBag.ReferenceTypels = Utility.GetReferenceTypeLs();
        //    //نمایش لیستی از انواع پرداخت
        //    ViewBag.Receipt_Pay_TypeDetailLs = Utility.GetReceipt_Pay_TypeDetailLs().Where(d => d.Receipt_Pay_TypeDetailID >= 7);

        //    //ذینفع اصلی 
        //    ViewBag.v_accounts = dc.v_accounts.ToList();

        //    //نوع ذینفع  
        //    var unique_account_type = dc.v_unique_account_type.ToList();
        //    unique_account_type.Add(new v_unique_account_type { Code = 0, Description = Resourse.Resources.Others });
        //    ViewBag.unique_account_type = unique_account_type;

        //    return View();
        //}

        //[HttpPost]
        //public ActionResult Create(Service.Request_PaymentService.Request_Payment obj, FormCollection frm)
        //{
        //    var db = new TreasuryFilesEntities();
        //    if (ModelState.IsValid)
        //    {
        //        //obj.UserID = GetcurrentUser().UserID;
        //        obj.UserID = User.Id;
        //        PersianCalendar pc = new PersianCalendar();
        //        pc.GetYear(DateTime.Now);
        //        obj.FiscalYear = pc.GetYear(DateTime.Now).ToString();
        //        obj.IsWithdrawType = true;
        //        string message = "";
        //        Icls_Request_PaymentClient RP = new Icls_Request_PaymentClient();
        //        Payment_ReferenceTypes_ValueClient PRV = new Payment_ReferenceTypes_ValueClient();
        //        Service.Payment_ReferenceTypes_ValueService.Payment_ReferenceTypes_Value ReferenceTypes_Value = new Service.Payment_ReferenceTypes_ValueService.Payment_ReferenceTypes_Value();

        //        var result = RP.Insert(obj, out message);
        //        if (result)
        //        {
        //            var RequestID = message.Split(';')[1];
        //            foreach (var item in frm.AllKeys)
        //            {
        //                if (item.StartsWith("Reference_") && frm[item] != "")
        //                {
        //                    var array = item.Split('_');
        //                    ReferenceTypes_Value.RequestId = decimal.Parse(RequestID);
        //                    ReferenceTypes_Value.ReferenceID = int.Parse(array[1]);
        //                    ReferenceTypes_Value.ReferenceValue = frm[item];
        //                    PRV.Insert(ReferenceTypes_Value, out message);
        //                }
        //            }


        //            Icls_Request_Payment_StatusDetailsClient RPS = new Icls_Request_Payment_StatusDetailsClient();
        //            //ثبت نامشخص در جدول وضعیت ها 
        //            Service.Request_Payment_StatusDetailsService.Request_Payment_StatusDetails StatusUnknown = new Service.Request_Payment_StatusDetailsService.Request_Payment_StatusDetails();
        //            var persianDate = Utility.ToPersianDate(DateTime.Now);
        //            StatusUnknown.UserID = User.Id;
        //            StatusUnknown.ActionDate = persianDate;
        //            StatusUnknown.ActionTime = DateTime.Now.TimeOfDay;
        //            StatusUnknown.RequestStatusTypeID = (decimal)RequestStatusTypes.Unknown;
        //            StatusUnknown.RequestID = decimal.Parse(RequestID);
        //            StatusUnknown.HostName = System.Net.Dns.GetHostName();
        //            RPS.Insert(StatusUnknown, out message);
        //            //ثبت وضعیت تایید
        //            Service.Request_Payment_StatusDetailsService.Request_Payment_StatusDetails StatusConfirmed = new Service.Request_Payment_StatusDetailsService.Request_Payment_StatusDetails();
        //            StatusConfirmed.UserID = User.Id;
        //            StatusConfirmed.ActionDate = persianDate;
        //            StatusConfirmed.ActionTime = DateTime.Now.TimeOfDay;
        //            StatusConfirmed.RequestStatusTypeID = (decimal)RequestStatusTypes.Confirmed;
        //            StatusConfirmed.RequestID = decimal.Parse(RequestID);
        //            StatusConfirmed.HostName = System.Net.Dns.GetHostName();
        //            RPS.Insert(StatusConfirmed, out message);
        //            //ثبت وضعیت تایید پرداخت
        //            Service.Request_Payment_StatusDetailsService.Request_Payment_StatusDetails StatusConfirmPayment = new Service.Request_Payment_StatusDetailsService.Request_Payment_StatusDetails();
        //            StatusConfirmPayment.UserID = User.Id;
        //            StatusConfirmPayment.ActionDate = persianDate;
        //            StatusConfirmPayment.ActionTime = DateTime.Now.TimeOfDay;
        //            StatusConfirmPayment.RequestStatusTypeID = (decimal)RequestStatusTypes.ConfirmPayment;
        //            StatusConfirmPayment.RequestID = decimal.Parse(RequestID);
        //            StatusConfirmPayment.HostName = System.Net.Dns.GetHostName();
        //            RPS.Insert(StatusConfirmPayment, out message);
        //            //ثبت وضعیت  پرداخت
        //            Service.Request_Payment_StatusDetailsService.Request_Payment_StatusDetails StatusPaid = new Service.Request_Payment_StatusDetailsService.Request_Payment_StatusDetails();
        //            StatusPaid.UserID = User.Id;
        //            StatusPaid.ActionDate = persianDate;
        //            StatusPaid.ActionTime = DateTime.Now.TimeOfDay;
        //            StatusPaid.RequestStatusTypeID = (decimal)RequestStatusTypes.Paid;
        //            StatusPaid.RequestID = decimal.Parse(RequestID);
        //            StatusPaid.HostName = System.Net.Dns.GetHostName();
        //            RPS.Insert(StatusPaid, out message);
        //        }

        //        return RedirectToAction("Index", "DirectDebit", new { Area = "Payment" });
        //    }
        //    return View();
        //}

        #endregion
        #region Search panel
        public ActionResult Search()
        {
            //لیستی از انواع پرداخت ها
            //ViewBag.Receipt_Pay_TypeDetailLs = Utility.GetReceipt_Pay_TypeDetailLs();
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
            ViewBag.BehalfsGroupLs = Utility.GetBehalfsGroupLs().Where(d => d.Receipt_Pay_TypeID == int.Parse(ConfigurationManager.AppSettings["Direct_Type_ID"]));


            //بدست آوردن سال مالی جاری
            PersianCalendar pc = new PersianCalendar();

            var Month = pc.GetMonth(DateTime.Now);
            ViewBag.FiscalYear = pc.GetYear(DateTime.Now).ToString();
            if (Month >= 10 && ConfigurationManager.AppSettings["YearPlus"] == "True")
            {
                ViewBag.FiscalYear = (pc.GetYear(DateTime.Now) + 1).ToString();
            }

            //لیستی از بانک ها
            ViewBag.BasicBanksLs = Utility.GetBasicBanks();



            ////لیستی از بابت ها
            //ViewBag.Behalfls = Utility.GetBehalfLs();
            return PartialView();
        }
        #endregion
        #region Edit
        [HttpGet]
        public ActionResult Edit(int requestId)
        {
            var db = new TreasuryEntities();
            //نوع ارزها
            ViewBag.ExchangeLs = Utility.GetExchangeLs();
            //انواع عطف مبانی پرداخت
            ViewBag.ReferenceTypels = Utility.GetReferenceTypeLs();

            //نمایش لیستی از انواع پرداخت
            //ViewBag.Receipt_Pay_TypeDetailLs = Utility.GetReceipt_Pay_TypeDetailLs();

            //لیستی از مبدا و منبع 
            ViewBag.SourceDestinationTypesLs = Utility.GetSourceDestinationTypesLs();

            Icls_Request_PaymentClient RP = new Icls_Request_PaymentClient();
            //var RequestPayment = RP.SelectRow(requestId).SingleOrDefault();
            var RequestPayment = db.Request_Payment.Find(requestId);

            Icls_BehalfsClient obj = new Icls_BehalfsClient();
            var BehalfGroupID = obj.SelectRow(RequestPayment.BehalfID).SingleOrDefault().BehalfGroupID;
            //بانک
            ViewBag.bankid = RequestPayment.Source_PaymentValue;
            ViewBag.BasicBankid = Utility.GetBankInfoBankId(int.Parse(RequestPayment.Source_PaymentValue)).BasicBankID;
            // بابت ها
            ViewBag.BehalfsLs = Utility.GetBehalfLs(BehalfGroupID);

            //گروه بابت ها
            ViewBag.BehalfGroupId = BehalfGroupID;
            ViewBag.BehalfsGroupLs = Utility.GetBehalfsGroupLs().Where(d => d.Receipt_Pay_TypeID == int.Parse(ConfigurationManager.AppSettings["Direct_Type_ID"]));


            //نوع ذینفع  
            var id = RequestPayment.Unique_Account_No.Trim();
            var unique_account = db.v_unique_accounts.SingleOrDefault(d => d.Account_Code == id);
            var unique_account2 = db.v_unique_accounts.SingleOrDefault(d => d.Account_Code == RequestPayment.Main_Unique_Account_No.Trim());
            if (unique_account == null)
            {
                //درصورتی که نوع ذینفع نداشته باشد
                ViewBag.uniqueTypeId = 0;
                ViewBag.SlectedLs = null;
            }
            else
            {
                //برگرداندن مقدار نوع ذینفع برای نمایش
                ViewBag.uniqueTypeId = unique_account.Type_Code.Trim();
                ViewBag.uniqueAccount_Code = unique_account.Account_Code;
                ViewBag.uniqueAccount_Name = unique_account.Account_Name;


                ViewBag.SlectedLs = (from p in db.v_unique_accounts
                                     where p.Type_Code == unique_account.Type_Code
                                     select new
                                     {
                                         Account_Code = p.Account_Code,
                                         Account_Name = p.Account_Name + "/" + p.Account_Code
                                     }).ToList();
            }

            //نوع ذینفع ها
            ViewBag.unique_account_type = db.v_unique_account_type.ToList();

            //ذیفع ها 
            ViewBag.unique_accounts = db.v_unique_accounts.Where(d => d.Type_Code == unique_account.Type_Code);

            //ذینفع اصلی  
            ViewBag.accounts = db.v_accounts.ToList();

            //نوع ذینفع  
            var unique_account_type = db.v_unique_account_type.ToList();
            unique_account_type.Add(new v_unique_account_type { Code = 0, Description = Resourse.Resources.Others });
            ViewBag.unique_account_type = unique_account_type;

            //ذینفع اصلی 
            ViewBag.v_accounts = db.v_accounts.ToList();
            //بانک /شماره حساب
            ViewBag.BasicBanksLs = Utility.GetBasicBanks();
            ViewBag.BankLS = Utility.GetBanks();

            return PartialView(RequestPayment);
        }
        [HttpPost]
        public ActionResult Edit(CSD.Treasury.DataModel.Request_Payment obj, FormCollection frm)
        {
            var db = new TreasuryEntities();
            //string Message = "", Mess1;
            int TotalRow;
            //var ProcedureClient = new Icls_SPsClient();
            //var VocherClient = new Voucher_RowClient();
            //Icls_Request_PaymentClient RP = new Icls_Request_PaymentClient();
            //Payment_ReferenceTypes_ValueClient PRV = new Payment_ReferenceTypes_ValueClient();
            var ReferenceTypes_Value = new CSD.Treasury.DataModel.Payment_ReferenceTypes_Value();
            //var RequestPayment = RP.SelectRow(int.Parse(obj.RequestID.ToString())).SingleOrDefault();
            var RequestPayment = db.Request_Payment.Find(obj.RequestID);
            if (ModelState.IsValid)
            {
                try
                {
                    //حذف نمودن عطف های بابت و ثبت مجدد آنها 
                    //string where = "RequestId =" + obj.RequestID;
                    //int totalRows;
                    //var ReferenceTypeValueLs = PRV.SelectBy_DynamicWhereClause(where, 1, 100, "ReferenceTypeValueID", "int", true, out totalRows);
                    var ReferenceTypeValueLs = db.Payment_ReferenceTypes_Value.Where(d => d.RequestId == obj.RequestID).ToList();
                    db.Payment_ReferenceTypes_Value.RemoveRange(ReferenceTypeValueLs);
                    //foreach (var item in ReferenceTypeValueLs)
                    //{
                    //    PRV.Delete(item.ReferenceTypeValueID, out Message);
                    //}
                    //ثبت عطف های بابت 

                    foreach (var item in frm.AllKeys)
                    {
                        if (item.StartsWith("Reference_") && frm[item] != "")
                        {

                            var array = item.Split('_');
                            ReferenceTypes_Value.RequestId = obj.RequestID;
                            ReferenceTypes_Value.ReferenceID = int.Parse(array[1]);
                            ReferenceTypes_Value.ReferenceValue = frm[item];
                            db.Payment_ReferenceTypes_Value.Add(ReferenceTypes_Value);
                            //var R = PRV.Insert(ReferenceTypes_Value, out Message);
                        }
                    }
                    RequestPayment.Main_Unique_Account_No = obj.Main_Unique_Account_No;
                    RequestPayment.Unique_Account_No = obj.Unique_Account_No;
                    RequestPayment.BehalfID = obj.BehalfID;
                    RequestPayment.PayAmount = obj.PayAmount;
                    RequestPayment.ExchangeRate = obj.ExchangeRate;

                    if (obj.Source_TypeID != null)
                    {
                        RequestPayment.Source_TypeID = obj.Source_TypeID;
                    }
                    if (obj.Source_PaymentValue != null)
                    {
                        RequestPayment.Source_PaymentValue = obj.Source_PaymentValue;
                    }
                    if (RequestPayment.Destination_TypesID != null)
                    {
                        RequestPayment.Destination_TypesID = obj.Destination_TypesID;
                    }
                    if (RequestPayment.Destination_PaymentValue != null)
                    {
                        RequestPayment.Destination_PaymentValue = obj.Destination_PaymentValue;
                    }
                    db.SaveChanges();
                    return JsonMessage(true, "ویرایش با موفقیت انجام شد", null, false);
                    //var result = RP.Update(RequestPayment, out Message);
                    //حذف سند ها در حالت ویرایش
                    //var VocherLs = VocherClient.SelectBy_DynamicWhereClause("ReferenceId=" + obj.RequestID, 1, 100, "RowId", "dec", false, out TotalRow);
                    //if (VocherLs.Count() != 0)
                    //{
                    //    foreach (var item in VocherLs)
                    //    {
                    //        VocherClient.Delete(int.Parse(item.RowId.ToString()), out Mess1);
                    //    }
                    //}


                    //var ls = ProcedureClient.Sp_CREATE_VOUCHER_PAYMENTS(false,(int)obj.RequestID, User.Id, false, true);

                    //return JsonMessage(true, Message);
                    //return RedirectToAction("Index", "DirectDebit", new { Model });
                }
                catch (Exception ex)
                {
                    return JsonMessage(false, "با خطا مواجه شد", null, false);
                }

            }
            return JsonMessage(false, "Error");

        }
        #endregion

        /// <summary>
        /// نمایش 
        /// </summary>
        /// <returns></returns>
        #region RequestPaymentDetails

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public PartialViewResult details(decimal requestId)
        {
            Icls_Request_PaymentClient RP = new Icls_Request_PaymentClient();
            Icls_Request_Payment_StatusDetailsClient RS = new Icls_Request_Payment_StatusDetailsClient();
            var db = new TreasuryEntities();
            var RequestPayment = db.Request_Payment.Find(requestId);
            //var RequestPayment = RP.SelectRow(requestId).SingleOrDefault();
            if (RequestPayment != null)
            {
                Icls_BehalfsClient BG = new Icls_BehalfsClient();
                ViewBag.BehalfGroupId = BG.SelectRow(RequestPayment.BehalfID).SingleOrDefault().BehalfGroupID;
            }
            return PartialView(RequestPayment);
        }

        #endregion

        #region Report

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
            StiReport report = new StiReport();
            var db = new TreasuryEntities();
            //int pageNum = 1;
            //int pageSize = 5000 * 5000;
            //string orderByFieldName = "RequestID";
            //bool isAscendingOrder = false;
            //string OrderByfeildtype = "dec";
            string whereclouse = "IsWithdrawType=" + true;
            //int totalRos;

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
                query = query.Where(d => d.RequestDate.CompareTo(obj.RequestDateFrom) >= 0 && d.RequestDate.CompareTo(obj.RequestDateTo) <= 0).ToList();
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
            if (obj.Source_PaymentValue != 0)
            {
                //whereclouse = whereclouse + " and Source_PaymentValue=\"" + obj.Source_PaymentValue + "\"";
                query = query.Where(d => d.Source_PaymentValue == obj.Source_PaymentValue.ToString()).ToList();
            }
            if (obj.Pay_TypeDetailID_source != 0)
            {
                //whereclouse = whereclouse + " and Pay_TypeDetailID_source=\"" + obj.Pay_TypeDetailID_source + "\"";
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
            string Path = Server.MapPath("~/Report/Directdebitreport.mrt");
            report.Load(Path);
            report.RegBusinessObject("RequestObj", query);
            report.Dictionary.SynchronizeBusinessObjects();
            report.Dictionary.Variables["CompanyName"].Value = ConfigurationManager.AppSettings["CompanyTitle"];


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

        #endregion
    }
}