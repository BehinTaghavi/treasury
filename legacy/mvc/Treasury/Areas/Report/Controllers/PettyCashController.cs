using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using CSD.Treasury.Service.PettyCash_TransactionHeaderService;
using CSD.Treasury.Service.PettyCash_TransactionDetailService;
using CSD.Treasury.Service.PettyCashService;
using CSD.Treasury.Service.PettyCash_BehalfGroupsService;
using CSD.Treasury.Service.PettyCash_ReferenceTypes_ValueService;
using CSD.Treasury.Service.ProceduresDB;
using Stimulsoft.Report.Mvc;
using Stimulsoft.Report;
using CSD.Treasury.UI.Models;
using CSD.Treasury.UI.Controllers;
using System.Configuration;
using CSD.Treasury.DataModel;

namespace CSD.Treasury.UI.Areas.Report.Controllers
{
    [Authorize(Roles = "Admin,Manager,Report,ReportInApp")]
    public class PettyCashController : BaseController
    {
        // GET: Report/PettyCash
        public ActionResult PettyCashReport(int headerId)
        {
            return View();
        }
        public ActionResult LoadPettyCashReport(int headerId)
        {
            StiReport report = new StiReport();

            string Path = Server.MapPath("~/Report/PettyCashReport.mrt");
            report.Load(Path);
            int TotalRow;
            Icls_PettyCash_TransactionHeaderClient HeaderClient = new Icls_PettyCash_TransactionHeaderClient();
            Icls_PettyCash_TransactionDetailClient DetailsClient = new Icls_PettyCash_TransactionDetailClient();
            Icls_PettyCash_ReferenceTypes_ValueClient RefrenceTypeValueClient = new Icls_PettyCash_ReferenceTypes_ValueClient();
            Icls_PettyCashClient PettyCashClient = new Icls_PettyCashClient();



            var Header = HeaderClient.SelectRow(headerId).SingleOrDefault();
            var PettyCash = PettyCashClient.SelectRow(Header.PettyCashID).SingleOrDefault();

            report.Dictionary.Variables["PettyCashName"].Value = PettyCash.PettyCashName;
            report.Dictionary.Variables["Unique_Account_Title"].Value = PettyCash.Unique_Account_No + " / " + Utility.GetUniqueAccount(PettyCash.Unique_Account_No);
            report.Dictionary.Variables["Account_No"].Value = PettyCash.Account_No + " / " + Utility.Getaccounts(PettyCash.Account_No);
            report.Dictionary.Variables["Price"].Value = ((int)PettyCash.PettyCash_Ammount.Value).ToString();
            report.Dictionary.Variables["headerId"].Value = headerId.ToString();
            report.Dictionary.Variables["Date"].Value = Utility.GetPersianDate(Utility.ToPersianDate(DateTime.Now));
            report.Dictionary.Variables["CompanyName"].Value = ConfigurationManager.AppSettings["CompanyTitle"];


            var DetailsLs = DetailsClient.SelectBy_DynamicWhereClause("PettyCashTransactionHeaderID = " + headerId, 1, 200, "PettyCashTransactionDetailID", "dec", false, out TotalRow);
            List<CustomPettyCash_TransactionDetail> ls = new List<CustomPettyCash_TransactionDetail>();
            foreach (var item in DetailsLs.OrderBy(q => q.PettyCashTransactionDetailID))
            {
                CustomPettyCash_TransactionDetail customOBJ = new CustomPettyCash_TransactionDetail();
                //customOBJ.BehalfGroupTiltle = Utility.GetBehalfGroup(item.BehalfGroupID);
                //customOBJ.BehalfTitle = Utility.GetBehalf(item.BehalfID);
                customOBJ.PayDate = Utility.GetPersianDate(item.PayDate);
                customOBJ.Price = (int)item.Price;
                customOBJ.Description = item.Description;
                customOBJ.Account_No = item.Account_no + " / " + Utility.Getaccounts(item.Account_no);
                customOBJ.Unique_Account_No1 = Utility.GetUniqueAccount(item.Unique_Account_No);
                customOBJ.Unique_Account_No2 = Utility.GetUniqueAccount(item.Unique_Account_No_two);
                //customOBJ.RefrenceTitle1 = "";
                //customOBJ.RefrenceTitle2 = "";
                customOBJ.Status = (item.IsAccept ? "تایید" : (item.IsVakhahi ? "واخواهی" : null));
                customOBJ.IsAcceptVakhahi = item.IsAcceptVakhahi;
                customOBJ.EllateVakhahi = item.EllateVakhahi;
                ls.Add(customOBJ);

            }
            report.RegBusinessObject("PettyCashLs", ls);
            report.Dictionary.SynchronizeBusinessObjects();

            return StiMvcViewer.GetReportSnapshotResult(HttpContext, report);
        }


        #region PettyCashListReport
        public ActionResult SearchView(SearchModelPettyCash2 model)
        {
            Icls_PettyCashClient PettyCashClient = new Icls_PettyCashClient();
            var dc = new TreasuryEntities();
            int totalrow;
            string where = "1=1";
            if (!User.IsInRole("Admin") && !User.IsInRole("Manager"))
            {
                where = where + " and UserID=" + User.Id;
            }
            //نوع ذینفع  
            var unique_account_type = dc.v_unique_account_type.ToList();
            unique_account_type.Add(new v_unique_account_type { Code = 0, Description = Resourse.Resources.Others });
            ViewBag.unique_account_type = unique_account_type;
            ViewBag.PettyCashLs = PettyCashClient.SelectBy_DynamicWhereClause(where, 1, 100, "PettyCashID", "int", false, out totalrow);
            ViewBag.v_accounts = (from a in dc.v_accounts
                                  select new
                                  {
                                      account_code = a.account_code.Replace(" ", ""),
                                      Account_Name = a.Account_Name.Replace("ي", "ی").Replace("ك", "ک")

                                  }).ToList();
            return View(model);
        }
        public ActionResult PettyCashList(SearchModelPettyCash2 model)
        {
            Icls_SPsClient spsClient = new Icls_SPsClient();
            var PettyCashLs = spsClient.Sp_GetPettyCashHeaderAndDetails_ByPettyCashId(model.PettyCashID).ToList();
            if (!string.IsNullOrEmpty(model.DateFrom) && !string.IsNullOrEmpty(model.DateTo))
            {
                PettyCashLs = PettyCashLs.Where(d => d.PayDate.CompareTo(model.DateFrom) >= 0 && d.PayDate.CompareTo(model.DateTo) <= 0).ToList();
            }
            if(!string.IsNullOrEmpty(model.Unique_Account_No))
            {
                PettyCashLs = PettyCashLs.Where(d => d.DetailsUnique_Account_No_two_Code == model.Unique_Account_No).ToList();
            }
            if (!string.IsNullOrEmpty(model.Account_NO))
            {
                PettyCashLs = PettyCashLs.Where(d => d.DetailAccount_no == model.Account_NO).ToList();
            }
            if(!string.IsNullOrEmpty(model.Description))
            {
                model.Description = model.Description.Replace("ی", "ي").Replace( "ک", "ك");
                PettyCashLs = PettyCashLs.Where(d => d.Description.Contains(model.Description)).ToList();
            }


            model.List = PettyCashLs.OrderBy(d => d.PayDate).Skip(model.PageSize * model.PageIndex).Take(model.PageSize).ToList();
            model.TotalPageCount = (int)Math.Ceiling((double)PettyCashLs.Count() / (double)model.PageSize);
            model.CurrentPage = model.PageIndex;
            model.CurrentPage = model.PageIndex;
            model.TotalItemCount = PettyCashLs.Count();
            return PartialView(model);
        }
        public ActionResult PettyCashListReport(int pettycashId = 0, string DateFrom = null, string DateTo = null)
        {
            if (pettycashId != 0)
            {
                return View();
            }
            else
            {
                return RedirectToAction("SearchView");
            }

        }
        public ActionResult LoadPettyCashListReport(int pettycashId, string DateFrom = null, string DateTo = null)
        {

            StiReport report = new StiReport();

            string Path = Server.MapPath("~/Report/PettyCashLIstReport2.mrt");
            report.Load(Path);
            int TotalRow;
            Icls_PettyCash_TransactionHeaderClient HeaderClient = new Icls_PettyCash_TransactionHeaderClient();
            Icls_PettyCash_TransactionDetailClient DetailsClient = new Icls_PettyCash_TransactionDetailClient();
            Icls_PettyCashClient PettyCashClient = new Icls_PettyCashClient();
            Icls_SPsClient spsClient = new Icls_SPsClient();


            var PettyCash = PettyCashClient.SelectRow(pettycashId).SingleOrDefault();
            report.Dictionary.Variables["Date"].Value = Utility.GetPersianDate(Utility.ToPersianDate(DateTime.Now));
            report.Dictionary.Variables["CompanyName"].Value = ConfigurationManager.AppSettings["CompanyTitle"];
            report.Dictionary.Variables["DateFrom"].Value = DateFrom;
            report.Dictionary.Variables["DateTo"].Value = DateTo;
            report.Dictionary.Variables["PettyCashName"].Value = PettyCash.PettyCashName;
            report.Dictionary.Variables["Unique_Account_Title"].Value = Utility.GetUniqueAccount(PettyCash.Unique_Account_No) + "/" + PettyCash.Unique_Account_No;
            report.Dictionary.Variables["Account_No"].Value = Utility.Getaccounts(PettyCash.Account_No) + "/" + PettyCash.Account_No;
            report.Dictionary.Variables["Price"].Value = ((int)PettyCash.PettyCash_Ammount).ToString();
            report.Dictionary.Variables["SumAccepted"].Value = ((int)spsClient.Sp_GetPettyCashHeaderAndDetails_ByPettyCashId(pettycashId).Where(q => q.IsAccept).Sum(q => q.Price)).ToString();
            report.Dictionary.Variables["SumVakhahi"].Value = ((int)spsClient.Sp_GetPettyCashHeaderAndDetails_ByPettyCashId(pettycashId).Where(q => q.IsVakhahi).Sum(q => q.Price)).ToString();
            //report.Dictionary.Variables["Date"].Value = Utility.GetPersianDate(Utility.ToPersianDate(DateTime.Now));


            //var HeaderLs = HeaderClient.SelectBy_DynamicWhereClause("PettyCashID="+pettycashId, 1, 1000 * 1000, "PettyCashTransactionHeaderID", "dec", false, out TotalRow);
            //var Detaills = DetailsClient.SelectAll(1, 1000 * 1000, "PettyCashTransactionDetailID", "dec", false, out TotalRow);


            //var PettyCashLs = (from H in HeaderLs
            //                   join D in Detaills on H.PettyCashTransactionHeaderID equals D.PettyCashTransactionHeaderID
            //                   select new PettyCash_TransactionCustom { }).ToList();

            //پرکردن لیست

            var PettyCashLs = spsClient.Sp_GetPettyCashHeaderAndDetails_ByPettyCashId(pettycashId).ToList();

            report.RegBusinessObject("PettyCashLs", PettyCashLs.Select(q => new
            {
                PettyCashTransactionDetailID = q.PettyCashTransactionDetailID,
                PayDate = q.PayDate,
                BehalfGroupDescription = q.BehalfGroupDescription,
                Price = (int)q.Price,
                DetailsUnique_Account_Title = Utility.GetUniqueAccount(q.DetailUnique_Account_No),
                DetailsUnique_Account_No_two_Title = Utility.GetUniqueAccount(q.DetailsUnique_Account_No_two_Code),
                DetailAccount_no = q.DetailAccount_no,
                DetailsAccountName = Utility.GetAccountNum(int.Parse(q.DetailAccount_no)),
                Description = q.Description,
                Status = (q.IsAccept ? "تایید" : (q.IsVakhahi ? "واخواهی" : ""))
            }));



            report.Dictionary.SynchronizeBusinessObjects();
            return StiMvcViewer.GetReportSnapshotResult(HttpContext, report);
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