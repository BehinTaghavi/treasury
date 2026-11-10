using CSD.Treasury.UI.Controllers;
using System;
using System.Data.Entity;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using CSD.Treasury.Service.BankService;
using CSD.Treasury.Service.CityService;
using CSD.Treasury.Service.ProvinceService;
using CSD.Treasury.Service.PermissiveSignatureForEachBankAccountService;
using CSD.Treasury.Service.PermissiveSignature_CompleteProccessService;
using CSD.Treasury.Service.PermissiveSignaturesService;
using CSD.Treasury.UI.Models;
using System.Text;
using CSD.Treasury.DataModel;
using System.Web.Script.Serialization;
using System.Data.Entity.Validation;
using CSD.Treasury.Service.ProceduresDB;

namespace CSD.Treasury.UI.Areas.BasicInfo.Controllers
{
    [Authorize(Roles = "Admin,Manager,BasicInfo,ReportInApp")]
    public class BanksController : BaseController
    {
        // GET: BasicInfo/Banks
        public ActionResult Index(int BasicBankID)
        {
            ViewBag.BasicBankID = BasicBankID;
            return View();
        }
        public ActionResult List(int BasicBankID)
        {
            //int pageNum = 1;
            //int pageSize = 1000;
            //string orderByFieldName = "BankID";
            //bool isAscendingOrder = true;
            //string OrderByfeildtype = "int";
            //string whereclouse = "BasicBankID =" + BasicBankID;
            //int totalRows;
            
            //Icls_BankClient obj = new Icls_BankClient();
            var dc = new TreasuryEntities();
            //var query = obj.SelectBy_DynamicWhereClause(whereclouse, pageNum, pageSize, orderByFieldName, OrderByfeildtype, isAscendingOrder, out totalRows).OrderBy(d=>d.SortOrder).ToList();
            var query = dc.Banks.Where(d => d.BasicBankID == BasicBankID);
            return PartialView(query);
            
        }
        public ActionResult HasUniqueAccount(string Account_Code)
        {
            var dc = new TreasuryEntities();
            var query = dc.v_accounts.SingleOrDefault(d => d.account_code == Account_Code);
            if (query.Has_Unique_Account && query.Has_Unique_Account_Two)
            {
                return Json(new { Has_Unique_Account = true, Has_Unique_Account_Two = true }, JsonRequestBehavior.AllowGet);
            }
            else if (query.Has_Unique_Account == false && query.Has_Unique_Account_Two == false)
            {
                return Json(new { Has_Unique_Account = false, Has_Unique_Account_Two = false }, JsonRequestBehavior.AllowGet);
            }
            else if (query.Has_Unique_Account == false && query.Has_Unique_Account_Two == true)
            {
                return Json(new { Has_Unique_Account = false, Has_Unique_Account_Two = true }, JsonRequestBehavior.AllowGet);
            }
            else if (query.Has_Unique_Account == true && query.Has_Unique_Account_Two == false)
            {
                return Json(new { Has_Unique_Account = true, Has_Unique_Account_Two = false }, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return Json(new { Has_Unique_Account = false, Has_Unique_Account_Two = false }, JsonRequestBehavior.AllowGet);
            }

        }
        /// <summary>
        /// لیستی از شهرها  را بر اساس کد استان می دهد  از دیتا بیس بر می گرداند
        /// </summary>
        /// <returns></returns>
        public string GetCityLS(int id)
        {
            int pageNum = 1;
            int pageSize = 1000;
            string orderByFieldName = "CityID";
            bool isAscendingOrder = true;
            string OrderByfeildtype = "int";
            string whereclouse = "provinceID=" + id;
            int totalRows;
            Icls_CityClient obj = new Icls_CityClient();
            List<Service.CityService.City> ls = new List<Service.CityService.City>();
            var query = obj.SelectBy_DynamicWhereClause(whereclouse, pageNum, pageSize, orderByFieldName, OrderByfeildtype, isAscendingOrder, out totalRows).ToList();
            var res = new StringBuilder();
            foreach (var i in query)
            {
                res.Append("<option value='" + i.CityID + "'>" + i.CityDescription + "</option>");
            }
            return res.ToString();
        }
        #region Create
        public ActionResult Create(int BasicBankID)
        {
            var dc = new TreasuryEntities();
            //Icls_BankClient BankClient = new Icls_BankClient();

            int totalRow;

            Icls_PermissiveSignaturesClient ps = new Icls_PermissiveSignaturesClient();

            ViewBag.provinceLs = Utility.GetProvinceLs();


            var usrls = dc.UserInfoes.Where(d => d.IsActive == true).Select(d => d.UserID);
            //var allBankLS = BankClient.SelectAll(1, 1000, "BankID", "int", false, out totalRow).Select(d => d.Backup_BankID);
            //var BankLs = BankClient.SelectBy_DynamicWhereClause(" BasicBankId=" + BasicBankID + "and HasBackup=" + false, 1, 1000, "BankID", "int", true, out totalRow).Where(d => !allBankLS.Contains(d.BankID)).OrderBy(d => d.SortOrder);
            var allBankLS = dc.Banks.Select(d => d.Backup_BankID);
            var BankLs = dc.Banks.Where(d => d.BasicBankID == BasicBankID && d.HasBackup == false).Where(d => !allBankLS.Contains(d.BankID)).OrderBy(d => d.SortOrder);

            List<Custom_Bank> ls = new List<Custom_Bank>();
            foreach (var item in BankLs)
            {
                Custom_Bank customBank = new Custom_Bank();
                customBank.BankID = item.BankID;
                customBank.Bank_AccountNumber = item.Bank_AccountNumber + Utility.GetBasicBankInfo(item.BasicBankID) + " " + item.BranchName;
                ls.Add(customBank);
            }
            ViewBag.BankLs = ls;

            //ViewBag.v_accounts = dc.v_accounts.ToList();
            //لیست کاربران دارای مجوز امضا
            ViewBag.PermissiveSignatures = ps.SelectAll(1, 100, "PermissiveSignatureID", "int", true, out totalRow).Where(d => usrls.Contains(d.UserID)).ToList();

            //نوع  
            var unique_account_type = dc.v_unique_account_type.ToList();
            unique_account_type.Add(new v_unique_account_type { Code = 0, Description = Resourse.Resources.Others });
            ViewBag.unique_account_type = unique_account_type;
            var AccountLS = from a in dc.v_accounts
                            where a.Has_Unique_Account == true
                            select a;
            //معین 
            ViewBag.v_accounts = (from a in AccountLS
                                  select new
                                  {
                                      account_code = a.account_code,
                                      Account_Name = a.Account_Name + "-" + a.account_code,
                                  }).ToList();

            ViewBag.v_accounts_Has_Exchange = (from a in AccountLS
                                               where a.Has_Exchange_Transaction == true
                                               select new
                                               {
                                                   account_code = a.account_code,
                                                   Account_Name = a.Account_Name + "-" + a.account_code,
                                               }).ToList();

            //نوع ارزها
            ViewBag.ExchangeLs = Utility.GetExchangeLs();

            //Service.BankService.Bank obj = new Service.BankService.Bank { BasicBankID = BasicBankID };
            return PartialView();
        }
        [HttpPost]
        public ActionResult Create(Treasury.DataModel.Bank obj, string Permission)
        {
            var message = "";
            try
            {
                //Icls_BankClient RP = new Icls_BankClient();
                //Icls_PermissiveSignatureForEachBankAccountClient PSB = new Icls_PermissiveSignatureForEachBankAccountClient();
                var db = new TreasuryEntities();
                if(obj.OpeningDate!=null)
                {
                    obj.OpeningDate = obj.OpeningDate.Replace(@"/", "");
                }
                
                //string message = "";
                db.Banks.Add(obj);
                //var result = RP.Insert(obj, out message);
                //var BankId = int.Parse(message.Split(';')[1]);

                //if (result)
                //{
                var PermissionLs = Request.Form["Permission"].Split(',');
                if (PermissionLs != null)
                {
                    foreach (var item in PermissionLs)
                    {
                        Treasury.DataModel.PermissiveSignatureForEachBankAccount objper = new Treasury.DataModel.PermissiveSignatureForEachBankAccount();

                        objper.PermissiveSignatureID = int.Parse(item);
                        obj.PermissiveSignatureForEachBankAccounts.Add(objper);
                        //PSB.Insert(objper, out message);
                    }
                }
                //}
                db.SaveChanges();
                message = "ثبت باموفقیت انجام شد";
                return JsonMessage(true, message, null, false);
            }
            catch (Exception ex)
            {
                message = "با خطا مواجه شد";
                if (ex.InnerException != null)
                {
                    if (ex.InnerException.InnerException.Message.Contains("Cannot insert duplicate key"))
                    {
                        message = "تفصیل  بانک تکراری است";
                    }
                    else if (ex.InnerException.InnerException.Message.Contains("\r\n"))
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

        #region Edit

        // GET: BasicInfo/Behalf/Edit/5
        public ActionResult Edit(int id)
        {
            var dc = new TreasuryEntities();
            ViewBag.HasExchangeTransaction = false;
            //بانک
            //Icls_BankClient BankClient = new Icls_BankClient();
            //var Bank = BankClient.SelectRow(id).SingleOrDefault();
            var Bank = dc.Banks.Find(id);
            var usrls = dc.UserInfoes.Where(d => d.IsActive).Select(d => d.UserID);

            Icls_PermissiveSignaturesClient ps = new Icls_PermissiveSignaturesClient();
            Icls_PermissiveSignatureForEachBankAccountClient psb = new Icls_PermissiveSignatureForEachBankAccountClient();
            int totalRow;
            ViewBag.ExchangeLs = Utility.GetExchangeLs();
            ViewBag.provinceLs = Utility.GetProvinceLs();




            //var BankLs = BankClient.SelectBy_DynamicWhereClause("BasicBankId=" + Bank.BasicBankID + "and BankID!=" + Bank.BankID + " and HasBackup="+false, 1, 1000, "BankID", "int", true, out totalRow).OrderBy(d => d.SortOrder);
            var BankLs = dc.Banks.Where(d => d.BasicBankID == Bank.BasicBankID && d.BankID != Bank.BankID && d.HasBackup == false).OrderBy(d => d.SortOrder);
            List<Custom_Bank> ls = new List<Custom_Bank>();

            foreach (var item in BankLs)
            {
                Custom_Bank customBank = new Custom_Bank();
                customBank.BankID = item.BankID;
                customBank.Bank_AccountNumber = item.Bank_AccountNumber + Utility.GetBasicBankInfo(item.BasicBankID) + " " + item.BranchName;
                ls.Add(customBank);
            }
            ViewBag.BankLs = ls;
            //ViewBag.BankLs = Utility.GetBanks();

            //ViewBag.PermissiveSignatures = ps.SelectAll(1, 100, "PermissiveSignatureID", "int", true, out totalRow).ToList();
            ViewBag.PermissiveSignatures = ps.SelectAll(1, 100, "PermissiveSignatureID", "int", true, out totalRow).Where(d => usrls.Contains(d.UserID)).ToList();

            ViewBag.PermissiveSignaturesSlected = psb.SelectBy_DynamicWhereClause("BankID = " + Bank.BankID, 1, 100, "PermissiveSignatureForEachBankAccountID", "int", true, out totalRow).ToList().Select(q => q.PermissiveSignatureID).ToList();
            //نوع  
            var unique_account_type = dc.v_unique_account_type.ToList();
            unique_account_type.Add(new v_unique_account_type { Code = 0, Description = Resourse.Resources.Others });
            ViewBag.unique_account_type = unique_account_type;


            var AccountLS = from a in dc.v_accounts
                            where a.Has_Unique_Account == true
                            select a;
            //معین 
            ViewBag.v_accounts = (from a in AccountLS
                                  select new
                                  {
                                      account_code = a.account_code,
                                      Account_Name = a.Account_Name + "-" + a.account_code,
                                  }).ToList();

            ViewBag.v_accounts_Has_Exchange = (from a in AccountLS
                                               where a.Has_Exchange_Transaction == true
                                               select new
                                               {
                                                   account_code = a.account_code,
                                                   Account_Name = a.Account_Name + "-" + a.account_code,
                                               }).ToList();



            //نوع ذینفع  



            if (Bank.Unique_Account_No == null)
            {
                //درصورتی که نوع ذینفع نداشته باشد
                ViewBag.uniqueTypeId = null;
                //ViewBag.uniqueTypeCode = string.Empty;
                //ViewBag.uniqueTypeName = string.Empty;
                ViewBag.SlectedLs = null;
            }
            else
            {
                var unique_account = dc.v_unique_accounts.SingleOrDefault(d => d.Account_Code == Bank.Unique_Account_No);
                //برگرداندن مقدار نوع ذینفع برای نمایش
                ViewBag.uniqueTypeId = unique_account.Type_Code.Trim();
                //ViewBag.uniqueAccount_Code = unique_account.Account_Code;
                //ViewBag.uniqueAccount_Name = unique_account.Account_Name;
                //ذیفع ها 
                ViewBag.unique_accounts = dc.v_unique_accounts.Where(d => d.Type_Code == unique_account.Type_Code);



                ViewBag.SlectedLs = (from p in dc.v_unique_accounts
                                     where p.Type_Code == unique_account.Type_Code
                                     select new
                                     {
                                         Account_Code = p.Account_Code,
                                         Account_Name = p.Account_Name + "/" + p.Account_Code
                                     }).ToList();

            }

            if (Bank.Unique_Account_No_two == null)
            {
                //درصورتی که نوع ذینفع نداشته باشد
                ViewBag.uniqueTypeId2 = null;
                //ViewBag.uniqueTypeCode2 = string.Empty;
                //ViewBag.uniqueTypeName2 = string.Empty;
                ViewBag.SlectedLs2 = null;
            }
            else
            {
                var unique_account2 = dc.v_unique_accounts.SingleOrDefault(d => d.Account_Code == Bank.Unique_Account_No_two);
                //برگرداندن مقدار نوع ذینفع برای نمایش
                ViewBag.uniqueTypeId2 = unique_account2.Type_Code;
                //ViewBag.uniqueTypeCode2 = unique_account2.Account_Code;
                //ViewBag.uniqueTypeName2 = unique_account2.Account_Name;
                //ذیفع ها 
                ViewBag.unique_accounts = dc.v_unique_accounts.Where(d => d.Type_Code == unique_account2.Type_Code);

                ViewBag.SlectedLs2 = (from p in dc.v_unique_accounts
                                      where p.Type_Code == unique_account2.Type_Code
                                      select new
                                      {
                                          Account_Code = p.Account_Code,
                                          Account_Name = p.Account_Name + "/" + p.Account_Code
                                      }).ToList();
            }


            Bank.OpeningDate = Utility.DateStructure(Bank.OpeningDate);
            if (Bank.CityID != null)
            {
                Icls_CityClient obj1 = new Icls_CityClient();
                var provinceId = obj1.SelectRow(Bank.CityID.Value).SingleOrDefault().ProvinceID;
                ViewBag.Province = provinceId;
                ViewBag.City = Utility.GetCity(Bank.CityID.Value).CityDescription;
            }
            else
            {
                ViewBag.Province = "";
                ViewBag.City = "";
            }
            
            return PartialView(Bank);
        }

        // POST: BasicInfo/Behalf/Edit/5
        [HttpPost]
        public ActionResult Edit(Treasury.DataModel.Bank obj, string Permission)
        {
            var message = "";
            try
            {
                ModelState.Remove("ExchangeType");
                if (ModelState.IsValid)
                {
                    //Icls_PermissiveSignatureForEachBankAccountClient PSB = new Icls_PermissiveSignatureForEachBankAccountClient();
                    //Icls_PermissiveSignature_CompleteProccessClient pscClient = new Icls_PermissiveSignature_CompleteProccessClient();
                    if(obj.OpeningDate!=null)
                    {
                        obj.OpeningDate = obj.OpeningDate.Replace(@"/", "");
                    }
                    if(obj.HasExchangeTransaction== false)
                    {
                        obj.ExchangeType = null;
                        obj.ExchangeTypeID = null;                     
                    }
                    //string message = "";
                    //string message1 = "";
                    //int totalRow;
                    //Icls_BankClient RP = new Icls_BankClient();
                    var db = new TreasuryEntities();
                    //var result = RP.Update(obj, out message);

                    db.Entry(obj).State = EntityState.Modified;



                    //حذف تمام امضاها مجاز              

                    //var permissivesignitureforBankLS = PSB.SelectBy_DynamicWhereClause("BankID =" + obj.BankID, 1, 100, "PermissiveSignatureForEachBankAccountID", "int", true, out totalRow);
                    var permissivesignitureforBankLS = db.PermissiveSignatureForEachBankAccounts.Where(d => d.BankID == obj.BankID);
                    db.PermissiveSignatureForEachBankAccounts.RemoveRange(permissivesignitureforBankLS);
                    //foreach (var item in permissivesignitureforBankLS)
                    //{
                    //    PSB.Delete(item.PermissiveSignatureForEachBankAccountID, out message1);
                    //}
                    //افزودن امضاهای مجاز
                    var PermissionLs = Request.Form["Permission"].Split(',');
                    if (PermissionLs != null)
                    {
                        foreach (var item in PermissionLs)
                        {
                            var objper = new Treasury.DataModel.PermissiveSignatureForEachBankAccount();
                            objper.BankID = obj.BankID;
                            objper.PermissiveSignatureID = int.Parse(item);
                            db.PermissiveSignatureForEachBankAccounts.Add(objper);
                            //PSB.Insert(objper, out message1);
                        }
                    }

                    message = "ویرایش با موفقیت انجام شد";
                    db.SaveChanges();
                    return JsonMessage(true, message, null, false);

                }
                message = "ویرایش با خطا مواجه شد";
                return JsonMessage(false, message, null, false);
            }
            catch (Exception ex)
            {
                message = "با خطا مواجه شد";
                if (ex.InnerException != null)
                {
                    if (ex.InnerException.InnerException.Message.Contains("Cannot insert duplicate key"))
                    {
                        message = "تفصیل  بانک تکراری است";
                    }
                    else if (ex.InnerException.InnerException.Message.Contains("\r\n"))
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



        [HttpGet]
        public ActionResult UniqueAccountLs(string q, int pageSize = 20, int pageNum = 1)
        {
            using (var context = new TreasuryEntities()) // نام کانتکست پروژه خودتان
            {
                var query = context.v_unique_accounts.AsQueryable();

                // فیلتر جستجو
                if (!string.IsNullOrWhiteSpace(q))
                {
                    query = query.Where(x => x.Account_Name.Contains(q) ||
                                             x.Account_Code.Contains(q));
                }

                // تعداد کل رکوردها
                int totalCount = query.Count();

                int takeAmount = pageNum * pageSize;
                int skipAmount = (pageNum - 1) * pageSize;

                // انتقال Select به قبل از ToList تا فیلدهای اضافی مثل Type_Code لود نشوند
                var data = query.OrderBy(x => x.Account_Code)
                                .Select(x => new
                                {
                                    id = x.Account_Code,
                                    text = x.Account_Code + " - " + x.Account_Name
                                })
                                .Take(takeAmount)
                                .ToList() // واکشی در حافظه برای دور زدن محدودیت SQL 2008
                                .Skip(skipAmount)
                                .ToList();

                return Json(new
                {
                    results = data,
                    Total = totalCount
                }, JsonRequestBehavior.AllowGet);
            }
        }






        #region Delete
        // GET: BasicInfo/Behalf/Delete/5
        //public ActionResult Delete(int id)
        //{
        //    Icls_BankClient obj = new Icls_BankClient();
        //    return PartialView(obj.SelectRow(id).SingleOrDefault());
        //}

        // POST: BasicInfo/Behalf/Delete/5
        [HttpPost]
        public ActionResult Delete(int id)
        {
            string message = "";
            try
            {               
          
                //حذف امضاهایی که به این بانک اختصاص داده شده
                //Icls_PermissiveSignatureForEachBankAccountClient psFB = new Icls_PermissiveSignatureForEachBankAccountClient();
                var db = new TreasuryEntities();
                //var ls = psFB.SelectBy_DynamicWhereClause("BankID=" + id, 1, 100, "PermissiveSignatureForEachBankAccountID", "int", true, out totalRow);
                var ls = db.PermissiveSignatureForEachBankAccounts.Where(d => d.BankID == id);
                if (ls != null)
                {
                    db.PermissiveSignatureForEachBankAccounts.RemoveRange(ls);
                    //foreach (var item in ls)
                    //{
                    //    psFB.Delete(item.PermissiveSignatureForEachBankAccountID, out message);
                    //}
                }

                //Icls_BankClient RP = new Icls_BankClient();

                //var result = RP.Delete(id, out message);
                var bank = db.Banks.Find(id);
                db.Banks.Remove(bank);
                message = "حذف با موفقیت انجام شد";
                db.SaveChanges();
                return JsonMessage(true, message);
            }
            catch(Exception ex)
            {

                message = "با خطا مواجه شد";
                if (ex.InnerException != null)
                {
                    if (ex.InnerException.InnerException.Message.Contains("Cannot insert duplicate key"))
                    {
                        message = "تفصیل  بانک تکراری است";
                    }
                    else if (ex.InnerException.InnerException.Message.Contains("FK_Receipt_Detail_PlanState_Banks"))
                    {
                        message = "این بانک گردش دارد";
                        
                     }
                    else if (ex.InnerException.InnerException.Message.Contains("\r\n"))
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

        /// <summary>
        /// لیستی از بابت ها  را از دیتا بیس بر می گرداند
        /// </summary>
        /// <returns></returns>
        public string GetAccount(bool HasExchange)
        {
            var dc = new TreasuryEntities();
            var res = new StringBuilder();
            //معین 
            var AccountLs = from a in dc.v_accounts
                            where a.Has_Unique_Account == true
                            select a;

            var query = (from a in AccountLs
                         select new
                         {
                             account_code = a.account_code,
                             Account_Name = a.Account_Name + "-" + a.account_code,
                         }).ToList();

            if (HasExchange)
            {
                query = (from a in AccountLs
                         where a.Has_Exchange_Transaction == true
                         select new
                         {
                             account_code = a.account_code,
                             Account_Name = a.Account_Name + "-" + a.account_code,
                         }).ToList();
            }

            foreach (var i in query)
            {
                res.Append("<option value='" + i.account_code + "'>" + i.Account_Name + "</option>");
            }
            return res.ToString();
        }
        /// <summary>
        /// جزییات حساب بانکی 
        /// </summary>
        /// <returns></returns>
        #region Details    
        public ActionResult Details(int BankId)
        {
            //Icls_BankClient BankClient = new Icls_BankClient();
            var db = new TreasuryEntities();
            ViewBag.exchangeType = Utility.GetExchangeLs();
            ViewBag.HasExchangeTransaction = false;

            //var Bank = BankClient.SelectRow(BankId).SingleOrDefault();
            var Bank = db.Banks.Find(BankId);
            if (Bank.ExchangeTypeID != null)
            {
                ViewBag.HasExchangeTransaction = true;
            }
            return PartialView(Bank);
        }
        #endregion

        /// <summary>
        /// چک نمودن برای این که عنوان تکراری ثبت نگردد
        /// </summary>
        /// <param name="BehalfGroupDescription"></param>
        /// <returns></returns>
        #region CheckTitle
        public JsonResult CheckTitle(string BranchCode, string InitialBranchCode, int BasicBankID)
        {
            //Icls_SPsClient db = new Icls_SPsClient();
            //int totalrow;
            //Icls_BankClient BankClient = new Icls_BankClient();
            var db = new TreasuryEntities();
            if (BranchCode == InitialBranchCode)
            {
                return Json(true, JsonRequestBehavior.AllowGet);
            }
            //var ifExit = BankClient.SelectBy_DynamicWhereClause("BranchCode =\"" + BranchCode + "\" and BasicBankID =" + BasicBankID, 1, 1000, "BankID", "int", false, out totalrow).Any();
            var ifExit = db.Banks.Where(d => d.BranchCode == BranchCode && BasicBankID == BasicBankID).Any();
            if (ifExit)
            {
                return Json(false, JsonRequestBehavior.AllowGet);
            }

            return Json(true, JsonRequestBehavior.AllowGet);

        }
        public JsonResult CheckUniqueAcount(string Unique_Account_No, string InitialUnique_Account_No, int BasicBankID)
        {
            Icls_SPsClient db = new Icls_SPsClient();
            int totalrow;
            //Icls_BankClient BankClient = new Icls_BankClient();

            if (Unique_Account_No == InitialUnique_Account_No)
            {
                return Json(true, JsonRequestBehavior.AllowGet);
            }
            //var ifExit = BankClient.SelectBy_DynamicWhereClause("Unique_Account_No =\"" +Unique_Account_No + "\" and BasicBankID =" + BasicBankID , 1, 1000, "BankID", "int", false, out totalrow).Any();
            if (db.CheckExistValueInTable("Banks", "Unique_Account_No", Unique_Account_No))
            {
                return Json(false, JsonRequestBehavior.AllowGet);
            }

            return Json(true, JsonRequestBehavior.AllowGet);

        }
        #endregion

    }
}