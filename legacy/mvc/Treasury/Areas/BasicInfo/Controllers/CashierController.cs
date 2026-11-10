using CSD.Treasury.UI.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using CSD.Treasury.Service.CashiersService;
using CSD.Treasury.DataModel;
using CSD.Treasury.UI.Models;
using CSD.Treasury.Service.ProceduresDB;

namespace CSD.Treasury.UI.Areas.BasicInfo.Controllers
{
    [Authorize(Roles = "Admin,Manager,BasicInfo,ReportInApp")]
    public class CashierController : BaseController
    {
        // GET: BasicInfo/Cashier
        public ActionResult Index()
        {
            return View();
        }
        public ActionResult List()
        {                 
            int totalRows;
            Icls_CashiersClient RP = new Icls_CashiersClient();

            var query = RP.SelectAll(1, 1000, "CashierID", "int", false, out totalRows).ToList();
            return PartialView(query);
        }
        #region Create
        // GET: BasicInfo/ExchangeType/Create
        public ActionResult Create()
        {
           
            var dc = new TreasuryEntities();
            //لیست حساب تفصیل  
            var unique_account_type = dc.v_unique_account_type.ToList();
            unique_account_type.Add(new v_unique_account_type { Code = 0, Description = Resourse.Resources.Others });
            ViewBag.unique_account_type = unique_account_type;

            //انواع 
            ViewBag.ReceiptPayTypeLS = Utility.GetReceiptPayTypeLS();

            //لیست کاربران
            ViewBag.UserLS = dc.UserInfoes.Where(d => d.IsActive).ToList();

            //لیست صندوق
            var db = new TreasuryEntities();
            var bank = db.CashDesks.ToList();
            ViewBag.CashdeskLsLS = bank;//Utility.GetCashdeskLs();

            //لیست حساب معین
            ViewBag.v_accounts = (from a in dc.v_accounts
                                  select new
                                  {
                                      account_code = a.account_code.Trim(),
                                      Account_Name = a.Account_Name + "-" + a.account_code.Trim()

                                  }).ToList();
            return PartialView();
        }

        // POST: BasicInfo/ExchangeType/Create
        [HttpPost]
        public ActionResult Create(Service.CashiersService.Cashier obj)
        {
            try
            {
                if (obj.Unique_Account_No != null)
                {
                    string message = "";
                    Icls_CashiersClient RP = new Icls_CashiersClient();
                    var result = RP.Insert(obj, out message);
                    return JsonMessage(result, message);
                }
                return JsonMessage(false, Resourse.Resources.PleaseSelect);
            }
            catch
            {
                return PartialView();
            }
        }
        #endregion

        #region Edit

        public ActionResult Edit(int id)
        {
            var dc = new TreasuryEntities();
            Icls_CashiersClient obj = new Icls_CashiersClient();

            //لیست کاربران
            ViewBag.UserLS = dc.UserInfoes.Where(d => d.IsActive).ToList();
            //لیست حساب معین
            ViewBag.v_accounts = (from a in dc.v_accounts
                                  from b in dc.CashDesks
                                  where a.account_code == b.Account_No 
                                  select new
                                  {
                                      account_code = a.account_code.Trim(),
                                      Account_Name = a.Account_Name + "-" + a.account_code.Trim()

                                  }).ToList();


            //لیست صندوق
            ViewBag.CashdeskLsLS = Utility.GetCashdeskLs();
            //نوع ذینفع  
            var unique_account_type = dc.v_unique_account_type.ToList();
            unique_account_type.Add(new v_unique_account_type { Code = 0, Description = Resourse.Resources.Others });
            ViewBag.unique_account_type = unique_account_type;



            var cashier = obj.SelectRow(id).SingleOrDefault();

            if (cashier.Unique_Account_No == null)
            {
                //درصورتی که نوع ذینفع نداشته باشد
                ViewBag.uniqueTypeId = 0;
            }
            else
            {
                var unique_account = dc.v_unique_accounts.SingleOrDefault(d => d.Account_Code == cashier.Unique_Account_No.Trim());
                var Type_Code = unique_account.Type_Code.Trim();
                ViewBag.uniqueTypeId = Type_Code;
                ViewBag.SlectedLs =(from p in dc.v_unique_accounts
                                    where p.Type_Code == Type_Code && p.Account_Code == unique_account.Account_Code
                                    select new 
                                    {
                                        Account_Code = p.Account_Code.Trim(),
                                        Account_Name = p.Account_Name + "/" + p.Account_Code
                                    }).ToList();

                //be select bala  && p.Account_Code == unique_account.Account_Code ezafe shod chon zaminke nabud avalin tafsil ra neshan midad

                //ViewBag.uniqueAccount_Code = unique_account.Account_Code;
                //ViewBag.uniqueAccount_Name = unique_account.Account_Name;

            }

            return PartialView(obj.SelectRow(id).SingleOrDefault());
            
        }
        [HttpPost]
        public ActionResult Edit(Service.CashiersService.Cashier obj)
        {
            try
            {
                string message = "";
                Icls_CashiersClient RP = new Icls_CashiersClient();
                var result = RP.Update(obj, out message);
                return JsonMessage(result, message);
            }
            catch
            {
                return PartialView();
            }
        }
        #endregion

        #region Delete

        //public ActionResult Delete(int id)
        //{
        //    Icls_CashiersClient obj = new Icls_CashiersClient();
        //    return PartialView(obj.SelectRow(id).SingleOrDefault());
        //}

        [HttpPost]
        public ActionResult Delete(int id)
        {
            try
            {
                string message = "";
                Icls_CashiersClient RP = new Icls_CashiersClient();
                var result = RP.Delete(id, out message);
                return JsonMessage(result, message);
            }
            catch
            {
                return PartialView();
            }
        }
        #endregion

        #region Details
        public ActionResult Details(int id)
        {
            Icls_CashiersClient RP = new Icls_CashiersClient();
            var Casheir= RP.SelectRow(id).SingleOrDefault();
            return PartialView(Casheir);
        }
        #endregion


        public string GetExchangeRateType(int CashDeskId)
        {
            var db = new TreasuryEntities();
            var bank = db.CashDesks.SingleOrDefault(d => d.CashDeskID == CashDeskId);
            var ExchangeRateType = bank.ExchangeTypeValues;
            return ExchangeRateType.ToString();

        }



        /// <summary>
        /// چک نمودن برای این که عنوان تکراری ثبت نگردد
        /// </summary>
        /// <param name="PettyCashName"></param>
        /// <returns></returns>
        #region CheckTitle
        public JsonResult CheckTitle(string Account_no, string InitialAccount_no)
        {
            Icls_SPsClient db = new Icls_SPsClient();
            if (Account_no == InitialAccount_no)
            {
                return Json(true, JsonRequestBehavior.AllowGet);
            }
            if (db.CheckExistValueInTable("Cashiers", "Account_no", Account_no))
            {
                return Json(false, JsonRequestBehavior.AllowGet);
            }

            return Json(true, JsonRequestBehavior.AllowGet);
        }
        #endregion
    }
}