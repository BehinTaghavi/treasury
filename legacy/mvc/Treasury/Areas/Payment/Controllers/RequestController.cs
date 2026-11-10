using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using CSD.Treasury.UI.Models;
using CSD.Treasury.Service.Request_PaymentService;
using CSD.Treasury.Service.Source_DestinationTypesService;
using CSD.Treasury.Service.Receipt_Pay_TypeDetailService;
using System.Text;
using CSD.Common.Models;
using CSD.Treasury.UI.Controllers;
using CSD.Treasury.Service.BehalfsService;
using CSD.Treasury.Service.BehalfGroupService;
using CSD.Treasury.Service.Payment_ReferenceTypes_ValueService;
using CSD.Treasury.Service.Request_Payment_StatusDetailsService;
using CSD.Treasury.Service.TransferFromBackupBankService;
using CSD.Treasury.Service.PermissiveSignaturesService;
using CSD.Treasury.Service.PaymentService;
using CSD.Treasury.Service.PermissiveSignature_CompleteProccessService;
using CSD.Treasury.Service.PermissiveSignatureForEachBankAccountService;
using CSD.Treasury.Service.BankCardsService;
using CSD.Treasury.Service.Vocher_RowService;
using CSD.Treasury.Service.ChequesService;
using CSD.Treasury.Service.ProceduresDB;
using CSD.Treasury.Service.Request_Payment_BreakPriceService;
using CSD.Treasury.Service.ChequeFormatTypeDetailsService;
using CSD.Common.Utility;
using CSD.Treasury.DataModel;
using System.Globalization;
using CSD.Treasury.Service.BankService;
using CSD.Treasury.Service.CashDeskService;
using CSD.Treasury.Common.Enumeration;
using CSD.Treasury.Service.ReferenceTypes_By_BehalfGroupService;
using System.Configuration;
using System.Data.Entity.Validation;
using Newtonsoft.Json;
using CSD.Treasury.Service.BeneficiariesDetailInfoService;
using CSD.Treasury.Service.BeneficiaryService;


namespace CSD.Treasury.UI.Areas.Payment.Controllers
{
    [Authorize(Roles = "Admin,Manager,RequestPayment,PettyCash")]
    public class RequestController : BaseController
    {
        /// <summary>
        /// نمایش لیستی از درخواست وجه ها به کاربر
        /// </summary>
        /// <returns></returns>
        public ActionResult Index(SearchModelRequestPayment obj)
        {
            return View(obj);
        }
        public ActionResult RequestPaymentHelp()
        {
            return View();
        }
        public ActionResult List(SearchModelRequestPayment obj)
        {
            int pageNum = obj.NotAccepted ? 0 : obj.PageIndex;
            int pageSize = obj.NotAccepted ? 3000 : 10;
            var UserId = User.Id;
            //string whereclouse = "";
            var db = new TreasuryEntities();
            string CurrentDate = Utility.ToPersianDate(DateTime.Now);
            var query = db.GetAllRequestStatus(CurrentDate).OrderByDescending(d => d.RequestID).ToList();
            var totalRows = query.Count();
            if (obj.PayAmount > 0 || obj.RequsetId > 0)
            {
                //whereclouse = "IsWithdrawType = " + false;
                query = query.Where(d => d.IsWithdrawType == false).ToList();
            }
            else
            {
                //whereclouse = "AggrigateOrDuplicationID=NULL and  IsWithdrawType = " + false;
                query = query.Where(d => d.AggrigateOrDuplicationID == null && d.IsWithdrawType == false).ToList();
            }
            //int totalRows;

            //Icls_Request_PaymentClient RP = new Icls_Request_PaymentClient();

            //درصورتی که مدیر سیستم باشد باید تمام درخواست وجه ها را مشاهده نماید
            if (!User.IsInRole("Admin") && !User.IsInRole("Manager"))
            {
                //whereclouse = " UserID=" + UserId;
                query = query.Where(d => d.UserID == UserId).ToList();
            }
            if (!string.IsNullOrEmpty(obj.RequestDateFrom) && !string.IsNullOrEmpty(obj.RequestDateTo))
            {
                //whereclouse = whereclouse + " and RequestDate >= \"" + obj.RequestDateFrom + "\"  and RequestDate <= \"" + obj.RequestDateTo + "\"";
                //query = query.Where(d=>d.RequestDate>=obj.RequestDateFrom );
                query = query.Where(d => d.RequestDate.CompareTo(obj.RequestDateFrom) >= 0 && d.RequestDate.CompareTo(obj.RequestDateTo) <= 0).ToList();
            }
            else
            {
                //whereclouse = whereclouse + " and FiscalYear=\"" + obj.FiscalYear + "\"";
                query = query.Where(d => d.FiscalYear == obj.FiscalYear).ToList();
            }
            if (obj.PayAmount != 0)
            {
                //whereclouse = whereclouse + " and  PayAmount ==" + obj.PayAmount + " ";
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
                //var Request = RP.SelectRow(obj.RequsetId);
                var Request = db.Request_Payment.Find(obj.RequsetId);
                if (Request != null)
                {
                    var AggrigateOrDuplicationID = Request.AggrigateOrDuplicationID;
                    if (AggrigateOrDuplicationID != null)
                    {
                        //whereclouse = whereclouse + " and RequestID=" + AggrigateOrDuplicationID;
                        query = query.Where(d => d.RequestID == AggrigateOrDuplicationID).ToList();
                    }
                    else
                    {
                        //whereclouse = whereclouse + " and RequestID=" + obj.RequsetId;
                        query = query.Where(d => d.RequestID == obj.RequsetId).ToList();
                    }
                }

            }
            if (obj.ChequeNo != 0)
            {
                //whereclouse = whereclouse + " and ChequeNo=" + obj.ChequeNo;
                query = query.Where(d => d.ChequeNo == obj.ChequeNo).ToList();
            }
            //if (!string.IsNullOrEmpty(obj.FiscalYear))
            //{
            //   
            //}
            if (!string.IsNullOrEmpty(obj.Unique_Account_No))
            {
                //whereclouse = whereclouse + " and Unique_Account_No=\"" + obj.Unique_Account_No + "\"";
                query = query.Where(d => d.Unique_Account_No == obj.Unique_Account_No).ToList();
            }

            //var query = RP.GetAllRequestStatus_DynamicWhereClause(CurrentDate, "1=1", pageNum + 1, pageSize, "RequestID", "dec", false, out totalRows);
            List<DataModel.GetAllRequestStatus_Result> list = new List<DataModel.GetAllRequestStatus_Result>();

            if (obj.NotAccepted)
            {

                //whereclouse += whereclouse + " and Cancellation is NULL and Confirmed is NULL and Pay_TypeDetailID_source is not NULL and Unique_Account_No <> 0";
                list = query.Where(q => q.Cancellation == null && q.Confirmed == null && q.Unknown != null).ToList();
            }
            else
            {
                list = query.ToList();
            }

            //var All = RP.GetAllRequestStatus_DynamicWhereClause(CurrentDate, " IsWithdrawType = " + false + " and FiscalYear=\"" + obj.FiscalYear + "\" and AggrigateOrDuplicationID !=NULL ", 1, 400, "RequestID", "dec", false, out totalRows1).ToList();
            //var qls = from q in list
            //          where All.Select(d => d.AggrigateOrDuplicationID).Contains(q.RequestID)
            //          select q;

            //foreach (var item in qls)
            //{
            //    //if (item.AggrigateOrDuplicationStatusID == (int)RequestDescription.Duplication || item.AggrigateOrDuplicationStatusID == (int)RequestDescription.ResultOfIntegration || item.AggrigateOrDuplicationStatusID == (int)RequestDescription.ResultOfIntegrationRequest)
            //    //{
            //    item.ChildList = RP.GetAllRequestStatus_DynamicWhereClause(CurrentDate, "AggrigateOrDuplicationID = " + item.RequestID, 1, 100, "RequestID", "dec", false, out totalRows1).ToList();
            //    if (obj.PayAmount > 0 || obj.RequsetId > 0)
            //    {
            //        item.AggrigateOrDuplicationID = null;
            //        item.AggrigateOrDuplicationStatusID = null;
            //    }
            //    //}
            //}
            obj.TotalPageCount = (int)Math.Ceiling(totalRows / (double)pageSize);
            obj.CurrentPage = pageNum;
            obj.List = list;
            obj.CurrentPage = obj.PageIndex;
            obj.TotalItemCount = list.Count;

            //ViewBag.SearchModelRequestPayment = obj;
            //ViewBag.TotalPageCount = Math.Ceiling((decimal)totalRows / pageSize);

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
            //var ls = RP.GetAllRequestStatus_DynamicWhereClause(CurrentDate, "AggrigateOrDuplicationID = " + obj.RequsetId, 1, 100, "RequestID", "dec", false, out totalRows);
            var ls = db.GetAllRequestStatus(CurrentDate).Where(d => d.AggrigateOrDuplicationID == obj.RequsetId);
            obj.List = ls.ToList();
            var count = obj.List.Count();
            totalRows = count;
            obj.TotalPageCount = (int)Math.Ceiling(totalRows / (double)pageSize);
            obj.CurrentPage = pageNum;

            obj.CurrentPage = obj.PageIndex;

            obj.TotalItemCount = count;
            return PartialView(obj);
        }


        /// <summary>
        /// ثبت درخواست وجه کابر ی که به سایت وارد شده
        /// </summary>
        /// <returns></returns>
        #region CreateRequest
        public string GetAccountShebaNo(string UniqueAccountNo, decimal RequestID = 0)
        {
            var db = new TreasuryEntities();
            Icls_Request_PaymentClient RP = new Icls_Request_PaymentClient();
            var RequestPayment = db.Request_Payment.Find(RequestID);
            //var RequestPayment = RP.SelectRow(RequestID).SingleOrDefault();
            var Description = new string[10];
            Description[0] = "0";
            if (RequestID != 0)
            {
                if (RequestPayment.Description != null)
                {
                    Description = RequestPayment.Description.Split(';');
                }
            }

            var res = new StringBuilder();
            int totalRows;
            Icls_BeneficiaryClient BeneficiaryClient = new Icls_BeneficiaryClient();
            var qry = BeneficiaryClient.SelectBy_DynamicWhereClause("Unique_Account_No =\"" + UniqueAccountNo + "\"", 1, 100, "BeneficiarID", "int", true, out totalRows);
            Icls_BeneficiariesDetailInfoClient bdetailsClinet = new Icls_BeneficiariesDetailInfoClient();
            if (qry != null && qry.Count() != 0)
            {
                var query = new List<Treasury.Service.BeneficiariesDetailInfoService.BeneficiariesDetailInfo>();
                foreach (var item in qry)
                {
                    var ls = bdetailsClinet.SelectBy_DynamicWhereClause("BeneficiarID =" + item.BeneficiarID, 1, 100, "BeneficiarDetailID", "int", false, out totalRows).ToList();
                    query.AddRange(ls);
                }

                //res.Append("<option  value = '0' >" + Resourse.Resources.PleaseSelect + "</option>");
                foreach (var i in query)
                {
                    if (i.Bank_AccountNumber != null)
                    {
                        res.Append("<option " + (int.Parse(Description[0]) == i.BeneficiarDetailID ? "selected = 'selected'" : "") + " value=" + i.BeneficiarDetailID + ">" + i.Bank_AccountNumber + "</option>");
                    }
                    else if (i.Bank_ShebaNo != null)
                    {
                        res.Append("<option " + (int.Parse(Description[0]) == i.BeneficiarDetailID ? "selected='selected'" : "") + " value=" + i.BeneficiarDetailID + ">" + i.Bank_ShebaNo + "</option>");

                    }
                }

                return res.ToString();
            }


            return res.ToString();
        }
        [HttpGet]
        public ActionResult Create1()
        {
            var dc = new TreasuryEntities();
            //نوع ذینفع  
            var unique_account_type = dc.v_unique_account_type.ToList();
            //ViewBag.unique_account_type2 = unique_account_type;

            unique_account_type.Add(new v_unique_account_type { Code = 0, Description = Resourse.Resources.Others });
            ViewBag.unique_account_type = unique_account_type;

            return View();
        }
        [HttpGet]
        public ActionResult Create()
        {
            //نوع ارزها
            ViewBag.ExchangeLs = Utility.GetExchangeLs();
            //گروه بابت ها
            ViewBag.BehalfsGroupLs = Utility.GetBehalfsGroupLs().Where(d => d.Receipt_Pay_TypeID == int.Parse(ConfigurationManager.AppSettings["Payment_Type_ID"]));
            //انواع عطف مبانی پرداخت
            ViewBag.ReferenceTypels = Utility.GetReferenceTypeLs();
            //نمایش لیستی از انواع پرداخت
            ViewBag.Receipt_Pay_TypeDetailLs = Utility.GetReceipt_Pay_TypeDetailLs().Where(d => d.Receipt_Pay_TypeID == 1).ToList();


            var dc = new TreasuryEntities();
            //نوع ذینفع  
            var unique_account_type = dc.v_unique_account_type.ToList();
            //ViewBag.unique_account_type2 = unique_account_type;

            unique_account_type.Add(new v_unique_account_type { Code = 0, Description = Resourse.Resources.Others });
            ViewBag.unique_account_type = unique_account_type;

            //ذینفع اصلی 
            ViewBag.v_accounts = (from a in dc.v_accounts
                                  select new
                                  {
                                      account_code = a.account_code.Trim(),
                                      Account_Name = a.Account_Name + "-" + a.account_code.Trim()

                                  }).ToList();

            //var q = from o in dc.v_accounts
            //        select new JsonObj { value =Int16.Parse(o.account_code), Text = o.Account_Name };
            //ViewBag.unique_account_type = Json(q, JsonRequestBehavior.AllowGet);
            var model = new Treasury.DataModel.Request_Payment();
            //model.Request_Payment_BreakPrice = new List<Treasury.DataModel.Request_Payment_BreakPrice>();
            //model.Request_Payment_BreakPrice.Add(new Treasury.DataModel.Request_Payment_BreakPrice());
            return View(model);
        }

        [HttpPost]
        public ActionResult Create(CSD.Treasury.DataModel.Request_Payment obj, FormCollection frm)
        {
            try
            {
                var db = new TreasuryFilesEntities();
                var dc = new TreasuryEntities();
                //درصورتی که چک نقل و انتقال باشد به گروه بابت احتیاج نیست 
                //بنابرین اولین بابت رادراینحالت ذخیره می نمایید ولی درسند از این گروه بابت استفاده نمی گردد
                if (obj.Pay_TypeDetailID_source == 5)
                {
                    var behalf = dc.Behalfs.FirstOrDefault();
                    if (behalf != null)
                    {
                        obj.BehalfID = behalf.BehalfID;
                    }
                    ModelState.Remove("BehalfGropId");
                }
                if (ModelState.IsValid)
                {
                    //obj.UserID = GetcurrentUser().UserID;
                    obj.UserID = User.Id;
                    PersianCalendar pc = new PersianCalendar();
                    var Month = pc.GetMonth(DateTime.Now);
                    obj.FiscalYear = pc.GetYear(DateTime.Now).ToString();

                    if (Month >= 10 && ConfigurationManager.AppSettings["YearPlus"] == "True")
                    {
                        obj.FiscalYear = (pc.GetYear(DateTime.Now) + 1).ToString();
                    }

                    if (obj.Source_TypeID == 1 || obj.Source_TypeID == 3)
                    {
                        obj.BankID = int.Parse(obj.Source_PaymentValue);
                    }

                    if (obj.Source_TypeID == 2)
                    {
                        obj.CashDeskID = int.Parse(obj.Source_PaymentValue);
                    }
                    if (obj.ExchangeTypeID != null)
                    {
                        //obj.ExchangeAmount = obj.PayAmount;
                        obj.ExchangeAmount = obj.PayAmount * obj.ExchangeRate;
                        obj.PayAmount = 0;
                    }
                    //decimal payAmount = 0;
                    //foreach (var item in obj.Request_Payment_BreakPrice)
                    //{
                    //    payAmount += (item.Exchange_Rate * item.Exchange_Amount);
                    //}
                    //چون چندین مبلغ می تواند اضافه کند مبلع و نرخ ارز در جدول Request_Payment_BreakPrice
                    //ذخیره شد و درواقع این دوفیلد در این جدول بلااستفاده است در طراحی اول استفاده میشد که یک مبلغ داشت

                    //obj.PayAmount = payAmount;
                    //obj.ExchangeTypeID = null;
                    //obj.ExchangeRate = 1;
                    if (obj.Description != null)
                    {
                        obj.Description = obj.Description + ";";
                    }
                    //درصورتی که پرداخت نقدی باشد شماره چک صفر و تاریخ سررسید هم تاریخ پرداخت می خورد

                    //if (obj.Source_TypeID == 2)
                    //{
                    //    obj.ChequeNo = 0;
                    //    obj.DueDate = obj.RequestDate;
                    //}
                    if (obj.Pay_TypeDetailID_source == 33 || obj.Pay_TypeDetailID_source == 32 || obj.Pay_TypeDetailID_source == 8 || obj.Pay_TypeDetailID_source == 7)
                    {
                        obj.RequestDate = obj.DueDate.Replace("/", "");
                        obj.DueDate = obj.DueDate.Replace("/", "");
                        //obj.ChequeNo = 0;
                    }

                    //string message = "", Message1 = "";
                    //Icls_Request_PaymentClient RP = new Icls_Request_PaymentClient();
                    //Payment_ReferenceTypes_ValueClient PRV = new Payment_ReferenceTypes_ValueClient();
                    //Icls_Request_Payment_BreakPriceClient BreakPriceClient = new Icls_Request_Payment_BreakPriceClient();
                    //Service.Payment_ReferenceTypes_ValueService.Payment_ReferenceTypes_Value ReferenceTypes_Value = new Service.Payment_ReferenceTypes_ValueService.Payment_ReferenceTypes_Value();
                    DataModel.Payment_ReferenceTypes_Value ReferenceTypes_Value = new DataModel.Payment_ReferenceTypes_Value();
                    List<DataModel.Payment_ReferenceTypes_Value> ReferenceTypes_Value_Ls = new List<DataModel.Payment_ReferenceTypes_Value>();
                    if (string.IsNullOrEmpty(obj.Unique_Account_No))
                    {
                        obj.Unique_Account_No = "000000";
                    }

                    //var result = RP.Insert(obj, out message);
                    dc.Request_Payment.Add(obj);
                    //if (result)
                    //{
                    //var RequestID = message.Split(';')[1];


                    //افزودن نرخ چند ارزی برای پرداخت نقدی
                    //if (obj.Request_Payment_BreakPrice.Where(d=>d.ExchangeTypeID!=0).Count()!=0)
                    //{

                    //    //Service.Request_Payment_BreakPriceService.Request_Payment_BreakPrice BreakPriceObj = new Service.Request_Payment_BreakPriceService.Request_Payment_BreakPrice();
                    //    Treasury.DataModel.Request_Payment_BreakPrice BreakPriceObj = new DataModel.Request_Payment_BreakPrice();
                    //    foreach (var item in obj.Request_Payment_BreakPrice)
                    //    {
                    //        if (item.ExchangeType !=null)
                    //        {


                    //            if (item.ExchangeTypeID == 0)
                    //            {
                    //                item.ExchangeTypeID = null;
                    //            }
                    //            //BreakPriceObj.RequestID = decimal.Parse(RequestID);
                    //            BreakPriceObj.Exchange_Rate = item.Exchange_Rate;
                    //            BreakPriceObj.Exchange_Amount = item.Exchange_Amount;
                    //            BreakPriceObj.ExchangeTypeID = item.ExchangeTypeID;
                    //            BreakPriceObj.Price_Rial = item.Exchange_Amount * item.Exchange_Rate;
                    //        }
                    //        //BreakPriceClient.Insert(BreakPriceObj, out Message1);

                    //    }
                    //      obj.Request_Payment_BreakPrice.Add(BreakPriceObj);
                    //}


                    //Icls_Request_Payment_StatusDetailsClient RPS = new Icls_Request_Payment_StatusDetailsClient();
                    //ثبت نامشخص در جدول وضعیت ها 
                    //Service.Request_Payment_StatusDetailsService.Request_Payment_StatusDetails objStatus = new Service.Request_Payment_StatusDetailsService.Request_Payment_StatusDetails();
                    CSD.Treasury.DataModel.Request_Payment_StatusDetails objStatus = new CSD.Treasury.DataModel.Request_Payment_StatusDetails();
                    var persianDate = Utility.ToPersianDate(DateTime.Now);
                    objStatus.UserID = User.Id;
                    if (obj.Pay_TypeDetailID_source == 33)
                    { objStatus.ActionDate = obj.DueDate; }
                    else
                    { objStatus.ActionDate = persianDate; }

                    objStatus.ActionTime = DateTime.Now.TimeOfDay;
                    objStatus.RequestStatusTypeID = (decimal)RequestStatusTypes.Unknown;

                    //objStatus.RequestID = decimal.Parse(RequestID);
                    objStatus.HostName = System.Net.Dns.GetHostName();
                    obj.Request_Payment_StatusDetails.Add(objStatus);
                    //dc.Request_Payment_StatusDetails.Add(objStatus);

                    dc.SaveChanges();

                    foreach (var item in frm.AllKeys)
                    {
                        if (item.StartsWith("Reference_") && frm[item] != "")
                        {
                            var array = item.Split('_');
                            //ReferenceTypes_Value.RequestId = decimal.Parse(RequestID);
                            ReferenceTypes_Value.ReferenceID = int.Parse(array[1]);
                            ReferenceTypes_Value.ReferenceValue = frm[item];
                            ReferenceTypes_Value.RequestId = obj.RequestID;
                            dc.Payment_ReferenceTypes_Value.Add(ReferenceTypes_Value);
                            dc.SaveChanges();
                            //PRV.Insert(ReferenceTypes_Value, out message);
                        }
                    }
                    //var result2 = RPS.Insert(objStatus, out message);
                    //}

                    return RedirectToAction("Index", "Request", new { Area = "Payment" });
                }
                return JsonMessage(false, "insert_error");
            }
            catch (Exception ex)
            {
                return JsonMessage(false, "insert_error");
                throw;
            }
            //return View(obj);

        }

        public string GetReferenceTypeByBehalfGroup(int BehalfGroupID)
        {
            string whereclouse = "BehalfGroupID=" + BehalfGroupID;
            int totalRows;
            Icls_ReferenceTypes_By_BehalfGroupClient obj = new Icls_ReferenceTypes_By_BehalfGroupClient();
            var res = new StringBuilder();
            var query = obj.SelectBy_DynamicWhereClause(whereclouse, 1, 10000, "ReferenceID", "int", true, out totalRows).ToList();

            //Payment_ReferenceTypes_ValueClient PRV = new Payment_ReferenceTypes_ValueClient();
            //string whereclouse1 = " RequestId=" + requestId;
            //var exist = PRV.SelectBy_DynamicWhereClause(whereclouse1, 1, 10000, "ReferenceTypeValueID", "int", true, out totalRows).ToList();
            if (query != null)
            {
                foreach (var i in query)
                {
                    res.Append("<label class='control-label col-sm-2' for='BehalfGropId'>" + Utility.GetReferenceTitle(i.referenceTypeID) + "</label><div class='col-sm-10' ><input class='form-control floating-label' required='required'  placeholder='" + Utility.GetReferenceTitle(i.referenceTypeID) + "' type='text'  id = 'Reference_" + i.ReferenceID + "' name = 'Reference_" + i.ReferenceID + "'/><span class='field - validation - error' data-valmsg-for='Reference_" + i.ReferenceID + "' data-valmsg-replace='true'><span for='Reference_" + i.ReferenceID + "'>این فیلد اجباری است.</span></div>");
                }
                return res.ToString();
            }
            return "";

        }

        public JsonResult HasUniqueAccount(int BehalfGroupId)
        {
            Icls_BehalfGroupClient BehalfGroupClient = new Icls_BehalfGroupClient();
            var dc = new TreasuryEntities();
            List<bool> Ls = new List<bool>();

            var behalfGroup = BehalfGroupClient.SelectRow(BehalfGroupId);
            if (behalfGroup != null)
            {
                var BehlafGroupAccount = behalfGroup.SingleOrDefault().BehalfGroup_Account;
                var query = dc.v_accounts.SingleOrDefault(d => d.account_code == BehlafGroupAccount);
                if (query.Has_Unique_Account)
                    Ls.Add(true);
                else
                    Ls.Add(false);
                if (query.Has_Unique_Account_Two)
                    Ls.Add(true);
                else Ls.Add(false);
            }
            return Json(Ls, JsonRequestBehavior.AllowGet);
        }


        #endregion


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
            var Month = pc.GetMonth(DateTime.Now);
            ViewBag.FiscalYear = pc.GetYear(DateTime.Now).ToString();
            if (Month >= 10 && ConfigurationManager.AppSettings["YearPlus"] == "True")
            {
                ViewBag.FiscalYear = (pc.GetYear(DateTime.Now) + 1).ToString();
            }

            ////لیستی از بابت ها
            //ViewBag.Behalfls = Utility.GetBehalfLs();
            return PartialView();
        }
        #endregion
        /// <summary>
        /// لیست ها ی استفاده شده در ویو
        /// </summary>
        /// <returns></returns>
        #region List

        /// <summary>
        /// لیستی از انواع مقصد یا منبع را بر می گرداند
        /// </summary>
        /// <returns></returns>
        public string GetSource(int id)
        {
            var res = new StringBuilder();

            Icls_Receipt_Pay_TypeDetailClient obj1 = new Icls_Receipt_Pay_TypeDetailClient();
            var Receipt_Pay_TypeDetail = obj1.SelectRow(id);

            Icls_Source_DestinationTypesClient obj = new Icls_Source_DestinationTypesClient();
            if (Receipt_Pay_TypeDetail.SingleOrDefault().Source_TypeID != null)
            {
                var list = obj.SelectRow(Receipt_Pay_TypeDetail.SingleOrDefault().Source_TypeID.Value);
                foreach (var i in list)
                {
                    res.Append("<option value='" + i.Source_DestinationTypeID + "'>" + i.Source_DestinationTypeDescription + "</option>");
                }
                return res.ToString();
            }
            else
                return "---";

        }

        public string GetDestination(int id)
        {
            var res = new StringBuilder();

            Icls_Receipt_Pay_TypeDetailClient obj1 = new Icls_Receipt_Pay_TypeDetailClient();
            var Receipt_Pay_TypeDetail = obj1.SelectRow(id);

            Icls_Source_DestinationTypesClient obj = new Icls_Source_DestinationTypesClient();
            if (Receipt_Pay_TypeDetail.SingleOrDefault().DestinationTypeID != null)
            {
                var list = obj.SelectRow(Receipt_Pay_TypeDetail.SingleOrDefault().Source_TypeID.Value);
                foreach (var i in list)
                {
                    res.Append("<option value='" + i.Source_DestinationTypeID + "'>" + i.Source_DestinationTypeDescription + "</option>");
                }
                return res.ToString();
            }
            else
                return "---";

        }

        /// <summary>
        /// چک نمودن نمایش یا عدم نمایش سررسید
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static bool CheckDueDate(int id)
        {
            var res = new StringBuilder();

            Icls_Receipt_Pay_TypeDetailClient obj = new Icls_Receipt_Pay_TypeDetailClient();
            var Receipt_Pay_TypeDetail = obj.SelectRow(id);

            if (Receipt_Pay_TypeDetail.SingleOrDefault().Has_due_date)
            {
                return true;
            }
            else
                return false;

        }

        /// <summary>
        /// لیستی از بابت ها  را از دیتا بیس بر می گرداند
        /// </summary>
        /// <returns></returns>
        public string GetBehalfs(int id)
        {
            var res = new StringBuilder();
            string whereclouse = "BehalfGroupID=" + id;
            int totalRows;
            Icls_BehalfsClient obj = new Icls_BehalfsClient();
            var query = obj.SelectBy_DynamicWhereClause(whereclouse, 1, 10000, "BehalfID", "int", true, out totalRows).ToList();

            foreach (var i in query)
            {
                if (i.BehalfDescription.Contains('ي'))
                {
                    i.BehalfDescription = i.BehalfDescription.Replace('ي', 'ی');
                }
                if (i.BehalfDescription.Contains('ك'))
                {
                    i.BehalfDescription = i.BehalfDescription.Replace('ك', 'ک');
                }
                res.Append("<option value='" + i.BehalfID + "'>" + i.BehalfDescription + "</option>");
            }
            return res.ToString();
        }
        public string GetBehalfGroup()
        {
            var res = new StringBuilder();

            int totalRows;
            Icls_BehalfGroupClient obj = new Icls_BehalfGroupClient();
            var query = obj.SelectBy_DynamicWhereClause("1=1", 1, 10000, "BehalfGroupID", "int", true, out totalRows).ToList();

            foreach (var i in query)
            {

                if (i.BehalfGroupDescription.Contains('ي'))
                {
                    i.BehalfGroupDescription = i.BehalfGroupDescription.Replace('ي', 'ی');
                }
                if (i.BehalfGroupDescription.Contains('ك'))
                {
                    i.BehalfGroupDescription = i.BehalfGroupDescription.Replace('ك', 'ک');
                }

                res.Append("<option value='" + i.BehalfGroupID + "'>" + i.BehalfGroupDescription + "</option>");
            }
            return res.ToString();
        }
        public string GetUniqueAccount(int id = 0)
        {
            var res = new StringBuilder();
            var dc = new TreasuryEntities();
            // درصورتی که نوع ذینفع نداشته باشد
            var list = dc.v_unique_accounts.Where(d => d.Type_Code == id.ToString()).ToList();
            if (id == 0)
            {
                list = dc.v_unique_accounts.Where(d => d.Type_Code == null).ToList();
            }
            //ذینفع ها 
            if (list != null)
            {
                foreach (var i in list)
                {
                    res.Append("<option value='" + i.Account_Code + "'>" + i.Account_Name + "</option>");
                }
                return res.ToString();
            }
            else
                return "";
        }

        /// <summary>
        /// لیستی از  بانک ها را بر می گرداند
        /// </summary>
        /// <returns></returns>
        /// 
        public ActionResult GetArziBankInfo(int BankId)
        {
            var db = new TreasuryEntities();
            var bank = db.Banks.SingleOrDefault(d => d.BankID == BankId);
            //****
            var nerkh = 1;
            var dateNow = Utility.ToPersianDate(DateTime.Now);
            var data = GetBankExchangeRate(BankId, dateNow);
            nerkh = data;
            //****
            //var ExchangeType = 0;
            //var dateNow = Utility.ToPersianDate(DateTime.Now);
            var ExchangeDesc = db.ExchangeTypes.SingleOrDefault(d => d.ExchangeTypeID == bank.ExchangeTypeID).ExchangeTypeDescription;
            return Json(new { ExchangeDesc = ExchangeDesc, ExchangeTypeID = bank.ExchangeTypeID, ExchangeType = bank.ExchangeType,ExchangeRate = nerkh }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult GetArziCashDeskInfo(int CashDeskId)
        {
            var db = new TreasuryEntities();
            var CashDesk = db.CashDesks.SingleOrDefault(d => d.CashDeskID == CashDeskId);
            //****
            var nerkh = 1;
            var dateNow = Utility.ToPersianDate(DateTime.Now);
            var data = GetCashExchangeRate(CashDeskId, dateNow);
            nerkh = data;
            //****
            //var ExchangeType = 0;
            //var dateNow = Utility.ToPersianDate(DateTime.Now);
            var ExchangeDesc = db.ExchangeTypes.SingleOrDefault(d => d.ExchangeTypeID == CashDesk.ExchangeTypeID).ExchangeTypeDescription;
            return Json(new { ExchangeDesc = ExchangeDesc, ExchangeTypeID = CashDesk.ExchangeTypeID, ExchangeType = CashDesk.ExchangeTypeValues, ExchangeRate = nerkh }, JsonRequestBehavior.AllowGet);
        }
        public string GetBanks(int selected = 0, bool destination = false, string type = "")
        {
            int totalRows;
            //Icls_BankClient BankClient = new Icls_BankClient();
            var db = new TreasuryEntities();
            Icls_ChequesClient ChequeClient = new Icls_ChequesClient();
            Icls_ChequeFormatTypeDetailsClient ChFormtTypeClient = new Icls_ChequeFormatTypeDetailsClient();
            var res = new StringBuilder();
            res.Append("<option>'لطفاانتخاب نمایید'</option>");
            List<Service.BankService.Bank> ls = new List<Service.BankService.Bank>();
            //var BankLS = BankClient.SelectAll(1, 1000, "BankID", "int", true, out totalRows).ToList();
            var BankLS = db.Banks.ToList();
            if (type != "satna")
            {
                BankLS = BankLS.Where(d => d.HasBackup == false).ToList();
            }
            if (type == "arzi")
            {
                BankLS = BankLS.Where(d => d.HasExchangeTransaction == true).ToList();

            }
            else  
            //http://localhost:4844/Payment/Pay?Status=Satna#
            {
                BankLS = BankLS.Where(d => d.HasExchangeTransaction == false).ToList();
            }
            //درصورتی که چک نقل و انتقال استفاده کرده باشد باید تمام بانک ها را جه انها که دسته چک دارد چه انها کهندارند ببیند
            if (destination || type == "satna")
            {
                if (BankLS != null)
                {
                    //res.Append("<option selected>انتخاب نمایید</option>");
                    foreach (var i in BankLS)
                    {
                        if (i.BankID == selected)
                        {
                            res.Append("<option value='" + i.BankID + "' selected=selected  >" + Utility.GetBasicBankInfo(i.BasicBankID) + "," + i.BranchName + "," + i.Bank_AccountNumber + "</option>");
                        }
                        else
                        {
                            res.Append("<option value='" + i.BankID + "'>" + Utility.GetBasicBankInfo(i.BasicBankID) + "," + i.BranchName + "," + i.Bank_AccountNumber + "</option>");
                        }
                        //res.Append("<option value='" + i.BankID + "' selected=" + (i.BankID == selected ? "selected" : "") + "  >" + Utility.GetBasicBankInfo(i.BasicBankID) + "," + i.BranchName + "," + i.Bank_AccountNumber + "</option>");
                    }
                    return res.ToString();
                }
                return "";
            }
            var checkFormatType = ChFormtTypeClient.SelectAll(1, 2000, "FormatID", "int", false, out totalRows).ToList();
            var cheques = ChequeClient.SelectBy_DynamicWhereClause("IsActive =" + true, 1, 1000, "ChequeID", "int", false, out totalRows).Where(q => checkFormatType.Select(d => d.ChequeFormatID).Contains(q.ChequeFormatID.Value) && BankLS.Select(u => u.BankID).Contains(q.BankID));


            BankLS = BankLS.Where(q => cheques.Select(d => d.BankID).Contains(q.BankID)).OrderBy(d => d.BranchName).ToList();




            var dc = new TreasuryEntities();

            if (BankLS != null)
            {
                //res.Append("<option selected>انتخاب نمایید</option>");
                foreach (var i in BankLS)
                {
                    if (i.BankID == selected)
                    {
                        res.Append("<option    value='" + i.BankID + "' selected=selected  >" + Utility.GetBasicBankInfo(i.BasicBankID) + "," + i.BranchName + "," + i.Bank_AccountNumber + "</option>");
                    }
                    else
                    {
                        res.Append("<option value='" + i.BankID + "'>" + Utility.GetBasicBankInfo(i.BasicBankID) + "," + i.BranchName + "," + i.Bank_AccountNumber + "</option>");
                    }
                    //res.Append("<option value='" + i.BankID + "' selected=" + (i.BankID == selected ? "selected" : "") + "  >" + Utility.GetBasicBankInfo(i.BasicBankID) + "," + i.BranchName + "," + i.Bank_AccountNumber + "</option>");
                }
                return res.ToString();
            }
            return "";
        }
        /// <summary>
        /// لیستی از  صندوق  ها را بر می گرداند
        /// </summary>
        /// <returns></returns>
        public string GetCashDesk()
        {
            int totalRows;
            Icls_CashDeskClient obj = new Icls_CashDeskClient();
            List<Service.CashDeskService.CashDesk> ls = new List<Service.CashDeskService.CashDesk>();
            var query = obj.SelectAll(1, 10000, "CashDeskID", "int", true, out totalRows).ToList();
            var res = new StringBuilder();
            var dc = new TreasuryEntities();
            //in khat baraie filter kardane sandoghe arzi va gheir arzi ezafe shod va meod GetCashDeskArzi ezafe shod
            var s = dc.CashDesks.Where(d => d.ExchangeTypeID == null);

            if (s != null)
            {
                foreach (var i in s)
                {
                    res.Append("<option value='" + i.CashDeskID + "'>" + i.CashDeskDescription + "</option>");
                }
                return res.ToString();
            }
            return "";
        }

        public string GetCashDeskArzi()
        {
            int totalRows;
            Icls_CashDeskClient obj = new Icls_CashDeskClient();
            List<Service.CashDeskService.CashDesk> ls = new List<Service.CashDeskService.CashDesk>();
            var query = obj.SelectAll(1, 10000, "CashDeskID", "int", true, out totalRows).ToList();
            
            
            var res = new StringBuilder();
            var dc = new TreasuryEntities();
            var s = dc.CashDesks.Where(d => d.ExchangeTypeID != null);
                

            if (s != null)
            {
                
                foreach (var i in s)
                {
                    res.Append("<option value='" + i.CashDeskID + "'>" + i.CashDeskDescription + "</option>");
                }
                return res.ToString();
            }
            return "";
        }
        /// <summary>
        /// لیستی از  کارت های بانکی  را بر می گرداند
        /// </summary>
        /// <returns></returns>
        public string GetBankCard()
        {
            int totalRows;
            //Icls_BankCardsClient obj = new Icls_BankCardsClient();
            var db = new TreasuryEntities();
            //List<Service.BankCardsService.BankCard> ls = new List<Service.BankCardsService.BankCard>();
            var Banks = db.Banks.Select(d => d.BankID);
            var query = db.BankCards.Where(d => Banks.Contains(d.BankID));
            //var query = obj.SelectAll(1, 10000, "CardID", "int", true, out totalRows).Where(d=>Banks.Contains(d=>d.)).ToList();
            var res = new StringBuilder();
            var dc = new TreasuryEntities();

            if (query.Count() != 0)
            {
                foreach (var i in query)
                {
                    res.Append("<option value='" + i.BankID + "'>" + Utility.GetBasicBankInfoWithBankId(i.BankID) + " " + Utility.GetBankInfoBankId(i.BankID).BranchName + " " + i.Card_AccountNo + "</option>");
                }
                return res.ToString();
            }
            return "";
        }
        #endregion

        /// <summary>
        /// حذف درخواست وجه در صورت عدم تایید
        /// </summary>
        /// <param name="requestId"></param>
        /// <returns></returns>
        #region DeleteRequest 
        [HttpGet]
        public PartialViewResult ShowDeleteConfirm(int requestId)
        {
            //Icls_Request_PaymentClient RP = new Icls_Request_PaymentClient();
            var db = new TreasuryEntities();
            var RequestPayment = db.Request_Payment.Find(requestId);
            //var RequestPayment = RP.SelectRow(requestId).SingleOrDefault();
            return PartialView(RequestPayment);
        }


        [HttpPost]
        public JsonResult Delete(int[] requestIds)
        {
            var db = new TreasuryEntities();
            TreasuryFilesEntities FileDB = new TreasuryFilesEntities();

            //Icls_Request_PaymentClient RP = new Icls_Request_PaymentClient();
            //Icls_Request_Payment_StatusDetailsClient PS = new Icls_Request_Payment_StatusDetailsClient();
            //Payment_ReferenceTypes_ValueClient PRV = new Payment_ReferenceTypes_ValueClient();

            //Icls_Request_Payment_BreakPriceClient BreakPriceClient = new Icls_Request_Payment_BreakPriceClient();

            //int totalRows;
            //string message = "", message1 = "";

            for (int i = 0; i < requestIds.Length; i++)
            {
                //Icls_Request_Payment_StatusDetailsClient StatusClient = new Icls_Request_Payment_StatusDetailsClient();
                //var Confirmls = StatusClient.SelectBy_DynamicWhereClause("RequestID =" + requestIds[i], 1, 100, "Request_Payment_StatusDetailsID", "int", false, out totalRows).OrderByDescending(d=>d.RequestStatusTypeID).Select(d=>d.RequestStatusTypeID);
                var RequestId = requestIds[i];
                var Status = db.Request_Payment_StatusDetails.OrderByDescending(d => d.Request_Payment_StatusDetailsID).FirstOrDefault(d => d.RequestID == RequestId);
                //var lastStatus= db.GetRequestPayment_Payment_WithLastStates("RP.RequestID =" + requestIds[i], "RP.RequestID DESC");
                var payment = db.Payments.SingleOrDefault(d => d.RequestID == RequestId);
                // درصورتیکه درخواست وجه تاییدیه ها را نگرفته باشد اجازه حذف هست
                if (Status.RequestStatusTypeID <= 2 && Status.RequestStatusTypeID >= 0 && payment == null)
                {
                    try
                    {
                        var AllStatus = db.Request_Payment_StatusDetails.Where(d => d.RequestID == RequestId);
                        //var Status = PS.SelectBy_DynamicWhereClause("RequestID=" + requestIds[i], 1, 100, "Request_Payment_StatusDetailsID", "int", true, out totalRows);
                        //var Refrences = PRV.SelectBy_DynamicWhereClause("RequestId=" + requestIds[i], 1, 100, "ReferenceTypeValueID", "int", true, out totalRows).ToList();
                        //var RequestPayment = RP.SelectRow(requestIds[i]).SingleOrDefault();
                        //var BreakPriceLs = BreakPriceClient.SelectBy_DynamicWhereClause("RequestID=" + requestIds[i], 1, 100, "BreakId", "dec", false, out totalRows);

                        var Refrences = db.Payment_ReferenceTypes_Value.Where(d => d.RequestId == RequestId);
                        var RequestPayment = db.Request_Payment.SingleOrDefault(d => d.RequestID == RequestId);
                        var BreakPriceLs = db.Request_Payment_BreakPrice.Where(d => d.RequestID == RequestId);
                        if (RequestPayment != null)
                        {
                            var File = FileDB.Files.SingleOrDefault(d => d.relatedID == RequestPayment.RequestID);
                            if (File != null)
                            {
                                FileDB.Files.Remove(File);
                                FileDB.SaveChanges();
                            }
                            db.Payment_ReferenceTypes_Value.RemoveRange(Refrences);
                            //foreach (var item in Refrences)
                            //{
                            //    db.Payment_ReferenceTypes_Value.Remove(item);
                            //    //PRV.Delete(item.ReferenceTypeValueID, out message);
                            //}

                            if (BreakPriceLs.Count() != 0)
                            {
                                db.Request_Payment_BreakPrice.RemoveRange(BreakPriceLs);
                                //foreach (var item in BreakPriceLs)
                                //{
                                //    db.Request_Payment_BreakPrice.Remove(item);
                                //    //BreakPriceClient.Delete(item.BreakId, out message);
                                //}
                            }

                            db.Request_Payment_StatusDetails.RemoveRange(AllStatus);
                            //PS.Delete(Status.Request_Payment_StatusDetailsID, out message);
                            //var result = RP.Delete(requestIds[i], out message1);
                            db.Request_Payment.Remove(RequestPayment);
                            db.SaveChanges();
                            return JsonMessage(true, "حذف با موفقیت انجام شد", null, false);
                        }
                    }
                    catch (Exception ex)
                    {
                        return JsonMessage(true, "حذف با مشکل مواجه شد", null, false);
                        throw;
                    }
                }
                else
                {
                    return JsonMessage(true, "امکان حذف وجود ندارد", null, false);
                }
            }
            return JsonMessage(false, "Error");

        }

        #endregion
        public ActionResult CancelAggregationRequest()
        {
            return View();
        }

        /// <summary>
        /// کنسل کردن درخواست
        /// </summary>
        /// <param name="RequestId"></param>
        /// <returns></returns>
        #region CancelRequest
        public ActionResult CancelRequest(int RequestId, string date = "")
        {
            var message = "";
            try
            {
                //int TotalRow;
                var DateNow = DateTime.Now;
                var db = new TreasuryEntities();
                //string message, message1, message2;
                //Icls_Request_Payment_StatusDetailsClient StatusDetailsClient = new Icls_Request_Payment_StatusDetailsClient();
                //var RequestStatus = StatusDetailsClient.SelectBy_DynamicWhereClause("RequestID =" + RequestId, 1, 10, "RequestStatusTypeID", "dec", false, out TotalRow);
                var RequestStatus = db.Request_Payment_StatusDetails.Where(d => d.RequestID == RequestId);
                //if ((RequestStatus.Last().RequestStatusTypeID <= 3 || RequestStatus.Last().RequestStatusTypeID >= 1))
                //{
                var iFTransferIsExict = db.TransferFromBackupBanks.Where(d => d.RequestID == RequestId);
                var statusls = RequestStatus.Select(d => d.RequestStatusTypeID);
                if (statusls.Contains(1) && statusls.Contains(2) && (!statusls.Contains(-2) || !statusls.Contains(-1)))
                {
                    //Icls_Request_PaymentClient RequestClient = new Icls_Request_PaymentClient();
                    //Icls_TransferFromBackupBankClient TransferClient = new Icls_TransferFromBackupBankClient();
                    //var Request = RequestClient.SelectRow(RequestId).SingleOrDefault();
                    var Request = db.Request_Payment.SingleOrDefault(d => d.RequestID == RequestId);
                    //امکان کنسل کردن درخواست های تایید شده ،تایید برای پرداخت شده است
                    //Service.Request_Payment_StatusDetailsService.Request_Payment_StatusDetails NewStatus = new Service.Request_Payment_StatusDetailsService.Request_Payment_StatusDetails();
                    DataModel.Request_Payment_StatusDetails NewStatus = new Treasury.DataModel.Request_Payment_StatusDetails();
                    NewStatus.RequestID = RequestId;
                    // درصورتی که چک بلند مدت یا انتظامی باشد باید سند بخورد در حسابداری 
                    //چک بلند مدت یا انتظامی 2و4 باید دوتا سند بخورد درحسابداری یک سند ابطال هم بخورد
                    //چون مجوز امضاها همه گرفته شده است 
                    //Icls_PermissiveSignature_CompleteProccessClient permisiveClient = new Icls_PermissiveSignature_CompleteProccessClient();
                    //var PermisiveLs = permisiveClient.SelectBy_DynamicWhereClause("RequestID =" + RequestId, 1, 10, "PermissiveSignature_CompleteProccessID", "int", false, out TotalRow);

                    //var PermisiveLs = db.PermissiveSignature_CompleteProccess.Where(d => d.RequestID == RequestId);
                    //درصورتی که چک را امضا کرده باشد
                    //و در payment ذخیره شده باشد
                    //دراین صورت چه بلند مدت چه کوتاه مدت باید سند صادر گردد
                    //درصورتی که از پشتیبان برداشت شده باشد کنسل شود
                    if (statusls.Contains(4))
                    {
                        NewStatus.RequestStatusTypeID = (decimal)RequestStatusTypes.CancelCheque;
                        NewStatus.StatusDetailDescription = "عودت چک";
                        message = "عودت چک با موفیقیت انجام شد";
                    }
                    else
                    {
                        NewStatus.RequestStatusTypeID = (decimal)RequestStatusTypes.Cancellation;
                        NewStatus.StatusDetailDescription = "ابطال چک";
                        message = "ابطال چک با موفقیت انجام شد";

                        if (iFTransferIsExict.Any())
                        {
                            //var TransferObj = iFTransferIsExict.SingleOrDefault();
                            //TransferObj.IsOk = false;
                            //TransferObj.TransferDate = null;
                            //TransferClient.Delete(iFTransferIsExict.SingleOrDefault().TransferId, out message1);
                            db.TransferFromBackupBanks.Remove(iFTransferIsExict.SingleOrDefault());

                        }
                    }


                    if (date != "")
                    {
                        NewStatus.ActionDate = date.Replace("/", string.Empty);
                    }
                    else
                    {
                        NewStatus.ActionDate = Utility.ToPersianDate(DateNow);
                    }

                    NewStatus.ActionTime = DateNow.TimeOfDay;
                    NewStatus.UserID = User.Id;
                    NewStatus.HostName = System.Net.Dns.GetHostName();
                    db.Request_Payment_StatusDetails.Add(NewStatus);

                    var Payment = db.Payments.SingleOrDefault(d => d.RequestID == RequestId);
                    if (Payment != null)
                    {
                        Payment.Canceled = true;
                    }

                    //StatusDetailsClient.Insert(NewStatus, out message);
                    //int Request_Payment_StatusDetailsID=int.Parse( message.Split(';')[1]);
                    //if (Request_Payment_StatusDetailsID != 0)
                    //{
                    //    string Mess1;
                    //    var VocherClient = new Voucher_RowClient();
                    //    var ProcedureClient = new Icls_SPsClient();
                    //    ProcedureClient.Sp_CREATE_VOUCHER_PAYMENTS(true, RequestId, User.Id, true, true);
                    //}
                    //var iFTransferIsExict = TransferClient.SelectBy_DynamicWhereClause("RequestID =" + RequestId, 1, 100, "TransferId", "Int64", false, out TotalRow);


                    //Request.Pay_TypeDetailID_source==1 چک روز است 
                    //در چک بلند و انتظامی وضعیت پرداخت شده و پرداخت در جدول paymentذخیره نمی شد

                    //if (Request.Pay_TypeDetailID_source == 1)
                    //{
                    //    Voucher_RowClient vocherClient = new Voucher_RowClient();
                    //    var VoucherLS = vocherClient.SelectBy_DynamicWhereClause("ReferenceId =" + RequestId, 1, 100, "RowId", "dec", false, out TotalRow);
                    //    foreach (var item in VoucherLS)
                    //    {
                    //        vocherClient.Delete(int.Parse(item.RowId.ToString()), out message1);
                    //    }
                    //}
                    db.SaveChanges();
                    if (iFTransferIsExict.Any())
                    {
                        return JsonMessage(true, "مبلغ این درخواست از حساب پشتیان برگشت داده شد", null, false);
                    }
                    else
                    {
                        return JsonMessage(true, message, null, false);
                    }


                    //return JsonMessage(true, message.Split(';')[0]);


                }
                return JsonMessage(false, "امکان کنسل نمودن نیست", null, false);
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

        #endregion

        #region ReturnConfirmation
        [HttpGet]
        public ActionResult ReturnConfirmation(int requestId)
        {
            var message = "";
            var db = new TreasuryEntities();
            try
            {
                var RequestStatus = db.Request_Payment_StatusDetails.Where(d => d.RequestID == requestId).OrderByDescending(d => d.Request_Payment_StatusDetailsID).FirstOrDefault();
                if (RequestStatus.RequestStatusTypeID == (decimal)RequestStatusTypes.ConfirmPayment)
                {
                    db.Request_Payment_StatusDetails.Remove(RequestStatus);
                    message = "برگشت از تایید پرداخت با موفقیت انجام شد";
                }
                if (RequestStatus.RequestStatusTypeID == (decimal)RequestStatusTypes.Confirmed)
                {
                    db.Request_Payment_StatusDetails.Remove(RequestStatus);
                    message = "برگشت از تایید درخواست با موفقیت انجام شد";
                }
                db.SaveChanges();

                return JsonMessage(true, message, null, false);
            }
            catch (Exception ex)
            {
                message = "با خطا مواجه شد";
                return JsonMessage(true, message, null, false);
            }
        }
        #endregion

        /// <summary>
        /// تایید درخواست های وجه به صورتی تکی و گروهی 
        /// </summary>
        /// <param name="requestIdList"></param>
        /// <param name="Status"> مشخص کننده این است که در چه مرحله ای ازتایید است وبرای چی می خواهد تایید شود</param>
        /// <returns></returns>
        #region AcceptRequest

        public PartialViewResult BardashtDate(int requestId, int state)
        {
            //Icls_Request_PaymentClient RP = new Icls_Request_PaymentClient();
            var db = new TreasuryEntities();
            var RequestPayment = db.Request_Payment.Find(requestId);
            //var RequestPayment = RP.SelectRow(requestId).SingleOrDefault();
            ViewBag.RequestId = requestId;
            ViewBag.State = state;
            ViewBag.MinDate = Utility.GetGregorianDate(RequestPayment.DueDate);
            return PartialView(RequestPayment);
        }

        public ActionResult Confirmation(int[] requestIds, int Status, string BardashtDate = null, Int64 BackUpPrice = 0, bool DueDate = false)
        {
            //Icls_Request_Payment_StatusDetailsClient PS = new Icls_Request_Payment_StatusDetailsClient();
            //Icls_Request_PaymentClient RequestClient = new Icls_Request_PaymentClient();
            //Icls_PaymentClient PaymentClient = new Icls_PaymentClient();
            //Icls_TransferFromBackupBankClient TransferClient = new Icls_TransferFromBackupBankClient();
            //Icls_BankClient BankClient = new Icls_BankClient();
            Icls_SPsClient dc = new Icls_SPsClient();
            var db = new TreasuryEntities();
            var message = "ثبت با موفقیت انجام شد";
            string CurrentDate = Utility.ToPersianDate(DateTime.Now);
            //int TotalRow;
            //string message = "", message1 = "", message2 = "", mess = "";
            //bool result = false;
            try
            {

                if (requestIds != null)
                {
                    for (int i = 0; i < requestIds.Length; i++)
                    {
                        //string where = "RequestID =" + requestIds[i];
                        //int totalRow;

                        //Service.Request_Payment_StatusDetailsService.Request_Payment_StatusDetails obj = new Service.Request_Payment_StatusDetailsService.Request_Payment_StatusDetails();
                        Treasury.DataModel.Request_Payment_StatusDetails obj = new Treasury.DataModel.Request_Payment_StatusDetails();
                        var persianDate = Utility.ToPersianDate(DateTime.Now);
                        obj.ActionDate = persianDate;
                        obj.ActionTime = DateTime.Now.TimeOfDay;
                        obj.RequestID = requestIds[i];
                        obj.HostName = System.Net.Dns.GetHostName();
                        obj.UserID = User.Id;
                        //بدست آوردن وضعیت های مربوط به هر درخواست وجه
                        //var AllStatus = PS.SelectBy_DynamicWhereClause(where, 1, 10000, "Request_Payment_StatusDetailsID", "int", true, out totalRow).Select(d => d.RequestStatusTypeID);
                        var AllStatus = db.Request_Payment_StatusDetails.Where(d => d.RequestID == obj.RequestID).ToList().Select(d => d.RequestStatusTypeID);

                        //var request = RequestClient.SelectRow(requestIds[i]).SingleOrDefault();
                        var request = db.Request_Payment.SingleOrDefault(d => d.RequestID == obj.RequestID);
                        if (Status == (decimal)RequestStatusTypes.Confirmed)
                        {
                            //درصورتی که تایید نشده باشد قبلا ثبت می شود
                            if (!AllStatus.Contains((decimal)RequestStatusTypes.Confirmed))
                            {
                                obj.ActionDate = request.RequestDate;
                                obj.RequestStatusTypeID = (decimal)RequestStatusTypes.Confirmed;
                                //result = PS.Insert(obj, out message);
                                db.Request_Payment_StatusDetails.Add(obj);
                                //اگر نحوه پرداخت صندوق باشد
                                if (request.Source_TypeID == 2)
                                {
                                    Treasury.DataModel.Payment payment = new Treasury.DataModel.Payment();
                                    payment.RequestID = request.RequestID;
                                    payment.PaymentDate = request.RequestDate;
                                    payment.HostName = System.Net.Dns.GetHostName();
                                    payment.UserID = User.Id;
                                    payment.Effective = true;
                                    db.Payments.Add(payment);

                                    Treasury.DataModel.Request_Payment_StatusDetails objConfirmPayment = new Treasury.DataModel.Request_Payment_StatusDetails();
                                    objConfirmPayment.ActionDate = payment.PaymentDate;
                                    objConfirmPayment.RequestID = request.RequestID;
                                    objConfirmPayment.HostName = System.Net.Dns.GetHostName();
                                    objConfirmPayment.ActionTime = DateTime.Now.TimeOfDay;
                                    objConfirmPayment.UserID = User.Id;
                                    objConfirmPayment.RequestStatusTypeID = (decimal)RequestStatusTypes.ConfirmPayment;
                                    db.Request_Payment_StatusDetails.Add(objConfirmPayment);


                                    Treasury.DataModel.Request_Payment_StatusDetails objPaid = new Treasury.DataModel.Request_Payment_StatusDetails();
                                    objPaid.ActionDate = payment.PaymentDate;
                                    objPaid.RequestID = request.RequestID;
                                    objPaid.HostName = System.Net.Dns.GetHostName();
                                    objPaid.ActionTime = DateTime.Now.TimeOfDay;
                                    objPaid.UserID = User.Id;
                                    objPaid.RequestStatusTypeID = (decimal)RequestStatusTypes.Paid;
                                    db.Request_Payment_StatusDetails.Add(objPaid);


                                    Treasury.DataModel.Request_Payment_StatusDetails objDelivered = new Treasury.DataModel.Request_Payment_StatusDetails();
                                    objDelivered.ActionDate = payment.PaymentDate;
                                    objDelivered.RequestID = request.RequestID;
                                    objDelivered.HostName = System.Net.Dns.GetHostName();
                                    objDelivered.ActionTime = DateTime.Now.TimeOfDay;
                                    objDelivered.UserID = User.Id;
                                    objDelivered.RequestStatusTypeID = (decimal)RequestStatusTypes.Delivered;
                                    db.Request_Payment_StatusDetails.Add(objDelivered);

                                    //obj.RequestStatusTypeID = (decimal)RequestStatusTypes.ConfirmPayment;
                                    //result = PS.Insert(obj, out message);
                                    //db.Request_Payment_StatusDetails.Add(obj);

                                    //obj.RequestStatusTypeID = (decimal)RequestStatusTypes.Paid;
                                    //result = PS.Insert(obj, out message);
                                    //db.Request_Payment_StatusDetails.Add(obj);

                                    //obj.RequestStatusTypeID = (decimal)RequestStatusTypes.Delivered;
                                    //result = PS.Insert(obj, out message);
                                    //db.Request_Payment_StatusDetails.Add(obj);
                                    //Service.PaymentService.Payment payment = new Service.PaymentService.Payment();

                                    //var Result = PaymentClient.Insert(payment, out message);
                                }
                            }
                        }
                        else if (Status == (decimal)RequestStatusTypes.ConfirmPayment)
                        {
                            //پرداخت ساتنا33
                            //پرداخت اینترنتی32
                            //پرداخت کارت8 
                            if (request.Pay_TypeDetailID_source == 33 || request.Pay_TypeDetailID_source == 32 || request.Pay_TypeDetailID_source == 8)
                            {
                                obj.ActionDate = request.DueDate;
                            }
                            //درصورتی که تایید درخواست کرده باشد  و قبلا تایید نکرده باشد می تواند تایید پرداخت نماید
                            if (AllStatus.Contains((decimal)RequestStatusTypes.Confirmed) && !AllStatus.Contains((decimal)RequestStatusTypes.ConfirmPayment))
                            {
                                obj.RequestStatusTypeID = (decimal)RequestStatusTypes.ConfirmPayment;
                                //result = PS.Insert(obj, out message);
                                db.Request_Payment_StatusDetails.Add(obj);
                            }

                        }
                        else if (Status == (decimal)RequestStatusTypes.Paid)
                        {
                            //در صورتی که چک مدت دار باشد و پرداخت باید زمان سررسید چک انجام شود
                            //درصورتی که چک نقل و انتقالی باشد که سررسید تاریخ روز نباشد
                            //request.Pay_TypeDetailID_source==4 چک مدت دار
                            //request.Pay_TypeDetailID_source==5 چک مدت دار
                            //ازکنسول ابتدای برنامه می آید اینجا
                            //request.Pay_TypeDetailID_source==2 چک انتظامی پرداخت نشده است
                            //Pay_TypeDetailID_source==7 پرداخت نقدی
                            //Pay_TypeDetailID_source==32 پرداخت اینترنتی
                            //Pay_TypeDetailID_source==8 پرداخت کارت
                            //درصورتی که پرداخت نقدی باشد یا پرداخت با کارت هنگام استتوس پرداخت ،پرداخت هم صورت میگیرد
                            if (((request.Pay_TypeDetailID_source == 4 || request.Pay_TypeDetailID_source == 5) && DueDate) || request.Pay_TypeDetailID_source == 8 || request.Pay_TypeDetailID_source == 32 || request.Pay_TypeDetailID_source == 33)
                            {
                                //بررسی وجود موجودی بانک و درصورت پشتیبان موجودی پشتیبان
                                var Date = Utility.ToPersianDate(DateTime.Now);
                                var BankID = request.Source_PaymentValue;

                                var Bank = dc.Sp_GetBankBalance("13000101", Date, Int32.Parse(BankID), 1, false).FirstOrDefault();
                                //var BackupBank = BankClient.SelectBy_DynamicWhereClause("Backup_BankID =" + Bank.BankID, 1, 10, "BankID", "int", false, out totalRows);
                                var BackupBank = db.Banks.Where(d => d.Backup_BankID == Bank.BankID);
                                GetBankBalance_Result Backup_Bank = new GetBankBalance_Result();
                                var ChequePrice = request.PayAmount;

                                if (BackupBank.Count() != 0)
                                {
                                    if (BackupBank.Count() != 0)
                                    {
                                        //$Sp_GetBankBalance$
                                        var qury = dc.Sp_GetBankBalance("13000101", Date, BackupBank.SingleOrDefault().BankID, 1, false);
                                        Backup_Bank = qury.FirstOrDefault();
                                    }
                                }


                                if (ChequePrice <= Bank.balance || (ConfigurationManager.AppSettings["CompletePayBackup"] == "True" && Backup_Bank.balance >= ChequePrice && (request.Pay_TypeDetailID_source == 8 || request.Pay_TypeDetailID_source == 32 || request.Pay_TypeDetailID_source == 33)))
                                {
                                    //"موجودی کافی است";


                                    //save payment
                                    //var PAymentOBj = PaymentClient.SelectBy_DynamicWhereClause("RequestID =" + request.RequestID, 1, 10, "PaymentID", "dec", false, out totalRow);
                                    var PAymentOBj = db.Payments.Where(d => d.RequestID == request.RequestID);
                                    if (!PAymentOBj.Any())
                                    {

                                        //var AnyTransfer = TransferClient.SelectBy_DynamicWhereClause("RequestID ="+request.RequestID, 1, 100, "TransferId", "Int64", false,out totalRow).Any();
                                        //پرداخت از پشتیبان
                                        //دریافت به حساب جاری
                                        //Service.PaymentService.Payment payment = new Service.PaymentService.Payment();
                                        Treasury.DataModel.Payment payment = new Treasury.DataModel.Payment();
                                        payment.RequestID = request.RequestID;
                                        if (!string.IsNullOrEmpty(BardashtDate))
                                            payment.PaymentDate = BardashtDate.Replace("/", string.Empty);
                                        else
                                            payment.PaymentDate = Utility.ToPersianDate(DateTime.Now);
                                        if (request.Pay_TypeDetailID_source == 7 || request.Pay_TypeDetailID_source == 8 || request.Pay_TypeDetailID_source == 32 || request.Pay_TypeDetailID_source == 33)
                                        {
                                            payment.PaymentDate = request.DueDate;
                                        }
                                        payment.HostName = System.Net.Dns.GetHostName();
                                        payment.UserID = User.Id;
                                        payment.Effective = true;
                                        //var Result = PaymentClient.Insert(payment, out message);
                                        db.Payments.Add(payment);
                                        if (ConfigurationManager.AppSettings["CompletePayBackup"] == "True" && Backup_Bank.BankID != 0 && Bank.BankID != 0)
                                        {
                                            if ((request.Pay_TypeDetailID_source != 32 && request.Pay_TypeDetailID_source != 33 && request.Pay_TypeDetailID_source != 8 && request.Pay_TypeDetailID_source != 7))
                                            {
                                                //Service.TransferFromBackupBankService.TransferFromBackupBank TransferFromBackupBankOBJ = new Service.TransferFromBackupBankService.TransferFromBackupBank();
                                                Treasury.DataModel.TransferFromBackupBank TransferFromBackupBankOBJ = new Treasury.DataModel.TransferFromBackupBank();
                                                TransferFromBackupBankOBJ.TransferDate = payment.PaymentDate;
                                                TransferFromBackupBankOBJ.From_BankID = Backup_Bank.BankID;
                                                TransferFromBackupBankOBJ.To_BankID = Bank.BankID;
                                                TransferFromBackupBankOBJ.Price = BackUpPrice;
                                                TransferFromBackupBankOBJ.RequestID = request.RequestID;
                                                TransferFromBackupBankOBJ.IsOk = true;
                                                db.TransferFromBackupBanks.Add(TransferFromBackupBankOBJ);
                                                //TransferClient.Insert(TransferFromBackupBankOBJ, out mess);
                                            }

                                        }
                                        if (request.Pay_TypeDetailID_source == 32 || request.Pay_TypeDetailID_source == 33 || request.Pay_TypeDetailID_source == 8 || request.Pay_TypeDetailID_source == 7 || request.Pay_TypeDetailID_source == 4)
                                        {
                                            Treasury.DataModel.Request_Payment_StatusDetails objPaid = new Treasury.DataModel.Request_Payment_StatusDetails();
                                            objPaid.ActionDate = payment.PaymentDate;
                                            objPaid.RequestID = request.RequestID;
                                            objPaid.HostName = System.Net.Dns.GetHostName();
                                            objPaid.ActionTime = DateTime.Now.TimeOfDay;
                                            objPaid.UserID = User.Id;
                                            objPaid.RequestStatusTypeID = (decimal)RequestStatusTypes.Paid;
                                            db.Request_Payment_StatusDetails.Add(objPaid);

                                            //result = PS.Insert(obj, out message);
                                        }
                                        //درصورتی که تایید درخواست کرده باشد  و قبلا تایید نکرده باشد می تواند تایید پرداخت نماید
                                        if (AllStatus.Contains((decimal)RequestStatusTypes.Confirmed) && !AllStatus.Contains((decimal)RequestStatusTypes.Paid) && !DueDate)
                                        {
                                            //پرداخت ساتنا
                                            //پرداخت کارت
                                            //پرداخت اینترنتی
                                            //obj.RequestStatusTypeID = (decimal)RequestStatusTypes.Paid;
                                            ////result = PS.Insert(obj, out message);
                                            //db.Request_Payment_StatusDetails.Add(obj);
                                            if (request.Pay_TypeDetailID_source == 33 || request.Pay_TypeDetailID_source == 32 || request.Pay_TypeDetailID_source == 8)
                                            {
                                                Treasury.DataModel.Request_Payment_StatusDetails objDelivered = new Treasury.DataModel.Request_Payment_StatusDetails();
                                                objDelivered.ActionDate = payment.PaymentDate;
                                                objDelivered.RequestID = request.RequestID;
                                                objDelivered.HostName = System.Net.Dns.GetHostName();
                                                objDelivered.ActionTime = DateTime.Now.TimeOfDay;
                                                objDelivered.UserID = User.Id;
                                                objDelivered.RequestStatusTypeID = (decimal)RequestStatusTypes.Delivered;
                                                db.Request_Payment_StatusDetails.Add(objDelivered);


                                                //result = PS.Insert(obj, out message);
                                            }
                                        }
                                    }
                                    //if (result)
                                    //    return JsonMessage(true, "برداشت با موفقیت انجام شد", null, false);
                                }
                                else if (BackupBank.Count() != 0 && ConfigurationManager.AppSettings["CompletePayBackup"] == "False")
                                {

                                    //اگر بانک حساب پشتیبان داشته باشد
                                    var remain = ChequePrice - Bank.balance;
                                    if (Bank.balance < 0)
                                    {
                                        return JsonMessage(false, "موجودی حساب جاری کمتر از صفر میباشد!", null, false);
                                    }

                                    //اگر موجودی حساب پشتیبان بیشتر یا مساوی مبلغ کمبود حساب جاری است
                                    if (Backup_Bank.balance >= remain)
                                    {
                                        var Transferobj = db.TransferFromBackupBanks.Where(d => d.RequestID == request.RequestID);
                                        //var Transferobj = TransferClient.SelectBy_DynamicWhereClause("RequestID=" + request.RequestID, 1, 10, "TransferId", "int64", false, out totalRows);
                                        if (!Transferobj.Any())
                                        {
                                            //پرداخت از پشتیبان
                                            //دریافت به حساب جاری
                                            Treasury.DataModel.TransferFromBackupBank TransferFromBackupBankOBJ = new Treasury.DataModel.TransferFromBackupBank();
                                            TransferFromBackupBankOBJ.TransferDate = Utility.ToPersianDate(DateTime.Now);
                                            TransferFromBackupBankOBJ.From_BankID = Backup_Bank.BankID;
                                            TransferFromBackupBankOBJ.To_BankID = Bank.BankID;
                                            TransferFromBackupBankOBJ.Price = (long)remain;
                                            TransferFromBackupBankOBJ.RequestID = request.RequestID;
                                            TransferFromBackupBankOBJ.IsOk = false;
                                            db.TransferFromBackupBanks.Add(TransferFromBackupBankOBJ);
                                            //TransferClient.Insert(TransferFromBackupBankOBJ, out mess);
                                        }

                                        //Treasury.DataModel.Payment payment = new Treasury.DataModel.Payment();
                                        //payment.RequestID = request.RequestID;
                                        //if (!string.IsNullOrEmpty(BardashtDate))
                                        //    payment.PaymentDate = BardashtDate.Replace("/", string.Empty);
                                        //else
                                        //    payment.PaymentDate = Utility.ToPersianDate(DateTime.Now);
                                        //payment.HostName = System.Net.Dns.GetHostName();
                                        //payment.UserID = User.Id;
                                        //payment.Effective = true;
                                        ////var Result = PaymentClient.Insert(payment, out message);
                                        //db.Payments.Add(payment);
                                        db.SaveChanges();
                                        return JsonMessage(false, "موجودی حساب جاری کافی نیست .درصورت 'تایید نقل و انتقال از پشتیبان' مبلغ از حساب پشتیبان کسر می گردد", null, false);
                                    }
                                    else
                                    {
                                        return JsonMessage(false, "موجودی حساب جاری و پشتیبان کافی نیست.امکان صدور چک نیست", null, false);
                                    }
                                }
                                else if (ChequePrice >= Bank.balance)
                                {
                                    return JsonMessage(false, "موجودی کافی نیست", null, false);
                                }
                            }

                        }
                        else if (Status == (decimal)RequestStatusTypes.Delivered)
                        {
                            //درصورتی که تایید درخواست  قبلا تایید نشده باشد می تواند تایید تحویل نماید
                            if (AllStatus.Contains((decimal)RequestStatusTypes.ConfirmPayment) && AllStatus.Contains((decimal)RequestStatusTypes.Confirmed) && !AllStatus.Contains((decimal)RequestStatusTypes.Delivered))
                            {
                                //چک مدت دارrequest.Pay_TypeDetailID_source == 4
                                //چک انتظامیrequest.Pay_TypeDetailID_source == 2
                                //درصورتی که چک مدت دارو انتظامی نباشد
                                //برداشت در زمان تحویل ست شده باشد در کانفیگ 
                            
                                if (request.Pay_TypeDetailID_source != 4 && request.Pay_TypeDetailID_source != 2 && ConfigurationManager.AppSettings["PayWhenCheckDelivered"] == "True")
                                {

                                    //درصورتی که از حساب پشتیبان انتقال داده باشد در جدول نقل و انتقال ذخیره می شود
                                    //var Request = RequestClient.SelectRow(requestIds[i]).SingleOrDefault();
                                    var Request = db.Request_Payment.SingleOrDefault(d => d.RequestID == obj.RequestID);
                                    var BankID = Request.Source_PaymentValue;
                                    var Date = Utility.ToPersianDate(DateTime.Now);
                                    //$Sp_GetBankBalance$
                                    var query = dc.Sp_GetBankBalance("13000101", Date, Int32.Parse(BankID), 1, false);
                                    var Bank = new GetBankBalance_Result();
                                    //if (query.Count() == 1)
                                    //{
                                    Bank = query.FirstOrDefault();
                                    //}
                                    //else
                                    //{
                                    //    Bank = query.LastOrDefault();
                                    //}

                                    var ChequePrice = Request.PayAmount;
                                    //var BackupBank = BankClient.SelectBy_DynamicWhereClause("Backup_BankID =" + Bank.BankID, 1, 10, "BankID", "int", false, out totalRow);
                                    var BackupBank = db.Banks.Where(d => d.Backup_BankID == Bank.BankID);
                                    if ((ChequePrice <= Bank.balance && (ConfigurationManager.AppSettings["CompletePayBackup"] == "True" && BackupBank.Count() == 0))
                                               || (ChequePrice <= Bank.balance && (ConfigurationManager.AppSettings["CompletePayBackup"] == "False")))

                                    {
                                        //درصورتی که حساب جاری موجودی کافی نداشته باشد
                                        //وحساب جاری پشتیبان داشته باشد
                                        if ((BackupBank.Count() != 0 && ChequePrice > Bank.balance) || (BackupBank.Count() != 0 && ConfigurationManager.AppSettings["CompletePayBackup"] == "True"))
                                        {
                                            GetBankBalance_Result Backup_Bank = new GetBankBalance_Result();

                                            if (BackupBank.Count() != 0)
                                            {
                                                //$Sp_GetBankBalance$
                                                var qury = dc.Sp_GetBankBalance("13000101", Date, BackupBank.SingleOrDefault().BankID, 1, false);

                                                //if (query.Count() == 1)
                                                //{
                                                Backup_Bank = qury.FirstOrDefault();
                                                //}
                                                //else
                                                //{
                                                //    Backup_Bank = qury.LastOrDefault();
                                                //}
                                            }
                                            //var Sumbalance = Bank.balance + Backup_Bank.balance;
                                            var remain = ChequePrice - Bank.balance;
                                            //اگر موجودی حساب پشتیبان بیشتر یا مساوی مبلغ کمبود حساب جاری است
                                            if (Backup_Bank.balance >= remain)
                                            {
                                                //var Transferobj = TransferClient.SelectBy_DynamicWhereClause("RequestID=" + requestIds[i], 1, 10, "TransferId", "int64", false, out totalRow);
                                                var Transferobj = db.TransferFromBackupBanks.Where(d => d.RequestID == obj.RequestID);
                                                if (!Transferobj.Any())
                                                {
                                                    //پرداخت از پشتیبان
                                                    //دریافت به حساب جاری

                                                    Treasury.DataModel.TransferFromBackupBank TransferFromBackupBankOBJ = new Treasury.DataModel.TransferFromBackupBank();
                                                    TransferFromBackupBankOBJ.TransferDate = Utility.ToPersianDate(DateTime.Now);
                                                    TransferFromBackupBankOBJ.From_BankID = Backup_Bank.BankID;
                                                    TransferFromBackupBankOBJ.To_BankID = Bank.Backup_BankID.Value;
                                                    TransferFromBackupBankOBJ.Price = (long)remain;
                                                    TransferFromBackupBankOBJ.RequestID = requestIds[i];
                                                    //TransferClient.Insert(TransferFromBackupBankOBJ, out mess);
                                                    db.TransferFromBackupBanks.Add(TransferFromBackupBankOBJ);
                                                }
                                            }
                                        }
                                    }

                                    obj.RequestStatusTypeID = (decimal)RequestStatusTypes.Paid;
                                    //result = PS.Insert(obj, out message);
                                    db.Request_Payment_StatusDetails.Add(obj);
                                    if (request.Pay_TypeDetailID_source != 1)
                                    {
                                        //save payment
                                        Treasury.DataModel.Payment payment = new Treasury.DataModel.Payment();
                                        payment.RequestID = requestIds[i];
                                        payment.PaymentDate = Utility.ToPersianDate(DateTime.Now);
                                        payment.HostName = System.Net.Dns.GetHostName();
                                        payment.UserID = User.Id;
                                        payment.Effective = true;
                                        db.Payments.Add(payment);
                                        //var Result = PaymentClient.Insert(payment, out message1);
                                    }

                                }

                                //درصورتی که درخواست وجه ای باشد که از تجمیع درخواست وجه های تایید پرداخت شده حاصل شده باشد
                                //string whereclouse = "AggrigateOrDuplicationID=" + requestIds[i] + " and AggrigateOrDuplicationStatusID=7 and Paid!=NULL and Delivered=NULL";
                                //var queryls1 = RequestClient.GetAllRequestStatus_DynamicWhereClause(CurrentDate, whereclouse, 1, 200, "RequestID", "dec", false, out totalRow).ToList();
                                //var queryls1 = db.GetAllRequestStatus(CurrentDate).Where(d => d.AggrigateOrDuplicationID == requestIds[i] && d.AggrigateOrDuplicationStatusID == 7 && d.Paid != null && d.Delivered == null);
                                //if (queryls1.Count() != 0)
                                //{
                                //    foreach (var item in queryls1)
                                //    {
                                //        //ثبت وضعیت پرداخت شده
                                //        Treasury.DataModel.Request_Payment_StatusDetails obj2 = new Treasury.DataModel.Request_Payment_StatusDetails();
                                //        obj2.ActionDate = persianDate;
                                //        obj2.ActionTime = DateTime.Now.TimeOfDay;
                                //        obj2.RequestID = item.RequestID;
                                //        obj2.UserID = User.Id;
                                //        obj2.HostName = System.Net.Dns.GetHostName();
                                //        obj2.RequestStatusTypeID = (decimal)RequestStatusTypes.Delivered;
                                //        db.Request_Payment_StatusDetails.Add(obj2);
                                //        //PS.Insert(obj2, out message);
                                //    }
                                //}

                                obj.RequestStatusTypeID = (decimal)RequestStatusTypes.Delivered;
                                db.Request_Payment_StatusDetails.Add(obj);
                                //result = PS.Insert(obj, out message);

                            }
                            //else
                            //{
                            //    result = false;
                            //}
                        }
                        else if (Status == (decimal)RequestStatusTypes.Pendding)
                        {
                            //درصورتی که تایید درخواست کرده باشد  و قبلا تایید نکرده باشد می تواند تایید پرداخت نماید

                            obj.RequestStatusTypeID = (decimal)RequestStatusTypes.Pendding;
                            //db.Request_Payment_StatusDetails.Add(obj);
                            db.Request_Payment_StatusDetails.Add(obj);
                        }
                        else if (Status == (decimal)RequestStatusTypes.Cancellation)
                        {
                            //  کنسل کردن درخواست وجه
                            //درصورت کنسل کردن درخواست وجه از چرخه کاری خارج می شود        
                            obj.RequestStatusTypeID = (decimal)RequestStatusTypes.Cancellation;
                            //result = PS.Insert(obj, out message);
                            db.Request_Payment_StatusDetails.Add(obj);
                        }

                    }

                }
                else
                {
                    return JsonMessage(false, "لطفا درخواست وجه مورد نظررا انتخاب نمایید", null, false);
                }
                db.SaveChanges();
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
        /// <summary>
        /// ویرایش درخواست وجه در صورت عدم تایید
        /// </summary>
        /// <param name="requestId"></param>
        /// <returns></returns>
        #region EditRequest

        [HttpGet]
        public ActionResult Edit(int requestId, int pageIndex, bool limitation = false)
        {
            int TotalRow;
            var db = new TreasuryEntities();
            Icls_Request_PaymentClient RP = new Icls_Request_PaymentClient();
            Icls_Request_Payment_StatusDetailsClient StatusClient = new Icls_Request_Payment_StatusDetailsClient();
            var Confirmls = StatusClient.SelectBy_DynamicWhereClause("RequestID =" + requestId, 1, 100, "Request_Payment_StatusDetailsID", "int", false, out TotalRow);
            // درصورتیکه درخواست وجه تاییدیه ها را نگرفته باشد اجازه ویرایش هست
            if (Confirmls.Count() <= 1)
            {
                Icls_Request_Payment_BreakPriceClient BreakPriceClient = new Icls_Request_Payment_BreakPriceClient();
                ViewBag.PageIndex = pageIndex;
                ViewBag.limitation = limitation;

                //نوع ارزها
                ViewBag.ExchangeLs = Utility.GetExchangeLs();
                //انواع عطف مبانی پرداخت
                ViewBag.ReferenceTypels = Utility.GetReferenceTypeLs();

                //نمایش لیستی از انواع پرداخت
                ViewBag.Receipt_Pay_TypeDetailLs = Utility.GetReceipt_Pay_TypeDetailLs().Where(d => d.Receipt_Pay_TypeID == 1);
                ViewBag.DisablePay_TypeDetailID_source = false;



                //var RequestPayment = RP.SelectRow(requestId).SingleOrDefault();
                var RequestPayment = db.Request_Payment.Find(requestId);
                if (int.Parse(ConfigurationManager.AppSettings["BehalfId"]) == RequestPayment.BehalfID)
                {
                    ViewBag.DisablePay_TypeDetailID_source = true;
                }
                //var BreakPriceLs = BreakPriceClient.SelectBy_DynamicWhereClause("RequestID=" + requestId, 1, 100, "BreakId", "dec", false, out TotalRow);
                //if (BreakPriceLs != null)
                //{
                //    ViewBag.Request_Payment_BreakPriceLs = BreakPriceLs.ToList();
                //}

                //لیستی از مبدا و منبع 
                ViewBag.SourceDestinationTypesLs = Utility.GetSourceDestinationTypesLs().Where(d => d.Source_DestinationTypeID == RequestPayment.Source_TypeID);

                Icls_BehalfsClient obj = new Icls_BehalfsClient();
                var BehalfGroupID = obj.SelectRow(RequestPayment.BehalfID).SingleOrDefault().BehalfGroupID;

                // بابت ها
                ViewBag.BehalfsLs = Utility.GetBehalfLs(BehalfGroupID);

                //گروه بابت ها
                ViewBag.BehalfGroupId = BehalfGroupID;
                ViewBag.BehalfsGroupLs = Utility.GetBehalfsGroupLs().Where(d => d.Receipt_Pay_TypeID == int.Parse(ConfigurationManager.AppSettings["Payment_Type_ID"]));

                var dc = new TreasuryEntities();
                //نوع ذینفع  
                var id = RequestPayment.Unique_Account_No;
                var unique_account = dc.v_unique_accounts.SingleOrDefault(d => d.Account_Code == id);
                var unique_account2 = dc.v_unique_accounts.SingleOrDefault(d => d.Account_Code == RequestPayment.Main_Unique_Account_No);
                if (unique_account == null)
                {
                    ViewBag.uniqueTypeId = 0;
                    ViewBag.uniqueAccount_Code = "---";
                    ViewBag.uniqueAccount_Name = "---";
                }
                else
                {
                    if (unique_account.Type_Code == null)
                    {
                        //درصورتی که نوع ذینفع نداشته باشد
                        ViewBag.uniqueTypeId = 0;
                    }
                    else
                    {
                        //برگرداندن مقدار نوع ذینفع برای نمایش
                        ViewBag.uniqueTypeId = unique_account.Type_Code.Trim();
                        //ViewBag.uniqueAccount_Code = unique_account.Account_Code;
                        //ViewBag.uniqueAccount_Name = unique_account.Account_Name;
                        ViewBag.SlectedLs = from p in dc.v_unique_accounts
                                            where p.Type_Code == unique_account.Type_Code
                                            select new
                                            {
                                                Account_Code = p.Account_Code,
                                                Account_Name = p.Account_Name + "/" + p.Account_Code
                                            };
                        //ViewBag.uniqueTypeId2 = unique_account2 != null ? unique_account2.Type_Code.Trim() : "";
                        //ViewBag.uniqueAccount_Code2 = unique_account2 != null ? unique_account2.Account_Code : "";
                        //ViewBag.uniqueAccount_Name2 = unique_account2 != null ? unique_account2.Account_Name : "";
                    }
                }
                if (unique_account2 == null)
                {
                    ViewBag.uniqueTypeId2 = null;
                    ViewBag.uniqueAccount_Code2 = "---";
                    ViewBag.uniqueAccount_Name2 = "---";
                }
                else
                {
                    if (unique_account2.Type_Code == null)
                    {
                        //درصورتی که نوع ذینفع نداشته باشد
                        ViewBag.uniqueTypeId2 = 0;
                    }
                    else
                    {
                        //برگرداندن مقدار نوع ذینفع برای نمایش
                        ViewBag.uniqueTypeId2 = unique_account2.Type_Code.Trim();
                        ////ViewBag.uniqueAccount_Code2 = unique_account.Account_Code;
                        ////ViewBag.uniqueAccount_Name2 = unique_account.Account_Name;
                        ViewBag.SlectedLsMain = from p in dc.v_unique_accounts
                                                where p.Type_Code == unique_account2.Type_Code
                                                select new
                                                {
                                                    Account_Code = p.Account_Code,
                                                    Account_Name = p.Account_Name + "/" + p.Account_Code
                                                };
                    }
                }


                //ذیفع ها 
                ViewBag.unique_accounts = dc.v_unique_accounts.Where(d => d.Type_Code == unique_account.Type_Code);

                //ذینفع اصلی  
                ViewBag.accounts = dc.v_accounts.ToList();

                //نوع ذینفع  
                var unique_account_type = dc.v_unique_account_type.ToList();
                ViewBag.unique_account_type2 = unique_account_type;
                unique_account_type.Add(new v_unique_account_type { Code = 0, Description = Resourse.Resources.Others });
                ViewBag.unique_account_type = unique_account_type;

                //ذینفع اصلی 
                ViewBag.v_accounts = dc.v_accounts.ToList();

                if (RequestPayment.Pay_TypeDetailID_source==7 && RequestPayment.ExchangeRate != 0)
                {
                    ViewBag.PaymentValueLs = GetCashDeskArzi();
                }
                else if ((RequestPayment.Pay_TypeDetailID_source == 7 && RequestPayment.ExchangeRate == 0))
                {
                    ViewBag.PaymentValueLs = GetCashDesk();
                }
                else
                {
                    if (RequestPayment.ExchangeRate==0)
                
                    {
                        ViewBag.PaymentValueLs = GetBanks(RequestPayment.Source_PaymentValue != null ? int.Parse(RequestPayment.Source_PaymentValue) : 0);
                    }
                    else
                    {
                        ViewBag.PaymentValueLs = GetBanks(RequestPayment.Source_PaymentValue != null ? int.Parse(RequestPayment.Source_PaymentValue) : 0,false,"arzi");
                    }
                }

                
                if (RequestPayment.Destination_PaymentValue != null)
                {
                    ViewBag.DestinationValueLs = GetBanks(RequestPayment.Source_PaymentValue != null ? int.Parse(RequestPayment.Destination_PaymentValue) : 0, true);
                }

                return View(RequestPayment);
            }

            return RedirectToAction("Index");
        }
        [HttpPost]
        public ActionResult Edit(CSD.Treasury.DataModel.Request_Payment obj, int pageIndex, FormCollection frm)
        {
            int totalrow;
            var db = new TreasuryEntities();
            Icls_Request_Payment_StatusDetailsClient StatusClient = new Icls_Request_Payment_StatusDetailsClient();
            var Confirmls = StatusClient.SelectBy_DynamicWhereClause("RequestID =" + obj.RequestID, 1, 100, "Request_Payment_StatusDetailsID", "int", false, out totalrow);
            // درصورتیکه درخواست وجه تاییدیه ها را نگرفته باشد اجازه ویرایش هست

            string Message, Message1 = "", DellMessage = "";
            Icls_Request_PaymentClient RP = new Icls_Request_PaymentClient();
            Payment_ReferenceTypes_ValueClient PRV = new Payment_ReferenceTypes_ValueClient();
            Icls_Request_Payment_BreakPriceClient BreakPriceClient = new Icls_Request_Payment_BreakPriceClient();

            Service.Payment_ReferenceTypes_ValueService.Payment_ReferenceTypes_Value ReferenceTypes_Value = new Service.Payment_ReferenceTypes_ValueService.Payment_ReferenceTypes_Value();
            if (Confirmls.Count() <= 1)
            {
                //var RequestPayment = RP.SelectRow(obj.RequestID).SingleOrDefault();
                var RequestPayment = db.Request_Payment.Find(obj.RequestID);
                //var count = RP.SelectRow(obj.RequestID).Count();
                if (ModelState.IsValid)
                {
                    try
                    {
                        //حذف نمودن عطف های بابت و ثبت مجدد آنها 
                        string where = "RequestId =" + obj.RequestID;
                        int totalRows;
                        var ReferenceTypeValueLs = PRV.SelectBy_DynamicWhereClause(where, 1, 100, "ReferenceTypeValueID", "int", true, out totalRows);
                        foreach (var item in ReferenceTypeValueLs)
                        {
                            PRV.Delete(item.ReferenceTypeValueID, out Message);
                        }
                        //ثبت عطف های بابت 

                        foreach (var item in frm.AllKeys)
                        {
                            if (item.StartsWith("Reference_") && frm[item] != "")
                            {

                                var array = item.Split('_');
                                ReferenceTypes_Value.RequestId = obj.RequestID;
                                ReferenceTypes_Value.ReferenceID = int.Parse(array[1]);
                                ReferenceTypes_Value.ReferenceValue = frm[item];
                                var R = PRV.Insert(ReferenceTypes_Value, out Message);
                            }
                        }

                        //افزودن نرخ چند ارزی برای پرداخت نقدی
                        if (obj.Request_Payment_BreakPrice != null)
                        {
                            //حذف نرخ چندارزی
                            var BreakPriceLs = BreakPriceClient.SelectBy_DynamicWhereClause("RequestID=" + obj.RequestID, 1, 100, "BreakId", "dec", false, out totalRows);
                            foreach (var item in BreakPriceLs)
                            {
                                BreakPriceClient.Delete(item.BreakId, out DellMessage);
                            }


                            //افزودن
                            Service.Request_Payment_BreakPriceService.Request_Payment_BreakPrice BreakPriceObj = new Service.Request_Payment_BreakPriceService.Request_Payment_BreakPrice();
                            foreach (var item in obj.Request_Payment_BreakPrice)
                            {
                                BreakPriceObj.RequestID = obj.RequestID;
                                BreakPriceObj.Exchange_Rate = item.Exchange_Rate;
                                BreakPriceObj.Exchange_Amount = item.Exchange_Amount;
                                BreakPriceObj.ExchangeTypeID = item.ExchangeTypeID.Value;
                                BreakPriceObj.Price_Rial = item.Exchange_Amount * item.Exchange_Rate;
                                BreakPriceClient.Insert(BreakPriceObj, out Message1);
                            }
                        }
                        //decimal payAmount = 0;
                        //foreach (var item in obj.Request_Payment_BreakPrice)
                        //{
                        //    payAmount += (item.Exchange_Rate * item.Exchange_Amount);
                        //}
                        //obj.PayAmount = payAmount;
                        
                        RequestPayment.Main_Unique_Account_No = obj.Main_Unique_Account_No;
                        RequestPayment.Unique_Account_No = obj.Unique_Account_No;
                        RequestPayment.BehalfID = obj.BehalfID;
                        RequestPayment.Priority = obj.Priority;
                        RequestPayment.PayAmount = obj.PayAmount;
                        RequestPayment.ExchangeRate = obj.ExchangeRate;
                        RequestPayment.ExchangeTypeID = obj.ExchangeTypeID;
                        RequestPayment.Pay_TypeDetailID_source = obj.Pay_TypeDetailID_source;

                        if (obj.Source_TypeID != null)
                        {
                            RequestPayment.Source_TypeID = obj.Source_TypeID;
                        }
                        if (obj.Source_PaymentValue != null)
                        {
                            RequestPayment.Source_PaymentValue = obj.Source_PaymentValue;
                            RequestPayment.BankID = int.Parse(obj.Source_PaymentValue);
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
                        //var result = RP.Update(RequestPayment, out Message1);
                        //return JsonMessage(result, Message);               
                        return RedirectToAction("Index", "Request", new { PageIndex = pageIndex });
                    }
                    catch (Exception ex)
                    {
                        //return JsonMessage(false, "Error");
                        return View(obj);
                    }

                }
                //return JsonMessage(false, "Error");
                return View(obj);
            }
            return RedirectToAction("Index");
        }

        #endregion


        #region CopyRequest
        [HttpGet]
        public ActionResult Copy(int requestId)
        {
            int totalRow;
            var db = new TreasuryEntities();
            //نوع ارزها
            ViewBag.ExchangeLs = Utility.GetExchangeLs();
            //انواع عطف مبانی پرداخت
            ViewBag.ReferenceTypels = Utility.GetReferenceTypeLs();

            //نمایش لیستی از انواع پرداخت
            ViewBag.Receipt_Pay_TypeDetailLs = Utility.GetReceipt_Pay_TypeDetailLs().Where(d => d.Receipt_Pay_TypeID == 1).ToList();

            //لیستی از مبدا و منبع 
            ViewBag.SourceDestinationTypesLs = Utility.GetSourceDestinationTypesLs();



            Icls_Request_Payment_BreakPriceClient BreakPriceClient = new Icls_Request_Payment_BreakPriceClient();
            Icls_Request_PaymentClient RP = new Icls_Request_PaymentClient();
            //var RequestPayment = RP.SelectRow(requestId).SingleOrDefault();
            var RequestPayment = db.Request_Payment.Find(requestId);

            Icls_BehalfsClient obj = new Icls_BehalfsClient();
            var BehalfGroupID = obj.SelectRow(RequestPayment.BehalfID).SingleOrDefault().BehalfGroupID;

            // بابت ها
            ViewBag.BehalfsLs = Utility.GetBehalfLs(BehalfGroupID);

            //گروه بابت ها
            ViewBag.BehalfGroupId = BehalfGroupID;
            ViewBag.BehalfsGroupLs = Utility.GetBehalfsGroupLs().Where(d => d.Receipt_Pay_TypeID == int.Parse(ConfigurationManager.AppSettings["Payment_Type_ID"]));

            var dc = new TreasuryEntities();
            //نوع ذینفع  
            var id = RequestPayment.Unique_Account_No.Trim();
            var unique_account = dc.v_unique_accounts.SingleOrDefault(d => d.Account_Code == id);
            var unique_account2 = dc.v_unique_accounts.SingleOrDefault(d => d.Account_Code == RequestPayment.Main_Unique_Account_No.Trim());

            if (unique_account == null)
            {
                ViewBag.uniqueTypeId = 0;
                ViewBag.uniqueAccount_Code = "---";
                ViewBag.uniqueAccount_Name = "---";
            }
            else
            {
                if (unique_account.Type_Code == null)
                {
                    //درصورتی که نوع ذینفع نداشته باشد
                    ViewBag.uniqueTypeId = 0;
                }
                else
                {
                    //برگرداندن مقدار نوع ذینفع برای نمایش
                    ViewBag.uniqueTypeId = unique_account.Type_Code.Trim();

                    ViewBag.SlectedLs = from p in dc.v_unique_accounts
                                        where p.Type_Code == unique_account.Type_Code
                                        select new
                                        {
                                            Account_Code = p.Account_Code,
                                            Account_Name = p.Account_Name + "/" + p.Account_Code
                                        };

                }
            }

            if (unique_account2 == null)
            {
                ViewBag.uniqueTypeId2 = null;
                ViewBag.uniqueAccount_Code2 = "---";
                ViewBag.uniqueAccount_Name2 = "---";
            }
            else
            {
                if (unique_account2.Type_Code == null)
                {
                    //درصورتی که نوع ذینفع نداشته باشد
                    ViewBag.uniqueTypeId2 = 0;
                }
                else
                {
                    //برگرداندن مقدار نوع ذینفع برای نمایش
                    ViewBag.uniqueTypeId2 = unique_account2.Type_Code.Trim();
                    ViewBag.SlectedLsMain = from p in dc.v_unique_accounts
                                            where p.Type_Code == unique_account2.Type_Code
                                            select new
                                            {
                                                Account_Code = p.Account_Code,
                                                Account_Name = p.Account_Name + "/" + p.Account_Code
                                            };
                }
            }

            //var BreakPriceLs = BreakPriceClient.SelectBy_DynamicWhereClause("RequestID=" + requestId, 1, 100, "BreakId", "dec", false, out totalRow).ToList();

            //if (BreakPriceLs.Count() != 0)
            //{
            //    ViewBag.Request_Payment_BreakPriceLs = BreakPriceLs;
            //}
            //else
            //{
            //    RequestPayment.Request_Payment_BreakPrice = new List<CSD.Treasury.DataModel.Request_Payment_BreakPrice>();
            //    RequestPayment.Request_Payment_BreakPrice.Add(new CSD.Treasury.DataModel.Request_Payment_BreakPrice());
            //}

            //نوع ذینفع ها
            ViewBag.unique_account_type = dc.v_unique_account_type.ToList();

            //ذیفع ها 
            ViewBag.unique_accounts = dc.v_unique_accounts.Where(d => d.Type_Code == unique_account.Type_Code);

            //ذینفع اصلی  
            ViewBag.accounts = dc.v_accounts.ToList();

            //نوع ذینفع  
            var unique_account_type = dc.v_unique_account_type.ToList();
            unique_account_type.Add(new v_unique_account_type { Code = 0, Description = Resourse.Resources.Others });
            ViewBag.unique_account_type = unique_account_type;

            //ذینفع اصلی 
            ViewBag.v_accounts = dc.v_accounts.ToList();
            ViewBag.PaymentValueLs = GetBanks(RequestPayment.Source_PaymentValue != null ? int.Parse(RequestPayment.Source_PaymentValue) : 0);


            return View(RequestPayment);
        }
        #endregion

        #region SavepermisiveForRequest
        public PartialViewResult SavepermisiveForRequest(int RequestId)
        {
            Icls_PermissiveSignatureForEachBankAccountClient psb = new Icls_PermissiveSignatureForEachBankAccountClient();
            Icls_PermissiveSignaturesClient p = new Icls_PermissiveSignaturesClient();
            //Icls_BankClient B = new Icls_BankClient();

            Icls_Request_PaymentClient R = new Icls_Request_PaymentClient();
            var db = new TreasuryEntities();
            int TotalRow;

            ViewBag.RequestId = RequestId;
            var Request = db.Request_Payment.SingleOrDefault(d => d.RequestID == RequestId);
            //var Request = R.SelectRow(RequestId).SingleOrDefault();
            //var Bank = B.SelectRow(int.Parse(Request.Source_PaymentValue)).SingleOrDefault();
            var Source_PaymentValue = int.Parse(Request.Source_PaymentValue);
            var Bank = db.Banks.SingleOrDefault(d => d.BankID == Source_PaymentValue);
            ViewBag.MinPermissiveSignatureCount = Bank.MinPermissiveSignatureCount;
            ViewBag.BankId = Bank.BankID;
            var PermissiveSignatureForEachBank = psb.SelectBy_DynamicWhereClause("BankID=" + Bank.BankID, 1, 100, "PermissiveSignatureForEachBankAccountID", "int", true, out TotalRow).ToList();
            var permissivesigniturels = p.SelectBy_DynamicWhereClause("1=1", 1, 100, "PermissiveSignatureID", "int", false, out TotalRow);
            var users = db.UserInfoes.Where(d => d.IsActive == true).ToList();

            var query = from user in users
                        join per in permissivesigniturels on user.UserID equals per.UserID
                        join perbank in PermissiveSignatureForEachBank on per.PermissiveSignatureID equals perbank.PermissiveSignatureID
                        select perbank;
            var count = query.ToList().Count;
            //var permissiveLs = p.SelectAll( 1, 100, "PermissiveSignatureID", "int", true, out TotalRow).ToList().Where(q=> PermissiveSignatureForEachBank.Contains(q.PermissiveSignatureID));
            return PartialView(query.ToList());
        }
        /// <summary>
        /// گرفتن امضا های مجاز
        /// درصورتی که چک روز باشد و کانفیگ پرداخت در زمان صدور چک باشد وقتی امضا مجاز می گیرد باید وضعیت پرداخت قطعی ثبت گردد
        ///   درصورتی که چک مدت دار باشد وقتی امضا مجاز می گیرد وکانفیگ پرداخت در زمان صدور چک باشد وضعیت تحویل شده ثبت می گردد
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult SavepermisiveForRequest(Service.PermissiveSignatureForEachBankAccountService.PermissiveSignatureForEachBankAccount obj)
        {
            int totalrow;
            Icls_PermissiveSignature_CompleteProccessClient psc = new Icls_PermissiveSignature_CompleteProccessClient();
            Icls_Request_Payment_StatusDetailsClient PS = new Icls_Request_Payment_StatusDetailsClient();
            Icls_Request_PaymentClient RP = new Icls_Request_PaymentClient();
            //Icls_BankClient BankClient = new Icls_BankClient();
            var db = new TreasuryEntities();
            var BankId = int.Parse(Request.Form["BankId"]);
            //var Bank = BankClient.SelectBy_DynamicWhereClause("BankID=" + BankId, 1, 100, "BankID", "int", false, out totalrow).SingleOrDefault();

            var Bank = db.Banks.SingleOrDefault(d => d.BankID == BankId);
            var dateNow = Utility.ToPersianDate(DateTime.Now);
            var PermissiveTicked = Request.Form["PermissiveTicked"].Split(',');

            if (PermissiveTicked.Count() >= Bank.MinPermissiveSignatureCount)
            {
                var RequestId = Request.Form["RequestId"];
                //var RequestObj = RP.SelectRow(decimal.Parse(RequestId)).SingleOrDefault();
                var RequestObj = db.Request_Payment.Find(decimal.Parse(RequestId));

                //string whereclouse = "AggrigateOrDuplicationID=" + RequestId + " and AggrigateOrDuplicationStatusID=7 and Paid=NULL and Delivered=NULL";
                //var queryls = RP.GetAllRequestStatus_DynamicWhereClause(dateNow, whereclouse, 1, 200, "RequestID", "dec", false, out totalrow).ToList();

                //var queryls = db.GetAllRequestStatus(dateNow).Where(d => d.RequestID == decimal.Parse(RequestId) && d.AggrigateOrDuplicationStatusID == 7 && d.Paid == null && d.Delivered == null).ToList();
                //var payment = p.SelectBy_DynamicWhereClause("RequestID =" + RequestId, 1, 100, "PaymentID", "dec", true, out totalrow).SingleOrDefault();
                string message = "";

                //if (payment != null)
                //{
                foreach (var item in PermissiveTicked)
                {
                    Service.PermissiveSignature_CompleteProccessService.PermissiveSignature_CompleteProccess pscObj = new Service.PermissiveSignature_CompleteProccessService.PermissiveSignature_CompleteProccess()
                    {
                        RequestID = decimal.Parse(RequestId),
                        PermissiveSignatureForEachBankAccountID = int.Parse(item)
                    };
                    psc.Insert(pscObj, out message);
                    //درصورتی که درخواست وجه ای باشد که از تجمیع درخواست وجه های تایید پرداخت شده حاصل شده باشد
                    //foreach (var item1 in queryls)
                    //{
                    //    Service.PermissiveSignature_CompleteProccessService.PermissiveSignature_CompleteProccess pscObj1 = new Service.PermissiveSignature_CompleteProccessService.PermissiveSignature_CompleteProccess()
                    //    {
                    //        RequestID = item1.RequestID,
                    //        PermissiveSignatureForEachBankAccountID = int.Parse(item)
                    //    };
                    //    psc.Insert(pscObj1, out message);
                    //}
                }
                //بدست آوردن وضعیت های مربوط به هر درخواست وجه
                var AllStatus = PS.SelectBy_DynamicWhereClause("RequestID =" + RequestId, 1, 10000, "Request_Payment_StatusDetailsID", "int", true, out totalrow).Select(d => d.RequestStatusTypeID);

                //RequestObj.Pay_TypeDetailID_source != 4 پرداخت در زمان صدور چک باشد
                // Pay_TypeDetailID_source=2 چک انتظامی
                // Pay_TypeDetailID_source=4 چک مدت دار
                //درصورتی که چک انتظامی باشد پرداخت نشده است
                //چک روز
                // اگر درست باشد باید برود صفحه اول انجا چک مدت دار را تعیین تکلیف کند 
                if ((RequestObj.Pay_TypeDetailID_source != 4 && RequestObj.Pay_TypeDetailID_source != 2 && ConfigurationManager.AppSettings["PayWhenCheckPrinted"] == "True") &&
                     AllStatus.Contains((decimal)RequestStatusTypes.ConfirmPayment) &&
                     !AllStatus.Contains((decimal)RequestStatusTypes.Paid))
                {
                    //ثبت وضعیت پرداخت شده
                    //Service.Request_Payment_StatusDetailsService.Request_Payment_StatusDetails obj1 = new Service.Request_Payment_StatusDetailsService.Request_Payment_StatusDetails();
                    DataModel.Request_Payment_StatusDetails obj1 = new DataModel.Request_Payment_StatusDetails();
                    var persianDate = Utility.ToPersianDate(DateTime.Now);
                    obj1.ActionDate = persianDate;
                    obj1.ActionTime = DateTime.Now.TimeOfDay;
                    obj1.RequestID = decimal.Parse(RequestId);
                    obj1.UserID = User.Id;
                    obj1.HostName = System.Net.Dns.GetHostName();
                    obj1.RequestStatusTypeID = (decimal)RequestStatusTypes.Paid;
                    db.Request_Payment_StatusDetails.Add(obj1);
                    db.SaveChanges();
                    //PS.Insert(obj1, out message);

                    //درصورتی که درخواست وجه ای باشد که از تجمیع درخواست وجه های تایید پرداخت شده حاصل شده باشد
                    //باید وضعیت تایید پرداخت را ثبت کنیم

                    //foreach (var item in queryls)
                    //{
                    //    //ثبت وضعیت پرداخت شده
                    //    //Service.Request_Payment_StatusDetailsService.Request_Payment_StatusDetails obj2 = new Service.Request_Payment_StatusDetailsService.Request_Payment_StatusDetails();
                    //    DataModel.Request_Payment_StatusDetails obj2 = new DataModel.Request_Payment_StatusDetails();
                    //    obj2.ActionDate = persianDate;
                    //    obj2.ActionTime = DateTime.Now.TimeOfDay;
                    //    obj2.RequestID = item.RequestID;
                    //    obj2.UserID = User.Id;
                    //    obj2.HostName = System.Net.Dns.GetHostName();
                    //    obj2.RequestStatusTypeID = (decimal)RequestStatusTypes.Paid;
                    //    db.Request_Payment_StatusDetails.Add(obj2);
                    //    db.SaveChanges();
                    //    //PS.Insert(obj2, out message);
                    //}
                }
                //چک مدت دار
                //else if (((RequestObj.Pay_TypeDetailID_source == 4 && ConfigurationManager.AppSettings["PayWhenCheckPrinted"] == "True") ) && AllStatus.Contains((decimal)RequestStatusTypes.ConfirmPayment) && !AllStatus.Contains((decimal)RequestStatusTypes.Delivered))
                //{
                //  //ثبت وضعیت تحویل شده
                //    Service.Request_Payment_StatusDetailsService.Request_Payment_StatusDetails obj1 = new Service.Request_Payment_StatusDetailsService.Request_Payment_StatusDetails();
                //    var persianDate = Utility.ToPersianDate(DateTime.Now);
                //    obj1.ActionDate = persianDate;
                //    obj1.ActionTime = DateTime.Now.TimeOfDay;
                //    obj1.RequestID = decimal.Parse(RequestId);
                //    obj1.UserID = User.Id;
                //    obj1.HostName = System.Net.Dns.GetHostName();
                //    obj1.RequestStatusTypeID = (decimal)RequestStatusTypes.Delivered;
                //    PS.Insert(obj1, out message);
                //}

                try
                {
                    return JsonMessage(true, "insert_success");
                }
                catch (Exception)
                {
                    return JsonMessage(false, "insert_error");
                }
                //}
            }
            return JsonMessage(false, "insert_error");
        }
        #endregion


        /// <summary>
        /// لیستی ازذینفع ها
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        #region UniqueAccount
        public JsonResult UniqueAccountLs(string q, string typeid, int pageSize, int page)
        {
            q = q.Replace("ی", "ي").Replace("ک", "ك");

            var dc = new TreasuryEntities();
            //var ls1 = dc.v_unique_accounts.Where(d=>d.Type_Code == typeid).ToList();
            var ls = (from v in dc.v_unique_accounts
                      where v.Type_Code == typeid && (v.Account_Code.Contains(q) || v.Account_Name.Contains(q))
                      select new { id = v.Account_Code, code = v.Account_Code, text = v.Account_Code + " - " + v.Account_Name });
            //var excludedRow = (page - 1) * pageSize;
            //.Skip(excludedRow)
            var query = ls.OrderBy(c => c.id).Take(pageSize).ToList();
            return Json(new { query, total_count = ls.Count() }, JsonRequestBehavior.AllowGet);
        }
        public PartialViewResult UniqueAccount(SearchModelUniqueAccount obj)
        {
            var dc = new TreasuryEntities();
            obj.Name = obj.Name.Replace("ی", "ي").Replace("ک", "ك");

            if (obj.UniqueAccountTypeId == "0")
            {
                List<v_unique_accounts> UniqueAccountLs = new List<v_unique_accounts>();
                v_unique_accounts objunique_account = new v_unique_accounts() { Account_Code = "0", Account_Name = "---" };
                UniqueAccountLs.Add(objunique_account);
                foreach (var item in UniqueAccountLs)
                {
                    item.Account_Name = item.Account_Name.Replace("ی", "ي").Replace("ک", "ك");
                }
                obj.List = UniqueAccountLs;
            }
            else
            {
                //obj.List = (from u in dc.v_unique_accounts
                //           where u.Type_Code == obj.UniqueAccountTypeId && u.Account_Name.Contains(obj.Name) && u.Account_Code.Contains(obj.Code)
                //           select new v_unique_accounts { Account_Code=u.Account_Code.Replace(" ",""), Account_Name=u.Account_Name }).OrderBy(c => c.Account_Name).Skip((obj.PageIndex - 1) * obj.PageSize).Take(obj.PageSize).ToList();
                obj.List = dc.v_unique_accounts.Where(d => d.Type_Code == obj.UniqueAccountTypeId && d.Account_Name.Contains(obj.Name) && d.Account_Code.Trim().Contains(obj.Code)).ToList();

            }

            obj.TotalPageCount = Convert.ToInt32(Math.Ceiling((decimal)dc.v_unique_accounts.Where(d => d.Type_Code == obj.UniqueAccountTypeId && d.Account_Name.Contains(obj.Name) && d.Account_Code.Contains(obj.Code)).Count() / obj.PageSize));
            obj.CurrentPage = obj.PageIndex;
            return PartialView(obj);
        }
        #endregion


        /// <summary>
        /// نمایش جزییات درخواست وجه
        /// </summary>
        /// <returns></returns>
        #region RequestPaymentDetails

        /// <summary>
        /// Return RequestPayment object
        /// type=0 از لیست درخواست وجه
        /// type=1 از لیست پرداخت 
        /// </summary>
        /// <returns></returns>
        public PartialViewResult RequestPayment(decimal requestId, int Type)
        {
            //type مشخص می کند این نمایش از سمت پرداخت است یا درخواست وجه           
            ViewBag.type = Type;

            int totalRow;
            var db = new TreasuryEntities();
            Icls_Request_PaymentClient RP = new Icls_Request_PaymentClient();
            Icls_Request_Payment_StatusDetailsClient StatusDetailsClient = new Icls_Request_Payment_StatusDetailsClient();
            //var RequestPayment = RP.SelectRow(requestId).SingleOrDefault();
            var RequestPayment = db.Request_Payment.Find(requestId);
            if (RequestPayment != null)
            {
                Icls_BehalfsClient BG = new Icls_BehalfsClient();
                //Voucher_RowClient voucherClient = new Voucher_RowClient();
                var voucher = db.Voucher_Row.Where(d => d.ReferenceId == requestId);
                //var voucher = voucherClient.SelectBy_DynamicWhereClause("ReferenceId =" + requestId, 1, 20, "RowId", "dec", false, out totalRow);
                var ifanyvoucher = voucher.Any();
                if (ifanyvoucher)
                {
                    ViewBag.VocherNo = voucher.FirstOrDefault().AccVoucherNo;
                }
                ViewBag.BehalfGroupID = BG.SelectRow(RequestPayment.BehalfID).SingleOrDefault().BehalfGroupID;

                //ViewBag.RequestStatus = RS.SelectBy_DynamicWhereClause(whereclouse, pageNum, pageSize, orderByFieldName, OrderByfeildtype, isAscendingOrder, out totalRows).ToList();
                //var RequestStatus = StatusDetailsClient.SelectBy_DynamicWhereClause("RequestID =" + requestId, 1, 10, "RequestStatusTypeID", "dec", false, out totalRow);
                //if(RequestStatus.Count()!=0)
                //{
                //    var RS  = RequestStatus.Select(d => d.RequestStatusTypeID);           
                //    if (RS.Contains(1) && RS.Contains(2) && RS.Contains(3) && !RS.Contains(-2) && !RS.Contains(-1) && 
                //        RequestPayment.AggrigateOrDuplicationStatusID != (int)RequestDescription.ResultOfIntegrationRequest)
                //    {
                //        ViewBag.CancelCheque = true;
                //    }
                //}

            }
            return PartialView(RequestPayment);
        }


        #endregion

        #region SaveAccountSheba
        public PartialViewResult SaveAccountSheba(decimal requestId, string Status)
        {
            //Icls_Request_PaymentClient RP = new Icls_Request_PaymentClient();
            //var RequestPayment = RP.SelectRow(requestId).SingleOrDefault();
            var db = new TreasuryEntities();
            var RequestPayment = db.Request_Payment.Find(requestId);
            //RequestPayment.Status = Status;
            if (RequestPayment.Description != null)
            {
                var desc = RequestPayment.Description.Split(';');
                RequestPayment.Description = desc[0];
            }
            return PartialView(RequestPayment);
        }
        [HttpPost]
        public ActionResult SaveAccountSheba(Service.Request_PaymentService.Request_Payment OBJ)
        {
            //Icls_Request_PaymentClient RP = new Icls_Request_PaymentClient();
            //var RequestPayment = RP.SelectRow(OBJ.RequestID).SingleOrDefault();
            var db = new TreasuryEntities();
            var RequestPayment = db.Request_Payment.Find(OBJ.RequestID);
            string Mess;
            if (RequestPayment.Description != null)
            {
                var description = RequestPayment.Description.Split(';');

                foreach (var item in description)
                {
                    description[0] = OBJ.Description;
                }
                RequestPayment.Description = string.Join(";", description);
                if (description.Count() == 1)
                {
                    RequestPayment.Description = RequestPayment.Description + ";";
                }
            }
            else
            {
                RequestPayment.Description = OBJ.Description + ";";
            }
            db.SaveChanges();
            //RP.Update(RequestPayment, out Mess);

            return RedirectToAction("Index", "Pay", new { Area = "Payment", Status = OBJ.Status });
        }
        #endregion

        #region Aggregation
        public ActionResult IndexAggregation()
        {
            return View();
        }
        /// <summary>
        /// تجمیع درصورتی که گروه بابت و تفصیل یکسان داشته باشند
        /// </summary>
        /// <returns></returns>
        public PartialViewResult AggregationList()
        {
            string CurrentDate = Utility.ToPersianDate(DateTime.Now);
            var UserId = User.Id;
            var db = new TreasuryEntities();
            // Pay_TypeDetailID_source = 7 صندوق را نباید بیاورد
            //string whereclouse = " Pay_TypeDetailID_source!=7 and AggrigateOrDuplicationID = NULL and AggrigateOrDuplicationStatusID = NULL  and Cancellation=NULL  and Confirmed=NULL and ConfirmPayment=NULL and Paid=NULL and Delivered=NULL";
            int totalRows;
            Icls_Request_PaymentClient RP = new Icls_Request_PaymentClient();


            //if (!User.IsInRole("Admin") && !User.IsInRole("Manager"))
            //{
            //    whereclouse = " and  UserID=" + UserId;
            //}

            //var query = RP.GetAllRequestStatus_DynamicWhereClause(CurrentDate, whereclouse, 1, 10000, "RequestID", "dec", false, out totalRows).ToList();
            var query = db.GetAllRequestStatus(CurrentDate).Where(d => d.Pay_TypeDetailID_source != 7 && d.AggrigateOrDuplicationID == null && d.AggrigateOrDuplicationStatusID == null && d.Cancellation == null && d.Confirmed == null && d.ConfirmPayment == null && d.Paid == null && d.Delivered == null);

            //درصورتی که مدیر سیستم باشد باید تمام درخواست وجه ها را مشاهده نماید
            if (!User.IsInRole("Admin") && !User.IsInRole("Manager"))
            {
                query = query.Where(d => d.UserID == UserId);
            }
            return PartialView(query.ToList());
        }
        /// تجمیع درخواست ها در صورتی که گروه بابت و بابت و ذینفع یکسانی داشته باشند 
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public ActionResult Aggregation(int[] requestIds)
        {
            string message = "";
            var db = new TreasuryEntities();
            List<CSD.Treasury.DataModel.Request_Payment> ls = new List<CSD.Treasury.DataModel.Request_Payment>();
            Icls_Request_PaymentClient RP = new Icls_Request_PaymentClient();
            Icls_Request_Payment_StatusDetailsClient RPS = new Icls_Request_Payment_StatusDetailsClient();
            var persianDate = Utility.ToPersianDate(DateTime.Now);
            for (int i = 0; i < requestIds.Length; i++)
            {
                //var rp = RP.SelectRow(requestIds[i]).SingleOrDefault();
                var rp = db.Request_Payment.SingleOrDefault(d => d.RequestID == requestIds[i]);
                //if (rp.Is_Paid == false && rp.Is_delivered == false)
                //{
                //} 
                ls.Add(rp);
            }
            if (ls != null)
            {
                //var diff = false;
                foreach (var items in ls.GroupBy(q => new { q.BehalfID, q.Unique_Account_No }))
                {
                    decimal PayAmountSum = 0;
                    if (items.Count() > 1)
                    {
                        foreach (var item in items)
                        {
                            PayAmountSum += item.PayAmount;
                        }
                        //ثبت درخواست تجمیع شده
                        PersianCalendar pc = new PersianCalendar();
                        var dateNow = DateTime.Now;
                        Service.Request_PaymentService.Request_Payment obj = new Service.Request_PaymentService.Request_Payment();
                        obj.UserID = User.Id;
                        obj.FiscalYear = pc.GetYear(dateNow).ToString();
                        obj.RequestDate = Utility.ToPersianDate(dateNow);
                        obj.RequestCreatedTime = dateNow.TimeOfDay;
                        obj.Priority = (short)Priority.Normal;
                        obj.Unique_Account_No = items.FirstOrDefault().Unique_Account_No;
                        obj.BehalfID = items.FirstOrDefault().BehalfID;
                        obj.ExchangeTypeID = items.FirstOrDefault().ExchangeTypeID.Value;
                        obj.PayAmount = PayAmountSum;
                        obj.AggrigateOrDuplicationStatusID = (int)RequestDescription.ResultOfIntegration;
                        obj.Unique_Account_No = items.FirstOrDefault().Unique_Account_No;
                        obj.Main_Unique_Account_No = items.FirstOrDefault().Main_Unique_Account_No;


                        var result = RP.Insert(obj, out message);

                        var RequestID = message.Split(';')[1];
                        if (result)
                        {

                            //ثبت نامشخص در جدول وضعیت ها 
                            //Service.Request_Payment_StatusDetailsService.Request_Payment_StatusDetails objStatus = new Service.Request_Payment_StatusDetailsService.Request_Payment_StatusDetails();
                            DataModel.Request_Payment_StatusDetails objStatus = new DataModel.Request_Payment_StatusDetails();
                            objStatus.UserID = User.Id;
                            objStatus.ActionDate = persianDate;
                            objStatus.ActionTime = DateTime.Now.TimeOfDay;
                            objStatus.RequestStatusTypeID = (decimal)RequestStatusTypes.Unknown;
                            objStatus.RequestID = decimal.Parse(RequestID);
                            objStatus.HostName = System.Net.Dns.GetHostName();
                            //var result2 = RPS.Insert(objStatus, out message);
                            db.Request_Payment_StatusDetails.Add(objStatus);
                            db.SaveChanges();
                        }
                        foreach (var item in items)
                        {

                            //ثبت تجمیع شده در جدول وضعیت ها به ازای تمام درخواست های که تجمیع شده اند 
                            //Request_Payment_StatusDetails objStatus = new Request_Payment_StatusDetails();
                            //objStatus.ActionDate = persianDate;
                            //objStatus.ActionTime = DateTime.Now.TimeOfDay;
                            //objStatus.RequestStatusTypeID = (decimal)RequestStatusTypes.Integration;
                            //objStatus.RequestID = item.RequestID;
                            //objStatus.HostName = System.Net.Dns.GetHostName();
                            //var result2 = RPS.Insert(objStatus, out message);

                            //کنسل نمودن درخواستی که تجمیع شده است
                            //Service.Request_Payment_StatusDetailsService.Request_Payment_StatusDetails objSts1 = new Service.Request_Payment_StatusDetailsService.Request_Payment_StatusDetails();
                            DataModel.Request_Payment_StatusDetails objSts1 = new DataModel.Request_Payment_StatusDetails();
                            objSts1.ActionDate = persianDate;
                            objSts1.ActionTime = DateTime.Now.TimeOfDay;
                            objSts1.RequestStatusTypeID = (decimal)RequestStatusTypes.Cancellation;
                            objSts1.RequestID = item.RequestID;
                            objSts1.HostName = System.Net.Dns.GetHostName();
                            objSts1.UserID = User.Id;
                            //var result1 = RPS.Insert(objSts1, out message);
                            db.Request_Payment_StatusDetails.Add(objSts1);
                            db.SaveChanges();

                            //Set AggrigationID
                            var message1 = "";
                            item.AggrigateOrDuplicationID = decimal.Parse(RequestID);
                            item.AggrigateOrDuplicationStatusID = (int)RequestDescription.Integration;
                            //RP.Update(item, out message1);
                            db.SaveChanges();
                        }
                    }
                }
                return JsonMessage(true, "insert_success");

                //if (diff == false)
                //{
                //    PersianCalendar pc = new PersianCalendar();
                //    var dateNow = DateTime.Now;

                //    Service.Request_PaymentService.Request_Payment obj = new Service.Request_PaymentService.Request_Payment();
                //    //obj.UserID = GetcurrentUser().UserID;
                //    obj.UserID = User.Id;
                //    obj.FiscalYear = pc.GetYear(dateNow).ToString();
                //    obj.Is_delivered = false;
                //    obj.Is_Ok = false;
                //    obj.Is_Paid = false;

                //    obj.RequestDate = Utility.ToPersianDate(dateNow);

                //    obj.RequestCreatedTime = dateNow.TimeOfDay;
                //    obj.Priority = (short)Priority.Normal;
                //    obj.Is_Paid = false;
                //    obj.Is_delivered = false;
                //    obj.Is_Ok = false;
                //    obj.Unique_Account_No = ls.FirstOrDefault().Unique_Account_No;
                //    obj.BehalfID = ls.FirstOrDefault().BehalfID;
                //    obj.ExchangeTypeID = 1;
                //    obj.PayAmount = PayAmountSum;


                //    var result = RP.Insert(obj, out message);
                //    //غیر فعال نمودن درخواست های وجه
                //    for (int i = 0; i < requestIds.Length; i++)
                //    {

                //    }
                //    return JsonMessage(result, message);
                //}
            }

            return JsonMessage(false, "insert_error");
        }

        /// <summary>
        /// تجمیع درخواست وجه هایی که تا مرحله تایید پرداخت رفته اند
        /// </summary>
        /// <returns></returns>
        ///
        public ActionResult AggregationRequest()
        {
            return View();
        }
        public JsonResult ShowAggregationRequest()
        {
            var dc = new TreasuryEntities();
            //نوع ذینفع  
            ViewBag.unique_account_type = dc.v_unique_account_type.ToList();
            //درصورتی که چک روز باشد امکان تجمیع وجود دارد
            //Pay_TypeDetailID_source=1 چک روز
            string CurrentDate = Utility.ToPersianDate(DateTime.Now);
            var UserId = User.Id;
            //  string whereclouse = "Pay_TypeDetailID_source=1 and AggrigateOrDuplicationID = NULL and AggrigateOrDuplicationStatusID = NULL  and Paid=NULL and Delivered=NULL";
            PersianCalendar pc = new PersianCalendar();
            var Month = pc.GetMonth(DateTime.Now);
            var FiscalYear = pc.GetYear(DateTime.Now).ToString();
            //سال مالی دی ماه سال جاری باشد
            if (Month >= 10 && ConfigurationManager.AppSettings["YearPlus"] == "True")
            {
                FiscalYear = (pc.GetYear(DateTime.Now) + 1).ToString();
            }

            //string whereclouse = " Pay_TypeDetailID_source=1 and FiscalYear= \"" + FiscalYear +
            //    "\"  and AggrigateOrDuplicationID = NULL and  Cancellation=NULL " +
            //    " and AggrigateOrDuplicationStatusID = NULL   and UnKnown != Null " +
            //    "and Confirmed != NULL and ConfirmPayment!= NULL  and  Paid = NULL" +
            //    " and Delivered=NULL and !(Pay_TypeDetailID_source =32 and ConfirmPayment != NULL)" +
            //    " and !(Pay_TypeDetailID_source =7 and ConfirmPayment != NULL)" +
            //    " and !(Pay_TypeDetailID_source =8 and ConfirmPayment != NULL)" +
            //    " and mojavezeEmza=" + false + " and IsWithdrawType= " + false;

            //string whereclouse = " Pay_TypeDetailID_source=1 and FiscalYear= \"" + FiscalYear +
            //   "\"  and AggrigateOrDuplicationID = NULL and  Cancellation=NULL " +
            //   " and AggrigateOrDuplicationStatusID = NULL   and UnKnown != Null " +
            //   "and Confirmed != NULL and ConfirmPayment!= NULL  and  Paid = NULL" +
            //   " and Delivered=NULL " +
            //   " and mojavezeEmza=" + false + " and IsWithdrawType= " + false;
            //  int totalRows;

            int totalRows;

            Icls_Request_PaymentClient RP = new Icls_Request_PaymentClient();

            //درصورتی که مدیر سیستم باشد باید تمام درخواست وجه ها را مشاهده نماید
            //if (!User.IsInRole("Admin") && !User.IsInRole("Manager"))
            //{
            //    whereclouse = " and  UserID=" + UserId;
            //}

            //var query = RP.GetAllRequestStatus_DynamicWhereClause(CurrentDate, whereclouse, 1, 10000, "RequestID", "dec", false, out totalRows).ToList();
            var query = dc.GetAllRequestStatus(CurrentDate).Where(d => d.Pay_TypeDetailID_source == 1 && d.FiscalYear == FiscalYear
               && d.AggrigateOrDuplicationID == null && d.Cancellation == null
               && d.AggrigateOrDuplicationStatusID == null && d.Unknown != null
               && d.Confirmed != null && d.ConfirmPayment != null && d.Paid == null
               && d.Delivered == null && !(d.Pay_TypeDetailID_source == 32 && d.ConfirmPayment != null)
               && !(d.Pay_TypeDetailID_source == 7 && d.ConfirmPayment != null)
               && !(d.Pay_TypeDetailID_source == 8 && d.ConfirmPayment != null)
               && d.mojavezeEmza == false && d.IsWithdrawType == false);
            if (!User.IsInRole("Admin") && !User.IsInRole("Manager"))
            {
                query = query.Where(d => d.UserID == UserId);
            }
            //Icls_BankClient BankClient = new Icls_BankClient();
            var db = new TreasuryEntities();
            Icls_ChequesClient ChequeClient = new Icls_ChequesClient();
            Icls_ChequeFormatTypeDetailsClient ChFormtTypeClient = new Icls_ChequeFormatTypeDetailsClient();
            Icls_BehalfsClient BehalfClient = new Icls_BehalfsClient();
            Icls_BehalfGroupClient BehlfGroupClient = new Icls_BehalfGroupClient();

            List<Service.BankService.Bank> ls = new List<Service.BankService.Bank>();
            //var BankLS = BankClient.SelectAll(1, 1000, "BankID", "int", true, out totalRows).Where(d => d.HasBackup == false).ToList();
            var BankLS = db.Banks.Where(d => d.HasBackup == false).ToList();
            var checkFormatType = ChFormtTypeClient.SelectAll(1, 2000, "FormatID", "int", false, out totalRows).ToList();
            var cheques = ChequeClient.SelectBy_DynamicWhereClause("IsActive =" + true, 1, 1000, "ChequeID", "int", false, out totalRows).Where(q => checkFormatType.Select(d => d.ChequeFormatID).Contains(q.ChequeFormatID.Value) && BankLS.Select(u => u.BankID).Contains(q.BankID));


            //گروه بابتی که برای تجمیع اختصاص داده شده است و معین ندارد
            var BehalfGroupId = BehlfGroupClient.SelectBy_DynamicWhereClause("BehalfGroup_Account = null", 1, 100, "BehalfGroupID", "int", false, out totalRows).FirstOrDefault().BehalfGroupID;
            var BehlfLs = BehalfClient.SelectBy_DynamicWhereClause("BehalfGroupID = " + BehalfGroupId, 1, 100, "BehalfID", "int", false, out totalRows).ToList();
            BankLS = BankLS.Where(q => cheques.Select(d => d.BankID).Contains(q.BankID)).OrderBy(d => d.BranchName).ToList();

            return Json(new
            {
                banks = BankLS.Select(q => new { BankID = q.BankID, Description = "بانک: " + Utility.GetBasicBankInfo(q.BasicBankID) + " شعبه: " + q.BranchName + " کد: " + q.BranchCode }),
                behlfLs = BehlfLs,
                list = query.Select(q => new
                {
                    RequestID = q.RequestID,
                    PayAmount = q.PayAmount,
                    Behalf = q.BehalfDescription,
                    User = Utility.GetUserName(q.UserID),
                    UniqueAccountNo = q.Account_Name,
                    PayTypeDetailIDSource = "چک نقدی",
                    SourcePaymentValue = q.BasicBankDescription + "-" + q.Bank_AccountNumber,

                })
            }, JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        public ActionResult AggregationRequest(AggrigationModel model)
        {
            string message = "";
            var db = new TreasuryEntities();
            List<CSD.Treasury.DataModel.Request_Payment> ls = new List<CSD.Treasury.DataModel.Request_Payment>();
            //Icls_Request_PaymentClient RP = new Icls_Request_PaymentClient();
            //Icls_Request_Payment_StatusDetailsClient RPS = new Icls_Request_Payment_StatusDetailsClient();
            var persianDate = Utility.ToPersianDate(DateTime.Now);
            try
            {

                foreach (var item in model.requestIds)
                {
                    //var rp = RP.SelectRow(item).SingleOrDefault();
                    var rp = db.Request_Payment.Find(item);
                    ls.Add(rp);
                }

                //for (int i = 0; i < model.requestIds.Length; i++)
                //{
                //    var rp = RP.SelectRow(model.requestIds[i]).SingleOrDefault();
                //    ls.Add(rp);
                //}
                if (ls != null)
                {
                    decimal PayAmountSum = ls.Sum(d => d.PayAmount);

                    //ثبت درخواست تجمیع شده
                    PersianCalendar pc = new PersianCalendar();
                    var dateNow = DateTime.Now;
                    var obj = new Treasury.DataModel.Request_Payment();
                    obj.UserID = User.Id;
                    obj.FiscalYear = pc.GetYear(dateNow).ToString();
                    obj.RequestCreatedTime = dateNow.TimeOfDay;
                    obj.Priority = (short)Priority.High;
                    obj.RequestDate = Utility.ToPersianDate(dateNow);
                    obj.BehalfID = model.BehalfId;
                    obj.ExchangeRate = 1;
                    //چک روز
                    obj.Pay_TypeDetailID_source = 1;
                    //بانک
                    obj.Source_TypeID = 1;
                    obj.Source_PaymentValue = model.Source_PaymentValue;
                    obj.Unique_Account_No = model.Unique_Account_No;
                    obj.Main_Unique_Account_No = model.Main_Unique_Account_No;
                    obj.BankID = int.Parse(model.Source_PaymentValue);
                    obj.PayAmount = PayAmountSum;
                    obj.AggrigateOrDuplicationStatusID = (int)RequestDescription.ResultOfIntegrationRequest;

                    //var result = RP.Insert(obj, out message);
                    db.Request_Payment.Add(obj);


                    //ثبت نامشخص در جدول وضعیت ها 
                    //برای درخواست جدید که حاصل تجمیع است
                    //Service.Request_Payment_StatusDetailsService.Request_Payment_StatusDetails objStatus = new Service.Request_Payment_StatusDetailsService.Request_Payment_StatusDetails();
                    DataModel.Request_Payment_StatusDetails objStatus = new DataModel.Request_Payment_StatusDetails();
                    objStatus.UserID = User.Id;
                    objStatus.ActionDate = persianDate;
                    objStatus.ActionTime = DateTime.Now.TimeOfDay;
                    objStatus.RequestStatusTypeID = (decimal)RequestStatusTypes.Unknown;
                    //objStatus.RequestID = decimal.Parse(RequestID);
                    objStatus.HostName = System.Net.Dns.GetHostName();
                    //var result2 = RPS.Insert(objStatus, out message);
                    obj.Request_Payment_StatusDetails.Add(objStatus);
                    //db.Request_Payment_StatusDetails.Add(objStatus);
                    //db.SaveChanges();


                    //Service.Request_Payment_StatusDetailsService.Request_Payment_StatusDetails objStatus1 = new Service.Request_Payment_StatusDetailsService.Request_Payment_StatusDetails();
                    DataModel.Request_Payment_StatusDetails objStatus1 = new DataModel.Request_Payment_StatusDetails();
                    objStatus1.UserID = User.Id;
                    objStatus1.ActionDate = persianDate;
                    objStatus1.ActionTime = DateTime.Now.TimeOfDay;
                    objStatus1.RequestStatusTypeID = (decimal)RequestStatusTypes.Confirmed;
                    //objStatus1.RequestID = decimal.Parse(RequestID);
                    objStatus1.HostName = System.Net.Dns.GetHostName();
                    //var result3 = RPS.Insert(objStatus1, out message);
                    //db.Request_Payment_StatusDetails.Add(objStatus1);
                    obj.Request_Payment_StatusDetails.Add(objStatus1);
                    //db.SaveChanges();

                    //Service.Request_Payment_StatusDetailsService.Request_Payment_StatusDetails objStatus2 = new Service.Request_Payment_StatusDetailsService.Request_Payment_StatusDetails();
                    DataModel.Request_Payment_StatusDetails objStatus2 = new DataModel.Request_Payment_StatusDetails();
                    objStatus2.UserID = User.Id;
                    objStatus2.ActionDate = persianDate;
                    objStatus2.ActionTime = DateTime.Now.TimeOfDay;
                    objStatus2.RequestStatusTypeID = (decimal)RequestStatusTypes.ConfirmPayment;
                    //objStatus2.RequestID = decimal.Parse(RequestID);
                    objStatus2.HostName = System.Net.Dns.GetHostName();
                    //var result4 = RPS.Insert(objStatus2, out message);
                    //db.Request_Payment_StatusDetails.Add(objStatus2);
                    obj.Request_Payment_StatusDetails.Add(objStatus2);
                    db.SaveChanges();




                    //ثبت تجمیع شده به ازای تمام درخواست های که تجمیع شده اند 
                    //ثبت بانکی که حاصل تجمیع است برای تمام درخواست ها
                    //Set AggrigationID
                    foreach (var item in ls)
                    {
                        //var message1 = "";
                        item.ExchangeTypeID = 1;
                        //چک روز
                        item.Pay_TypeDetailID_source = 1;
                        //بانک
                        item.Source_TypeID = 1;
                        item.Source_PaymentValue = model.Source_PaymentValue;
                        item.AggrigateOrDuplicationID = obj.RequestID;
                        item.AggrigateOrDuplicationStatusID = (int)RequestDescription.IntegrationRequest;
                        db.SaveChanges();
                        //RP.Update(item, out message1);

                    }
                }

            }
            catch (Exception)
            {

                throw;
            }
            return View();
        }


        #endregion

        #region Duplication
        //[HttpGet]
        //public ActionResult DeleteDuplication(int requestId, int pageIndex)
        //{
        //    ViewBag.PageIndex = pageIndex;
        //    ViewBag.RequestId = requestId;
        //    int TotalRow;
        //    Icls_Request_PaymentClient RP = new Icls_Request_PaymentClient();
        //    var Requestls = RP.SelectBy_DynamicWhereClause("AggrigateOrDuplicationID=" + requestId, 1, 50, "RequestID", "dec", false, out TotalRow).ToList();
        //    return PartialView(Requestls);
        //}
        //[HttpPost]
        //public ActionResult DeleteDuplication(string RequestID, int pageIndex)
        //{
        //    Icls_Request_PaymentClient RP = new Icls_Request_PaymentClient();
        //    Icls_Request_Payment_StatusDetailsClient RPS = new Icls_Request_Payment_StatusDetailsClient();
        //    Payment_ReferenceTypes_ValueClient PRV = new Payment_ReferenceTypes_ValueClient();
        //    string message = "";
        //    var Counter = 0;
        //    int TotalRow;

        //    //درصورتی که بخواهد اصلاح کند رکورد های حاصل تکثیر را،همه ی تکثیر شده های قبل را حذف می نماییم
        //    var Requestls = RP.SelectBy_DynamicWhereClause("AggrigateOrDuplicationID=" + RequestID, 1, 50, "RequestID", "dec", false, out TotalRow).ToList();
        //    string CurrentDate = Utility.ToPersianDate(DateTime.Now);
        //    //var Requestls = RP.SelectBy_DynamicWhereClause("AggrigateOrDuplicationID=" + requestId, 1, 50, "RequestID", "dec", false, out TotalRow);
        //    foreach (var item in Requestls)
        //    {
        //        var Status = RP.GetAllRequestStatus_DynamicWhereClause(CurrentDate, "RequestID =" + item.RequestID, 1, 100, "RequestID", "dec", false, out TotalRow).FirstOrDefault();
        //        if (Status.Confirmed != null)
        //            Counter++;
        //    }
        //    //درصورتی که تایید درخواست را انجام نداده باشد بتواند اصلاح نماید
        //    if (Counter == 0)
        //    {
        //        foreach (var item in Requestls)
        //        {
        //            var RefrenceLS = PRV.SelectBy_DynamicWhereClause("RequestId=" + item.RequestID, 1, 50, "ReferenceTypeValueID", "int", false, out TotalRow);
        //            var StatusLS = RPS.SelectBy_DynamicWhereClause("RequestID=" + item.RequestID, 1, 50, "Request_Payment_StatusDetailsID", "int", false, out TotalRow);
        //            foreach (var R in RefrenceLS)
        //            {
        //                PRV.Delete(R.ReferenceTypeValueID, out message);
        //            }
        //            foreach (var S in StatusLS)
        //            {
        //                RPS.Delete(S.Request_Payment_StatusDetailsID, out message);
        //            }
        //            var result = RP.Delete(int.Parse(item.RequestID.ToString()), out message);

        //        }
        //        return JsonMessage(true, message);
        //    }
        //    return JsonMessage(false, "delete_error");
        //    //return RedirectToAction("Index", new { PageIndex = PageIndex });
        //}

        //****
        public int GetCashExchangeRate(int CashDeskID, string ReceiptDate)
        {
            var db = new TreasuryEntities();
            var ExchangeTypeDescription = "";

            var nerkh = 0;
            db.Configuration.ProxyCreationEnabled = false;
            var Cashdesk = db.CashDesks.Find(CashDeskID);
            if (Cashdesk != null)
            {
                var exchangetype = db.ExchangeTypes.Find(Cashdesk.ExchangeTypeID);
                ExchangeTypeDescription = exchangetype.ExchangeTypeDescription;
                switch (Cashdesk.ExchangeTypeValues)
                {
                    case 1:
                        var FiscalYear = int.Parse(ConfigurationManager.AppSettings["FiscalYear"]);
                        var ExchangeStandard = db.ExchangeStandards.SingleOrDefault(d => d.ExchangeTypeID == Cashdesk.ExchangeTypeID && d.FiscalYear == FiscalYear);
                        if (ExchangeStandard != null)
                        {
                            nerkh = ExchangeStandard.Price;
                        }
                        break;
                    case 2:
                        var ExchangeRate = db.ExchangeRates.SingleOrDefault(d => d.ExchangeTypeID == Cashdesk.ExchangeTypeID && d.ExchangeDateRate == ReceiptDate);
                        if (ExchangeRate != null)
                        {
                            nerkh = ExchangeRate.FormalPrice;
                        }
                        break;
                    case 3:
                        var ExchangeRate1 = db.ExchangeRates.SingleOrDefault(d => d.ExchangeTypeID == Cashdesk.ExchangeTypeID && d.ExchangeDateRate == ReceiptDate);
                        if (ExchangeRate1 != null)
                        {
                            nerkh = ExchangeRate1.InformalPrice;
                        }
                        break;
                    case 4:
                        break;
                }
            }


            return nerkh;
        }
        //****
        public int GetBankExchangeRate(int BankId, string ReceiptDate)
        {
            var db = new TreasuryEntities();
            var ExchangeTypeDescription = "";

            var nerkh = 0;
            db.Configuration.ProxyCreationEnabled = false;
            var Bank = db.Banks.Find(BankId);
            if (Bank != null)
            {
                var exchangetype = db.ExchangeTypes.Find(Bank.ExchangeTypeID);
                ExchangeTypeDescription = exchangetype.ExchangeTypeDescription;
                switch (Bank.ExchangeType)
                {
                    case 1:
                        var FiscalYear = int.Parse(ConfigurationManager.AppSettings["FiscalYear"]);
                        var ExchangeStandard = db.ExchangeStandards.SingleOrDefault(d => d.ExchangeTypeID == Bank.ExchangeTypeID && d.FiscalYear == FiscalYear);
                        if (ExchangeStandard != null)
                        {
                            nerkh = ExchangeStandard.Price;
                        }
                        break;
                    case 2:
                        var ExchangeRate = db.ExchangeRates.SingleOrDefault(d => d.ExchangeTypeID == Bank.ExchangeTypeID && d.ExchangeDateRate == ReceiptDate);
                        if (ExchangeRate != null)
                        {
                            nerkh = ExchangeRate.FormalPrice;
                        }
                        break;
                    case 3:
                        var ExchangeRate1 = db.ExchangeRates.SingleOrDefault(d => d.ExchangeTypeID == Bank.ExchangeTypeID && d.ExchangeDateRate == ReceiptDate);
                        if (ExchangeRate1 != null)
                        {
                            nerkh = ExchangeRate1.InformalPrice;
                        }
                        break;
                    case 4:
                        break;
                }
            }


            return nerkh;
        }
        //****




        public ActionResult ShowDuplication(int requestId, int pageIndex)
        {
            ViewBag.PageIndex = pageIndex;
            ViewBag.RequestId = requestId;
            return View("Duplication");
        }
        public JsonResult Duplication(int requestId, int pageIndex)
        {
            ViewBag.PageIndex = pageIndex;
            ViewBag.RequestId = requestId;
            //Icls_Request_PaymentClient RP = new Icls_Request_PaymentClient();
            var db = new TreasuryEntities();
            //RP.SelectRow(requestId).SingleOrDefault()
            var RequestPayment = db.Request_Payment.Find(requestId);
            return Json(RequestPayment, JsonRequestBehavior.AllowGet);
            //return Json(Request, JsonRequestBehavior.AllowGet);
        }
        //لیستی از درخواست های تکثیر شده از یک درخواست وجه را می دهد
        public ActionResult RequestChildByRequestId(int RequestID)
        {
            int TotalRow;
            var db = new TreasuryEntities();
            Icls_Request_PaymentClient RP = new Icls_Request_PaymentClient();
            string whereclouse = "AggrigateOrDuplicationID =" + RequestID;
            var ls = RP.SelectBy_DynamicWhereClause(whereclouse, 1, 100, "RequestID", "dec", false, out TotalRow);
            List<CustomRequestPayment> customLs = new List<CustomRequestPayment>();
            foreach (var item in ls)
            {
                CustomRequestPayment obj = new CustomRequestPayment();
                obj.RequestId = item.RequestID;
                obj.price = item.PayAmount;
                obj.UniqAccountTtitle = Utility.GetUniqueAccount(item.Unique_Account_No);
                obj.UniqAccountNum = item.Unique_Account_No;
                customLs.Add(obj);
            }
            //var Request = RP.SelectRow(RequestID).SingleOrDefault();
            var Request = db.Request_Payment.Find(RequestID);
            RequestObj requestObj = new RequestObj()
            {
                requestId = Request.RequestID,
                BehalfTitle = Utility.GetBehalf(Request.BehalfID),
                price = Request.PayAmount,
                ExchangeTypeID = Request.ExchangeTypeID,
                RequestDate = Request.RequestDate,
                Exchange = Utility.GetExchaneName(Request.ExchangeTypeID.Value)
            };
            return Json(new { request = requestObj, ChildLs = customLs }, JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        public ActionResult SaveDuplication(Dupplication duplicationObj)
        {
            Icls_Request_PaymentClient RP = new Icls_Request_PaymentClient();
            Icls_Request_Payment_StatusDetailsClient RPS = new Icls_Request_Payment_StatusDetailsClient();
            Payment_ReferenceTypes_ValueClient PRV = new Payment_ReferenceTypes_ValueClient();
            var persianDate = Utility.ToPersianDate(DateTime.Now);
            int TotalRow;
            var result = false;
            var db = new TreasuryEntities();
            var requestId = duplicationObj.requestId;
            //var request = RP.SelectRow(requestId).SingleOrDefault();
            var request = db.Request_Payment.Find(requestId);
            string message = "", message1 = "";
            var RefrencesLs = PRV.SelectBy_DynamicWhereClause("RequestId=" + requestId, 1, 100, "ReferenceTypeValueID", "int", false, out TotalRow);

            foreach (var item in duplicationObj.list)
            {
                if (item.requestId == 0 && item.price != 0 && !string.IsNullOrEmpty(item.uniqueAccountCode))
                {
                    var dateNow = DateTime.Now;
                    Service.Request_PaymentService.Request_Payment obj = new Service.Request_PaymentService.Request_Payment();
                    PersianCalendar pc = new PersianCalendar();
                    obj.FiscalYear = pc.GetYear(dateNow).ToString();
                    obj.UserID = User.Id;
                    obj.RequestDate = Utility.ToPersianDate(dateNow);
                    obj.RequestCreatedTime = dateNow.TimeOfDay;

                    obj.Unique_Account_No = item.uniqueAccountCode;
                    obj.Main_Unique_Account_No = request.Main_Unique_Account_No;
                    obj.PayAmount = item.price;
                    obj.ExchangeTypeID = request.ExchangeTypeID.Value;
                    obj.Priority = request.Priority;
                    obj.ExchangeRate = request.ExchangeRate;
                    obj.EquivalencePrice = item.price;
                    obj.BehalfID = request.BehalfID;

                    obj.Pay_TypeDetailID_source = request.Pay_TypeDetailID_source;
                    obj.Source_TypeID = request.Source_TypeID;
                    obj.Source_PaymentValue = request.Source_PaymentValue;
                    obj.DueDate = request.DueDate;
                    obj.Destination_PaymentValue = request.Destination_PaymentValue;
                    obj.AggrigateOrDuplicationID = requestId;
                    obj.AggrigateOrDuplicationStatusID = (int)RequestDescription.ResultOfDuplication;

                    result = RP.Insert(obj, out message);
                    var ID = message.Split(';')[1];
                    if (result)
                    {
                        //ثبت نامشخص در جدول وضعیت ها 
                        //Service.Request_Payment_StatusDetailsService.Request_Payment_StatusDetails objStatus = new Service.Request_Payment_StatusDetailsService.Request_Payment_StatusDetails();
                        DataModel.Request_Payment_StatusDetails objStatus = new DataModel.Request_Payment_StatusDetails();
                        objStatus.ActionDate = persianDate;
                        objStatus.UserID = User.Id;
                        objStatus.ActionTime = DateTime.Now.TimeOfDay;
                        objStatus.RequestStatusTypeID = (decimal)RequestStatusTypes.Unknown;
                        objStatus.RequestID = decimal.Parse(ID);
                        objStatus.HostName = System.Net.Dns.GetHostName();
                        //RPS.Insert(objStatus, out message);
                        db.Request_Payment_StatusDetails.Add(objStatus);
                        db.SaveChanges();
                    }


                    foreach (var item1 in RefrencesLs)
                    {
                        Service.Payment_ReferenceTypes_ValueService.Payment_ReferenceTypes_Value objvalue = new Service.Payment_ReferenceTypes_ValueService.Payment_ReferenceTypes_Value();
                        objvalue.ReferenceID = item1.ReferenceID;
                        objvalue.ReferenceValue = item1.ReferenceValue;
                        objvalue.RequestId = decimal.Parse(ID);
                        PRV.Insert(objvalue, out message);

                    }
                }
                else if (item.requestId != 0)
                {
                    //var requestpay = RP.SelectRow(item.requestId).SingleOrDefault();
                    var requestpay = db.Request_Payment.Find(item.requestId);
                    requestpay.PayAmount = item.price;
                    requestpay.Unique_Account_No = item.uniqueAccountCode;
                    db.SaveChanges();
                    //RP.Update(requestpay, out message1);
                }

            }
            if (result)
            {
                //کنسل نمودن درخواستی که تکثیر شده است
                //Service.Request_Payment_StatusDetailsService.Request_Payment_StatusDetails objSts1 = new Service.Request_Payment_StatusDetailsService.Request_Payment_StatusDetails();
                DataModel.Request_Payment_StatusDetails objSts1 = new DataModel.Request_Payment_StatusDetails();
                objSts1.ActionDate = persianDate;
                objSts1.ActionTime = DateTime.Now.TimeOfDay;
                objSts1.RequestStatusTypeID = (decimal)RequestStatusTypes.Cancellation;
                objSts1.RequestID = requestId;
                objSts1.UserID = User.Id;
                objSts1.HostName = System.Net.Dns.GetHostName();
                //var result1 = RPS.Insert(objSts1, out message1);
                db.Request_Payment_StatusDetails.Add(objSts1);
                db.SaveChanges();
                //ویرایش درخواست که تکثیر شده و ثبت توضیهات در فیلد desc
                request.AggrigateOrDuplicationStatusID = (int)RequestDescription.Duplication;
                //RP.Update(request, out message1);
                db.SaveChanges();
                return JsonMessage(true, message);
            }
            else
            {
                return JsonMessage(false, "insert_error");
            }


        }
        [HttpPost]
        public ActionResult Duplication(string AllPrice, int RequestID)
        {
            if (ModelState.IsValid)
            {
                var db = new TreasuryEntities();
                Icls_Request_PaymentClient RP = new Icls_Request_PaymentClient();
                Icls_Request_Payment_StatusDetailsClient RPS = new Icls_Request_Payment_StatusDetailsClient();
                Payment_ReferenceTypes_ValueClient PRV = new Payment_ReferenceTypes_ValueClient();
                var request = db.Request_Payment.Find(RequestID);
                //var request = RP.SelectRow(RequestID).SingleOrDefault();
                string message = "", message1 = "";
                var persianDate = Utility.ToPersianDate(DateTime.Now);
                int TotalRow;

                var Prices = AllPrice.Split(',');
                foreach (var item in Prices)
                {
                    if (item != "")
                    {
                        var dateNow = DateTime.Now;
                        Service.Request_PaymentService.Request_Payment obj = new Service.Request_PaymentService.Request_Payment();
                        PersianCalendar pc = new PersianCalendar();
                        obj.FiscalYear = pc.GetYear(dateNow).ToString();
                        obj.UserID = User.Id;
                        obj.RequestDate = Utility.ToPersianDate(dateNow);
                        obj.RequestCreatedTime = dateNow.TimeOfDay;

                        obj.Unique_Account_No = request.Unique_Account_No;
                        obj.Main_Unique_Account_No = request.Main_Unique_Account_No;
                        obj.PayAmount = decimal.Parse(item);
                        obj.ExchangeTypeID = request.ExchangeTypeID.Value;
                        obj.Priority = request.Priority;
                        obj.ExchangeRate = request.ExchangeRate;
                        obj.EquivalencePrice = request.EquivalencePrice;
                        obj.BehalfID = request.BehalfID;

                        obj.Pay_TypeDetailID_source = request.Pay_TypeDetailID_source;
                        obj.Source_TypeID = request.Source_TypeID;
                        obj.Source_PaymentValue = request.Source_PaymentValue;
                        obj.DueDate = request.DueDate;
                        obj.Destination_PaymentValue = request.Destination_PaymentValue;
                        obj.AggrigateOrDuplicationID = RequestID;
                        obj.AggrigateOrDuplicationStatusID = (int)RequestDescription.ResultOfDuplication;

                        var result = RP.Insert(obj, out message);
                        var ID = message.Split(';')[1];
                        if (result)
                        {
                            //ثبت نامشخص در جدول وضعیت ها 
                            //Service.Request_Payment_StatusDetailsService.Request_Payment_StatusDetails objStatus = new Service.Request_Payment_StatusDetailsService.Request_Payment_StatusDetails();
                            DataModel.Request_Payment_StatusDetails objStatus = new DataModel.Request_Payment_StatusDetails();
                            objStatus.ActionDate = persianDate;
                            objStatus.UserID = User.Id;
                            objStatus.ActionTime = DateTime.Now.TimeOfDay;
                            objStatus.RequestStatusTypeID = (decimal)RequestStatusTypes.Unknown;
                            objStatus.RequestID = decimal.Parse(ID);
                            objStatus.HostName = System.Net.Dns.GetHostName();
                            //RPS.Insert(objStatus, out message);
                            db.Request_Payment_StatusDetails.Add(objStatus);
                            db.SaveChanges();
                        }
                        //return JsonMessage(result, message);
                        var RefrencesLs = PRV.SelectBy_DynamicWhereClause("RequestId=" + RequestID, 1, 100, "ReferenceTypeValueID", "int", false, out TotalRow);
                        foreach (var item1 in RefrencesLs)
                        {
                            Service.Payment_ReferenceTypes_ValueService.Payment_ReferenceTypes_Value objvalue = new Service.Payment_ReferenceTypes_ValueService.Payment_ReferenceTypes_Value();
                            objvalue.ReferenceID = item1.ReferenceID;
                            objvalue.ReferenceValue = item1.ReferenceValue;
                            objvalue.RequestId = decimal.Parse(ID);
                            PRV.Insert(objvalue, out message);

                        }

                    }
                }
                //if (result)
                //{
                //ثبت وضعیت تکثیر شده برای درخواستی که تکثیر شده است
                //Request_Payment_StatusDetails objSts = new Request_Payment_StatusDetails();
                //objSts.ActionDate = persianDate;
                //objSts.ActionTime = DateTime.Now.TimeOfDay;
                //objSts.RequestStatusTypeID = (decimal)RequestStatusTypes.Duplication;
                //objSts.RequestID = RequestID;
                //objSts.HostName = System.Net.Dns.GetHostName();
                //var result1 = RPS.Insert(objSts, out message);

                //کنسل نمودن درخواستی که تکثیر شده است
                //Service.Request_Payment_StatusDetailsService.Request_Payment_StatusDetails objSts1 = new Service.Request_Payment_StatusDetailsService.Request_Payment_StatusDetails();
                DataModel.Request_Payment_StatusDetails objSts1 = new DataModel.Request_Payment_StatusDetails();
                objSts1.ActionDate = persianDate;
                objSts1.ActionTime = DateTime.Now.TimeOfDay;
                objSts1.RequestStatusTypeID = (decimal)RequestStatusTypes.Cancellation;
                objSts1.RequestID = RequestID;
                objSts1.UserID = User.Id;
                objSts1.HostName = System.Net.Dns.GetHostName();
                //var result1 = RPS.Insert(objSts1, out message1);
                db.Request_Payment_StatusDetails.Add(objSts1);
                db.SaveChanges();
                //ویرایش درخواست که تکثیر شده و ثبت توضیهات در فیلد desc
                request.AggrigateOrDuplicationStatusID = (int)RequestDescription.Duplication;
                //RP.Update(request, out message1);
                db.SaveChanges();
                return JsonMessage(true, message);
                //}
            }
            return Json(false, JsonRequestBehavior.AllowGet);
        }
        #endregion

        /// <summary>
        /// نمایش درخواست های ابطال شده
        /// </summary>
        /// <returns></returns>
        #region RequestsCanceled
        public ActionResult RequestsCanceled(SearchModelRequestPayment obj)
        {
            return View(obj);
        }

        public ActionResult ShowRequestsCanceled(SearchModelRequestPayment obj)
        {
            int pageNum = obj.PageIndex;
            int pageSize = 10;
            var UserId = User.Id;
            //string whereclouse = "AggrigateOrDuplicationID = NULL and Cancellation!=NULL and IsWithdrawType = " + false;
            int totalRows;
            //Icls_Request_PaymentClient RP = new Icls_Request_PaymentClient();
            var db = new TreasuryEntities();
            string CurrentDate = Utility.ToPersianDate(DateTime.Now);
            var query = db.GetAllRequestStatus(CurrentDate).Where(d => d.AggrigateOrDuplicationID == null && d.Cancellation != null && d.IsWithdrawType == false);

            //درصورتی که مدیر سیستم باشد باید تمام درخواست وجه ها را مشاهده نماید
            if (!User.IsInRole("Admin") && !User.IsInRole("Manager"))
            {
                //whereclouse = whereclouse + " and  UserID=" + UserId;
                query = query.Where(d => d.UserID == UserId);
            }
            if (!string.IsNullOrEmpty(obj.RequestDateFrom) && !string.IsNullOrEmpty(obj.RequestDateTo))
            {
                //whereclouse = whereclouse + " and RequestDate >= \"" + obj.RequestDateFrom + "\"  and RequestDate <= \"" + obj.RequestDateTo + "\"";
                query = query.Where(d => d.RequestDate.CompareTo(obj.RequestDateFrom) <= 0 && d.RequestDate.CompareTo(obj.RequestDateTo) >= 0);
            }
            else
            {
                //whereclouse = whereclouse + " and FiscalYear=\"" + obj.FiscalYear + "\"";
                query = query.Where(d => d.FiscalYear == obj.FiscalYear);
            }
            if (obj.PayAmount != 0)
            {
                //whereclouse = whereclouse + " and  PayAmount >" + obj.PayAmount + " ";
                query = query.Where(d => d.PayAmount == obj.PayAmount);
            }
            if (obj.ExchangeTypeID != 0)
            {
                //whereclouse = whereclouse + " and ExchangeTypeID=" + obj.ExchangeTypeID;
                query = query.Where(d => d.ExchangeTypeID == obj.ExchangeTypeID);
            }
            if (obj.BehalfID != 0)
            {
                //whereclouse = whereclouse + " and BehalfID=" + obj.BehalfID;
                query = query.Where(d => d.BehalfID == obj.BehalfID);
            }
            if (obj.Pay_TypeDetailID_source != 0)
            {
                //whereclouse = whereclouse + " and Pay_TypeDetailID_source=" + obj.Pay_TypeDetailID_source;
                query = query.Where(d => d.Pay_TypeDetailID_source == obj.Pay_TypeDetailID_source);
            }
            if (obj.RequsetId != 0)
            {
                //whereclouse = whereclouse + " and RequestID=" + obj.RequsetId;
                query = query.Where(d => d.RequestID == obj.RequsetId);
            }

            if (!string.IsNullOrEmpty(obj.Unique_Account_No))
            {
                //whereclouse = whereclouse + " and Unique_Account_No=\"" + obj.Unique_Account_No + "\"";
                query = query.Where(d => d.Unique_Account_No == obj.Unique_Account_No);
            }

            //var query = RP.GetAllRequestStatus_DynamicWhereClause(CurrentDate, whereclouse, pageNum + 1, pageSize, "RequestID", "dec", false, out totalRows).ToList();


            obj.CurrentPage = pageNum;
            obj.List = query.ToList();
            totalRows = obj.List.Count();
            obj.TotalPageCount = (int)Math.Ceiling(totalRows / (double)pageSize);

            foreach (var item in obj.List)
            {
                if (item.AggrigateOrDuplicationStatusID == (int)RequestDescription.Duplication || item.AggrigateOrDuplicationStatusID == (int)RequestDescription.ResultOfIntegration)
                {
                    item.ChildList = db.GetAllRequestStatus(CurrentDate).Where(d => d.AggrigateOrDuplicationID == item.RequestID).ToList();
                    //item.ChildList = RP.GetAllRequestStatus_DynamicWhereClause(CurrentDate, "AggrigateOrDuplicationID =" + item.RequestID, 1, 100, "RequestID", "dec", false, out totalRows1).ToList();
                }
            }

            obj.CurrentPage = obj.PageIndex;
            obj.TotalItemCount = totalRows;

            return PartialView(obj);

        }
        #endregion



    }
}


