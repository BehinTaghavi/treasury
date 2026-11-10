using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using CSD.Treasury.Service.BasicBanksService;
using CSD.Treasury.UI.Controllers;
using CSD.Treasury.Service.ProceduresDB;

namespace CSD.Treasury.UI.Areas.BasicInfo.Controllers
{
    [Authorize(Roles = "Admin,Manager,BasicInfo,ReportInApp")] 
    public class BasicBankController : BaseController
    {       
        // GET: BasicInfo/BasicBank
        public ActionResult Index()
        {           
            return View();
        }
        public ActionResult List()
        {
            int totalRows;
            Icls_BasicBanksClient obj = new Icls_BasicBanksClient();
            var query = obj.SelectAll(1, 1000, "BasicBankId", "int", false, out totalRows);
            return PartialView(query);
        }
        /// <summary>
        /// create basicBank
        /// </summary>
        /// <returns></returns>
        #region Create
        public ActionResult Create()
        {
            return PartialView();
        }
        [HttpPost]
        public ActionResult Create( BasicBank obj)
        {
            try
            {
                string message = "";
                Icls_BasicBanksClient RP = new Icls_BasicBanksClient();
                var result = RP.Insert(obj, out message);
                return JsonMessage(result, message);
            }
            catch
            {
                return PartialView();
            }
        }
        #endregion
        /// <summary>
        /// Edit basicBank
        /// </summary>
        /// <returns></returns>
        #region Edit
        public ActionResult Edit(int id)
        {
            Icls_BasicBanksClient obj = new Icls_BasicBanksClient();
            return PartialView(obj.SelectRow(id).SingleOrDefault());
        }
        [HttpPost]
        public ActionResult Edit(BasicBank obj)
        {
            try
            {
                string message = "";
                Icls_BasicBanksClient RP = new Icls_BasicBanksClient();
                var result = RP.Update(obj, out message);
                return JsonMessage(result, message);
            }
            catch
            {
                return PartialView();
            }
        }
        #endregion
        /// <summary>
        /// Delete basicBank
        /// </summary>
        /// <returns></returns>
        #region Delete
        //public ActionResult Delete(int id)
        //{
        //    Icls_BasicBanksClient obj = new Icls_BasicBanksClient();
        //    return PartialView(obj.SelectRow(id).SingleOrDefault());
        //}
        [HttpPost]
        public ActionResult Delete(int BasicBankId)
        {
            try
            {
                string message = "";
                Icls_BasicBanksClient RP = new Icls_BasicBanksClient();
                var result = RP.Delete(BasicBankId, out message);
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
        public JsonResult CheckTitle(string BasicBankDescription,string InitialBasicBankDescription)
        {
            Icls_SPsClient db = new Icls_SPsClient();
            if (BasicBankDescription == InitialBasicBankDescription)
            {
                return Json(true, JsonRequestBehavior.AllowGet);
            }
            if (db.CheckExistValueInTable("BasicBanks", "BasicBankDescription", BasicBankDescription))
            {
                return Json(false, JsonRequestBehavior.AllowGet);
            }

            return Json(true, JsonRequestBehavior.AllowGet);
        }
        #endregion
    }
}