using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using CSD.Treasury.Service.ExchangeTypesService;
using CSD.Treasury.UI.Controllers;
using CSD.Treasury.Service.ProceduresDB;
using CSD.Treasury.DataModel;
using ExchangeType = CSD.Treasury.Service.ExchangeTypesService.ExchangeType;

namespace CSD.Treasury.UI.Areas.BasicInfo.Controllers
{
    [Authorize(Roles = "Admin,Manager,BasicInfo,ReportInApp")]
    public class ExchangeTypeController : BaseController
    {
        private TreasuryEntities db = new TreasuryEntities();
        public ActionResult Index()
        {
            return View();
        }

        public PartialViewResult List()
        {
            int pageNum = 1;
            int pageSize = 1000;
            string orderByFieldName = "ExchangeTypeID";
            bool isAscendingOrder = true;
            string OrderByfeildtype = "int";
            int totalRows;

            Icls_ExchangeTypesClient obj = new Icls_ExchangeTypesClient();
            var query = obj.SelectAll(pageNum, pageSize, orderByFieldName, OrderByfeildtype, isAscendingOrder, out totalRows);
            return PartialView(query);
        }

        #region Create

        public ActionResult Create()
        {

            //var AccpuntLs = (from a in db.v_ExchangeAccounts
            //                 select new
            //                 {
            //                     account_code = a.account_code.Trim(),
            //                     Account_Name = a.Account_Name + "-" + a.account_code.Trim(),
            //                 }).ToList();
            //AccpuntLs.Add(new { account_code = "-1", Account_Name = "---" });

            //ViewBag.v_accounts = AccpuntLs;

            return PartialView();
        }


        [HttpPost]
        public ActionResult Create(DataModel.ExchangeType obj)
        {
            var message = "";
            try
            {
                //string message = "";
                //Icls_ExchangeTypesClient RP = new Icls_ExchangeTypesClient();
                //var result = RP.Insert(obj, out message);
                //return JsonMessage(result, message);
                //if (obj.AccountNo != "-1")
                //{
                db.ExchangeTypes.Add(obj);
                db.SaveChanges();
                message = "ثبت با موفقیت انجام شد";
                    return JsonMessage(true, message, null, false);
                //}
                //message = "لطفا معین را انتخاب نمایید";
                //return JsonMessage(false, message, null, false);
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
        #endregion

        #region Edit

        public ActionResult Edit(int id)
        {
            //var AccpuntLs = (from a in db.v_ExchangeAccounts
            //                 select new
            //                 {
            //                     account_code = a.account_code.Trim(),
            //                     Account_Name = a.Account_Name + "-" + a.account_code.Trim(),
            //                 }).ToList();
        

            //ViewBag.v_accounts = AccpuntLs;
            //Icls_ExchangeTypesClient obj = new Icls_ExchangeTypesClient();
            var exchangeType = db.ExchangeTypes.Find(id);
            //exchangeType.AccountNo = exchangeType.AccountNo.Trim();
            return PartialView(exchangeType);
        }


        [HttpPost]
        public ActionResult Edit(DataModel.ExchangeType obj)
        {
            var message = "";
            try
            {
                //string message = "";
                //Icls_ExchangeTypesClient RP = new Icls_ExchangeTypesClient();
                //var result = RP.Update(obj, out message);
                //return JsonMessage(result, message);


                var ExchangeType = db.ExchangeTypes.Find(obj.ExchangeTypeID);
                ExchangeType.ExchangeTypeDescription = obj.ExchangeTypeDescription;
                ExchangeType.AccountNo = obj.AccountNo;
                db.SaveChanges();
                message = "ثبت با موفقیت انجام شد";
                return JsonMessage(true, message, null, false);

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
        #endregion

            #region Delete
            // GET: BasicInfo/ExchangeType/Delete/5
            //public ActionResult Delete(int id)
            //{
            //    Icls_ExchangeTypesClient obj = new Icls_ExchangeTypesClient();
            //    return PartialView(obj.SelectRow(id).SingleOrDefault());
            //}

            // POST: BasicInfo/ExchangeType/Delete/5
        [HttpPost]
        public ActionResult Delete(int ExchangeTypeID)
        {
            try
            {
                string message = "";
                Icls_ExchangeTypesClient RP = new Icls_ExchangeTypesClient();
                var result = RP.Delete(ExchangeTypeID, out message);
                return JsonMessage(result, message);
            }
            catch
            {
                return PartialView();
            }
        }
        #endregion
        /// <summary>
        /// چک نمودن برای این که عنوان تکراری ثبت نگردد
        /// </summary>
        /// <param name="BehalfGroupDescription"></param>
        /// <returns></returns>
        #region CheckTitle
        public JsonResult CheckTitle(string ExchangeTypeDescription, string InitialExchangeTypeDescription)
        {
            Icls_SPsClient db = new Icls_SPsClient();
            if (ExchangeTypeDescription == InitialExchangeTypeDescription)
            {
                return Json(true, JsonRequestBehavior.AllowGet);
            }
            if (db.CheckExistValueInTable("ExchangeTypes", "ExchangeTypeDescription", ExchangeTypeDescription))
            {
                return Json(false, JsonRequestBehavior.AllowGet);
            }

            return Json(true, JsonRequestBehavior.AllowGet);
        }
        #endregion

        #region AngularForm
        public ActionResult Index1()
        {
            return View();
        }

        public JsonResult List1()
        {
            int totalRows;
            Icls_ExchangeTypesClient obj = new Icls_ExchangeTypesClient();
            var query = obj.SelectAll(1, 1000, "ExchangeTypeID", "int", false, out totalRows);
            return Json(query, JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        public JsonResult Edit1(ExchangeType obj)
        {
            string message = "";
            Icls_ExchangeTypesClient RP = new Icls_ExchangeTypesClient();
            var result = RP.Update(obj, out message);
            return JsonMessage(result, message);

        }
        [HttpPost]
        public JsonResult Delete1(int ExchangeTypeID)
        {
            string message = "";
            Icls_ExchangeTypesClient RP = new Icls_ExchangeTypesClient();
            var result = RP.Delete(ExchangeTypeID, out message);
            return JsonMessage(result, message);
        }
        [HttpPost]
        public JsonResult Create1(ExchangeType obj)
        {
            string message = "";
            Icls_ExchangeTypesClient RP = new Icls_ExchangeTypesClient();
            var result = RP.Insert(obj, out message);
            return JsonMessage(result, message);
        }
        #endregion
    }
}
