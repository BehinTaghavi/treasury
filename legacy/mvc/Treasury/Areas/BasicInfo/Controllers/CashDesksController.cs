using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using System.Web.Mvc;
using CSD.Treasury.DataModel;
using CSD.Treasury.Service.CountryService;
using CSD.Treasury.Service.ProceduresDB;
using CSD.Treasury.UI.Controllers;

namespace CSD.Treasury.UI.Areas.BasicInfo.Controllers
{
    [Authorize(Roles = "Admin,Manager,BasicInfo,ReportInApp")]
    public class CashDesksController : BaseController
    {
        private TreasuryEntities db = new TreasuryEntities();


        public ActionResult Index()
        {
            return View();
        }
        public ActionResult List()
        {
            var cashDesks = db.CashDesks.Include(c => c.cmpBranch);
            return PartialView(cashDesks.ToList());
        }

        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            DataModel.CashDesk cashDesk = db.CashDesks.Find(id);
            if (cashDesk == null)
            {
                return HttpNotFound();
            }
            return View(cashDesk);
        }


        public ActionResult Create()
        {
            //نوع ارز
            var ExchangeTypeIDls = db.ExchangeTypes.ToList();
            //new SelectList(db.ExchangeTypes, "ExchangeTypeID", "ExchangeTypeDescription");
            //ExchangeTypeIDls.Add(new DataModel.ExchangeType { ExchangeTypeID = 0 , ExchangeTypeDescription ="---"});
            ViewBag.ExchangeTypeID = ExchangeTypeIDls;
            ViewBag.cmpBranchID = new SelectList(db.cmpBranches, "cmpBranchID", "cmpBranchName");
            //نوع  
            var unique_account_type = db.v_unique_account_type.ToList();
            unique_account_type.Add(new v_unique_account_type { Code = 0, Description = Resourse.Resources.Others });
            ViewBag.unique_account_type = unique_account_type;

            //معین
            var AccpuntLs = (from a in db.v_ExchangeAccounts
                             select new
                             {
                                 account_code = a.account_code.Trim(),
                                 Account_Name = a.Account_Name + "-" + a.account_code.Trim(),
                             }).ToList();
            AccpuntLs.Add(new { account_code = "-1", Account_Name = "---" });

            ViewBag.v_accounts = AccpuntLs;


            return PartialView();
        }

        public string GetAcount(bool isarzi)
        {
            var res = new StringBuilder();
            var db = new TreasuryEntities();
            var AccpuntLs = (from a in db.v_ExchangeAccounts
                             select new
                             {
                                 Has_Exchange_Transaction = a.Has_Exchange_Transaction,
                                 account_code = a.account_code.Trim(),
                                 Account_Name = a.Account_Name + "-" + a.account_code.Trim(),
                             });
            if(isarzi)
            {
                AccpuntLs = AccpuntLs.Where(d => d.Has_Exchange_Transaction == true);
            }
            else
            {
                AccpuntLs = AccpuntLs.Where(d => d.Has_Exchange_Transaction == false);
            }
            foreach (var i in AccpuntLs.ToList())
            {
                res.Append("<option value='" + i.account_code + "'>" + i.Account_Name + "</option>");
            }
            return res.ToString();
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(DataModel.CashDesk cashDesk)
        {
            var message = "";
            try
            {
                cashDesk.IsActive = true;
                cashDesk.CreateDateTime = DateTime.Now;


                if (ModelState.IsValid)
                {
                    if (cashDesk.ExchangeTypeID == 0)
                    {
                        cashDesk.ExchangeTypeID = null;
                    }
                    db.CashDesks.Add(cashDesk);
                    db.SaveChanges();
                    message = "ثبت با موفقیت انجام شد";
                    return JsonMessage(true, message, null, false);
                }
                return JsonMessage(false, "باخطا مواجه شد", null, false);
            }
            catch (Exception ex)
            {
                message = "با خطا مواجه شد";
                if (ex.InnerException != null)
                {
                    if (ex.InnerException.Message.Contains("duplicate"))
                    {
                        message = ex.InnerException.InnerException.Message.Split(new string[] { "\r\n" }, StringSplitOptions.None)[0];
                    }
                    else if (ex.InnerException.InnerException.Message.Contains("\r\n"))
                    {
                        message = "این تفصیل و معین قبلا ثبت شده است";
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

        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            DataModel.CashDesk cashDesk = db.CashDesks.Find(id);
            if (cashDesk == null)
            {
                return HttpNotFound();
            }
            cashDesk.arzi = false;
            if (cashDesk.ExchangeTypeID != null)
            {
                cashDesk.arzi = true;
                
            }
            
            var unique_account = db.v_unique_accounts.SingleOrDefault(d => d.Account_Code == cashDesk.Unique_Account_No);
            var unique_account2 = db.v_unique_accounts.SingleOrDefault(d => d.Account_Code == cashDesk.Unique_Account_No_two);
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
                    ViewBag.SlectedLs = from p in db.v_unique_accounts
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
                    ViewBag.SlectedLsMain = from p in db.v_unique_accounts
                                            where p.Type_Code == unique_account2.Type_Code.Trim()
                                            select new
                                            {
                                                Account_Code = p.Account_Code.Trim(),
                                                Account_Name = p.Account_Name + "/" + p.Account_Code
                                            };
                }
            }

            ViewBag.ReadOnly = db.Receipt_Detail.Any(d => d.CashDeskID == id) && cashDesk.ExchangeTypeID !=null;
            //نوع ارز
            var ExchangeTypeIDls = db.ExchangeTypes.ToList();
            //new SelectList(db.ExchangeTypes, "ExchangeTypeID", "ExchangeTypeDescription");
            //ExchangeTypeIDls.Add(new DataModel.ExchangeType { ExchangeTypeID = 0 , ExchangeTypeDescription ="---"});
            ViewBag.ExchangeTypeID = ExchangeTypeIDls;
            //ViewBag.ExchangeTypeID = new SelectList(db.ExchangeTypes, "ExchangeTypeID", "ExchangeTypeDescription", cashDesk.ExchangeTypeID);
            ViewBag.cmpBranchID = new SelectList(db.cmpBranches, "cmpBranchID", "cmpBranchAddress");
            //نوع  
            var unique_account_type = db.v_unique_account_type.ToList();
            unique_account_type.Add(new v_unique_account_type { Code = 0, Description = Resourse.Resources.Others });
            ViewBag.unique_account_type = unique_account_type;

            //معین 
            var AccpuntLs = (from a in                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                     db.v_ExchangeAccounts
                             select new
                             { 
                                 account_code = a.account_code,
                                 Account_Name = a.Account_Name + "-" + a.account_code,
                             }).ToList();
            AccpuntLs.Add(new { account_code = "-1", Account_Name = "---" });

            ViewBag.v_accounts = AccpuntLs;

            ViewBag.cmpBranchID = new SelectList(db.cmpBranches, "cmpBranchID", "cmpBranchName", cashDesk.cmpBranchID);
            return PartialView(cashDesk);
        }





        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "CashDeskID,ExtensionData,Account_No,CashDeskDescription,IsActive,CreateDateTime,Unique_Account_No,Unique_Account_No_two,cmpBranchID,ExchangeTypeID,ExchangeTypeValues")] DataModel.CashDesk cashDesk)
        {
            var message = "";
            try
            {
                if (ModelState.IsValid)
                {
                    if (cashDesk.ExchangeTypeID == null)
                    {
                        cashDesk.ExchangeTypeValues = null;
                    }
                    if (cashDesk.ExchangeTypeValues == null)
                    {
                        cashDesk.ExchangeTypeID = null;
                    }
                    db.Entry(cashDesk).State = EntityState.Modified;
                    db.SaveChanges();
                    message = "ثبت با موفقیت انجام شد";
                    return JsonMessage(true, message, null, false);
                }
                return JsonMessage(false, "با خطا مواجه شد", null, false);
            }
            catch (Exception ex)
            {
                message = "با خطا مواجه شد";
                if (ex.InnerException != null)
                {
                    if (ex.InnerException.Message.Contains("duplicate"))
                    {
                        message = ex.InnerException.InnerException.Message.Split(new string[] { "\r\n" }, StringSplitOptions.None)[0];
                    }
                    else if (ex.InnerException.InnerException.Message.Contains("\r\n"))
                    {
                        message = "این تفصیل و معین قبلا ثبت شده است";
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


        //public ActionResult Delete(int? id)
        //{
        //    if (id == null)
        //    {
        //        return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
        //    }
        //    DataModel.CashDesk cashDesk = db.CashDesks.Find(id);
        //    if (cashDesk == null)
        //    {
        //        return HttpNotFound();
        //    }
        //    return View(cashDesk);
        //}


        [HttpPost, ActionName("Delete")]
        public ActionResult Delete(int id)
        {
            var message = "";
            try
            {
                if (id != 0)
                {
                    DataModel.CashDesk cashDesk = db.CashDesks.Find(id);
                    db.CashDesks.Remove(cashDesk);
                    db.SaveChanges();
                }

                message = "ثبت با موفقیت انجام شد";
                return JsonMessage(true, message, null, false);
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

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        /// <summary>
        /// چک نمودن برای این که عنوان تکراری ثبت نگردد
        /// </summary>
        /// <param name="BehalfGroupDescription"></param>
        /// <returns></returns>
        #region CheckTitle

        //    public JsonResult CheckUniqueAcount(string Unique_Account_No, string InitialUnique_Account_No)
        //    {
        //        Icls_SPsClient db = new Icls_SPsClient();

        //        if (Unique_Account_No == InitialUnique_Account_No)
        //        {
        //            return Json(true, JsonRequestBehavior.AllowGet);
        //        }

        //        if (db.CheckExistValueInTable("Banks", "Unique_Account_No", Unique_Account_No))
        //        {
        //            return Json(false, JsonRequestBehavior.AllowGet);
        //        }

        //        return Json(true, JsonRequestBehavior.AllowGet);

        //    }
        #endregion
    }
}
