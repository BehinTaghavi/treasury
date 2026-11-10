using CSD.Treasury.UI.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using CSD.Treasury.Service.BankCardsService;
using CSD.Treasury.UI.Models;
using CSD.Treasury.DataModel;

namespace CSD.Treasury.UI.Areas.BasicInfo.Controllers
{
    [Authorize(Roles = "Admin,Manager,BasicInfo,ReportInApp")]
    public class BankCardsController : BaseController
    {
      
        public ActionResult Index(int BankId,int BasicBankID)
        {
            ViewBag.BankId = BankId;
            ViewBag.BasicBankID = BasicBankID;
            return View();
        }
        public ActionResult List(int BankId, int BasicBankID)
        {
            //int totalRows;
            //Icls_BankCardsClient obj = new Icls_BankCardsClient();
            //var query = obj.SelectBy_DynamicWhereClause("BankId =" + BankId, 1, 1000, "CardID", "int", true, out totalRows).ToList();
            var dc = new TreasuryEntities();
            var query = dc.BankCards.Where(d => d.BankID == BankId).ToList();
            return PartialView(query);
        }
        #region Create
        public ActionResult Create(int BankId)
        {
            var dc = new TreasuryEntities();
            var obj = new DataModel.BankCard { BankID = BankId };
            //نوع ذینفع  
            var unique_account_type = dc.v_unique_account_type.ToList();
        

            unique_account_type.Add(new v_unique_account_type { Code = 0, Description = Resourse.Resources.Others });
            ViewBag.unique_account_type = unique_account_type;
            return PartialView();
        }
        [HttpPost]
        public ActionResult Create(DataModel.BankCard obj)
        {
            try
            {
                var dc = new TreasuryEntities();
                obj.ExpiryDate = obj.ExpiryDate.Replace(@"/", "");
                obj.CreateDateTime = DateTime.Now;
                dc.BankCards.Add(obj);
                dc.SaveChanges();
                return JsonMessage(true, "ثبت با موفقیت انجام شد", null, false);
                //string message = "";
                //Icls_BankCardsClient RP = new Icls_BankCardsClient();
                //var result = RP.Insert(obj, out message);
                //return JsonMessage(result, message);
            }
            catch(Exception ex)
            {
                return JsonMessage(false, "با خطا مواجه شد", null, false);
            }

        }
        #endregion

        #region Edit

        // GET: BasicInfo/BankCard/Edit/5
        public ActionResult Edit(int id)
        {
            //Icls_BankCardsClient obj = new Icls_BankCardsClient();
            var dc = new TreasuryEntities();
            var BankCard = dc.BankCards.Find(id);
            //نوع  
            var unique_account_type = dc.v_unique_account_type.ToList();
            unique_account_type.Add(new v_unique_account_type { Code = 0, Description = Resourse.Resources.Others });
            ViewBag.unique_account_type = unique_account_type;

            if (BankCard.Unique_Account_No == null)
            {
                //درصورتی که نوع ذینفع نداشته باشد
                ViewBag.uniqueTypeId = 0;
            }
            else
            {
                var unique_account = dc.v_unique_accounts.SingleOrDefault(d => d.Account_Code == BankCard.Unique_Account_No.Trim());
                var Type_Code = unique_account.Type_Code.Trim();
                ViewBag.uniqueTypeId = Type_Code;
                ViewBag.SlectedLs = (from p in dc.v_unique_accounts
                                     where p.Type_Code == Type_Code
                                     select new
                                     {
                                         Account_Code = p.Account_Code,
                                         Account_Name = p.Account_Name + "/" + p.Account_Code
                                     }).ToList();

            }

            BankCard.ExpiryDate = Utility.DateStructure(BankCard.ExpiryDate);
            return PartialView(BankCard);
        }

        // POST: BasicInfo/BankCard/Edit/5
        [HttpPost]
        public ActionResult Edit(DataModel.BankCard obj)
        {
            try
            {
                //string message = "";
                //Icls_BankCardsClient RP = new Icls_BankCardsClient();
                //obj.ExpiryDate = obj.ExpiryDate.Replace(@"/", "");
                //var result = RP.Update(obj, out message);
                //return JsonMessage(result, message);
                var dc = new TreasuryEntities();
                obj.ExpiryDate = obj.ExpiryDate.Replace(@"/", "");
                var bankcard = dc.BankCards.Find(obj.CardID);
                bankcard.Card_AccountNo = obj.Card_AccountNo;
                bankcard.Card_NO = obj.Card_NO;
                bankcard.CVV2 = obj.CVV2;
                bankcard.ExpiryDate = obj.ExpiryDate;
                bankcard.IsActive = obj.IsActive;
                bankcard.Unique_Account_No = obj.Unique_Account_No;
                dc.SaveChanges();
                return JsonMessage(true, "ویرایش با موفقیت انجام شد", null, false);
            }
            catch
            {
                return JsonMessage(false, "با خطا مواجه شد", null, false);
            }
        }

        #endregion

        #region Delete
        // GET: BasicInfo/BankCard/Delete/5
        //public ActionResult Delete(int id)
        //{
        //    Icls_BankCardsClient obj = new Icls_BankCardsClient();
        //    return PartialView(obj.SelectRow(id).SingleOrDefault());
        //}

        // POST: BasicInfo/Behalf/Delete/5
        [HttpPost]
        public ActionResult Delete(int id)
        {
            try
            {
                //string message = "";
                //Icls_BankCardsClient RP = new Icls_BankCardsClient();
                //var result = RP.Delete(id, out message);
                //return JsonMessage(result, message);
                var dc = new TreasuryEntities();
                dc.BankCards.Find(id);
                dc.SaveChanges();
                return JsonMessage(true, "حذف باموفقیت انجام شد", null, false);
            }
            catch
            {
                return JsonMessage(false, "با خطا مواجه شد", null, false);
            }
        }
        #endregion
    }
}