using CSD.Treasury.Service.PlaningTypeService;
using CSD.Treasury.Service.ProceduresDB;
using CSD.Treasury.Service.Receipt_Detail_PlanStateService;
using CSD.Treasury.UI.Controllers;
using CSD.Treasury.UI.Models;
using Stimulsoft.Report;
using Stimulsoft.Report.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Configuration;
using CSD.Treasury.DataModel;

namespace CSD.Treasury.UI.Areas.Report.Controllers
{
    [Authorize(Roles = "Admin,Manager,Report,ReportInApp")]
    public class ReceiptCheckController : BaseController
    {
        // GET: Report/ReceiptCheck
        public ActionResult Index(ReceiptSearch SearchObj)
        {
            //نمایش لیستی از انواع پرداخت
            ViewBag.Receipt_Pay_TypeDetailLs = Utility.GetReceipt_Pay_TypeDetailLs().Where(d => d.Receipt_Pay_TypeID == 2).ToList();
            //لیستی از نوع ذینفع 
            var dc = new TreasuryEntities();
            var unique_account_type = dc.v_unique_account_type.ToList();
            unique_account_type.Add(new v_unique_account_type { Code = 0, Description = Resourse.Resources.Others });
            ViewBag.unique_account_type = unique_account_type;

            //گروه بابت ها
            ViewBag.BehalfsGroupLs = Utility.GetBehalfsGroupLs().Where(d => d.Receipt_Pay_TypeID == int.Parse(ConfigurationManager.AppSettings["Receipt_Type_ID"]));



            ViewBag.Receipt_Pay_TypeDetailLs = Utility.GetReceipt_Pay_TypeDetailLs().Where(d => d.Receipt_Pay_TypeID == 2).ToList();
            return View(SearchObj);
        }
        public ActionResult List(ReceiptSearch SearchObj)
        {
            int pageNum = SearchObj.PageIndex;
            Icls_SPsClient spsClient = new Icls_SPsClient();
            Icls_PlaningTypeClient PlaningTypeClient = new Icls_PlaningTypeClient();
            Icls_Receipt_Detail_PlanStateClient ReceiptDetailsClinet = new Icls_Receipt_Detail_PlanStateClient();
            //var ReceiptPayTypeDetailLs = Utility.GetReceipt_Pay_TypeDetailLs().Where(d => d.Receipt_Pay_TypeID == 2).ToList();
            //int TotalRow;
            ////براساس نوع دریافت فیلتر میگردد
            //if(string.IsNullOrEmpty(SearchObj.RecieptTypeId2))
            //{
            //    SearchObj .RecieptTypeId2= "18,19,21,25,28";
            //}

            //var ReceiptLs = spsClient.GetRecieptHeaderAndDetail_ByRecieptTypeId( SearchObj.RecieptTypeId);
            List<Service.ProceduresDB.GetRecieptHeaderAndDetail_ByRecieptTypeIdsss_Result> ReceiptLs;
            if (SearchObj.isCheck)
            {
                ReceiptLs=spsClient.GetRecieptHeaderAndDetail_ByRecieptTypeIdsss("18,19,21,25,28").ToList();
            }
            else
            {
                ReceiptLs = spsClient.GetRecieptHeaderAndDetail_ByRecieptTypeIdsss("22,23,24,27").ToList();
            }
            if(SearchObj.RecieptTypeId!=0)
            {
                ReceiptLs = spsClient.GetRecieptHeaderAndDetail_ByRecieptTypeIdsss(SearchObj.RecieptTypeId.ToString()).ToList();
            }

            if (SearchObj.ExchangeTypeID!= 0)
            {
                ReceiptLs = ReceiptLs.Where(d => d.ExchangeTypeID == SearchObj.ExchangeTypeID).ToList();
            }

            if (SearchObj.Price != 0)
            {
                ReceiptLs = ReceiptLs.Where(d => d.EquivalencePrice == SearchObj.Price).ToList();
            }

            if (SearchObj.ChequeNo !=0)
            {
                ReceiptLs = ReceiptLs.Where(d => d.Cheque_Number == (SearchObj.ChequeNo).ToString()).ToList();
            }

            if (SearchObj.BehalfID != 0)
            {
                ReceiptLs = ReceiptLs.Where(d => d.BehalfID == SearchObj.BehalfID).ToList();
            }
            //براساس وضعیت فیلتر میشود
            if (SearchObj.State != 0)
            {
                ReceiptLs = ReceiptLs.Where(d => d.PlaningTypeID == SearchObj.State).ToList();
            }
            //else if (SearchObj.State == 2)
            //{
            //    ReceiptLs = ReceiptLs.Where(d => d.PlaningTypeID == 2).ToList();
            //}
            //else if (SearchObj.State == 3)
            //{
            //    ReceiptLs = ReceiptLs.Where(d => d.PlaningTypeID == 1).ToList();
            //}
            //else if (SearchObj.State == 4)
            //{
            //    ReceiptLs = ReceiptLs.Where(d => d.PlaningTypeID == 5).ToList();
            //}

            if (SearchObj.ReceiptId != 0)
            {
                ReceiptLs = ReceiptLs.Where(d => d.ReceiptId == SearchObj.ReceiptId).ToList();
            }
            
            if (!string.IsNullOrEmpty(SearchObj.DueDateFrom) && !string.IsNullOrEmpty(SearchObj.DueDateTo))
            {
                ReceiptLs = ReceiptLs.Where(d => d.DueDate.CompareTo(SearchObj.DueDateFrom) >= 0 && d.DueDate.CompareTo(SearchObj.DueDateTo) <= 0).ToList();
            }
            if (!string.IsNullOrEmpty(SearchObj.RecieptDateFrom) && !string.IsNullOrEmpty(SearchObj.RecieptDateTo))
            {
                ReceiptLs = ReceiptLs.Where(d => d.ReceiptDate.CompareTo(SearchObj.RecieptDateFrom.Replace("/", String.Empty)) >= 0 && d.ReceiptDate.CompareTo(SearchObj.RecieptDateTo.Replace("/", String.Empty)) <= 0).ToList();
            }
            //else if(!string.IsNullOrEmpty(SearchObj.FiscalYear))
            //{
            //    ReceiptLs = ReceiptLs.Where(d => d.FiscalYear == SearchObj.FiscalYear).ToList();
            //}
            if (!string.IsNullOrEmpty(SearchObj.Unique_Account_No))
            {
                ReceiptLs = ReceiptLs.Where(d => d.Unique_Account_No == SearchObj.Unique_Account_No).ToList();
            }

            var QueryLs = ReceiptLs.OrderBy(d => d.DueDate).Skip(SearchObj.PageSize * SearchObj.PageIndex).Take(SearchObj.PageSize).ToList();

            List<CustomReceiptHeaderDetails> CustomLs = new List<CustomReceiptHeaderDetails>();


            foreach (var item in QueryLs)
            {
                CustomReceiptHeaderDetails Custom = new CustomReceiptHeaderDetails();
                Custom.ReceiptId = item.ReceiptId;
                Custom.ReceiptDetailId = item.ReceiptDetailId;
                Custom.Price = item.EquivalencePrice.Value;
                if (Utility.GetExchaneName(item.ExchangeTypeID) == "")
                {
                    Custom.ExchangeTypeTitle = "ریال";
                }
                else
                {
                    Custom.ExchangeTypeTitle = Utility.GetExchaneName(item.ExchangeTypeID);
                }
                
                Custom.ChequeNumber = item.Cheque_Number;
                Custom.DueDate = item.DueDate;
                Custom.AssignmentAccountNum = item.Bank_AccountNo;
                Custom.Unique_Account_No = item.Unique_Account_No +"/"+item.Account_Name;
                Custom.Receipt_Pay_TypeTitle =item.Receipt_Pay_TypeDetailDescription;
                Custom.AssignmentDate = item.StateDate;
                Custom.PlaningTypeDesc = item.PlaningTypeDesc;
                Custom.BehalfTitle = Utility.GetBehalf(item.BehalfID);
                Custom.UserName = Utility.GetUserName(item.UserID);
                Custom.ReceiptDate = item.ReceiptDate;
                
                
                CustomLs.Add(Custom);
            }

            SearchObj.List = CustomLs;
            SearchObj.BankAccountLs = Utility.GetBanks();
            SearchObj.v_accountslS = Utility.v_accountsLS();
            //SearchObj.ReceiptPayTypeDetailLs = ReceiptPayTypeDetailLs;
            //SearchObj.PlaningTypeLS = PlaningTypeClient.SelectAll(1, 100, "PlaningTypeID", "int", false, out TotalRow).ToList();

            SearchObj.TotalPageCount = (int)Math.Ceiling((double)ReceiptLs.Count() / (double)SearchObj.PageSize);
            SearchObj.CurrentPage = pageNum;

            SearchObj.CurrentPage = SearchObj.PageIndex;
            SearchObj.TotalItemCount = ReceiptLs.Count();


            return PartialView(SearchObj);
        }

        #region Report
        [HttpPost]
        public ActionResult ShowReport(ReceiptSearch obj)
        {
            return View(obj);
        }
        public ActionResult LoadReport(ReceiptSearch SearchObj)
        {
            StiReport report = new StiReport();
            SearchObj.isCheck = true;
            //Icls_SPsClient spsClient = new Icls_SPsClient();
            //if (SearchObj.RecieptTypeId == 0)
            //{
            //    SearchObj.RecieptTypeId = 18;
            //}
            ////براساس نوع دریافت فیلتر میگردد
            //var ReceiptLs = spsClient.GetRecieptHeaderAndDetail_ByRecieptTypeId(SearchObj.RecieptTypeId).ToList();

            ////براساس وضعیت فیلتر میشود
            //if (SearchObj.State == 1)
            //{
            //    ReceiptLs = ReceiptLs.Where(d => d.PlaningTypeID == 1).ToList();
            //}
            //else if (SearchObj.State == 2)
            //{
            //    ReceiptLs = ReceiptLs.Where(d => d.PlaningTypeID == 2).ToList();
            //}
            //else if (SearchObj.State == 3)
            //{
            //    ReceiptLs = ReceiptLs.Where(d => d.PlaningTypeID == 1).ToList();
            //}
            //else if (SearchObj.State == 4)
            //{
            //    ReceiptLs = ReceiptLs.Where(d => d.PlaningTypeID == 5).ToList();
            //}

            //if (SearchObj.ReceiptId != 0)
            //{
            //    ReceiptLs = ReceiptLs.Where(d => d.ReceiptId == SearchObj.ReceiptId).ToList();
            //}
            //if (!string.IsNullOrEmpty(SearchObj.DueDateFrom) && !string.IsNullOrEmpty(SearchObj.DueDateTo))
            //{
            //    ReceiptLs = ReceiptLs.Where(d => d.DueDate.CompareTo(SearchObj.DueDateFrom) >= 0 && d.DueDate.CompareTo(SearchObj.DueDateTo) <= 0).ToList();
            //}
            //if (!string.IsNullOrEmpty(SearchObj.RecieptDateFrom) && !string.IsNullOrEmpty(SearchObj.RecieptDateTo))
            //{
            //    ReceiptLs = ReceiptLs.Where(d => d.DueDate.CompareTo(SearchObj.RecieptDateFrom.Replace("/", String.Empty)) >= 0 && d.DueDate.CompareTo(SearchObj.RecieptDateTo.Replace("/", String.Empty)) <= 0).ToList();
            //}
            //List<CustomReceiptHeaderDetails> CustomLs = new List<CustomReceiptHeaderDetails>();


            //foreach (var item in ReceiptLs)
            //{
            //    CustomReceiptHeaderDetails Custom = new CustomReceiptHeaderDetails();
            //    Custom.ReceiptId = item.ReceiptId;
            //    Custom.ReceiptDetailId = item.ReceiptDetailId;
            //    Custom.Price = decimal.Parse(Math.Truncate(item.EquivalencePrice.Value).ToString("#,##0"));
            //    Custom.ExchangeTypeTitle = item.ExchangeTypeDescription;
            //    Custom.ChequeNumber = item.Cheque_Number;
            //    Custom.DueDate =Utility.GetPersianDate(item.DueDate);
            //    Custom.AssignmentAccountNum = item.Bank_AccountNo;
            //    Custom.Unique_Account_No =item.DetailUnique_Account_No_One_Title+"-"+item.DetailUnique_Account_No;
            //    Custom.Receipt_Pay_TypeTitle = item.Receipt_Pay_TypeDetailDescription;
            //    Custom.AssignmentDate = item.StateDate;
            //    Custom.PlaningTypeDesc = item.PlaningTypeDesc;
            //    Custom.BehalfTitle =item.BehalfDescription;
            //    Custom.Refrences = Utility.GetRecieveReferenceTypeValue(item.ReceiptDetailId, true);
            //    Custom.UserName = item.User_FullName;
            //    CustomLs.Add(Custom);
            //}
            int pageNum = SearchObj.PageIndex;
            Icls_SPsClient spsClient = new Icls_SPsClient();
            Icls_PlaningTypeClient PlaningTypeClient = new Icls_PlaningTypeClient();
            Icls_Receipt_Detail_PlanStateClient ReceiptDetailsClinet = new Icls_Receipt_Detail_PlanStateClient();
            //var ReceiptPayTypeDetailLs = Utility.GetReceipt_Pay_TypeDetailLs().Where(d => d.Receipt_Pay_TypeID == 2).ToList();
            //int TotalRow;
            ////براساس نوع دریافت فیلتر میگردد
            //if(string.IsNullOrEmpty(SearchObj.RecieptTypeId2))
            //{
            //    SearchObj .RecieptTypeId2= "18,19,21,25,28";
            //}

            //var ReceiptLs = spsClient.GetRecieptHeaderAndDetail_ByRecieptTypeId( SearchObj.RecieptTypeId);
            List<Service.ProceduresDB.GetRecieptHeaderAndDetail_ByRecieptTypeIdsss_Result> ReceiptLs;
            if (SearchObj.isCheck)
            {
                ReceiptLs = spsClient.GetRecieptHeaderAndDetail_ByRecieptTypeIdsss("18,19,21,25,28").ToList();
            }
            else
            {
                ReceiptLs = spsClient.GetRecieptHeaderAndDetail_ByRecieptTypeIdsss("22,23,24,27").ToList();
            }
            if (SearchObj.RecieptTypeId != 0)
            {
                ReceiptLs = spsClient.GetRecieptHeaderAndDetail_ByRecieptTypeIdsss(SearchObj.RecieptTypeId.ToString()).ToList();
            }
            //براساس وضعیت فیلتر میشود
            if (SearchObj.State != 0)
            {
                ReceiptLs = ReceiptLs.Where(d => d.PlaningTypeID == SearchObj.State).ToList();
            }
            
            if (SearchObj.ExchangeTypeID!=0)
            {
                ReceiptLs = ReceiptLs.Where(d => d.ExchangeTypeID == SearchObj.State).ToList();
            }
            //else if (SearchObj.State == 2)
            //{
            //    ReceiptLs = ReceiptLs.Where(d => d.PlaningTypeID == 2).ToList();
            //}
            //else if (SearchObj.State == 3)
            //{
            //    ReceiptLs = ReceiptLs.Where(d => d.PlaningTypeID == 1).ToList();
            //}
            //else if (SearchObj.State == 4)
            //{
            //    ReceiptLs = ReceiptLs.Where(d => d.PlaningTypeID == 5).ToList();
            //}

            if (SearchObj.ReceiptId != 0)
            {
                ReceiptLs = ReceiptLs.Where(d => d.ReceiptId == SearchObj.ReceiptId).ToList();
            }
            if (!string.IsNullOrEmpty(SearchObj.DueDateFrom) && !string.IsNullOrEmpty(SearchObj.DueDateTo))
            {
                ReceiptLs = ReceiptLs.Where(d => d.DueDate.CompareTo(SearchObj.DueDateFrom) >= 0 && d.DueDate.CompareTo(SearchObj.DueDateTo) <= 0).ToList();
            }
            if (!string.IsNullOrEmpty(SearchObj.RecieptDateFrom) && !string.IsNullOrEmpty(SearchObj.RecieptDateTo))
            {
                ReceiptLs = ReceiptLs.Where(d => d.ReceiptDate.CompareTo(SearchObj.RecieptDateFrom.Replace("/", String.Empty)) >= 0 && d.ReceiptDate.CompareTo(SearchObj.RecieptDateTo.Replace("/", String.Empty)) <= 0).ToList();
            }
            //if (!string.IsNullOrEmpty(SearchObj.FiscalYear))
            //{
            //    ReceiptLs = ReceiptLs.Where(d => d.FiscalYear == SearchObj.FiscalYear).ToList();
            //}
            if (!string.IsNullOrEmpty(SearchObj.Unique_Account_No))
            {
                ReceiptLs = ReceiptLs.Where(d => d.Unique_Account_No == SearchObj.Unique_Account_No).ToList();
            }
            var QueryLs = ReceiptLs.OrderBy(d => d.DueDate).ToList();

            List<CustomReceiptHeaderDetails> CustomLs = new List<CustomReceiptHeaderDetails>();


            foreach (var item in QueryLs)
            {
                CustomReceiptHeaderDetails Custom = new CustomReceiptHeaderDetails();
                Custom.ReceiptId = item.ReceiptId;
                Custom.ReceiptDetailId = item.ReceiptDetailId;
                Custom.Price = decimal.Parse(Math.Truncate(item.EquivalencePrice.Value).ToString("#,##0")); 
                Custom.ExchangeTypeTitle = Utility.GetExchaneName(item.ExchangeTypeID);
                Custom.ChequeNumber = item.Cheque_Number;
                Custom.DueDate = item.DueDate;
                Custom.AssignmentAccountNum = item.Bank_AccountNo;
                Custom.Unique_Account_No = item.Unique_Account_No + "/" + item.Account_Name;
                Custom.Receipt_Pay_TypeTitle = item.Receipt_Pay_TypeDetailDescription;
                Custom.AssignmentDate = item.StateDate;
                Custom.PlaningTypeDesc = item.PlaningTypeDesc;
                Custom.BehalfTitle = Utility.GetBehalf(item.BehalfID);
                Custom.UserName = Utility.GetUserName(item.UserID);
                Custom.Refrences =Utility.GetRecieveReferenceTypeValue(item.ReceiptDetailId, true,true);
                CustomLs.Add(Custom);
            }

            string Path = Server.MapPath("~/Report/ReceiptChecksLs.mrt");
            report.Load(Path);
            report.Dictionary.Variables["CompanyName"].Value = ConfigurationManager.AppSettings["CompanyTitle"];
            report.RegBusinessObject("ReceiptObj", CustomLs);
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

        #endregion
    }
}