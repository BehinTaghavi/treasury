using CSD.Treasury.UI.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using CSD.Treasury.DataModel;
using CSD.Treasury.UI.Models;
using CSD.Treasury.Service.TransferFromBackupBankService;

using CSD.Treasury.Service.ProceduresDB;
using CSD.Treasury.Service.BankService;

namespace CSD.Treasury.UI.Areas.BasicInfo.Controllers
{
    [Authorize(Roles = "Admin,Manager,BasicInfo,ReportInApp")]
    public class BankBalanceController : BaseController
    {
        
        public ActionResult Index(BankBalanceModel Model)
        {
            var Banks = Utility.GetBanks();
            var BankLs = Utility.GetBasicBanks();
            ViewBag.Banks = BankLs.Where(d => Banks.Select(t => t.BasicBankID).Contains(d.BasicBankId)).ToList();
            
            return PartialView(Model);
        }
        public JsonResult GetBanks(int BasicBankId)
        {
           
            //int totalRows;
            //Icls_BankClient BankClient = new Icls_BankClient();
            var db = new TreasuryEntities();
            db.Configuration.ProxyCreationEnabled = false;
            //List<Custom_Bank> ls = new List<Custom_Bank>();
            //var query = BankClient.SelectBy_DynamicWhereClause("BasicBankId=" + BasicBankId, 1, 1000, "BankID", "int", true, out totalRows).OrderBy(d => d.SortOrder);
            var query = db.Banks.Where(d => d.BasicBankID == BasicBankId).ToList();
            return Json(query, JsonRequestBehavior.AllowGet);
        }
        // GET: BasicInfo/BankBalance
        public ActionResult ShowBalance(BankBalanceModel Model)
        {
            Model.Date = Utility.ToPersianDate(DateTime.Now);
            Icls_SPsClient db = new Icls_SPsClient();
            ViewBag.BankBalanceModel = Model;
            //$Sp_GetBankBalance$
            var ls = db.Sp_GetBankBalance("13000101", Model.Date, Model.BankId,0, false).ToList();
            if (Model.Short)
                return PartialView("ShowBalanceShort", ls);
            return PartialView(ls);
        }
      
        /// <summary>
        /// بررسی موجودی بانک و بانک پشتیبان
        /// </summary>
        /// <returns></returns>
        //public ActionResult CheckBankBalance(int BankId ,int ChequePrice)
        //{
        //    Icls_TransferFromBackupBankClient TransferClient = new Icls_TransferFromBackupBankClient();
        //    string mess;

        //    var  Date = Utility.ToPersianDate(DateTime.Now);
        //    Icls_SPsClient db = new Icls_SPsClient();
        //    var Bank = db.Sp_GetBankBalance(Date, BankId, false).Last();
        //    var Backup_BankID= Bank.Backup_BankID;
        //    GetBankBalance_Result Backup_Bank = new GetBankBalance_Result();
        //    if (Backup_BankID!=null)
        //    {
        //         Backup_Bank= db.Sp_GetBankBalance(Date, BankId, false).Last();
        //    }
        //    if(ChequePrice <= Bank.balance)
        //    { 
        //        //موجودی کافیست .تایید صدور چک
        //    }
        //    else if(Backup_BankID!=null )
        //    {
        //        //اگر بانک حساب پشتیبان داشته باشد

        //        var Sumbalance = Bank.balance + Backup_Bank.balance;
        //        var remain = ChequePrice - Bank.balance;
        //        //اگر موجودی حساب پشتیبان بیشتر یا مساوی مبلغ کمبود حساب جاری است
        //        if (Backup_Bank.balance>=remain)
        //        {
        //            //پرداخت از پشتیبان
        //            //دریافت به حساب جاری
        //            TransferFromBackupBank TransferFromBackupBankOBJ = new TransferFromBackupBank();
        //            TransferFromBackupBankOBJ.TransferDate = Utility.ToPersianDate(DateTime.Now);
        //            TransferFromBackupBankOBJ.From_BankID = Backup_Bank.Backup_BankID.Value;
        //            TransferFromBackupBankOBJ.To_BankID = Bank.BankID.Value;
        //            TransferFromBackupBankOBJ.Price = (long)remain;
        //            TransferClient.Insert(TransferFromBackupBankOBJ, out mess);

        //        }
        //        else
        //        {
        //            //موجوی کافی نیست 
        //        }                                                                                                                                                                                   
        //    }

        //    return View();
        //}
    }
}