using CSD.Treasury.DataModel;
using CSD.Treasury.Service.BankService;
using CSD.Treasury.Service.ProceduresDB;
using CSD.Treasury.UI.Areas.Report.Models;
using CSD.Treasury.UI.Controllers;
using CSD.Treasury.UI.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data.Entity.Core.Objects;
using System.Data.SqlClient;
using System.Data;

namespace CSD.Treasury.UI.Areas.Report.Controllers
{
    [Authorize(Roles = "Admin,Manager,Report,ReportInApp")]
    public class NaghdinegiController : BaseController
    {

        public ActionResult Show()
        {
            return View();
        }
        public ActionResult SearchViewStatus()
        {
            return View();
        }

        // GET: Report/Naghdinegi
        public ActionResult SearchView()
        {
            return View();
        }
        public ActionResult SearchViewSeporde()
        {
            return View();
        }
        
        public ActionResult list(GardeshHesabSearchModel SearchObj)
        {
            Icls_SPsClient db = new Icls_SPsClient();
            int TotalRow;
            //Icls_BankClient BankCLient = new Icls_BankClient();
            
            if (SearchObj.Seporde)
            {
                SearchObj.ToDate = SearchObj.FromDate;
            }
            SearchObj.ToDate = SearchObj.FromDate;
            var ls= db.Sp_GetAllBankBalance(SearchObj.FromDate.Replace(@"/", ""), SearchObj.ToDate.Replace(@"/", "")).ToList();
            //if (SearchObj.Seporde)
            //{
            //    var BankLS = BankCLient.SelectBy_DynamicWhereClause("IsActive=" + true + " and Deposited=" + true, 1, 1000, "BankID", "int", false, out TotalRow);
            //    ls = ls.Where(d => BankLS.Select(i => i.BankID).Contains(d.BankID)).ToList();
            //}
               
                return PartialView(ls);
        }
        public ActionResult listStatus(GardeshHesabSearchModel SearchObj)
        {
            var FromDate = Utility.GetMiladiDate(SearchObj.ToDate.Replace("/", "")).Value;
            var ToDate = Utility.GetMiladiDate(SearchObj.FromDate.Replace("/", "")).Value;
            var Result=DateTime.Compare(FromDate,ToDate);
            if (Result<0)
            {
                ViewBag.ErrorTime = "روزآینده بزرگتر یا مساوی تاریخ امروز انتخاب گردد";
                return PartialView();
            }
            else
            {
                using (var db = new TreasuryEntities())
                {
                    var DateParam = new SqlParameter("ToDate", SearchObj.FromDate);
                    var DateToparam = new SqlParameter("ToFutureDate", SearchObj.ToDate.Replace("/", ""));
                    var NazdeSandoghMarkazParam = new SqlParameter("NazdeSandoghMarkaz", SqlDbType.BigInt)
                    {
                        Direction = System.Data.ParameterDirection.Output
                    };

                    var NazdeSandoghKharejAzMarkazparam = new SqlParameter("NazdeSandoghKharejAzMarkaz", SqlDbType.BigInt)
                    {
                        Direction = System.Data.ParameterDirection.Output
                    };

                    var result = db.Database.SqlQuery<DataModel.CashFlow_Result>("exec CashFlow @ToDate,@ToFutureDate,@NazdeSandoghMarkaz out, @NazdeSandoghKharejAzMarkaz out ", DateParam, DateToparam, NazdeSandoghMarkazParam, NazdeSandoghKharejAzMarkazparam).ToList();
                    ViewBag.NazdeSandoghMarkaz = NazdeSandoghMarkazParam.Value;
                    ViewBag.NazdeSandoghKharejAzMarkaz = NazdeSandoghKharejAzMarkazparam.Value;
                    return PartialView(result);
                }
            }
            
       
        }
        public ActionResult SearchViewburse()
        {
            var Banks = Utility.GetBanks();
            var BankLs = Utility.GetBasicBanks();
            ViewBag.Banks = BankLs.Where(d => Banks.Select(t => t.BasicBankID).Contains(d.BasicBankId)).ToList();
            return View();
        }
        public ActionResult listburse(GardeshHesabSearchModel SearchObj)
        {
            Icls_SPsClient spsClient = new Icls_SPsClient();
            
            var ls = spsClient.Sp_GetBankBalance(SearchObj.FromDate.Replace(@"/", ""), SearchObj.ToDate.Replace(@"/", ""), SearchObj.BankId, 2, true).Where(d=>d.RecordNo!=0).GroupBy(d=>d.BehalfGroupDescription,(key, g) => new bankbalanceModel { BehalfGroupDescription = g.FirstOrDefault().BehalfGroupDescription,  ls = g.ToList() }).ToList();

         
            return PartialView(ls);
        }
    }
}