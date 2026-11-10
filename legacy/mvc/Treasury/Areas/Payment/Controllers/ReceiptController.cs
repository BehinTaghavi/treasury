using CSD.Treasury.UI.Controllers;
using CSD.Treasury.UI.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using CSD.Treasury.Service.Receipt_Pay_TypeService;
using CSD.Treasury.Service.Reciept_OtherProperyService;
using CSD.Treasury.Service.Reciept_OtherPropery_DetailService;
using CSD.Treasury.Service.Reciept_OtherPropery_BY_Receipt_Pay_TypeDetailService;
using CSD.Treasury.Service.PlaningTypeService;
using CSD.Treasury.Service.Reciept_ReferenceTypes_ValueService;
using CSD.Treasury.Service.Receipt_Detail_PlanStateService;
using CSD.Treasury.Service.CityService;
using CSD.Treasury.Service.ProceduresDB;
using CSD.Treasury.Service.BankService;
using CSD.Treasury.Service.CashiersService;
using CSD.Treasury.Service.BehalfGroupService;
using CSD.Treasury.Service.Receipt_Pay_TypeDetailService;
using CSD.Treasury.Service.BasicBanksService;
using CSD.Treasury.Service.CashDeskService;
using CSD.Treasury.DataModel;
using CSD.Treasury.Service.Receipt_Detail_BreakPriceService;
using Newtonsoft.Json;
using System.Globalization;
using CSD.Treasury.Common.Enumeration;
using CSD.Treasury.UI;
using CSD.Treasury.Service.ReferenceTypes_By_BehalfGroupService;
using System.Configuration;
using CSD.Common.Models;
using Stimulsoft.Report.Mvc;
using Stimulsoft.Report;
using CSD.Common.Utility;
using System.Text;
using Receipt_Detail_PlanState = CSD.Treasury.Service.Receipt_Detail_PlanStateService.Receipt_Detail_PlanState;
using CSD.Treasury.Service.Voucher_Row_RecieptService;
using CSD.Treasury.UI.Areas.Payment.Models;

namespace CSD.Treasury.UI.Areas.Payment.Controllers
{
    [Authorize(Roles = "Admin,Manager,Reciept")]
    public class ReceiptController : BaseController
    {
        // GET: Payment/Receipt


        #region Cheque
        public ActionResult Cheque(int ReceiptPayTypeId, int headerId = 0)
        {
            ViewBag.HeaderId = headerId;
            return View();
        }
        [HttpGet]
        public ActionResult ShowReceiptForm(int ReceiptPayTypeId, int headerId = 0, int ReceiptDetailId = 0)
        {
            var message = "";
            try
            {
                //Icls_Reciept_HeaderClient HeaderClient = new Icls_Reciept_HeaderClient();
                //Icls_Receipt_DetailClient ReceiptClient = new Icls_Receipt_DetailClient();
                var db = new TreasuryEntities();
                ViewBag.Price = 0;
                var voucherNo = Utility.HasVoucher(ReceiptDetailId);
                ViewBag.DateNow = Utility.DateStructure(Utility.ToPersianDate(DateTime.Now));
                
                if (voucherNo != 0)
                {
                    ViewBag.hasvoucher = "برای این دریافت سند باشماره" + voucherNo + " ارسال شده است در صورت ویرایش سندهاحذف می گردد";
                }

                if (ReceiptDetailId != 0 && headerId == 0)
                {
                    //var rd = ReceiptClient.SelectRow(ReceiptDetailId).SingleOrDefault();
                    var rd = db.Receipt_Detail.Find(ReceiptDetailId);
                    var LastState = db.Receipt_Detail_PlanState.Where(d => d.ReceiptDetailId == ReceiptDetailId).OrderByDescending(d => d.StateID).FirstOrDefault();
                   
                    ViewBag.lastStatedate = Utility.GetPersianDate(LastState.StateDate);
                    StringBuilder sb = new StringBuilder();
                    sb.Append(" تعویض چک به شماره : ");
                    sb.Append(rd.Cheque_Number);
                    sb.Append(" شماره رسید: ");
                    sb.Append(rd.ReceiptDetailId);
                    sb.Append(" پرداخت کننده: ");
                    sb.Append(rd.Unique_Account_No);
                    sb.Append(" مبلغ: ");
                    sb.Append(Math.Truncate(rd.ChequePrice_Rial).ToString("#,##0"));
                    ViewBag.Info = sb.ToString();
                    ViewBag.Price = rd.ChequePrice_Rial;
                    ViewBag.Unique_Account_No = rd.Unique_Account_No.ToString();
                }
                ViewBag.ReceiptPayTypeName = Utility.GetReceipt_Pay_TypeDetailTitle(ReceiptPayTypeId);

                PersianCalendar pc = new PersianCalendar();
                if (headerId != 0)
                {
                    ViewBag.ReceiptDate = Utility.GetPersianDate(db.Reciept_Header.Find(headerId).ReceiptDate);
                }
                else
                {
                    ViewBag.ReceiptDate = pc.GetYear(DateTime.Now).ToString() + "/" + (pc.GetMonth(DateTime.Now).ToString().Length == 1 ? "0" + pc.GetMonth(DateTime.Now).ToString() : pc.GetMonth(DateTime.Now).ToString()) + "/" + (pc.GetDayOfMonth(DateTime.Now).ToString().Length == 1 ? "0" + pc.GetDayOfMonth(DateTime.Now).ToString() : pc.GetDayOfMonth(DateTime.Now).ToString());
                }


                ViewBag.ReceiptPayTypeId = ReceiptPayTypeId;
                ViewBag.HeaderId = headerId;
                ViewBag.ExchangeLS = Utility.GetExchangeLs();


            }
            catch (Exception ex)
            {
                message = "با خطا مواجه شد";
                //Object reference not set to an instance of an object
                if (ex.Message == "Object reference not set to an instance of an object.")
                {
                    message = "رکوردی یافت نشد";
                }


                //return JsonMessage(false, message, null, false);
                ViewBag.message = message;

            }

            return View();
        }

        public ActionResult Index(ReceiptSearch1 obj)
        {
            //گروه بابت ها
            ViewBag.BehalfsGroupLs = Utility.GetBehalfsGroupLs().Where(d => d.Receipt_Pay_TypeID == int.Parse(ConfigurationManager.AppSettings["Receipt_Type_ID"]));
            var dc = new TreasuryEntities();
            //نوع ذینفع  
            ViewBag.unique_account_type = dc.v_unique_account_type.ToList();
            return View(obj);
        }
        public ActionResult List(ReceiptSearch1 Obj)
        {
            Icls_SPsClient spsClient = new Icls_SPsClient();
            Icls_Receipt_Detail_PlanStateClient ReceiptDetailsClinet = new Icls_Receipt_Detail_PlanStateClient();
            int pageNum = Obj.PageIndex;
            var UserId = User.Id;
            Obj.PageSize = 10;

            //planingTYpe=1 نزد صندوق
            var ReceiptLs = spsClient.GetRecieptHeaderAndDetail_ByRecieptTypeId(Obj.RecieptTypeId).ToList();
            if (!string.IsNullOrEmpty(Obj.DueDateFrom) && !string.IsNullOrEmpty(Obj.DueDateTo))
            {
                //whereclouse = whereclouse + " and RequestDate >= \"" + SearchObj.DueDateFrom + "\"  and RequestDate <= \"" + SearchObj.DueDateTo + "\"";
            }
            if (Obj.BehalfID != 0)
            {
                ReceiptLs = ReceiptLs.Where(d => d.BehalfID == Obj.BehalfID).ToList();
            }
            if (!string.IsNullOrEmpty(Obj.Unique_Account_No))
            {
                ReceiptLs = ReceiptLs.Where(d => d.DetailUnique_Account_No.Trim() == Obj.Unique_Account_No.Trim()).ToList();
            }
            if (Obj.PayAmount != 0)
            {
                ReceiptLs = ReceiptLs.Where(d => (int)d.ChequePrice_Rial == Obj.PayAmount).ToList();
            }

            if (!string.IsNullOrEmpty(Obj.FiscalYear))
            {
                ReceiptLs = ReceiptLs.Where(d => d.FiscalYear == Obj.FiscalYear).ToList();
            }
            if (Obj.ReceiptId != 0)
            {
                ReceiptLs = ReceiptLs.Where(d => d.ReceiptDetailId == Obj.ReceiptId).ToList();
            }
            if (!string.IsNullOrEmpty(Obj.DueDateFrom) && !string.IsNullOrEmpty(Obj.DueDateTo))
            {
                var FromDate = Obj.DueDateFrom.Replace("/", String.Empty);
                var ToDate = Obj.DueDateTo.Replace("/", String.Empty);
                ReceiptLs = ReceiptLs.Where(d => d.DueDate.CompareTo(FromDate) >= 0 && d.DueDate.CompareTo(ToDate) <= 0).ToList();
            }
            Obj.List = ReceiptLs.OrderByDescending(d => d.ReceiptId).Skip(Obj.PageSize * Obj.PageIndex).Take(Obj.PageSize).ToList();

            Obj.CurrentPage = Obj.PageIndex;
            Obj.TotalItemCount = ReceiptLs.Count();
            double s = (double)Obj.TotalItemCount / (double)Obj.PageSize;
            Obj.TotalPageCount = (int)Math.Ceiling(s);
            return PartialView(Obj);
        }
        #endregion

        public ActionResult GetCash(int headerId = 0)
        {
            ViewBag.HeaderId = headerId;
            return View();
        }
        public ActionResult CashDeposit(int headerId = 0)
        {
            ViewBag.HeaderId = headerId;
            return View();
        }
        public ActionResult ATM(int headerId = 0)
        {
            ViewBag.HeaderId = headerId;
            return View();
        }
        public ActionResult PromissoryNote(int ReceiptPayTypeId, int headerId = 0)
        {
            ViewBag.HeaderId = headerId;
            return View();
        }

        public JsonResult BanksAccountLS(string q, int pageSize, int page)
        {
            int totalRows;
            var db = new TreasuryEntities();
            //Icls_BankClient BankClient = new Icls_BankClient();
            //BankClient.SelectAll(1, 100, "BankID", "int", false, out totalRows)
            var ls = (from v in db.Banks
                      where v.Bank_AccountNumber.Contains(q)
                      select new { id = v.BankID, text = "<div class='clearfix'>" + "<div class='pull-right pl15'>" + v.Bank_AccountNumber + "</div><div class='pull-right pl15'>" + v.BranchName + "</div><div class='pull-right'>" + Utility.GetBasicBankInfo(v.BasicBankID) + "</div>" });
            //var excludedRow = (page - 1) * pageSize;
            //.Skip(excludedRow)
            var query = ls.OrderBy(c => c.id).Take(pageSize).ToList();
            return Json(new { query, total_count = ls.Count() }, JsonRequestBehavior.AllowGet);
        }
        public ActionResult Show(int headerId, int receiptPayTypeId, int PageIndex = 0, int ReceiptDetailId = 0)
        {
            int TotalRow;
            Icls_Receipt_Pay_TypeClient rePayTypeclient = new Icls_Receipt_Pay_TypeClient();
            var ReceiptPayTypeDetailLs = Utility.GetReceipt_Pay_TypeDetailLs().Where(d => d.Receipt_Pay_TypeID == 2).ToList();
            var otherPropertyLs = Utility.GetOtherPropertyLS(receiptPayTypeId);
            var selectedotherPropertyLs = new List<int>();
            var GetCashdeskLs = Utility.GetCashdeskLs();
            var db = new TreasuryEntities();
            db.Configuration.ProxyCreationEnabled = false;
            //Edit Mode
            if (headerId != 0)
            {
                //Icls_Reciept_HeaderClient RHclient = new Icls_Reciept_HeaderClient();
                Receipt_Detail_BreakPriceClient Detail_BreakPriceClient = new Receipt_Detail_BreakPriceClient();
                //Icls_Receipt_DetailClient RDetaislClient = new Icls_Receipt_DetailClient();
                Icls_SPsClient SpsClient = new Icls_SPsClient();
                Icls_ReferenceTypes_By_BehalfGroupClient RB = new Icls_ReferenceTypes_By_BehalfGroupClient();
                Icls_Reciept_OtherPropery_DetailClient otherPropertyClient = new Icls_Reciept_OtherPropery_DetailClient();
                Icls_Reciept_ReferenceTypes_ValueClient referenceTypes_ValueClient = new Icls_Reciept_ReferenceTypes_ValueClient();
                Icls_Reciept_OtherPropery_BY_Receipt_Pay_TypeDetailClient OtherPropery_BY_Receipt_Pay_TypeDetailClient = new Icls_Reciept_OtherPropery_BY_Receipt_Pay_TypeDetailClient();
                Icls_Receipt_Detail_PlanStateClient Planstateclient = new Icls_Receipt_Detail_PlanStateClient();
                Icls_BasicBanksClient BasicBankClient = new Icls_BasicBanksClient();
                //Icls_BankClient bankClient = new Icls_BankClient();

                Icls_CashDeskClient cashdeskClient = new Icls_CashDeskClient();
                //var selectedHeader = RHclient.SelectRow(headerId).SingleOrDefault();
                var selectedHeader = db.Reciept_Header.Find(headerId);
                //var DetailsLs = RDetaislClient.SelectBy_DynamicWhereClause("ReceiptDetailId=" +headerId , 1, 100, "ReceiptDetailId", "dec", true, out TotalRow).ToList();
                var DetailsLs = db.GetRecieptHeaderAndDetail_ByReceiptId(headerId).Where(d => d.ReceiptDetailId == ReceiptDetailId).ToList();
                var basicbank = new CSD.Treasury.Service.BasicBanksService.BasicBank();
                var bank = new CSD.Treasury.DataModel.Bank();
                if (receiptPayTypeId == 23 || receiptPayTypeId == 24)
                {
                    var item = DetailsLs.FirstOrDefault();
                    basicbank = BasicBankClient.SelectBy_DynamicWhereClause(" BasicBankId =" + item.BankID, 1, 10, "BasicBankId", "int", false, out TotalRow).SingleOrDefault();
                    //bank = bankClient.SelectBy_DynamicWhereClause("Bank_AccountNumber=\"" + DetailsLs.FirstOrDefault().Bank_AccountNo + "\"", 1, 10, "BankID", "int", false, out TotalRow).SingleOrDefault();
                    bank = db.Banks.Where(d => d.Bank_AccountNumber == item.Bank_AccountNo).SingleOrDefault();
                }

                Custom_Reciept_Header customHeader = new Custom_Reciept_Header()
                {
                    ReceiptPayTypeId = selectedHeader.RecieptTypeId,
                    ReceiptPayTypeTitle = Utility.GetReceipt_Pay_TypeDetailTitle(selectedHeader.RecieptTypeId),
                    ReceiptPayTypeDetailLs = ReceiptPayTypeDetailLs,
                    ReceiptDate = Utility.GetPersianDate(selectedHeader.ReceiptDate),
                    ReceiptId = selectedHeader.ReceiptId,
                    //Arzi = true,//!(selectedHeader.isRiali),
                    Arzi = !(selectedHeader.isRiali),
                    BranchLs = Utility.GetBranches(),
                    BranchId = selectedHeader.cmpBranchID,
                    basicbank = basicbank,
                    bank = bank,

                };

                List<Custom_Reciept_Details> CustomDetailsLs = new List<Custom_Reciept_Details>();
                foreach (var item in DetailsLs)
                {
                    var nerkh = 1;
                    if (item.CashDeskID != null)
                    {
                        var data = GetCashExchangeRate(item.CashDeskID.Value, item.ReceiptDate);
                        nerkh = data;
                    }





                    var BehalfGroupId = Utility.GetBehalfGroupBybehalfId(item.BehalfID).BehalfGroupID;
                    Custom_Reciept_Details obj = new Custom_Reciept_Details();
                    var PlanState = Planstateclient.SelectBy_DynamicWhereClause(" PlaningTypeID=1 and ReceiptDetailId=" + item.ReceiptDetailId, 1, 1000, "StateID", "dec", false, out TotalRow);
                    //var BehalfGroupAccount =
                    obj.RecieptTypeID = item.RecieptTypeId;
                    obj.PlaningTypeID = item.PlaningTypeID.Value;
                    obj.Cheque_Number = item.Cheque_Number;
                    if (item.CashDeskID != null)
                    {
                        obj.CashDeskID = item.CashDeskID.Value;
                    }
                    obj.nerkh = nerkh;
                    if (item.DueDate != null && item.DueDate.Trim() != "")
                    {
                        obj.DueDate = Utility.GetPersianDate(item.DueDate);
                    }
                    //obj.Unique_Account_No = item.Unique_Account_No;
                    //obj.Unique_Account_Title = Utility.GetUniqueAccount(item.Unique_Account_No);
                    obj.Cheque_OwnerName = item.Cheque_OwnerName;
                    obj.ChequePrice_Rial = item.ChequePrice_Rial;
                    obj.Bank_AccountNo = item.Bank_AccountNo;
                    if (item.BankID != null)
                    {
                        obj.BankID = item.BankID.Value;
                        obj.BankName = Utility.GetBasicBankInfo(item.BankID.Value);
                    }
                    
                    obj.Bank_BranchDesc = item.Bank_BranchDesc;
                    obj.Bank_BranchCode = item.Bank_BranchCode;
                    obj.ProvinceID = Utility.GetCity(item.CityID).ProvinceID;
                    obj.CityID = item.CityID;
                    obj.CityLs = Utility.GetCityLs(Utility.GetCity(item.CityID).ProvinceID);
                    //obj.CityName = Utility.GetCityName(item.CityID);
                    obj.ExchangeRate = item.ExchangeRate;
                    if (item.ExchangeAmount != null)
                    {
                        obj.ExchangeAmount = item.ExchangeAmount.Value;
                    }

                    obj.ExchangeTypeID = item.ExchangeTypeID;
                    obj.BehalfID = item.BehalfID;
                    obj.ReceiptDetailsid = item.ReceiptDetailId;
                    var BreakLs = new List<Service.Request_Payment_BreakPriceService.Receipt_Detail_BreakPrice>();
                    // Rahimi
                    //var q1 = Detail_BreakPriceClient.SelectBy_DynamicWhereClause("ReceiptDetailId=" + item.ReceiptDetailId, 1, 100, "BreakId", "dec", false, out TotalRow);
                    var q1 = db.Receipt_Detail_BreakPrice.Where(w => w.ReceiptDetailId == item.ReceiptDetailId).OrderByDescending(o => o.BreakId);
                    obj.BreakPriceLs = new List<DataModel.Receipt_Detail_BreakPrice>();// q1.ToList();
                    
                    //obj.BehalfName = Utility.GetBehalf(item.BehalfID);



                    var Refrencels = new List<Reference>();
                    var query1 = referenceTypes_ValueClient.SelectBy_DynamicWhereClause("ReceiptDetailId =" + item.ReceiptDetailId, 1, 50, "ReferenceTypeValueID", "int", false, out TotalRow).Select(q => new Reference() { Id = q.ReferenceID, Value = q.ReferenceValue, Title = Utility.GetReferenceTitleByRefrenceId(q.ReferenceID) }).ToList();
                    var query = RB.SelectBy_DynamicWhereClause("BehalfGroupID=" + BehalfGroupId, 1, 10000, "ReferenceID", "int", true, out TotalRow).Select(q => new Reference() { Id = q.ReferenceID, Value = "", Title = Utility.GetReferenceTitleByRefrenceId(q.ReferenceID) }).ToList();
                    var ls = query.Where(q => !query1.Select(d => d.Id).Contains(q.Id)).ToList();

                    Refrencels.AddRange(query1);
                    Refrencels.AddRange(ls);
                    obj.Refrences = Refrencels;
                    obj.BehalfLs = Utility.GetBehalfLs(BehalfGroupId);
                    obj.BehalfGroupId = BehalfGroupId;
                    obj.BehalfGroupTitle = Utility.GetBehalfGroupBybehalfId(item.BehalfID).BehalfGroupDescription;
                    ////obj.BehalfGroupAccount = Utility.Getaccounts(item.);
                    // obj.OtherProperyLS = otherPropertyClient.SelectBy_DynamicWhereClause("ReceiptDetailId=" + item.ReceiptDetailId, 1, 50, "Reciept_OtherPropery_DetailId", "int", false, out TotalRow).Select(q => q.Reciept_OtherProperyID).ToList();


                    var list = otherPropertyClient.SelectBy_DynamicWhereClause("ReceiptDetailId=" + item.ReceiptDetailId, 1, 50, "Reciept_OtherPropery_DetailId", "int", false, out TotalRow);
                    List<Custom_Reciept_OtherPropery_Detail> customLs = new List<Custom_Reciept_OtherPropery_Detail>();
                    var qq1 = Utility.GetOTherPeropertyDetails(receiptPayTypeId).Where(d => !list.Select(s => s.Reciept_OtherProperyID).Contains(d.Reciept_OtherProperyID));


                    Custom_Reciept_OtherPropery_Detail custom2 = new Custom_Reciept_OtherPropery_Detail();
                    //ساخت پراپرتی ها ی انتخاب نشده
                    foreach (var i in qq1)
                    {
                        Custom_Reciept_OtherPropery_Detail customItem = new Custom_Reciept_OtherPropery_Detail();
                        customItem.Description1_Lable = i.Description1_Lable;
                        customItem.Description2_Lable = i.Description2_Lable;
                        customItem.Description3_Lable = i.Description3_Lable;
                        customItem.Description4_Lable = i.Description4_Lable;
                        customItem.Description5_Lable = i.Description5_Lable;
                        customItem.Description1_Value = null;
                        customItem.Description2_Value = null;
                        customItem.Description3_Value = null;
                        customItem.Description4_Value = null;
                        customItem.Description5_Value = null;
                        customItem.Reciept_OtherProperyID = i.Reciept_OtherProperyID;
                        customLs.Add(customItem);
                    }

                    //ساخت پراپرتی ها ی انتخاب شده
                    foreach (var item1 in list.GroupBy(d => d.Reciept_OtherProperyID).ToList())
                    {
                        foreach (var item2 in item1)
                        {
                            Custom_Reciept_OtherPropery_Detail customItem = new Custom_Reciept_OtherPropery_Detail();
                            var s = OtherPropery_BY_Receipt_Pay_TypeDetailClient.SelectBy_DynamicWhereClause("Reciept_OtherProperyID=" + item2.Reciept_OtherProperyID + "and Receipt_Pay_TypeDetailID=" + receiptPayTypeId, 1, 100, "Reciept_OtherPropery_BY_Receipt_Pay_TypeDetailID", "int", false, out TotalRow).SingleOrDefault();

                            customItem.Description1_Lable = s.Description1_Lable;
                            customItem.Description2_Lable = s.Description2_Lable;
                            customItem.Description3_Lable = s.Description3_Lable;
                            customItem.Description4_Lable = s.Description4_Lable;
                            customItem.Description5_Lable = s.Description5_Lable;
                            customItem.Description1_Value = item2.description1_Value;
                            customItem.Description2_Value = item2.description2_Value;
                            customItem.Description3_Value = item2.description3_Value;
                            customItem.Description4_Value = item2.description3_Value;
                            customItem.Description5_Value = item2.description5_Value;
                            customItem.Reciept_OtherProperyID = item2.Reciept_OtherProperyID;

                            customLs.Add(customItem);
                        }


                    }
                    
                    obj.OtherPropertyDetailsList = customLs;

                    //obj.ProperySelected = otherPropertyClient.SelectBy_DynamicWhereClause("ReceiptDetailId=" + item.ReceiptDetailId, 1, 50, "Reciept_OtherPropery_DetailId", "int", false, out TotalRow).Select(q => q.Reciept_OtherProperyID).ToList();
                    obj.UniqueAccount = new UCItem() { Code = item.Unique_Account_No.Trim(), Name = item.Unique_Account_No + " - " + Utility.GetUniqueAccount(item.Unique_Account_No) };

                    if (PlanState != null && PlanState.Count() != 0)
                    {
                        var Unique_Account_No_Two = PlanState.SingleOrDefault().Unique_Account_No_Two;

                        obj.UniqueAccount2 = new UCItem() { Code = item.Unique_Account_No.Trim(), Name = Unique_Account_No_Two + " - " + Utility.GetUniqueAccount(Unique_Account_No_Two) };

                    }
                    obj.UniqueAccount2 = new UCItem() { Code = item.Unique_Account_No.Trim(), Name = "<div class='clearfix'>" + "<div class='pull-right pl15'>انتخاب</div><div class='pull-right'></div>" };
                    CustomDetailsLs.Add(obj);
                    selectedotherPropertyLs = otherPropertyClient.SelectBy_DynamicWhereClause("ReceiptDetailId=" + item.ReceiptDetailId, 1, 50, "Reciept_OtherPropery_DetailId", "int", false, out TotalRow).Select(d => d.Reciept_OtherProperyID).Distinct().ToList();
                    
                }





                List<Custom_OtherPeroperty> listProperty = new List<Custom_OtherPeroperty>();
                foreach (var item in otherPropertyLs)
                {
                    Custom_OtherPeroperty customItem = new Custom_OtherPeroperty();
                    if (selectedotherPropertyLs.Contains(item.Reciept_OtherProperyID))
                    {
                        customItem.Selected = true;
                    }
                    else
                    {
                        customItem.Selected = false;
                    }
                    customItem.Reciept_OtherProperyID = item.Reciept_OtherProperyID;
                    customItem.Reciept_OtherProperyDescription = item.Reciept_OtherProperyDescription;
                    listProperty.Add(customItem);
                }
                //rahimi

                var RecId = db.Receipt_Detail.SingleOrDefault(d => d.ReceiptId == headerId);
                var relatedBreakLS = db.Receipt_Detail_BreakPrice.SingleOrDefault(d => d.ReceiptDetailId == RecId.ReceiptDetailId);
                var CurExchangeRate= db.Receipt_Detail_BreakPrice.SingleOrDefault(d => d.ReceiptDetailId == RecId.ReceiptDetailId);
                //CustomDetailsLs.First().BreakPriceLs.Add(relatedBreakLS);
                if (relatedBreakLS != null)
                {
                    CustomDetailsLs.First().BreakPriceLs.Add(new DataModel.Receipt_Detail_BreakPrice
                    {
                        CashDeskId = (int)RecId.CashDeskID,
                        BreakId = relatedBreakLS.BreakId,
                        ExchangeTypeID = relatedBreakLS.ExchangeTypeID,
                        Exchange_Amount = relatedBreakLS.Exchange_Amount,
                        Price_Rial = relatedBreakLS.Price_Rial,
                        ReceiptDetailId = relatedBreakLS.ReceiptDetailId,
                        Exchange_Rate = RecId.ExchangeRate

                    });
                }
                var res = new
                {
                    Header = customHeader,
                    detailsLs = CustomDetailsLs,
                    AccountNumLs = Utility.v_accountsLS(),
                    ReferenceTypeLs = Utility.GetReferenceTypeLs(),
                    ProvinceLs = Utility.GetProvinceLs(),
                    Cashdesk = GetCashdeskLs,
                    CityLs = "",
                    BankLs = Utility.GetBasicBanks(),
                    Accountls = Utility.v_accountsLS(),
                    ExchangeTypeList = Utility.GetExchangeLs(),
                    BehalfGroupLs = Utility.GetBehalfsGroupLs().Where(d => d.Receipt_Pay_TypeID == int.Parse(ConfigurationManager.AppSettings["Receipt_Type_ID"])),
                    BehalfLs = "",
                    OtherProperty = listProperty,
                    Banks = Utility.GetBanks()
                    //OtherPropertyDetails = Utility.GetOTherPeropertyDetails(receiptPayTypeId)
                };
                return Json( res , JsonRequestBehavior.AllowGet);

            }

            //Create Mode
            //Create OtherPeropertyList with labels and Values
            var listOtherPropery_BY_Receipt_Pay_TypeDetail = Utility.GetOTherPeropertyDetails(receiptPayTypeId);

            List<Custom_Reciept_OtherPropery_Detail> customLss = new List<Custom_Reciept_OtherPropery_Detail>();
            Custom_Reciept_OtherPropery_Detail custom = new Custom_Reciept_OtherPropery_Detail();
            foreach (var item in listOtherPropery_BY_Receipt_Pay_TypeDetail)
            {
                Custom_Reciept_OtherPropery_Detail customItem = new Custom_Reciept_OtherPropery_Detail();

                customItem.Description1_Lable = item.Description1_Lable;
                customItem.Description2_Lable = item.Description2_Lable;
                customItem.Description3_Lable = item.Description3_Lable;
                customItem.Description4_Lable = item.Description4_Lable;
                customItem.Description5_Lable = item.Description5_Lable;
                customItem.Description1_Value = null;
                customItem.Description2_Value = null;
                customItem.Description3_Value = null;
                customItem.Description4_Value = null;
                customItem.Description5_Value = null;
                customItem.Reciept_OtherProperyID = item.Reciept_OtherProperyID;
                customLss.Add(customItem);
            }

            customLss.Add(custom);
            var Banks = Utility.GetBanks();
            var BankLs = Utility.GetBasicBanks();
            if (receiptPayTypeId == 23 || receiptPayTypeId == 24)
            {
                BankLs = BankLs.Where(d => Banks.Select(t => t.BasicBankID).Contains(d.BasicBankId)).ToList();
            }



            return Json(new
            {
                Header = new Custom_Reciept_Header() { ReceiptPayTypeDetailLs = ReceiptPayTypeDetailLs, BranchLs = Utility.GetBranches() },
                AccountNumLs = Utility.v_accountsLS(),
                ProvinceLs = Utility.GetProvinceLs(),
                CityLs = "",
                ExchangeTypeLs = "",
                detailsLs = new List<Custom_Reciept_Details>(),
                BankLs = BankLs,
                Accountls = Utility.v_accountsLS(),
                ReferenceTypeLs = Utility.GetReferenceTypeLs(),
                Cashdesk = GetCashdeskLs,
                ExchangeTypeList = Utility.GetExchangeLs(),
                BehalfGroupLs = Utility.GetBehalfsGroupLs().Where(d => d.Receipt_Pay_TypeID == int.Parse(ConfigurationManager.AppSettings["Receipt_Type_ID"])),
                BehalfLs = "",
                OtherProperty = otherPropertyLs,
                Banks = Banks,
                //OtherPropertyDetailsList = Utility.GetOTherPeropertyDetails(receiptPayTypeId)
                OtherPropertyDetailsList = customLss
            }, JsonRequestBehavior.AllowGet);
           
        }
        //لیست شهرها به صورت Json
        public JsonResult GetCityLs(int provinceID)
        {
            string whereclouse = "provinceID=" + provinceID;
            int totalRows;
            Icls_CityClient obj = new Icls_CityClient();
            List<Service.CityService.City> ls = new List<Service.CityService.City>();
            var query = obj.SelectBy_DynamicWhereClause(whereclouse, 1, 100, "CityID", "int", false, out totalRows).ToList();
            return Json(query, JsonRequestBehavior.AllowGet);
        }
        //نوع ارز براساس صندوقی که انتخاب شده است
        public JsonResult GetCashExchange(int CashDeskID, string receiptDate)
        {
            var db = new TreasuryEntities();
            var ExchangeTypeValues = "";
            var ExchangeTypeDescription = "";
            var ExchangeTypeValueId = 0;
            receiptDate = receiptDate.Replace("/", "");
            var nerkh = 1;
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
                        ExchangeTypeValues = "استاندارد";
                        ExchangeTypeValueId = 1;
                        var ExchangeStandard = db.ExchangeStandards.SingleOrDefault(d => d.ExchangeTypeID == Cashdesk.ExchangeTypeID && d.FiscalYear == FiscalYear);
                        if (ExchangeStandard != null)
                        {
                            nerkh = ExchangeStandard.Price;
                        }
                        break;
                    case 2:
                        ExchangeTypeValues = "رسمی";
                        ExchangeTypeValueId = 2;
                        var ExchangeRate = db.ExchangeRates.SingleOrDefault(d => d.ExchangeTypeID == Cashdesk.ExchangeTypeID && d.ExchangeDateRate == receiptDate);
                        if (ExchangeRate != null)
                        {
                            nerkh = ExchangeRate.FormalPrice;
                        }
                        break;
                    case 3:
                        ExchangeTypeValues = "غیررسمی";
                        ExchangeTypeValueId = 3;
                        var ExchangeRate1 = db.ExchangeRates.SingleOrDefault(d => d.ExchangeTypeID == Cashdesk.ExchangeTypeID && d.ExchangeDateRate == receiptDate);
                        if (ExchangeRate1 != null)
                        {
                            nerkh = ExchangeRate1.InformalPrice;
                        }
                        break;
                    case 4:
                        ExchangeTypeValues = "واقعی";
                        ExchangeTypeValueId = 4;
                        break;
                    default:
                        ExchangeTypeValues = "ریال";
                        ExchangeTypeValueId = 1;
                        break;
                }
            }

           

                var obj = new
                {
                    ExchangeTypeDescription = ExchangeTypeDescription,
                    ExchangeTypeValues = ExchangeTypeValues,
                    ExchangeTypeValueId = ExchangeTypeValueId,
                    nerkh = nerkh,
                };
                return Json(obj, JsonRequestBehavior.AllowGet);
            
            
        }
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
        //لیست از گروه بابت به صورت Json
        public JsonResult GetBehlafGroupLs(bool IsReceipt = false)
        {
            string whereclouse;
            if (IsReceipt)
                whereclouse = "Receipt_Pay_TypeID = 2";
            else
                whereclouse = "Receipt_Pay_TypeID = 1";
            int totalRows;
            Icls_BehalfGroupClient obj = new Icls_BehalfGroupClient();
            List<Service.BehalfGroupService.BehalfGroup> ls = new List<Service.BehalfGroupService.BehalfGroup>();
            var query = obj.SelectBy_DynamicWhereClause(whereclouse, 1, 100, "BehalfGroupID", "int", false, out totalRows).ToList();
            return Json(query, JsonRequestBehavior.AllowGet);
        }
        //لیستی از بابت ها
        public JsonResult GetBehalfLs(int BehalfGroupId)
        {
            int totalRows;
            Service.BehalfsService.Icls_BehalfsClient obj = new Service.BehalfsService.Icls_BehalfsClient();
            Service.BehalfGroupService.Icls_BehalfGroupClient behalfGroupClient = new Service.BehalfGroupService.Icls_BehalfGroupClient();
            var behalfGroup = behalfGroupClient.SelectRow(BehalfGroupId).SingleOrDefault();
            var behalfGroupTiltle = Utility.Getaccounts(behalfGroup.BehalfGroup_Account) + "/" + behalfGroup.BehalfGroup_Account;

            List<Service.BehalfsService.Behalf> ls = new List<Service.BehalfsService.Behalf>();
            string whereclouse = "1=1";
            whereclouse = " BehalfGroupId=" + BehalfGroupId.ToString();
            var query = obj.SelectBy_DynamicWhereClause(whereclouse, 1, 100, "BehalfGroupID", "int", true, out totalRows);
            ls.AddRange(query);

            //Refrence by BehalfGroupId
            Icls_ReferenceTypes_By_BehalfGroupClient RTBG = new Icls_ReferenceTypes_By_BehalfGroupClient();
            var Ls = RTBG.SelectBy_DynamicWhereClause("BehalfGroupID = " + BehalfGroupId, 1, 10000, "ReferenceID", "int", true, out totalRows).ToList();
            //List<Service.ReferenceTypeService.ReferenceType> lsRefrence = new List<Service.ReferenceTypeService.ReferenceType>();
            var LSRefrences = new List<Reference>();
            foreach (var item in Ls)
            {
                //Service.ReferenceTypeService.ReferenceType objRefrence = new Service.ReferenceTypeService.ReferenceType();
                //objRefrence.referenceTypeID = item.ReferenceID;
                //objRefrence.referenceTypeDescription = Utility.GetReferenceTitle(item.referenceTypeID);
                Reference R = new Reference();
                R.Id = item.ReferenceID;
                R.Title = Utility.GetReferenceTitle(item.referenceTypeID);
                LSRefrences.Add(R);
            }
            List<Service.ReferenceTypes_By_BehalfGroupService.ReferenceTypes_By_BehalfGroup> refList = new List<Service.ReferenceTypes_By_BehalfGroupService.ReferenceTypes_By_BehalfGroup>();
            return Json(new { BehalfList = ls, Refrences = LSRefrences, BehalfGroup_Account = behalfGroupTiltle }, JsonRequestBehavior.AllowGet);

        }
        public ActionResult getReceiveCashUC()
        {
            var dc = new TreasuryEntities();
            //نوع ذینفع  
            var unique_account_type = dc.v_unique_account_type.ToList();
            unique_account_type.Add(new v_unique_account_type { Code = 0, Description = Resourse.Resources.Others });
            ViewBag.unique_account_type = unique_account_type;
            return PartialView();
        }


        #region CreateAndEdit
        /// <summary>
        /// insert in Receipt_Detail table and Receipt_Header and Receipt_Detail_PlanState
        /// </summary>
        /// <param name="HeaderId"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult Save(Custom_Reciept Obj)
        {
            var PlaningTypeIDTaviz = false;
            decimal StateId = 0;
            using (TreasuryEntities db = new TreasuryEntities())
            {
                var message = "";
                try
                {

                    var receiptplan = new CSD.Treasury.DataModel.Receipt_Detail_PlanState();
                    var headerId = 0;
                    var ReceiptDetails = new DataModel.Receipt_Detail();
                    //Receipt Header
                    if (Obj.HeaderId != 0)
                    {
                        //Edit Header             
                        var query = db.Reciept_Header.FirstOrDefault(d => d.ReceiptId == Obj.HeaderId);
                        query.isRiali = !(Obj.isRiali);
                        query.ReceiptDate = Obj.ReceiptDate.Replace("/", String.Empty);
                    }
                    else if (Obj.HeaderId == 0)
                    {

                        //Add Reciept_Header
                        DataModel.Reciept_Header header = new DataModel.Reciept_Header();
                        header.ReceiptDate = Obj.ReceiptDate.Replace("/", String.Empty);
                        header.RecieptTypeId = Obj.ReceiptPayTypeId;
                        header.isRiali = !(Obj.isRiali);
                        header.cmpBranchID = User.BranchId;
                        header.ReferenceReceiptId = 0;
                        //header.ReferenceReceiptId = Obj.CashDeskID;
                        db.Reciept_Header.Add(header);
                        db.SaveChanges();
                        headerId = int.Parse(header.ReceiptId.ToString());
                        //درصورتی که میخواهد چک را تعویض کند 
                        if (Obj.ReceiptDetailId != 0)
                        {
                            ReceiptDetails = db.Receipt_Detail.FirstOrDefault(d => d.ReceiptDetailId == Obj.ReceiptDetailId);
                            var account_no = ReceiptDetails.Behalf.BehalfGroup.BehalfGroup_Account;
                            //var Rh = db.Reciept_Header.FirstOrDefault(d => d.ReceiptId == header.ReceiptId);
                            if (ReceiptDetails != null && header != null)
                            {
                                PlaningTypeIDTaviz = true;
                                //همین هدری که الان ثبت شد باید والدش را شماره دریافت چکی که تعویض کردیم قراردهیم
                                header.ReferenceReceiptId = Obj.ReceiptDetailId;
                                var persianDate = Utility.ToPersianDate(DateTime.Now);
                                receiptplan = new CSD.Treasury.DataModel.Receipt_Detail_PlanState()
                                {
                                    ReceiptDetailId = Obj.ReceiptDetailId,
                                    PlaningTypeID = 21,
                                    StateDate = Obj.ReceiptDate.Replace("/", String.Empty),
                                    StateTime = DateTime.Now.TimeOfDay,
                                    Account_No = account_no,
                                    Unique_Account_No = ReceiptDetails.Unique_Account_No,
                                    UserID = User.Id,
                                    HostName = System.Net.Dns.GetHostName(),
                                };
                                db.Receipt_Detail_PlanState.Add(receiptplan);
                            }
                        }
                    }
                    //Receipt Details
                    foreach (var item in Obj.DetailsLs)
                    {
                        //اسناد نزد صندوق
                        var PlaningTypeID = 1;
                        //درصورتی که پول نقد به صندوق وارد شود باید وضعیت موجودی نقد نزد صندوق ثبت گردد
                        if (Obj.ReceiptPayTypeId == 22)
                        {
                            PlaningTypeID = 17;
                        }
                        //دریافت چک و سفته انتظامی
                        if (Obj.ReceiptPayTypeId == 28 || Obj.ReceiptPayTypeId == 21)
                        {
                            PlaningTypeID = 18;
                        }
                        var persianDate = Obj.ReceiptDate.Replace("/", String.Empty);
                        var PlaningType = db.PlaningTypes.FirstOrDefault(d => d.PlaningTypeID == PlaningTypeID);
                        var AccountNo = PlaningType.Defualt_AccountNo;
                        var UniqueAccountNO = PlaningType.Defualt_UniqueAccountNo;
                        var Unique_Account_No_Two = item.Unique_Account_no_Two;
                        //درصورتی که واریز نقدی به بانک یا عابر بانک باشد معین و تفصیل را از بانکی که واریز به آن صورت کرفته انتخاب می کنیم
                        //درغیر این صورت از PlaningType
                        if (Obj.ReceiptPayTypeId == 23 || Obj.ReceiptPayTypeId == 24)
                        {
                            PlaningTypeID = 19;
                            if (Obj.ReceiptPayTypeId == 23)
                            {
                                PlaningTypeID = 19;
                            }
                            var bank = db.Banks.FirstOrDefault(d => d.BankID == item.mainBankID.Value);
                            AccountNo = bank.Account_No;
                            UniqueAccountNO = bank.Unique_Account_No;
                            item.Bank_AccountNo = bank.Bank_AccountNumber;
                            item.Bank_BranchDesc = bank.BranchName;
                            item.Bank_BranchCode = bank.BranchCode;
                            
                            
                        }
                        
                        //Insert MOde
                        if (Obj.HeaderId == 0)
                        {
                            //Add receipt_Detail
                            DataModel.Receipt_Detail details = new DataModel.Receipt_Detail();
                            //int ReceiptDetailId = 0;
                            details.ReceiptId = headerId;
                            PersianCalendar pc = new PersianCalendar();
                            var Month = pc.GetMonth(DateTime.Now);
                            details.FiscalYear = pc.GetYear(DateTime.Now).ToString();
                            if (Month >= 10 && ConfigurationManager.AppSettings["YearPlus"] == "True")
                            {
                                details.FiscalYear = (pc.GetYear(DateTime.Now) + 1).ToString();
                            }
                            details.UserID = User.Id;
                            if (!string.IsNullOrEmpty(item.Cheque_OwnerName))
                            {
                                details.Cheque_OwnerName = item.Cheque_OwnerName;
                            }
                            if (!string.IsNullOrEmpty(item.DueDate))
                            {
                                details.DueDate = item.DueDate.Replace("/", String.Empty);
                            }
                            else
                            {
                                details.DueDate = "";
                            }
                            //درصورتی که درحالت تعویض چک باشد شماره تفصیل همان شماره تفضیل چک قبلی می خورد
                            if (Obj.ReceiptDetailId != 0)
                            {
                                details.Unique_Account_No = ReceiptDetails.Unique_Account_No;
                            }
                            else
                            {
                                details.Unique_Account_No = item.Unique_Account_no;
                            }
                            details.ChequePrice_Rial = item.ChequePrice_Rial;


                            details.BehalfID = item.BehalfID;
                            if (item.BankID != null)
                            {
                                details.BankID = item.BankID;
                            }
                            if (!string.IsNullOrEmpty(item.Bank_AccountNo))
                            {
                                details.Bank_AccountNo = item.Bank_AccountNo;
                            }
                            if (!string.IsNullOrEmpty(item.Bank_BranchDesc))
                            {
                                details.Bank_BranchDesc = item.Bank_BranchDesc;
                            }
                            if (!string.IsNullOrEmpty(item.Bank_BranchCode))
                            {
                                details.Bank_BranchCode = item.Bank_BranchCode;
                            }
                            if (item.CityID != null)
                            {
                                details.CityID = item.CityID.Value;
                            }
                            else
                            {
                                details.CityID = int.Parse(ConfigurationManager.AppSettings["CityId"]);
                            }
                            if (!string.IsNullOrEmpty(item.Cheque_Number))
                            {
                                details.Cheque_Number = item.Cheque_Number;
                            }
                            else
                            {
                                details.Cheque_Number = " ";
                            }
                            if (Obj.isRiali)
                            {
                                //details.CashDeskID = 21;
                                details.CashDeskID = item.CashDeskID;
                                details.ExchangeRate = item.ExchangeRate.Value;
                                details.ExchangeAmount = item.ExchangeAmount;
                                details.ChequePrice_Rial = item.ExchangeRate.Value * item.ExchangeAmount;
                            }
                            //Added By Rahimi 14020213
                            if ((item.BreakPriceLs !=null) &&(item.BreakPriceLs.Count>0) && (Obj.isRiali))
                            {
                                //details.CashDeskID = 21;
                                details.CashDeskID = item.BreakPriceLs[0].CashDeskId;
                                details.ExchangeRate = item.BreakPriceLs[0].Exchange_Rate;
                                details.ExchangeAmount = item.BreakPriceLs[0].Exchange_Amount;
                                details.ChequePrice_Rial = item.BreakPriceLs[0].Exchange_Rate * item.BreakPriceLs[0].Exchange_Amount;
                            }
                            //Added By Rahimi 14020213


                            db.Receipt_Detail.Add(details);
                            //اضافه نمودن مبالغ خورد شده مبلغ اصلی
                            if (item.Refrences != null)
                            {
                                if (item.Refrences.Any())
                                {
                                    DataModel.Reciept_ReferenceTypes_Value RefrenceOBJ = new DataModel.Reciept_ReferenceTypes_Value();
                                    //ذخیره عطف ها
                                    foreach (var itemRefrence in item.Refrences)
                                    {
                                        if (itemRefrence.Value != null)
                                        {

                                            RefrenceOBJ.ReferenceID = itemRefrence.Id;
                                            RefrenceOBJ.ReferenceValue = itemRefrence.Value;


                                        }
                                    }
                                    details.Reciept_ReferenceTypes_Value.Add(RefrenceOBJ);
                                }

                            }
                            if (item.BreakPriceLs != null)
                            {
                                if (item.BreakPriceLs.Any() && item.BreakPriceLs.Where(d => d.Exchange_Amount != 0 && d.ExchangeTypeID != 0 && d.Exchange_Rate != 0).Count() != 0)
                                {
                                    //Add Receipt_Detail_BreakPrice
                                    foreach (var node in item.BreakPriceLs)
                                    {
                                        item.CashDeskID = node.CashDeskId;
                                        node.Price_Rial = node.Exchange_Amount * node.Exchange_Rate;
                                        details.Receipt_Detail_BreakPrice.Add(node);
                                    }
                                }
                            }

                            db.SaveChanges();
                            if (receiptplan != null)
                            {
                                StateId = receiptplan.StateID;
                            }

                            //ثبت وضعیت 
                            CSD.Treasury.DataModel.Receipt_Detail_PlanState planStateObj = new CSD.Treasury.DataModel.Receipt_Detail_PlanState()
                            {
                                StateDate = persianDate,
                                StateTime = DateTime.Now.TimeOfDay,
                                ReceiptDetailId = details.ReceiptDetailId,
                                PlaningTypeID = PlaningTypeID,
                                Account_No = AccountNo,
                                Unique_Account_No = UniqueAccountNO,
                                Unique_Account_No_Two = Unique_Account_No_Two,
                                //Account_No = ConfigurationManager.AppSettings["Account_No"],
                                //Unique_Account_No = ConfigurationManager.AppSettings["Unique_Account_No"],
                                UserID = User.Id,
                                HostName = System.Net.Dns.GetHostName(),

                            };
                            if (Obj.ReceiptPayTypeId == 23 || Obj.ReceiptPayTypeId == 24)
                            {
                                planStateObj.BankID = item.mainBankID;
                            }
                            details.Receipt_Detail_PlanState.Add(planStateObj);
                            var selectedLs = item.OthersPropertyLs;
                            if (item.OtherPropertyDetailsList != null)
                            {
                                if (item.OtherPropertyDetailsList.Any())
                                {
                                    foreach (var item1 in item.OtherPropertyDetailsList)
                                    {
                                        if (selectedLs != null)
                                        {
                                            if (selectedLs.Contains(item1.Reciept_OtherProperyID))
                                            {
                                                DataModel.Reciept_OtherPropery_Detail OtherDetails = new DataModel.Reciept_OtherPropery_Detail();
                                                OtherDetails.description1_Value = item1.Description1_Value;
                                                OtherDetails.description2_Value = item1.Description2_Value;
                                                OtherDetails.description3_Value = item1.Description3_Value;
                                                OtherDetails.description4_Value = item1.Description4_Value;
                                                OtherDetails.description5_Value = item1.Description5_Value;
                                                OtherDetails.Reciept_OtherProperyID = item1.Reciept_OtherProperyID;
                                                OtherDetails.ReceiptDetailId = details.ReceiptDetailId;
                                                db.Reciept_OtherPropery_Detail.Add(OtherDetails);
                                            }
                                        }
                                    }
                                }

                            }

                        }

                        //Edit Mode
                        if (Obj.HeaderId != 0)
                        {
                            var selectedDetails = db.Receipt_Detail.FirstOrDefault(d => d.ReceiptDetailId == item.ReceiptDetailsid);
                            var bank2 = db.Banks.FirstOrDefault(d => d.BankID == item.mainBankID.Value);
                            if (selectedDetails.Reciept_Header != null)
                            {
                                if (Utility.CanEditReceipt(selectedDetails.ReceiptDetailId))
                                {
                                    //darsorti ke ba yek header chandin details sabt karde bashad 
                                    //hameye planstateha edit mishavad
                                    var Receipt_DetailLs = db.Receipt_Detail.Where(d => d.ReceiptId == selectedDetails.Reciept_Header.ReceiptId);
                                    foreach (var rd in Receipt_DetailLs)
                                    {
                                        var newdate = Obj.ReceiptDate.Replace("/", String.Empty);

                                        var plan = rd.Receipt_Detail_PlanState.SingleOrDefault(d => d.ReceiptDetailId == rd.ReceiptDetailId);
                                        plan.StateDate = newdate;
                                        var voucherReceiptLs = rd.Voucher_Row_Reciept.Where(d => d.ReceiptDetailId == rd.ReceiptDetailId);

                                        foreach (var vr in voucherReceiptLs)
                                        {
                                            vr.StateDate = Obj.ReceiptDate.Replace("/", String.Empty);
                                        }

                                    }
                                }
                                if (item.Cheque_OwnerName != null)
                                {
                                    selectedDetails.Cheque_OwnerName = item.Cheque_OwnerName;
                                }
                                if (item.DueDate != null)
                                {
                                    selectedDetails.DueDate = item.DueDate.Replace("/", String.Empty);
                                }
                                selectedDetails.Unique_Account_No = item.Unique_Account_no;

                                selectedDetails.ChequePrice_Rial = item.ChequePrice_Rial;
                                if (item.ExchangeAmount != 0)
                                {
                                    selectedDetails.ExchangeAmount = item.ExchangeAmount;
                                    selectedDetails.CashDeskID = item.CashDeskID;
                                    selectedDetails.ExchangeRate = item.ExchangeRate.Value;
                                    selectedDetails.ChequePrice_Rial = item.ExchangeRate.Value * item.ExchangeAmount;

                                }

                                //Added By Rahimi 14020213
                                if ((item.BreakPriceLs != null) && (item.BreakPriceLs.Count > 0) && (Obj.isRiali))
                                {
                                    //details.CashDeskID = 21;
                                    selectedDetails.CashDeskID = item.BreakPriceLs[0].CashDeskId;
                                    selectedDetails.ExchangeRate = item.BreakPriceLs[0].Exchange_Rate;
                                    selectedDetails.ExchangeAmount = item.BreakPriceLs[0].Exchange_Amount;
                                    selectedDetails.ChequePrice_Rial = item.BreakPriceLs[0].Exchange_Rate * item.BreakPriceLs[0].Exchange_Amount;
                                }
                                //Added By Rahimi 14020213

                                selectedDetails.BehalfID = item.BehalfID;
                                if (item.BankID != null)
                                {
                                    selectedDetails.BankID = item.BankID;
                                }

                                if (item.Bank_AccountNo != null)
                                {
                                    selectedDetails.Bank_AccountNo = item.Bank_AccountNo;
                                }

                                if (item.Bank_BranchDesc != null)
                                {
                                    selectedDetails.Bank_BranchDesc = item.Bank_BranchDesc;
                                }

                                if (item.Bank_BranchCode != null)
                                {
                                    selectedDetails.Bank_BranchCode = item.Bank_BranchCode;
                                }

                                if (item.CityID != null)
                                {
                                    selectedDetails.CityID = item.CityID.Value;
                                }
                                else
                                {
                                    selectedDetails.CityID = int.Parse(ConfigurationManager.AppSettings["CityId"]);
                                }
                                if (item.Cheque_Number != null)
                                {
                                    selectedDetails.Cheque_Number = item.Cheque_Number;
                                }
                                if (item.CashDeskID != 0)
                                {
                                    selectedDetails.CashDeskID = item.CashDeskID;
                                }

                                var Detail_BreakPriceLs = db.Receipt_Detail_BreakPrice.Where(d => d.ReceiptDetailId == item.ReceiptDetailsid);
                                db.Receipt_Detail_BreakPrice.RemoveRange(Detail_BreakPriceLs);

                                if (item.BreakPriceLs != null)
                                {
                                    if (item.BreakPriceLs.Any() && item.BreakPriceLs.Where(d => d.Exchange_Amount != 0 && d.ExchangeTypeID != 0 && d.Exchange_Rate != 0).Count() != 0)
                                    {
                                        //Add Receipt_Detail_BreakPrice
                                        foreach (var node in item.BreakPriceLs)
                                        {
                                            node.Price_Rial = node.Exchange_Amount * node.Exchange_Rate;
                                            node.ReceiptDetailId = item.ReceiptDetailsid;
                                            selectedDetails.Receipt_Detail_BreakPrice.Add(node);
                                            //db.Receipt_Detail_BreakPrice.Add(node);
                                        }
                                    }
                                }
                                var otherPropertyDetails = db.Reciept_OtherPropery_Detail.Where(d => d.ReceiptDetailId == item.ReceiptDetailsid);

                                if (otherPropertyDetails.Any())
                                {
                                    db.Reciept_OtherPropery_Detail.RemoveRange(otherPropertyDetails);
                                }
                                var RefrenceLs = db.Reciept_ReferenceTypes_Value.Where(d => d.ReceiptDetailId == item.ReceiptDetailsid);
                                if (RefrenceLs.Any())
                                {
                                    db.Reciept_ReferenceTypes_Value.RemoveRange(RefrenceLs);
                                }
                                if (item.Refrences != null)
                                {
                                    if (item.Refrences.Any())
                                    {
                                        //ذخیره عطف ها
                                        foreach (var itemRefrence in item.Refrences)
                                        {
                                            if (itemRefrence.Value != null)
                                            {
                                                DataModel.Reciept_ReferenceTypes_Value RefrenceOBJ = new DataModel.Reciept_ReferenceTypes_Value();
                                                RefrenceOBJ.ReferenceID = itemRefrence.Id;
                                                RefrenceOBJ.ReferenceValue = itemRefrence.Value;
                                                selectedDetails.Reciept_ReferenceTypes_Value.Add(RefrenceOBJ);
                                                //RefrenceOBJ.ReceiptDetailId = item.ReceiptDetailsid;
                                                //db.Reciept_ReferenceTypes_Value.Add(RefrenceOBJ);
                                            }
                                        }
                                    }
                                }

                                var selectedLs = item.OthersPropertyLs;
                                if (item.OtherPropertyDetailsList != null)
                                {
                                    if (item.OtherPropertyDetailsList.Any())
                                    {
                                        foreach (var item1 in item.OtherPropertyDetailsList)
                                        {
                                            if (selectedLs != null)
                                            {
                                                if (selectedLs.Contains(item1.Reciept_OtherProperyID))
                                                {
                                                    DataModel.Reciept_OtherPropery_Detail OtherDetails = new DataModel.Reciept_OtherPropery_Detail();
                                                    OtherDetails.description1_Value = item1.Description1_Value;
                                                    OtherDetails.description2_Value = item1.Description2_Value;
                                                    OtherDetails.description3_Value = item1.Description3_Value;
                                                    OtherDetails.description4_Value = item1.Description4_Value;
                                                    OtherDetails.description5_Value = item1.Description5_Value;
                                                    OtherDetails.Reciept_OtherProperyID = item1.Reciept_OtherProperyID;
                                                    //OtherDetails.ReceiptDetailId = item.ReceiptDetailsid;
                                                    //db.Reciept_OtherPropery_Detail.Add(OtherDetails);

                                                }
                                            }
                                        }
                                    }
                                }

                            }


                        }
                    }

                    db.SaveChanges();
                    message = "ثبت با موفقیت انجام شد";
                    return JsonMessage(true, message, null, false);

                }
                catch (Exception ex)
                {

                    try
                    {
                        if (PlaningTypeIDTaviz)
                            db.DelTavizReceipt_Detail_PlanStates(Obj.ReceiptDetailId, StateId);
                    }
                    catch (Exception ex1)
                    {

                        throw;
                    }
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
        }

        #endregion

        #region ShowReceiveDetails
        public ActionResult ShowReceiveDetails(int ReceiptId)
        {
            int TotalRow;
            string where = "ReceiptId = " + ReceiptId;
            var db = new TreasuryEntities();
            //Icls_Receipt_DetailClient redetailsClient = new Icls_Receipt_DetailClient();
            //var DetailsLs = redetailsClient.SelectBy_DynamicWhereClause(where, 1, 100, "ReceiptDetailId", "dec", false, out TotalRow);
            var DetailsLs = db.Receipt_Detail;
            return PartialView(DetailsLs);
        }
        #endregion


        /// <summary>
        /// برنامه ریزی
        /// </summary>
        /// <returns></returns>
        #region ReceiptChequeAssignment 
        public ActionResult ReceiptList(ReceiptSearch SearchObj)
        {

            ViewBag.State = SearchObj.State;

            return View(SearchObj);
        }
        public ActionResult PlannedReceiptList(ReceiptSearch SearchObj)
        {
            ViewBag.State = SearchObj.State;
            return View(SearchObj);
        }
        public ActionResult ShowReceipt(ReceiptSearch SearchObj)
        {
            Icls_SPsClient spsClient = new Icls_SPsClient();
            Icls_PlaningTypeClient PlaningTypeClient = new Icls_PlaningTypeClient();
            Icls_Receipt_Detail_PlanStateClient ReceiptDetailsPlanClinet = new Icls_Receipt_Detail_PlanStateClient();
            Icls_CashiersClient CashierClient = new Icls_CashiersClient();
            //Icls_BankClient BankClient = new Icls_BankClient();
            //var db = new TreasuryEntities();
            var ReceiptPayTypeDetailLs = Utility.GetReceipt_Pay_TypeDetailLs().Where(d => d.Receipt_Pay_TypeID == 2).ToList();
            int TotalRow;
            //براساس نوع دریافت فیلتر میگردد
            var ReceiptLs = spsClient.GetRecieptHeaderAndDetail_ByRecieptTypeId(SearchObj.RecieptTypeId).ToList();
            //چک های سررسید شده نزد صندوق
            if (SearchObj.State == -20 || SearchObj.State == -21)
            {
                var persianDate = Utility.ToPersianDate(DateTime.Now);
                ReceiptLs = ReceiptLs.Where(d =>d.ExchangeTypeID == 0 && ((( d.PlaningTypeID == 1 || d.PlaningTypeID == 20 ||  d.PlaningTypeID == 16 || d.PlaningTypeID == 9 || d.PlaningTypeID == 7) && d.DueDate.CompareTo(persianDate) <= 0) || d.PlaningTypeID == 7)).ToList();
            }
            //چک های مدت دار نزد صندوق
            if (SearchObj.State == -22)
            {
                var persianDate = Utility.ToPersianDate(DateTime.Now);
                //d.PlaningTypeID == 1 || d.PlaningTypeID == 9 || d.PlaningTypeID == 20
                ReceiptLs = ReceiptLs.Where(d => (d.PlaningTypeID == 2) && d.RecieptTypeId == 19 && d.DueDate.CompareTo(persianDate) > 0).ToList();
            }
            //براساس وضعیت فیلتر میشود
            if (SearchObj.State == 1 || SearchObj.State == -1 || SearchObj.State == -3 || SearchObj.State == -4 || SearchObj.State == -5 || SearchObj.State == -6 || SearchObj.State == -10 || SearchObj.State == -18)
            {
                //نزد صندوق
                ReceiptLs = ReceiptLs.Where(d => (d.PlaningTypeID == 1 || d.PlaningTypeID == 9 || d.PlaningTypeID == 7 || d.PlaningTypeID == 16 || d.PlaningTypeID == 20 || d.PlaningTypeID == 21 || d.PlaningTypeID == 12  )).ToList();
            }
            else if (SearchObj.State == -2000)
            {
                //|| d.PlaningTypeID == 24 || d.PlaningTypeID == 26
                //واگذار به بانک
                ReceiptLs = ReceiptLs.Where(d => d.PlaningTypeID == 2 && d.ExchangeTypeID == 0).ToList();
            }
            else if (SearchObj.State == 2 || SearchObj.State == -2)
            {
                //|| d.PlaningTypeID == 24 || d.PlaningTypeID == 26
                //واگذار به بانک
                ReceiptLs = ReceiptLs.Where(d => d.PlaningTypeID == 2).ToList();
            }
            else if (SearchObj.State == 3)
            {
                //وصول مستفیم - نقد
                ReceiptLs = ReceiptLs.Where(d => d.PlaningTypeID == 3).ToList();
            }
            else if (SearchObj.State == 4)
            {
                //واریز به بانک
                ReceiptLs = ReceiptLs.Where(d => d.PlaningTypeID == 4).ToList();
            }
            else if (SearchObj.State == 6)
            {
                //عودت به صندوق
                ReceiptLs = ReceiptLs.Where(d => d.PlaningTypeID == 6).ToList();
            }
            else if (SearchObj.State == 7)
            {
                //تبدیل چک انتظامی به دریافتنی
                ReceiptLs = ReceiptLs.Where(d => d.PlaningTypeID == 7).ToList();
            }
            else if (SearchObj.State == -7 || SearchObj.State == -15)
            {
                //چک های انتظامی که تبدیل نشده اند 
                ReceiptLs = ReceiptLs.Where(d => d.PlaningTypeID != 7 &&  d.PlaningTypeID == 18).ToList();
            }
            else if (SearchObj.State == 8)
            {
                //حقوقی کردن چک برگشتی
                ReceiptLs = ReceiptLs.Where(d => d.PlaningTypeID == 8).ToList();
            }
            else if (SearchObj.State == 9)
            {
                //عودت پس از برگشت از بانک
                ReceiptLs = ReceiptLs.Where(d => d.PlaningTypeID == 9).ToList();
            }

            else if (SearchObj.State == -8 || SearchObj.State == -9)
            {
                //|| SearchObj.State == -29 || SearchObj.State == -28 || SearchObj.State == -25|| d.PlaningTypeID ==8
                //چک های برگشت شده
                ReceiptLs = ReceiptLs.Where(d => d.PlaningTypeID == 12).ToList();
            }
            else if (SearchObj.State == 5 || SearchObj.State == -16 || SearchObj.State == -23)
            {
                //چک های خرج شده
                ReceiptLs = ReceiptLs.Where(d => d.PlaningTypeID == 5).ToList();
            }
            else if (SearchObj.State == 18 || SearchObj.State == -16)
            {
                //چک های خرج شده
                ReceiptLs = ReceiptLs.Where(d => d.PlaningTypeID == 5).ToList();
            }
            else if (SearchObj.State == 11)
            {
                //وصول شده
                ReceiptLs = ReceiptLs.Where(d => d.PlaningTypeID == 11).ToList();
            }
            //|| SearchObj.State == -17 || SearchObj.State == -24
            else if (SearchObj.State == 12)
            {
                //چک های برگشت شده
                ReceiptLs = ReceiptLs.Where(d => d.PlaningTypeID == 12).ToList();
            }
            else if (SearchObj.State == 13)
            {
                //واریز به تنخواه
                ReceiptLs = ReceiptLs.Where(d => d.PlaningTypeID == 13).ToList();
            }
            else if (SearchObj.State == 15)
            {
                //تحویل لاشه چک انتظامی
                ReceiptLs = ReceiptLs.Where(d => d.PlaningTypeID == 15).ToList();
            }
            else if (SearchObj.State == -26)
            {
                //|| SearchObj.State == -27
                //چک های حقوقی شده
                ReceiptLs = ReceiptLs.Where(d => d.PlaningTypeID == 8).ToList();
            }

            if (SearchObj.ReceiptDetailId != 0)
            {
                ReceiptLs = ReceiptLs.Where(d => d.ReceiptDetailId == SearchObj.ReceiptDetailId).ToList();
            }
            if (!string.IsNullOrEmpty(SearchObj.DueDateFrom) && !string.IsNullOrEmpty(SearchObj.DueDateTo))
            {
                var FromDate = SearchObj.DueDateFrom.Replace("/", String.Empty);
                var ToDate = SearchObj.DueDateTo.Replace("/", String.Empty);
                //ReceiptLs = ReceiptLs.Where(d => d.DueDate.Contains(SearchObj.DueDateFrom) && d.DueDate.Contains( SearchObj.DueDateTo)).ToList();
                ReceiptLs = ReceiptLs.Where(d => d.DueDate.CompareTo(FromDate) >= 0 && d.DueDate.CompareTo(ToDate) <= 0).ToList();
                //ReceiptLs = ReceiptLs.Where(d => d.DueDate >= FromDate && d.DueDate <= ToDate)
                //= whereclouse + " and RequestDate >= \"" + obj.RequestDateFrom + "\"  and RequestDate <= \"" + obj.RequestDateTo + "\"";
            }
            if (!string.IsNullOrEmpty(SearchObj.RecieptDateFrom) && !string.IsNullOrEmpty(SearchObj.RecieptDateTo))
            {
                ReceiptLs = ReceiptLs.Where(d => d.DueDate.CompareTo(SearchObj.RecieptDateFrom.Replace("/", String.Empty)) >= 0 && d.DueDate.CompareTo(SearchObj.RecieptDateTo.Replace("/", String.Empty)) <= 0).ToList();
            }
            if (!string.IsNullOrEmpty(SearchObj.Unique_Account_No) && SearchObj.Unique_Account_No != "0")
            {
                ReceiptLs = ReceiptLs.Where(d => d.DetailUnique_Account_No.Trim() == SearchObj.Unique_Account_No.Trim()).ToList();
            }
            if (SearchObj.PlaningTypeReferenceID != 0)
            {
                ReceiptLs = ReceiptLs.Where(d => d.PlaningTypeReferenceID == SearchObj.PlaningTypeReferenceID).ToList();
            }
            var QueryLs = ReceiptLs.OrderByDescending(d => d.StateDate).Skip(SearchObj.PageSize * SearchObj.PageIndex).Take(SearchObj.PageSize).ToList();

            List<CustomReceiptHeaderDetails> CustomLs = new List<CustomReceiptHeaderDetails>();


            foreach (var item in QueryLs)
            {
                CustomReceiptHeaderDetails Custom = new CustomReceiptHeaderDetails();
                Custom.ReceiptId = item.ReceiptId;
                Custom.ExchangeTypeID = item.ExchangeTypeID;
                Custom.ReceiptDetailId = item.ReceiptDetailId;
                Custom.Price = item.EquivalencePrice.Value;
                Custom.ExchangeTypeTitle = item.ExchangeTypeDescription;
                Custom.ChequeNumber = item.Cheque_Number;
                Custom.DueDate = Utility.GetPersianDate(item.DueDate.Trim());
                Custom.LastStateDate = Utility.GetPersianDate(item.StateDate.Trim());
                //Custom.BankId = item.BankID;
                Custom.DueDate_en = Utility.GetGregorianDate(item.DueDate.Trim());
                Custom.AssignmentAccountNum = item.Bank_AccountNo;
                Custom.Unique_Account_No_Code = item.DetailUnique_Account_No;
                Custom.Unique_Account_No = item.DetailUnique_Account_No_One_Title + "-" + item.DetailUnique_Account_No;
                Custom.Receipt_Pay_TypeTitle = item.Receipt_Pay_TypeDetailDescription;
                Custom.AssignmentDate = item.StateDate;
                if (item.PlaningTypeID != 0)
                {
                    Custom.PlaningTypeID = item.PlaningTypeID;
                }
                if (item.StatusUnique_Account_No_One != null)
                {
                    Custom.Unique_Account_No_One = item.StatusUnique_Account_No_One_Title + "/" + item.StatusUnique_Account_No_One;
                }
                if (item.StatusUnique_Account_No_Two != null)
                {
                    Custom.Unique_Account_No_Two = item.StatusUnique_Account_No_Two_Title + "/" + item.StatusUnique_Account_No_Two;
                }
                if (item.PlaningType_Value != null)
                {
                    Custom.PlaningType_Value = item.PlaningType_Value;
                }
                Custom.Bank = item.Bank_BranchDesc;
                Custom.BehalfGroupID = item.BehalfGroupID;
                Custom.StateID = item.StateID;
                Custom.ExchangeRate = item.ExchangeRate;
                CustomLs.Add(Custom);
            }

            SearchObj.List = CustomLs;
            SearchObj.BankAccountLs = Utility.GetBanks();
            SearchObj.BankAccountLsArzi = Utility.GetBanks(0, true);
            SearchObj.v_accountslS = Utility.v_accountsLS();
            SearchObj.ReceiptPayTypeDetailLs = ReceiptPayTypeDetailLs;


            SearchObj.BehalfGroupLs = Utility.GetBehalfsGroupLs().Where(d => d.Receipt_Pay_TypeID == int.Parse(ConfigurationManager.AppSettings["Receipt_Type_ID"])).ToList();
            SearchObj.BehlafLS = Utility.GetBehalfLs();
            SearchObj.CashierLS = Utility.CashiersLs();



            SearchObj.PlaningTypeLS = PlaningTypeClient.SelectAll(1, 100, "PlaningTypeID", "int", false, out TotalRow).ToList();
            SearchObj.TotalItemCount = ReceiptLs.Count();


            return Json(SearchObj);
        }
        public ActionResult ShowReceiptNew(ReceiptSearch SearchObj)
        {
            Icls_SPsClient spsClient = new Icls_SPsClient();
            Icls_PlaningTypeClient PlaningTypeClient = new Icls_PlaningTypeClient();
            Icls_Receipt_Detail_PlanStateClient ReceiptDetailsPlanClinet = new Icls_Receipt_Detail_PlanStateClient();
            Icls_CashiersClient CashierClient = new Icls_CashiersClient();
            //Icls_BankClient BankClient = new Icls_BankClient();
            //var db = new TreasuryEntities();
            var ReceiptPayTypeDetailLs = Utility.GetReceipt_Pay_TypeDetailLs().Where(d => d.Receipt_Pay_TypeID == 2).ToList();
            int TotalRow;
            //براساس نوع دریافت فیلتر میگردد
            var ReceiptLs = spsClient.GetRecieptHeaderAndDetail_ByRecieptTypeId(SearchObj.RecieptTypeId).ToList();

            if (SearchObj.ReceiptDetailId != 0)
            {
                ReceiptLs = ReceiptLs.Where(d => d.ReceiptDetailId == SearchObj.ReceiptDetailId).ToList();
            }
            if (!string.IsNullOrEmpty(SearchObj.DueDateFrom) && !string.IsNullOrEmpty(SearchObj.DueDateTo))
            {
                var FromDate = SearchObj.DueDateFrom.Replace("/", String.Empty);
                var ToDate = SearchObj.DueDateTo.Replace("/", String.Empty);
                //ReceiptLs = ReceiptLs.Where(d => d.DueDate.Contains(SearchObj.DueDateFrom) && d.DueDate.Contains( SearchObj.DueDateTo)).ToList();
                ReceiptLs = ReceiptLs.Where(d => d.DueDate.CompareTo(FromDate) >= 0 && d.DueDate.CompareTo(ToDate) <= 0).ToList();
                //ReceiptLs = ReceiptLs.Where(d => d.DueDate >= FromDate && d.DueDate <= ToDate)
                //= whereclouse + " and RequestDate >= \"" + obj.RequestDateFrom + "\"  and RequestDate <= \"" + obj.RequestDateTo + "\"";
            }
            if (!string.IsNullOrEmpty(SearchObj.RecieptDateFrom) && !string.IsNullOrEmpty(SearchObj.RecieptDateTo))
            {
                ReceiptLs = ReceiptLs.Where(d => d.DueDate.CompareTo(SearchObj.RecieptDateFrom.Replace("/", String.Empty)) >= 0 && d.DueDate.CompareTo(SearchObj.RecieptDateTo.Replace("/", String.Empty)) <= 0).ToList();
            }
            if (!string.IsNullOrEmpty(SearchObj.Unique_Account_No) && SearchObj.Unique_Account_No != "0")
            {
                ReceiptLs = ReceiptLs.Where(d => d.DetailUnique_Account_No.Trim() == SearchObj.Unique_Account_No.Trim()).ToList();
            }
            if (SearchObj.PlaningTypeReferenceID != 0)
            {
                ReceiptLs = ReceiptLs.Where(d => d.PlaningTypeReferenceID == SearchObj.PlaningTypeReferenceID).ToList();
            }
            var QueryLs = ReceiptLs.OrderByDescending(d => d.StateDate).Skip(SearchObj.PageSize * SearchObj.PageIndex).Take(SearchObj.PageSize).ToList();

            List<CustomReceiptHeaderDetails> CustomLs = new List<CustomReceiptHeaderDetails>();


            foreach (var item in QueryLs)
            {
                CustomReceiptHeaderDetails Custom = new CustomReceiptHeaderDetails();
                Custom.ReceiptId = item.ReceiptId;
                Custom.ExchangeTypeID = item.ExchangeTypeID;
                Custom.ReceiptDetailId = item.ReceiptDetailId;
                Custom.Price = item.EquivalencePrice.Value;
                Custom.ExchangeTypeTitle = item.ExchangeTypeDescription;
                Custom.ChequeNumber = item.Cheque_Number;
                Custom.DueDate = Utility.GetPersianDate(item.DueDate.Trim());
                Custom.LastStateDate = Utility.GetPersianDate(item.StateDate.Trim());
                //Custom.BankId = item.BankID;
                Custom.DueDate_en = Utility.GetGregorianDate(item.DueDate.Trim());
                Custom.AssignmentAccountNum = item.Bank_AccountNo;
                Custom.Unique_Account_No_Code = item.DetailUnique_Account_No;
                Custom.Unique_Account_No = item.DetailUnique_Account_No_One_Title + "-" + item.DetailUnique_Account_No;
                Custom.Receipt_Pay_TypeTitle = item.Receipt_Pay_TypeDetailDescription;
                Custom.AssignmentDate = item.StateDate;
                if (item.PlaningTypeID != 0)
                {
                    Custom.PlaningTypeID = item.PlaningTypeID;
                }
                if (item.StatusUnique_Account_No_One != null)
                {
                    Custom.Unique_Account_No_One = item.StatusUnique_Account_No_One_Title + "/" + item.StatusUnique_Account_No_One;
                }
                if (item.StatusUnique_Account_No_Two != null)
                {
                    Custom.Unique_Account_No_Two = item.StatusUnique_Account_No_Two_Title + "/" + item.StatusUnique_Account_No_Two;
                }
                if (item.PlaningType_Value != null)
                {
                    Custom.PlaningType_Value = item.PlaningType_Value;
                }
                Custom.Bank = item.Bank_BranchDesc;
                Custom.BehalfGroupID = item.BehalfGroupID;
                Custom.StateID = item.StateID;
                Custom.ExchangeRate = item.ExchangeRate;
                CustomLs.Add(Custom);
            }

            SearchObj.List = CustomLs;
            SearchObj.BankAccountLs = Utility.GetBanks();
            SearchObj.BankAccountLsArzi = Utility.GetBanks(0, true);
            SearchObj.v_accountslS = Utility.v_accountsLS();
            SearchObj.ReceiptPayTypeDetailLs = ReceiptPayTypeDetailLs;


            SearchObj.BehalfGroupLs = Utility.GetBehalfsGroupLs().Where(d => d.Receipt_Pay_TypeID == int.Parse(ConfigurationManager.AppSettings["Receipt_Type_ID"])).ToList();
            SearchObj.BehlafLS = Utility.GetBehalfLs();
            SearchObj.CashierLS = Utility.CashiersLs();



            SearchObj.PlaningTypeLS = PlaningTypeClient.SelectAll(1, 100, "PlaningTypeID", "int", false, out TotalRow).ToList();
            SearchObj.TotalItemCount = ReceiptLs.Count();


            return Json(SearchObj);
        }
        /// <summary>
        /// ذخیره نمودن وضعیت در Receipt_detail_PlanState
        /// </summary>
        /// <returns></returns>
        public ActionResult SavePlanState(List<chequeInfo> chequeLs)
        {
            using (TreasuryEntities dc = new TreasuryEntities())
            {
                var message = "";
                try
                {
                    var persianDate = Utility.ToPersianDate(DateTime.Now);
                    //بانک
                    var BanKObj = new DataModel.Bank();
                    //صندوق
                    var CasheirObj = new DataModel.Cashier();
                    //گروه بابت
                    var BehalfGroupObj = new DataModel.BehalfGroup();

                    foreach (var item in chequeLs)
                    {
                        var planState = new DataModel.Receipt_Detail_PlanState();
                        if (item.StateDate != null)
                        {
                            planState.StateDate = item.StateDate.Replace("/", String.Empty);
                        }
                        else
                        {
                            planState.StateDate = persianDate;
                        }

                        planState.StateTime = DateTime.Now.TimeOfDay;
                        planState.ReceiptDetailId = item.ReceiptDetailId;
                        planState.PlaningTypeID = item.ChequeState;
                        var planingType = dc.PlaningTypes.Where(d => d.PlaningTypeID == item.ChequeState);
                        //عودت
                        if (item.State == -2000)
                        {
                            planState.PlaningTypeID = 9;
                            planingType = dc.PlaningTypes.Where(d=>d.PlaningTypeID == 9);
                        }
                        
                        if (item.PlaningTypeValue != null)
                        {
                            planState.PlaningType_Value = item.PlaningTypeValue;
                        }
                        if (item.CashierID != 0)
                        {
                            planState.PlaningTypeReferenceID = item.CashierID;
                        }
                        if (item.BankId != 0)
                        {
                            planState.PlaningTypeReferenceID = item.BankId;
                        }
                        

                        BanKObj = dc.Banks.FirstOrDefault(d => d.BankID == item.BankId);
                        var Receipt_DetailObj = dc.Receipt_Detail.FirstOrDefault(d => d.ReceiptDetailId == item.ReceiptDetailId);

                        // item.State == -24 || item.State == -26 ||
                        //تفصیل اول و دوم
                        if (item.State == -1 || item.State == -22 ||
                            item.State == -2 || item.State == -4 || item.State == -17)
                        {
                            //|| item.State == -29|| item.State == -28
                            //نزد صندوق1
                            //وصول مستقیم -نقد
                            //واگذار به بانک
                            //تنزیل اسناد دریافتنی22
                            // BanKObj = bankClient.SelectBy_DynamicWhereClause("BankID =" + item.BankId, 1, 10, "BankID", "int", false, out TotalRow).SingleOrDefault();
                            //واگذاربه بانک 2
                            //وصول مستقیم4
                            //موجودی نقد نزد بانک17
                            //یا واگذار به بانک
                            //نزد صندوق

                            if (item.State == -2 || item.State == -17)
                            {
                                //وصول
                                var PlanDetails = dc.Receipt_Detail_PlanState.Where(d => d.ReceiptDetailId == item.ReceiptDetailId && d.PlaningTypeID == 2).OrderByDescending(d => d.StateID).FirstOrDefault();
                                BanKObj = dc.Banks.FirstOrDefault(d => d.Unique_Account_No == PlanDetails.Unique_Account_No);
                                planState.BankID = BanKObj.BankID;
                            }
                            if (BanKObj != null)
                            {
                                //حساب تفصیل را از بانک میخواند
                                planState.Unique_Account_No = BanKObj.Unique_Account_No;
                                planState.BankID = BanKObj.BankID;
                                if (BanKObj.Unique_Account_No_two != null)
                                {
                                    planState.Unique_Account_No_Two = CasheirObj.Unique_Account_No_two;
                                }
                                //از کاربر گرفته می شود
                                if (item.unique_account2 != null)
                                {
                                    //درصورتی که تفصیل دوم را کاربر وارد کرده باشد
                                    planState.Unique_Account_No_Two = item.unique_account2;
                                }
                                else
                                {
                                    //ثبت تفصیل دوم در صورتی که معین در معین بانک تفصیل دوم داشته باشد
                                    planState.Unique_Account_No_Two = BanKObj.Unique_Account_No_two;
                                }
                            }
                            //if (item.State == -29)
                            //{
                            //    // planState.Unique_Account_No = Receipt_DetailObj.Unique_Account_No;
                            //    planState.Unique_Account_No = item.Unique_Account_No_Code;
                            //}

                        }
                        //else if (item.State == -2 || item.State == -4 || item.State == -17 || item.State == -28 || item.State == -29)
                        //{
                        //    //واگذاربه بانک 2
                        //    //وصول مستقیم4
                        //    //موجودی نقد نزد بانک17
                        //    if (item.State == -2 || item.State == -17)
                        //    {
                        //        //وصول
                        //        var PlanDetails = dc.Receipt_Detail_PlanState.FirstOrDefault(d => d.ReceiptDetailId == item.ReceiptDetailId && d.PlaningTypeID == 2);
                        //        BanKObj = dc.Banks.FirstOrDefault(d => d.Unique_Account_No == PlanDetails.Unique_Account_No);

                        //    }

                        //    //واگذاربه بانک
                        //    //یا واریز به بانک
                        //    //حساب تفصیل
                        //    if (BanKObj != null)
                        //    {
                        //        planState.Unique_Account_No = BanKObj.Unique_Account_No;
                        //        if (BanKObj.Unique_Account_No_two != null)
                        //        {
                        //            planState.Unique_Account_No_Two = CasheirObj.Unique_Account_No_two;
                        //        }
                        //    }
                        //    //if(item.State==-4 || item.State== -28)
                        //    //{
                        //    //    planState.StateDate = item.StateDate.Replace("/", String.Empty);
                        //    //}
                        //    if ( item.State == -29)
                        //    {
                        //        planState.Unique_Account_No = Receipt_DetailObj.Unique_Account_No;
                        //    }
                        //}
                        else if (item.State == -3)
                        {
                            //وصول مستقیم -نقد                          
                            CasheirObj = dc.Cashiers.FirstOrDefault(d => d.CashierID == item.CashierID);
                            //وصول مستقیم نقد
                            if (CasheirObj != null)
                            {
                                planState.Unique_Account_No = CasheirObj.Unique_Account_No;
                                if (CasheirObj.Unique_Account_No_two != null)
                                {
                                    planState.Unique_Account_No_Two = CasheirObj.Unique_Account_No_two;
                                }
                            }
                        }
                        else if (item.State == -6 || item.State == -8)
                        {
                            //|| item.State == -7 //  تبديل انتظامي به دريافتني
                            //عودت به صندوق

                            // کردن چک برگشتيحقوقي
                            // لاشه چک انتظامي تحويل
                            // چک هاي خرج شده عودت
                            //بابت
                            BehalfGroupObj = dc.BehalfGroups.FirstOrDefault(d => d.BehalfGroupID == item.BehalfGroupID);
                            if (BehalfGroupObj != null)
                            {
                                var accountObj = dc.v_accounts.SingleOrDefault(d => d.account_code == BehalfGroupObj.BehalfGroup_Account);
                            }
                            planState.PlaningTypeReferenceID = item.BehalfID;
                            planState.Unique_Account_No = item.unique_account1;
                            planState.Unique_Account_No_Two = item.unique_account2;


                            if (item.State == -8 || item.State == -6)
                            {
                                //planState.StateDate= item.StateDate.Replace("/", String.Empty);
                                //planState.Unique_Account_No = Receipt_DetailObj.Unique_Account_No;
                                planState.Unique_Account_No = item.Unique_Account_No_Code;
                            }
                        }
                        else if (item.State == -5 || item.State == -9 || item.State == -12 || item.State == -18)
                        {
                            if (item.State == -5)
                            {
                                BehalfGroupObj = dc.BehalfGroups.FirstOrDefault(d => d.BehalfGroupID == item.BehalfGroupID);
                            }
                            planState.PlaningTypeReferenceID = item.BehalfGroupID;
                            planState.Unique_Account_No = item.unique_account1;
                            planState.Unique_Account_No_Two = item.unique_account2;
                        }
                        else if (item.State == -10 || item.State == -20 || item.State == -16 || item.State == -23)
                        {
                            //ابطال چک نزد صندوق
                            //تمدید چک سررسید شده
                            //if (item.State == -20)
                            //{
                            //    if (Receipt_DetailObj != null)
                            //    {
                            //        Receipt_DetailObj.DueDate = item.DueDate.Replace("/", "");
                            //    }
                            //}
                            if (item.State == -10)
                            {
                                BehalfGroupObj = dc.BehalfGroups.FirstOrDefault(d => d.BehalfGroupID == item.BehalfGroupID);
                                if (BehalfGroupObj != null)
                                {
                                    var accountObj = dc.v_accounts.SingleOrDefault(d => d.account_code == BehalfGroupObj.BehalfGroup_Account);
                                }
                            }
                            // planState.Account_No = planingType.SingleOrDefault().Defualt_AccountNo;
                            planState.Unique_Account_No = item.Unique_Account_No_Code;

                        }

                        //معین /معین درمعین            
                        if (BanKObj != null && BanKObj.Account_No != null)
                        {
                            // || item.State == -24 || item.State == -26
                            //درصورتی که به بانک واگذار کند باید معین درمعین واگذاری بانک ثبت گردد
                            if (item.State == -1)
                            {
                                // عودت //معین  واگذاری بانک 
                                planState.Account_No = BanKObj.Assign_Account_No;

                                //زمانی که چک انتظامی به دریافتنی تبدیل شده باشد
                                var renewchequedate = dc.Receipt_Detail_PlanState.OrderByDescending(d => d.StateID).SingleOrDefault(d => d.ReceiptDetailId == item.ReceiptDetailId && d.PlaningTypeID == 7);
                                if (renewchequedate != null)
                                {
                                    planState.RenewChequeDate = renewchequedate.RenewChequeDate;
                                }

                                //planState.StateDate = item.StateDate.Replace("/", String.Empty);
                            }

                            else if (item.State == -1 || item.State == -2 || item.State == -17)
                            {

                                //درصورتیکه کاربر وارد کرده باشد تاریخ را تاریخ ورودی کاربر ذخیره می گردد
                                //planState.StateDate = item.StateDate.Replace("/", String.Empty);

                                if (item.ChequeState == 11 || item.ChequeState == 17)
                                {
                                    //وصول
                                    planState.Account_No = BanKObj.Account_No;
                                }
                                else if (item.ChequeState == 12 || item.ChequeState == 16)
                                {
                                    //برگشت
                                    planState.Account_No = BanKObj.Returns_Account_No;
                                }
                                else if (item.ChequeState == 9 || item.ChequeState == 23)
                                {
                                    //عودت
                                    planState.Account_No = planingType.SingleOrDefault().Defualt_AccountNo;
                                }

                            }
                            else
                            {
                                planState.Account_No = BanKObj.Account_No;
                            }
                        }
                        else if (CasheirObj != null && CasheirObj.Account_no != null)
                        {
                            planState.Account_No = CasheirObj.Account_no;
                        }
                        else if (BehalfGroupObj != null && BehalfGroupObj.BehalfGroup_Account != null &&
                            !(item.State == -20 || item.State == -16 || item.State == -23 || item.State == -8))
                        {
                            planState.Account_No = BehalfGroupObj.BehalfGroup_Account;
                        }
                        //else if(item.State == -29)
                        //{
                        //    planState.Account_No = Receipt_DetailObj.Behalf.BehalfGroup.BehalfGroup_Account;
                        //}
                        else if (item.State == -22)
                        {
                            var lastState = Receipt_DetailObj.Receipt_Detail_PlanState.OrderByDescending(d => d.StateID);
                            planState.Account_No = lastState.FirstOrDefault().Account_No;
                        }
                        else
                        {
                            planState.Account_No = planingType.SingleOrDefault().Defualt_AccountNo;
                        }


                        planState.PlaningType_Value = item.PlaningTypeValue;
                        planState.UserID = User.Id;
                        planState.HostName = System.Net.Dns.GetHostName();

                        //var ifexict = planStateClient.SelectBy_DynamicWhereClause(" PlaningTypeID=" + planState.PlaningTypeID + " and  ReceiptDetailId=" + planState.ReceiptDetailId, 1, 1000 * 1000, "StateID", "dec", false, out TotalRow).Any();

                        //if (!ifexict)
                        //{
                        // Result = planStateClient.Insert(planState, out message);
                        // var StateId = message.Split(';')[1];

                        if (item.State == -22)
                        {
                            var BankId = Receipt_DetailObj.Receipt_Detail_PlanState.OrderByDescending(d => d.StateID).SingleOrDefault(d => d.PlaningTypeID == 2).BankID;
                            planState.BankID = BankId;
                            planState.Unique_Account_No = Receipt_DetailObj.Receipt_Detail_PlanState.OrderByDescending(d => d.StateID).FirstOrDefault(d => d.PlaningTypeID == 2).Unique_Account_No;
                            planState.DecreaseChequePrice = decimal.Parse(planState.PlaningType_Value);
                        }
                        //درصورت تبدیل چک انتظامی به دریافتنی یا تمدید چک دریافتنی باید تاریخ سررسید جدید گرفته شود
                        //درمرحله تولید سند نیاز بود که اینجا تکرار شود

                        var TabdilBeEntezami = Receipt_DetailObj.Receipt_Detail_PlanState.OrderByDescending(d => d.StateID).Any(d => d.PlaningTypeID == 7);
                        if (TabdilBeEntezami || item.State == -7)
                        {
                            planState.RenewChequeDate = item.DueDate.Replace("/", "");
                        }
                        if (item.State == -20)
                        {
                            planState.RenewChequeDate = item.DueDate.Replace("/", "");
                            planState.StateDate = item.StateDate.Replace("/", "");
                        }

                        dc.Receipt_Detail_PlanState.Add(planState);


                        if (string.IsNullOrEmpty(planState.Account_No))
                        {
                            message += "برای این وضعیت معین تعریف نشده است. برنامه ریزی شماره دریافت" + planState.ReceiptDetailId + "با خطا مواجه شد";
                        }
                    }
                    //}

                    dc.SaveChanges();
                    return JsonMessage(true, "ثبت برنامه ریزی با موفقیت انجام شد", null, false);
                }
                catch (Exception ex)
                {
                    if (ex.InnerException != null)
                    {
                        message = ex.InnerException.InnerException.Message.Split(new string[] { "\r\n" }, StringSplitOptions.None)[0];
                    }
                    return JsonMessage(false, message, null, false);
                    //return JsonMessage(false, "ثبت برنامه ریزی با خطا مواجه شد", null, false);
                }
            }

        }
        #endregion

        #region Delete
        //public ActionResult Delete(int headerId)
        //{
        //    string message;
        //    int TotalRow;
        //    Icls_Receipt_DetailClient ReceiptDetailsClient = new Icls_Receipt_DetailClient();
        //    Service.Reciept_HeaderService.Icls_Reciept_HeaderClient headerClient = new Service.Reciept_HeaderService.Icls_Reciept_HeaderClient();
        //    Icls_Reciept_ReferenceTypes_ValueClient RefrenceValueClient = new Icls_Reciept_ReferenceTypes_ValueClient();
        //    Icls_Receipt_Detail_PlanStateClient PlanStateClient = new Icls_Receipt_Detail_PlanStateClient();

        //    var DetailsLs = ReceiptDetailsClient.SelectBy_DynamicWhereClause("ReceiptId =" + headerId, 1, 100, "ReceiptDetailId", "dec", false, out TotalRow);


        //    foreach (var item in DetailsLs)
        //    {
        //        var RefrencesLs = RefrenceValueClient.SelectBy_DynamicWhereClause("ReceiptDetailId=" + item.ReceiptDetailId, 1, 100, "ReferenceTypeValueID", "int", false, out TotalRow);
        //        foreach (var item1 in RefrencesLs)
        //        {
        //            RefrenceValueClient.Delete(item1.ReferenceTypeValueID, out message);
        //        }
        //        var PlanStateLs = PlanStateClient.SelectBy_DynamicWhereClause("ReceiptDetailId =" + item.ReceiptDetailId, 1, 100, "StateID", "dec", false, out TotalRow);
        //        foreach (var item2 in PlanStateLs)
        //        {
        //            PlanStateClient.Delete((int)item2.StateID, out message);
        //        }
        //    }

        //    foreach (var item in DetailsLs)
        //    {
        //        ReceiptDetailsClient.Delete(item.ReceiptDetailId, out message);
        //    }
        //    headerClient.Delete(headerId, out message);

        //    var db = new TreasuryFilesEntities();
        //    var FileObj = db.Files.SingleOrDefault(d => d.tableName == "Reciept_Header" && d.relatedID == headerId);
        //    if (FileObj != null)
        //    {
        //        db.Files.Remove(FileObj);
        //    }

        //    return JsonMessage(true, message);

        //}

        /// <summary>
        /// حذف وضعیت دریافتها
        /// </summary>
        /// <param name="StateId"></param>
        /// <returns>JsonResult message</returns>     
        public ActionResult DeleteReceiptDetailsPlanState(int StateId)
        {
            var message = "";
            //string message;
            //Icls_Receipt_Detail_PlanStateClient receiptdetailsPlanStateClient = new Icls_Receipt_Detail_PlanStateClient();
            //receiptdetailsPlanStateClient.Delete(StateId, out message);
            try
            {
                using (var db = new TreasuryEntities())
                {
                    var Receipt_Detail_PlanState = db.Receipt_Detail_PlanState.SingleOrDefault(d => d.StateID == StateId);
                    //درصورتیکه بخواهد وضعیت تبدیل چک انتظامی به دریافتنی را حذف کند باید تاریخ سررید خالی گردد
                    if (Receipt_Detail_PlanState.PlaningTypeID == 7)
                    {
                        var ReceiptDeails = db.Receipt_Detail.SingleOrDefault(d => d.ReceiptDetailId == Receipt_Detail_PlanState.ReceiptDetailId);
                        ReceiptDeails.DueDate = "";
                    }
                    db.Receipt_Detail_PlanState.Remove(Receipt_Detail_PlanState);
                    db.SaveChanges();
                    message = "حذف وضعیت با موفقیت انجام شد";
                    return JsonMessage(true, message, null, false);
                }
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
                throw;
            }

        }
        public ActionResult DeleteALLReceiptDetailsPlanState(decimal ReceiptDetailId)
        {
            //string message = "";
            //int TotalRow;
            //Icls_Receipt_Detail_PlanStateClient receiptdetailsPlanStateClient = new Icls_Receipt_Detail_PlanStateClient();
            //var receiptdetailsPlanStatels = receiptdetailsPlanStateClient.SelectBy_DynamicWhereClause("ReceiptDetailId =" + ReceiptDetailId, 1, 100, "StateID", "dec", false, out TotalRow);

            var message = "";
            try
            {
                var db = new TreasuryEntities();
                db.DelReceipt_Detail_PlanStates((int)ReceiptDetailId);
                //var receiptdetailsPlanStatels = db.Receipt_Detail_PlanState.Where(d => d.ReceiptDetailId == ReceiptDetailId && d.PlaningTypeID!=1);
                //////درصورتیکه بخواهد وضعیت تبدیل چک انتظامی به دریافتنی را حذف کند باید تاریخ سررید خالی گردد
                //if (receiptdetailsPlanStatels.Any(d => d.PlaningTypeID == 7))
                //{
                //    var ReceiptDeails = db.Receipt_Detail.SingleOrDefault(d => d.ReceiptDetailId == receiptdetailsPlanStatels.FirstOrDefault(e => e.PlaningTypeID == 7).ReceiptDetailId);
                //    ReceiptDeails.DueDate = "";
                //}
                //db.Receipt_Detail_PlanState.RemoveRange(receiptdetailsPlanStatels);
                //foreach (var item in receiptdetailsPlanStatels)
                //{
                //    var Receipt_Detail_PlanState = db.Receipt_Detail_PlanState.SingleOrDefault(d => d.StateID == item.StateID);
                //    //درصورتیکه بخواهد وضعیت تبدیل چک انتظامی به دریافتنی را حذف کند باید تاریخ سررید خالی گردد
                //    if (Receipt_Detail_PlanState.PlaningTypeID == 7)
                //    {
                //        var ReceiptDeails = db.Receipt_Detail.SingleOrDefault(d => d.ReceiptDetailId == Receipt_Detail_PlanState.ReceiptDetailId);
                //        ReceiptDeails.DueDate = "";
                //    }

                //        db.Receipt_Detail_PlanState.Remove(item);

                //    //receiptdetailsPlanStateClient.Delete((int)item.StateID, out message);

                //}
                db.SaveChanges();
                //return JsonMessage(true, "حذف وضعیت با موفقیت انجام شد");
                message = "حذف وضعیت با موفقیت انجام شد";
                return JsonMessage(true, message, null, false);
            }
            catch (Exception ex)
            {

                message = "با خطا مواجه شد";
                if (ex.InnerException.InnerException != null)
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
                else if (ex.InnerException.Message != null)
                {

                    if (ex.InnerException.Message.Contains("\r\n"))
                    {
                        message = ex.InnerException.Message.Split(new string[] { "\r\n" }, StringSplitOptions.None)[0];
                    }
                    else
                    {
                        message += "</br>" + ex.InnerException.Message;
                    }
                }
                else
                {
                    message += "</br>" + ex.Message;
                }
                return JsonMessage(false, message, null, false);
                throw;
                //return JsonMessage(true, "با خطا مواجه شد");

            }


        }
        public ActionResult hasrelated(int ReceiptDetailId)
        {
            var hasrelated = false;
            try
            {
                var db = new TreasuryEntities();
                var receiptdetails = db.Receipt_Detail.SingleOrDefault(d => d.ReceiptDetailId == ReceiptDetailId);
                var receipt = db.Reciept_Header.SingleOrDefault(d => d.ReceiptId == receiptdetails.ReceiptId);
                if (receipt.ReferenceReceiptId == 1)
                {
                    hasrelated = true;
                }
                return JsonMessage(true, "حذف با موفقیت انجام شد.", null, hasrelated);
            }
            catch (Exception ex)
            {

                throw;
            }
        }
        /// <summary>
        /// delete ReceiptDetils
        /// </summary>
        /// <param name="ReceiptDetailId"></param>
        /// <returns></returns>
        public ActionResult Delete(int ReceiptDetailId)
        {
            TreasuryEntities db = new TreasuryEntities();
            var dbFile = new TreasuryFilesEntities();
            //string message;
            //int TotalRow;
            //Icls_Receipt_DetailClient ReceiptDetailsClient = new Icls_Receipt_DetailClient();
            //Icls_Reciept_HeaderClient ReceiptHeaderClient = new Icls_Reciept_HeaderClient();
            //Icls_Reciept_ReferenceTypes_ValueClient RefrenceValueClient = new Icls_Reciept_ReferenceTypes_ValueClient();
            //Icls_Receipt_Detail_PlanStateClient PlanStateClient = new Icls_Receipt_Detail_PlanStateClient();
            // Voucher_Row_RecieptClient VocherClient = new Voucher_Row_RecieptClient();
            //TreasuryEntities treasurycontext = new TreasuryEntities();

            //var RefrencesLs = RefrenceValueClient.SelectBy_DynamicWhereClause("ReceiptDetailId=" + ReceiptDetailId, 1, 100, "ReferenceTypeValueID", "int", false, out TotalRow);
            //foreach (var item1 in RefrencesLs)
            //{
            //    RefrenceValueClient.Delete(item1.ReferenceTypeValueID, out message);
            //}
            //var PlanStateLs = PlanStateClient.SelectBy_DynamicWhereClause("ReceiptDetailId =" + ReceiptDetailId, 1, 100, "StateID", "dec", false, out TotalRow);
            //foreach (var item2 in PlanStateLs)
            //{
            //    PlanStateClient.Delete((int)item2.StateID, out message);
            //}
            //var ReceiptDetail = ReceiptDetailsClient.SelectRow(ReceiptDetailId).SingleOrDefault();
            //ReceiptDetailsClient.Delete(ReceiptDetailId, out message);
            //ReceiptHeaderClient.Delete(ReceiptDetail.ReceiptId, out message);
            //var vocherls = VocherClient.SelectBy_DynamicWhereClause("ReceiptDetailId = " + ReceiptDetailId, 1, 100, "Voucher_Row_RecieptID", "Int64", false, out TotalRow);
            //if (vocherls != null)
            //{
            //    foreach (var item in vocherls)
            //    {
            //        VocherClient.Delete((int)item.Voucher_Row_RecieptID, out message);
            //    }
            //}
            try
            {
                var ReceiptDetail = db.Receipt_Detail.FirstOrDefault(d => d.ReceiptDetailId == ReceiptDetailId);
                var receiptheader = db.Reciept_Header.FirstOrDefault(d => d.ReceiptId == ReceiptDetail.ReceiptId);
                //درصورتیکه که چکی تعویضی رابخواهد حذف کند اخرین وضعیت پرنتش هم باید حذف گردد
                if (receiptheader.ReferenceReceiptId != 0)
                {
                    var receiptDetailsls = db.Receipt_Detail.Where(d => d.ReceiptId == receiptheader.ReceiptId);
                    // درصورتیکه یک چک را با چندین چک تعویص کرده باشد و وضعیت تعویض را بخواهد حذف کند
                    foreach (var item in receiptDetailsls)
                    {
                        //delete Reciept_ReferenceTypes_Value
                        var RefrencesLs = db.Reciept_ReferenceTypes_Value.Where(d => d.ReceiptDetailId == item.ReceiptDetailId);
                        db.Reciept_ReferenceTypes_Value.RemoveRange(RefrencesLs);

                        //delete Receipt_Detail_PlanState
                        var PlanStateLs = db.Receipt_Detail_PlanState.Where(d => d.ReceiptDetailId == item.ReceiptDetailId);
                        db.Receipt_Detail_PlanState.RemoveRange(PlanStateLs);


                        //remove Receipt_Detail
                        db.Receipt_Detail.Remove(item);

                        var FileObj = dbFile.Files.FirstOrDefault(d => d.tableName == "Receipt_Detail" && d.relatedID == item.ReceiptDetailId);
                        if (FileObj != null)
                        {
                            dbFile.Files.Remove(FileObj);
                        }
                    }

                    //delete Receipt_Detail and Reciept_Header
                    db.Reciept_Header.Remove(receiptheader);
                    if (receiptheader.ReferenceReceiptId != null)
                    {
                        var parentreceiptdetails = db.Receipt_Detail.SingleOrDefault(d => d.ReceiptDetailId == receiptheader.ReferenceReceiptId);
                        var parentPlanstate = parentreceiptdetails.Receipt_Detail_PlanState.OrderByDescending(d => d.StateID).FirstOrDefault(d => d.ReceiptDetailId == receiptheader.ReferenceReceiptId);
                        db.Receipt_Detail_PlanState.Remove(parentPlanstate);

                    }
                }
                else
                {
                    //delete Reciept_ReferenceTypes_Value
                    var RefrencesLs = db.Reciept_ReferenceTypes_Value.Where(d => d.ReceiptDetailId == ReceiptDetailId);
                    db.Reciept_ReferenceTypes_Value.RemoveRange(RefrencesLs);

                    //delete Receipt_Detail_PlanState
                    var PlanStateLs = db.Receipt_Detail_PlanState.Where(d => d.ReceiptDetailId == ReceiptDetailId);
                    db.Receipt_Detail_PlanState.RemoveRange(PlanStateLs);

                    //delete Receipt_Detail and Reciept_Header

                    db.Receipt_Detail.Remove(ReceiptDetail);

                    //درصورتی که چندین چایلد با یک پرنت ثبت نشده باشد
                    var receiptdetails = db.Receipt_Detail.Where(d => d.ReceiptId == receiptheader.ReceiptId);
                    if (receiptdetails.Count() == 1)
                    {
                        db.Reciept_Header.Remove(receiptheader);
                    }

                    var FileObj = dbFile.Files.FirstOrDefault(d => d.tableName == "Receipt_Detail" && d.relatedID == ReceiptDetailId);
                    if (FileObj != null)
                    {
                        dbFile.Files.Remove(FileObj);
                    }

                }
                db.SaveChanges();
                dbFile.SaveChanges();
                return JsonMessage(true, "حذف با موفقیت انجام شد.", null, false);
            }
            catch (Exception ex)
            {
                var message = ex.InnerException.InnerException.Message.Split(new string[] { "\r\n" }, StringSplitOptions.None)[0];
                return JsonMessage(false, message, null, false);
            }
        }

        //public ActionResult Delete(int ReceiptDetailId)
        //{
        //    Icls_SPsClient spsClient = new Icls_SPsClient();
        //    Icls_Receipt_DetailClient ReceiptDetailsClient = new Icls_Receipt_DetailClient();
        //    var ReceiptDetail = ReceiptDetailsClient.SelectRow(ReceiptDetailId).SingleOrDefault();
        //    //var res = spsClient.Sp_DeleteReciept(Convert.ToInt32(ReceiptDetail.ReceiptDetailId), int.Parse(ReceiptDetail.FiscalYear));

        //   // if (res)
        //    //{
        //        return JsonMessage(true, "");
        //   // }
        //    //else
        //    //{
        //    //    return JsonMessage(false, "");
        //    //}
        //}

        //public ActionResult DeletePlaning(int ReceiptDetailId, int planingTypeID)
        //{
        //    Icls_SPsClient spsClient = new Icls_SPsClient();
        //    Icls_Receipt_DetailClient ReceiptDetailsClient = new Icls_Receipt_DetailClient();
        //    var ReceiptDetail = ReceiptDetailsClient.SelectRow(ReceiptDetailId).SingleOrDefault();
        //    //var res = spsClient.Sp_DeleteRecieptPlaning(Convert.ToInt32(ReceiptDetail.ReceiptDetailId), planingTypeID, int.Parse(ReceiptDetail.FiscalYear));

        //    //if (res)
        //    //{
        //        return JsonMessage(true, "");
        //    //}
        //    //else
        //    //{
        //    //    return JsonMessage(false, "");
        //    //}
        //}


        #endregion


        /// <summary>
        /// نمایش دریافت
        /// </summary>
        /// <returns></returns>
        #region ShowReceipt
        public ActionResult ShowReceiptDetails(int ReceiptDetailsId)
        {
            int totalRow;
            //Icls_Reciept_HeaderClient receiptHeaderClient = new Icls_Reciept_HeaderClient();
            //Icls_Receipt_DetailClient receiptDetailsClient = new Icls_Receipt_DetailClient();
            Icls_Reciept_ReferenceTypes_ValueClient ReceiptRefrenceTypeValue = new Icls_Reciept_ReferenceTypes_ValueClient();
            var db = new TreasuryEntities();
            var RefrenceValue = ReceiptRefrenceTypeValue.SelectBy_DynamicWhereClause("ReceiptDetailId =" + ReceiptDetailsId, 1, 1000 * 1000, "ReferenceTypeValueID", "int", false, out totalRow);

            //var ReceiptDetails = receiptDetailsClient.SelectRow(ReceiptDetailsId).SingleOrDefault();
            var ReceiptDetails = db.Receipt_Detail.Find(ReceiptDetailsId);
            //var ReceiptHeader = receiptHeaderClient.SelectRow(ReceiptDetails.ReceiptId);
            var ReceiptHeader = db.Reciept_Header.Find(ReceiptDetails.ReceiptId);
            ViewBag.RecieptTypeId = ReceiptHeader.RecieptTypeId;
            if (RefrenceValue != null)
            {
                ViewBag.RefrenceValue = RefrenceValue.SingleOrDefault();
            }
            return PartialView(ReceiptDetails);
        }
        #endregion



        #region Has_Unique_Account_Two
        public JsonResult HasUniqueAccountTwo(int BankId, int ReceiptDetailId, Account_No_Type Account_No_Type, int State)
        {
            string Account_No = "";
            string Assign_Account_No = "";
            string Returns_Account_No = "";
            int TotalRow;
            //Icls_BankClient BankClient = new Icls_BankClient();
            var db = new TreasuryEntities();
            Icls_Receipt_Detail_PlanStateClient ReceiptPLanClient = new Icls_Receipt_Detail_PlanStateClient();

            if (State == -1)
            {
                //var Bank = BankClient.SelectRow(BankId).SingleOrDefault();
                var Bank = db.Banks.SingleOrDefault(d => d.BankID == BankId);
                Account_No = Bank.Account_No.Trim();
                Assign_Account_No = Bank.Assign_Account_No.Trim();
                Returns_Account_No = Bank.Returns_Account_No.Trim();
            }
            else if (State == -2)
            {
                var PlanDetails = ReceiptPLanClient.SelectBy_DynamicWhereClause("ReceiptDetailId=" + ReceiptDetailId + "and PlaningTypeID=2", 1, 100, "StateID", "dec", false, out TotalRow).FirstOrDefault();
                //var Bank = BankClient.SelectBy_DynamicWhereClause("BankID =" + PlanDetails.PlaningTypeReferenceID, 1, 1000, "BankID", "int", false, out TotalRow);
                var Bank = db.Banks.Where(d => d.BankID == PlanDetails.PlaningTypeReferenceID);
                if (Bank != null && Bank.Count() != 0)
                {
                    Account_No = Bank.SingleOrDefault().Account_No.Trim();
                    Assign_Account_No = Bank.SingleOrDefault().Assign_Account_No.Trim();
                    Returns_Account_No = Bank.SingleOrDefault().Returns_Account_No.Trim();
                }
            }
            var V_Account = Utility.v_accountsLS();
            var Result = false;
            if (Account_No_Type == Account_No_Type.Account_No && Account_No != "")
            {
                Result = V_Account.SingleOrDefault(d => d.account_code.Trim() == Account_No).Has_Unique_Account_Two;
            }
            else if (Account_No_Type == Account_No_Type.Assign_Account_No && Assign_Account_No != "")
            {
                Result = V_Account.SingleOrDefault(d => d.account_code.Trim() == Assign_Account_No).Has_Unique_Account_Two;
            }
            else if (Account_No_Type == Account_No_Type.Returns_Account_No && Returns_Account_No != "")
            {
                Result = V_Account.SingleOrDefault(d => d.account_code.Trim() == Returns_Account_No).Has_Unique_Account_Two;
            }
            return Json(Result, JsonRequestBehavior.AllowGet);
        }

        #endregion



        /// <summary>
        /// پیگیری وضعیت دریافت ها
        /// </summary>
        /// <returns></returns>
        #region FollowUpReceipt
        public ActionResult FollowUpReceipt(ReceiptSearch SearchObj)
        {
            //نمایش لیستی از انواع پرداخت
            ViewBag.Receipt_Pay_TypeDetailLs = Utility.GetReceipt_Pay_TypeDetailLs().Where(d => d.Receipt_Pay_TypeID == 2).ToList();
            //لیستی از نوع ذینفع 
            var dc = new TreasuryEntities();
            var unique_account_type = dc.v_unique_account_type.ToList();
            unique_account_type.Add(new v_unique_account_type { Code = 0, Description = Resourse.Resources.Others });
            ViewBag.unique_account_type = unique_account_type;

            return View(SearchObj);
        }
        public ActionResult ShowFollowUpReceipt(ReceiptSearch SearchObj)
        {
            Icls_SPsClient spsClient = new Icls_SPsClient();
            //براساس نوع دریافت فیلتر میگردد
            var ReceiptLs = spsClient.GetRecieptHeaderAndDetail_ByRecieptTypeId(SearchObj.RecieptTypeId).ToList();
            if (!string.IsNullOrEmpty(SearchObj.DueDateFrom) && !string.IsNullOrEmpty(SearchObj.DueDateTo))
            {
                var FromDate = SearchObj.DueDateFrom.Replace("/", String.Empty);
                var ToDate = SearchObj.DueDateTo.Replace("/", String.Empty);
                ReceiptLs = ReceiptLs.Where(d => d.DueDate.CompareTo(FromDate) >= 0 && d.DueDate.CompareTo(ToDate) <= 0).ToList();
            }
            //if (!string.IsNullOrEmpty(SearchObj.RecieptDateFrom) && !string.IsNullOrEmpty(SearchObj.RecieptDateTo))
            //{
            //    ReceiptLs = ReceiptLs.Where(d => d.DueDate.CompareTo(SearchObj.RecieptDateFrom.Replace("/", String.Empty)) >= 0 && d.DueDate.CompareTo(SearchObj.RecieptDateTo.Replace("/", String.Empty)) <= 0).ToList();
            //}
            if (!string.IsNullOrEmpty(SearchObj.Unique_Account_No) && SearchObj.Unique_Account_No != "0")
            {
                ReceiptLs = ReceiptLs.Where(d => d.DetailUnique_Account_No.Trim() == SearchObj.Unique_Account_No.Trim()).ToList();
            }
            if (SearchObj.ReceiptId != 0)
            {
                ReceiptLs = ReceiptLs.Where(d => d.ReceiptDetailId == SearchObj.ReceiptId).ToList();
            }
            if (SearchObj.RecieptTypeId != 0)
            {
                ReceiptLs = ReceiptLs.Where(d => d.RecieptTypeId == SearchObj.RecieptTypeId).ToList();
            }
            if (SearchObj.ChequeNo != 0)
            {
                ReceiptLs = ReceiptLs.Where(d => Int64.Parse(d.Cheque_Number) == SearchObj.ChequeNo).ToList();
            }

            var QueryLs = ReceiptLs.OrderByDescending(d => d.DueDate).Skip(SearchObj.PageSize * SearchObj.PageIndex).Take(SearchObj.PageSize).ToList();
            List<CustomReceiptHeaderDetails> CustomLs = new List<CustomReceiptHeaderDetails>();
            foreach (var item in QueryLs)
            {
                CustomReceiptHeaderDetails Custom = new CustomReceiptHeaderDetails();
                Custom.ReceiptId = item.ReceiptId;
                Custom.ReceiptDetailId = item.ReceiptDetailId;
                Custom.Price = item.EquivalencePrice.Value;
                Custom.ExchangeTypeTitle = item.ExchangeTypeDescription;
                Custom.ChequeNumber = item.Cheque_Number;
                Custom.DueDate = Utility.GetPersianDate(item.DueDate);
                Custom.AssignmentAccountNum = item.Bank_AccountNo;
                Custom.Unique_Account_No = item.DetailUnique_Account_No_One_Title + "/" + item.DetailUnique_Account_No;
                Custom.Receipt_Pay_TypeTitle = item.Receipt_Pay_TypeDetailDescription;
                Custom.AssignmentDate = item.StateDate;
                if (item.BankID != null)
                {
                    Custom.Bank = item.BasicBankDescription + "-" + item.Bank_BranchDesc;
                }
                CustomLs.Add(Custom);
            }
            var totalrowcount = ReceiptLs.Count();
            SearchObj.List = CustomLs;
            SearchObj.TotalItemCount =
            SearchObj.TotalPageCount = (int)Math.Ceiling(totalrowcount / (double)SearchObj.PageSize);
            SearchObj.CurrentPage = SearchObj.PageIndex;

            return PartialView(SearchObj);
        }

        /// <summary>
        /// نمایش وضعیت های چک دریافتی 
        /// </summary>
        /// <returns></returns>
        public ActionResult ReceiptDetailsById(decimal ReceiptDetailId)
        {
            int TotalRow;
            //Icls_Receipt_DetailClient ReceiptDetailsClient = new Icls_Receipt_DetailClient();
            var db = new TreasuryEntities();
            Icls_Receipt_Detail_PlanStateClient ReceiptPlanestateClient = new Icls_Receipt_Detail_PlanStateClient();
            //var ReceiptDetails = ReceiptDetailsClient.SelectRow(ReceiptDetailId).SingleOrDefault();
            var ReceiptDetails = db.Receipt_Detail.Find(ReceiptDetailId);
            ViewBag.Receipt_Detail_PlanState = ReceiptPlanestateClient.SelectBy_DynamicWhereClause("ReceiptDetailId=" + ReceiptDetailId, 1, 100, "StateID", "dec", false, out TotalRow).ToList();
            return PartialView(ReceiptDetails);
        }
        public ActionResult ShowVocher(int ReceiptDetailId, int StateID)
        {
            var db = new TreasuryEntities();
            var ls = db.Show_Voucher_Row_Reciept(ReceiptDetailId, StateID).ToList();
            return PartialView(ls.ToList());
        }
        #endregion
        /// <summary>
        /// تبدیل چک انتظامی دریافتنی به نقد
        /// </summary>
        /// <returns></returns>
        #region  ConvertOrDeleiverdLasheCheque


        public ActionResult convertCheque(int ReceiptDetailId, string DueDate)
        {
            string message, Mess1 = "";
            //Icls_Receipt_DetailClient ReceiptDetailsClient = new Icls_Receipt_DetailClient();
            var db = new TreasuryEntities();
            //var Receipt_DetailObj = ReceiptDetailsClient.SelectRow(ReceiptDetailId).SingleOrDefault();
            var Receipt_DetailObj = db.Receipt_Detail.Find(ReceiptDetailId);
            Receipt_DetailObj.DueDate = DueDate;
            //ReceiptDetailsClient.Update(Receipt_DetailObj, out Mess1);

            db.SaveChanges();
            Icls_Receipt_Detail_PlanStateClient planStateClient = new Icls_Receipt_Detail_PlanStateClient();
            var persianDate = Utility.ToPersianDate(DateTime.Now);
            Service.Receipt_Detail_PlanStateService.Receipt_Detail_PlanState planState = new Service.Receipt_Detail_PlanStateService.Receipt_Detail_PlanState();
            planState.StateDate = persianDate;
            planState.StateTime = DateTime.Now.TimeOfDay;
            planState.ReceiptDetailId = ReceiptDetailId;
            planState.Account_No = " ";
            //تبدیل چک انتظامی به نقد
            planState.PlaningTypeID = 14;


            planState.UserID = User.Id;
            planState.HostName = System.Net.Dns.GetHostName();

            var Result = planStateClient.Insert(planState, out message);

            return JsonMessage(Result, message);
        }
        #endregion
        /// <summary>
        /// تبدیل چک انتظامی دریافتنی به نقد
        /// </summary>
        /// <returns></returns>
        #region  ConvertOrDeleiverdLasheCheque

        public ActionResult GetChequePaper(int ReceiptDetailId)
        {
            string message;
            Icls_Receipt_Detail_PlanStateClient planStateClient = new Icls_Receipt_Detail_PlanStateClient();
            var persianDate = Utility.ToPersianDate(DateTime.Now);
            Service.Receipt_Detail_PlanStateService.Receipt_Detail_PlanState planState = new Service.Receipt_Detail_PlanStateService.Receipt_Detail_PlanState();
            planState.StateDate = persianDate;
            planState.StateTime = DateTime.Now.TimeOfDay;
            planState.ReceiptDetailId = ReceiptDetailId;

            //تحویل لاشه چک انتظامی
            planState.PlaningTypeID = 15;


            planState.UserID = User.Id;
            planState.HostName = System.Net.Dns.GetHostName();

            var Result = planStateClient.Insert(planState, out message);

            return JsonMessage(Result, message);
        }
        #endregion


        #region GetAllReceipt
        public ActionResult ShowAllReceiptList()
        {
            return View();
        }
        [HttpPost]
        public JsonResult GetAllReceiptList (ReceiptSearch SearchObj)
        {

            //ReceiptPayTypeId;
            //headerId;
            int totalRow, totalRow1;
            string RecieptTypeIdsss = "";
            Icls_Receipt_Pay_TypeDetailClient Receipt_Pay_TypeDetailClient = new Icls_Receipt_Pay_TypeDetailClient();
            var Receipt_Pay_TypeDetailIDLs = Receipt_Pay_TypeDetailClient.SelectAll(1, 100, "Receipt_Pay_TypeDetailID", "int", false, out totalRow).Select(d => d.Receipt_Pay_TypeDetailID);

            Icls_Reciept_ReferenceTypes_ValueClient ReceiptRefrenceTypeValue = new Icls_Reciept_ReferenceTypes_ValueClient();


            foreach (var item in Receipt_Pay_TypeDetailIDLs)
            {
                RecieptTypeIdsss += item.ToString() + ",";
            }
            var db = new TreasuryEntities();
            //Icls_SPsClient SpsClient = new Icls_SPsClient();
            var LS = (IEnumerable<DataModel.GetRecieptHeaderAndDetail_ByRecieptTypeIdsss_Result>)db.GetRecieptHeaderAndDetail_ByRecieptTypeIdsss(RecieptTypeIdsss).Where(d => d.FiscalYear == SearchObj.FiscalYear).ToList();

            if (!string.IsNullOrEmpty(SearchObj.ReferenceValue))
            {
                var RefrenceValueLs = ReceiptRefrenceTypeValue.SelectAll(1, 1000 * 1000, "ReferenceTypeValueID", "int", false, out totalRow1).Where(d => d.ReferenceValue.Contains(SearchObj.ReferenceValue));

                LS = from l in LS
                     join R in RefrenceValueLs
                     on l.ReceiptDetailId equals R.ReceiptDetailId
                     select l;
            }
            if (!string.IsNullOrEmpty(SearchObj.RecieptDateFrom) && !string.IsNullOrEmpty(SearchObj.RecieptDateTo))
            {
                LS = LS.Where(d => d.ReceiptDate.CompareTo(SearchObj.RecieptDateFrom.Replace("/", String.Empty)) >= 0 && d.ReceiptDate.CompareTo(SearchObj.RecieptDateTo.Replace("/", String.Empty)) <= 0).ToList();
            }
            if (!string.IsNullOrEmpty(SearchObj.DueDateFrom) && !string.IsNullOrEmpty(SearchObj.DueDateTo))
            {

                LS = LS.Where(d => d.DueDate.CompareTo(SearchObj.DueDateFrom) >= 0 && d.DueDate.CompareTo(SearchObj.DueDateTo) <= 0).ToList();
            }

            if (SearchObj.ReceiptDetailId != 0)
            {
                LS = LS.Where(q => q.ReceiptDetailId == SearchObj.ReceiptDetailId);
            }
            if (!string.IsNullOrEmpty(SearchObj.Unique_Account_No))
            {
                LS = LS.Where(q => q.Unique_Account_No.Trim() == SearchObj.Unique_Account_No.Trim());
            }
            if (SearchObj.ChequeNo != 0)
            {
                LS = LS.Where(q => q.Cheque_Number.Trim() == SearchObj.ChequeNo.ToString());
            }
            if (SearchObj.Price != 0)
            {
                LS = LS.Where(q => q.EquivalencePrice == SearchObj.Price);
            }
            var list = LS.OrderByDescending(q => q.ReceiptDetailId).Skip(SearchObj.PageSize * SearchObj.PageIndex).Take(SearchObj.PageSize);
            SearchObj.TotalItemCount = LS.Count();
            return Json(new
            {
                Paging = SearchObj,
                FiscalYear = SearchObj.FiscalYear,
                List = list.Select(q => new
                {
                    DueDate = q.DueDate,
                    ReferenceReceiptId = q.ReferenceReceiptId,
                    Date = Utility.GetPersianDate(q.ReceiptDate),
                    Price = q.EquivalencePrice,
                    Receipt_Pay_TypeDetailDescription = q.Receipt_Pay_TypeDetailDescription,
                    Behalf = q.BehalfDescription,
                    //Behalf = Utility.GetBehalf(q.BehalfID).Length > 15 ? Utility.GetBehalf(q.BehalfID).Substring(0, 15) + "..." : Utility.GetBehalf(q.BehalfID),
                    Cheque_Number = q.Cheque_Number.Trim(),
                    Bank = q.Bank_BranchDesc + "/" + q.Bank_AccountNo,
                    PlaningTypeDesc = q.RecieptTypeId,
                    ReceiptDetailId = q.ReceiptDetailId,
                    Unique_Account = q.Unique_Account_No + "/" + q.Account_Name,
                    //Unique_Account = q.Unique_Account_No.Trim() + " / " + Utility.GetUniqueAccount(q.Unique_Account_No.Trim()),
                    headerId = q.ReceiptId,
                    RecieptTypeId = q.RecieptTypeId,
                    //HasPlanState = Utility.HasPlanState(q.ReceiptDetailId),
                    //CanEdit = Utility.CanEditReceipt(q.ReceiptDetailId, q.ReceiptId),

                }),
                BehalfGroupLs = Utility.GetBehalfsGroupLs().Where(d => d.Receipt_Pay_TypeID == int.Parse(ConfigurationManager.AppSettings["Receipt_Type_ID"])),
                BehalfLs = "",
            }, JsonRequestBehavior.AllowGet);


        }
        public JsonResult checkAccess(decimal ReceiptDetailId)
        {
            var db = new TreasuryEntities();
            var CanDeleteReceiptDetail = db.Database.SqlQuery<bool>("SELECT [dbo].[CanDeleteReceiptDetail] (" + ReceiptDetailId + ")").Single();
            var CanEditReceiptDetail = db.Database.SqlQuery<bool>("SELECT [dbo].[CanEditReceiptDetail] (" + ReceiptDetailId + ")").Single();
            var CanDeleteReceiptDetailStates = db.Database.SqlQuery<bool>("SELECT [dbo].[CanDeleteReceiptDetailStates] (" + ReceiptDetailId + ")").Single();
            return Json(new {
                CanDeleteReceiptDetail = CanDeleteReceiptDetail,
                CanEditReceiptDetail = CanEditReceiptDetail,
                CanDeleteReceiptDetailStates = CanDeleteReceiptDetailStates
            }, JsonRequestBehavior.AllowGet);
        }
        public JsonResult GetReceiptPlanStateList(decimal ReceiptDetailId)
        {
            //int TotalRow;
            var db = new TreasuryEntities();
            //Icls_Receipt_Detail_PlanStateClient ReceiptPlanestateClient = new Icls_Receipt_Detail_PlanStateClient();
            // var Receipt_Detail_PlanState = ReceiptPlanestateClient.SelectBy_DynamicWhereClause("ReceiptDetailId=" + ReceiptDetailId, 1, 100, "StateID", "dec", false, out TotalRow).ToList();
            var Receipt_Detail_PlanState = db.GetReceipt_Detail_PlanState_Info(ReceiptDetailId);
            return Json(Receipt_Detail_PlanState.Select(q => new
            {
                PlaningTypeID = q.PlaningTypeDesc,
                PlaningID = q.PlaningTypeID,
                Date = Utility.GetPersianDate(q.StateDate),
                Account_code = q.Account_No,
                Unique_Account_No = q.Unique_Account_No,
                Unique_Account_No_Two = q.Unique_Account_No_Two,
                StateID = q.StateID,
                ReceiptDetailId = q.ReceiptDetailId,
                hasVoucher = q.AccVoucherNo,
                IsLastStateId = q.IsLastStateId

            }), JsonRequestBehavior.AllowGet);
        }
        #endregion
        /// <summary>
        /// گزارش تحویل لاشه چک /سفته انتظامی
        /// </summary>
        /// <param name="ReceiptPayTypeId"></param>
        /// <param name="headerId"></param>
        /// <param name="ReceiptDetailId"></param>
        /// <returns></returns>
        #region Checkdelivey
        public ActionResult CheckdeliveyReport(int ReceiptPayTypeId, int headerId = 0, int ReceiptDetailId = 0)
        {
            return View();
        }
        public ActionResult LoadCheckdeliveyReport(int ReceiptPayTypeId, int headerId = 0, int ReceiptDetailId = 0)
        {
            StiReport report = new StiReport();
            string Path = Server.MapPath("~/Report/Checkdelivey.mrt");
            report.Load(Path);
            int totalRow;
            var db = new TreasuryEntities();
            Icls_SPsClient spsClient = new Icls_SPsClient();
            //Icls_Receipt_DetailClient receiptDetailsClient = new Icls_Receipt_DetailClient();
            Icls_Receipt_Detail_PlanStateClient planstateClient = new Icls_Receipt_Detail_PlanStateClient();
            //var ReceiptDetail = receiptDetailsClient.SelectRow(ReceiptDetailId).SingleOrDefault();
            var ReceiptDetail = db.Receipt_Detail.Find(ReceiptDetailId);
            Icls_Reciept_ReferenceTypes_ValueClient ReceiptRefrenceTypeValue = new Icls_Reciept_ReferenceTypes_ValueClient();
            var RefrenceValue = ReceiptRefrenceTypeValue.SelectBy_DynamicWhereClause("ReceiptDetailId =" + ReceiptDetailId, 1, 1000 * 1000, "ReferenceTypeValueID", "int", false, out totalRow);
            Icls_Receipt_Detail_PlanStateClient ReceiptDetailsPlan = new Icls_Receipt_Detail_PlanStateClient();


            report.Dictionary.Variables["DateNow"].Value = Utility.DateStructure(Utility.ToPersianDate(DateTime.Now));
            report.Dictionary.Variables["Price"].Value = (string.Format("{0:n0}", (Int64)ReceiptDetail.ChequePrice_Rial)).ToString();  //obj.FirstOrDefault().PayAmount.ToString().Replace(".00", "").ToString(); //obj.FirstOrDefault().PayAmount.ToString().Replace(".00", "").ToString();
            report.Dictionary.Variables["PriceInWord"].Value = NumberToPersianText.num2str(ReceiptDetail.ChequePrice_Rial.ToString("0.00").Replace(".00", ""));
            //report.Dictionary.Variables["Behalf"].Value = ReceiptDetail.BehalfDescription;
            //report.Dictionary.Variables["BehalfGroup"].Value = ReceiptDetail.BehalfGroupDescription;
            report.Dictionary.Variables["Unique_Account"].Value = Utility.GetUniqueAccount(ReceiptDetail.Unique_Account_No);
            report.Dictionary.Variables["ChequeNum"].Value = ReceiptDetail.Cheque_Number;
            report.Dictionary.Variables["DueDate"].Value = Utility.GetPersianDate(ReceiptDetail.DueDate);
            report.Dictionary.Variables["AccountNum"].Value = ReceiptDetail.Bank_AccountNo;
            report.Dictionary.Variables["Bank"].Value = Utility.GetBasicBankInfo(ReceiptDetail.BankID.Value);
            report.Dictionary.Variables["Branch"].Value = ReceiptDetail.Bank_BranchDesc;
            report.Dictionary.Variables["PaymentNum"].Value = ReceiptDetail.ReceiptDetailId.ToString();
            report.Dictionary.Variables["CompanyName"].Value = ConfigurationManager.AppSettings["CompanyTitle"];


            report.Dictionary.SynchronizeBusinessObjects();
            return StiMvcViewer.GetReportSnapshotResult(HttpContext, report);

        }

        #endregion
        /// <summary>
        ///رسیددریافت
        /// </summary>
        /// <returns></returns>
        #region ReceiptReport
        public ActionResult ReceiptReportdelivey(int ReceiptPayTypeId, int headerId = 0, int ReceiptDetailId = 0)
        {
            return View();
        }
        public ActionResult LoadReceiptReportdelivey(int ReceiptPayTypeId, int headerId = 0, int ReceiptDetailId = 0)
        {
            StiReport report = new StiReport();
            string Path = Server.MapPath("~/Report/ReceiptReportdelivey.mrt");
            report.Load(Path);

            var db = new TreasuryEntities();
            int totalRow;
            Icls_SPsClient spsClient = new Icls_SPsClient();
            //Icls_Receipt_DetailClient receiptDetailsClient = new Icls_Receipt_DetailClient();
            Icls_Receipt_Detail_PlanStateClient planstateClient = new Icls_Receipt_Detail_PlanStateClient();
            //var ReceiptDetail = receiptDetailsClient.SelectRow(ReceiptDetailId).SingleOrDefault();
            var ReceiptDetail = db.Receipt_Detail.Find(ReceiptDetailId);
            Icls_Reciept_ReferenceTypes_ValueClient ReceiptRefrenceTypeValue = new Icls_Reciept_ReferenceTypes_ValueClient();
            var RefrenceValue = ReceiptRefrenceTypeValue.SelectBy_DynamicWhereClause("ReceiptDetailId =" + ReceiptDetailId, 1, 1000 * 1000, "ReferenceTypeValueID", "int", false, out totalRow);
            Icls_Receipt_Detail_PlanStateClient ReceiptDetailsPlan = new Icls_Receipt_Detail_PlanStateClient();


            report.Dictionary.Variables["Price"].Value = (string.Format("{0:n0}", ReceiptDetail.ChequePrice_Rial)).ToString();
            report.Dictionary.Variables["BehalfGroup"].Value = Utility.GetBehalfGroupBybehalfId(ReceiptDetail.BehalfID).BehalfGroupDescription;
            report.Dictionary.Variables["Behalf"].Value = Utility.GetBehalf(ReceiptDetail.BehalfID);
            report.Dictionary.Variables["ChequeNum"].Value = ReceiptDetail.Cheque_Number;
            report.Dictionary.Variables["Branch"].Value = ReceiptDetail.Bank_BranchDesc;
            if (ReceiptPayTypeId == 18 || ReceiptPayTypeId == 19 || ReceiptPayTypeId == 21)
            {
                report.Dictionary.Variables["DueDate"].Value = Utility.DateStructure(ReceiptDetail.DueDate);
            }
            report.Dictionary.Variables["AccountNum"].Value = ReceiptDetail.Bank_AccountNo;
            report.Dictionary.Variables["Branch"].Value = ReceiptDetail.Bank_BranchDesc;
            report.Dictionary.Variables["Bank"].Value = Utility.GetBasicBankInfo(ReceiptDetail.BankID.Value);
            report.Dictionary.Variables["Unique_Account"].Value = Utility.GetUniqueAccount(ReceiptDetail.Unique_Account_No);
            report.Dictionary.Variables["DateNow"].Value = Utility.DateStructure(Utility.ToPersianDate(DateTime.Now));
            report.Dictionary.Variables["PriceInWord"].Value = NumberToPersianText.num2str(ReceiptDetail.ChequePrice_Rial.ToString("0.00").Replace(".00", ""));
            report.Dictionary.Variables["ReceiptNum"].Value = ReceiptDetail.ReceiptDetailId.ToString();
            report.Dictionary.Variables["Type"].Value = Utility.GetReceipt_Pay_TypeDetailTitle(ReceiptPayTypeId);

            var planstateLs = planstateClient.SelectBy_DynamicWhereClause("ReceiptDetailId =" + ReceiptDetailId, 1, 100, "StateID", "dec", false, out totalRow);

            report.Dictionary.SynchronizeBusinessObjects();
            return StiMvcViewer.GetReportSnapshotResult(HttpContext, report);

        }

        public ActionResult ReceiptReport(int ReceiptPayTypeId, int headerId = 0, int ReceiptDetailId = 0)
        {
            return View();
        }
        public ActionResult LoadReceiptReport(int ReceiptPayTypeId, int headerId = 0, int ReceiptDetailId = 0)
        {
            StiReport report = new StiReport();
            string Path = Server.MapPath("~/Report/ReceiptReport.mrt");
            report.Load(Path);
            var db = new TreasuryEntities();

            int totalRow;
            Icls_SPsClient spsClient = new Icls_SPsClient();
            //Icls_Receipt_DetailClient receiptDetailsClient = new Icls_Receipt_DetailClient();
            Icls_Receipt_Detail_PlanStateClient planstateClient = new Icls_Receipt_Detail_PlanStateClient();
            //var ReceiptDetail = receiptDetailsClient.SelectRow(ReceiptDetailId).SingleOrDefault();
            var ReceiptDetail = db.Receipt_Detail.Find(ReceiptDetailId);
            Icls_Reciept_ReferenceTypes_ValueClient ReceiptRefrenceTypeValue = new Icls_Reciept_ReferenceTypes_ValueClient();
            var RefrenceValue = ReceiptRefrenceTypeValue.SelectBy_DynamicWhereClause("ReceiptDetailId =" + ReceiptDetailId, 1, 1000 * 1000, "ReferenceTypeValueID", "int", false, out totalRow);
            Icls_Receipt_Detail_PlanStateClient ReceiptDetailsPlan = new Icls_Receipt_Detail_PlanStateClient();


            report.Dictionary.Variables["Price"].Value = (string.Format("{0:n0}", ReceiptDetail.ChequePrice_Rial)).ToString();
            report.Dictionary.Variables["BehalfGroup"].Value = Utility.GetBehalfGroupBybehalfId(ReceiptDetail.BehalfID).BehalfGroupDescription;
            report.Dictionary.Variables["Behalf"].Value = Utility.GetBehalf(ReceiptDetail.BehalfID);
            report.Dictionary.Variables["ChequeNum"].Value = ReceiptDetail.Cheque_Number;
            report.Dictionary.Variables["Branch"].Value = ReceiptDetail.Bank_BranchDesc;
            if (ReceiptPayTypeId == 18 || ReceiptPayTypeId == 19 || ReceiptPayTypeId == 21)
            {
                report.Dictionary.Variables["DueDate"].Value = Utility.DateStructure(ReceiptDetail.DueDate);
            }
            report.Dictionary.Variables["AccountNum"].Value = ReceiptDetail.Bank_AccountNo;
            report.Dictionary.Variables["Branch"].Value = ReceiptDetail.Bank_BranchDesc;
            report.Dictionary.Variables["Bank"].Value = Utility.GetBasicBankInfo(ReceiptDetail.BankID.Value);
            report.Dictionary.Variables["Unique_Account"].Value = Utility.GetUniqueAccount(ReceiptDetail.Unique_Account_No);
            report.Dictionary.Variables["DateNow"].Value = Utility.DateStructure(Utility.ToPersianDate(DateTime.Now));
            report.Dictionary.Variables["PriceInWord"].Value = NumberToPersianText.num2str(ReceiptDetail.ChequePrice_Rial.ToString("0.00").Replace(".00", ""));
            report.Dictionary.Variables["ReceiptNum"].Value = ReceiptDetail.ReceiptDetailId.ToString();
            report.Dictionary.Variables["Type"].Value = Utility.GetReceipt_Pay_TypeDetailTitle(ReceiptPayTypeId);
            //report.Dictionary.Variables["RefrenceValue"].Value = RefrenceValue.SingleOrDefault().ReferenceValue;

            var planstateLs = planstateClient.SelectBy_DynamicWhereClause("ReceiptDetailId =" + ReceiptDetailId, 1, 100, "StateID", "dec", false, out totalRow);
            var ls = new List<Show_Voucher_Row_Reciept_Result>();

            foreach (var item in planstateLs)
            {
                var ls1 = db.Show_Voucher_Row_Reciept(ReceiptDetailId, Int64.Parse(item.StateID.ToString()));
                ls.AddRange(ls1);
            }

            report.RegBusinessObject("VocherLS", ls);
            report.Dictionary.SynchronizeBusinessObjects();
            return StiMvcViewer.GetReportSnapshotResult(HttpContext, report);

        }

        //ایجاد پرینت
        public ActionResult PrintReport()
        {
            return StiMvcViewer.PrintReportResult(this.HttpContext);

        }
        //ایجاد خروجی
        public ActionResult ExportReport()
        {
            return StiMvcViewer.ExportReportResult();
        }

        public ActionResult ViewerEvent()
        {
            return StiMvcViewer.ViewerEventResult(this.HttpContext);
        }

        public ActionResult Interaction()
        {
            return StiMvcViewer.InteractionResult(this.HttpContext);
        }
        #endregion
        //#region ReceiptPlaning

        //public ActionResult ReceiptPlaning(int ReceiptDetailId)
        //{
        //    var dc = new TreasuryEntities();
        //    int TotalRow;
        //    //نوع ذینفع  
        //    var unique_account_type = dc.v_unique_account_type.ToList();
        //    unique_account_type.Add(new v_unique_account_type { Code = 0, Description = Resourse.Resources.Others });
        //    ViewBag.unique_account_type = unique_account_type;

        //    Icls_PlaningTypeClient planningTypeClient = new Icls_PlaningTypeClient();
        //    ViewBag.planningTypeLs = planningTypeClient.SelectAll(1, 100, "PlaningTypeID", "int", false, out TotalRow).ToList();
        //    return View(new Service.Receipt_Detail_PlanState.Receipt_Detail_PlanState() { ReceiptDetailId = ReceiptDetailId });
        //}
        //[HttpPost]
        //public ActionResult ReceiptPlaning(Service.Receipt_Detail_PlanState.Receipt_Detail_PlanState obj)
        //{
        //    string message;
        //    Service.Receipt_Detail_PlanState.Icls_Receipt_Detail_PlanStateClient PlanStateClient = new Service.Receipt_Detail_PlanState.Icls_Receipt_Detail_PlanStateClient();
        //    PlanStateClient.Insert(obj, out message);
        //    return View();
        //}
        //#endregion


    }
}