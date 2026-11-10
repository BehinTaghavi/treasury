using CSD.Treasury.UI.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using CSD.Treasury.Service.ReferenceTypeService;
using CSD.Treasury.UI.Models;
using CSD.Treasury.Service.ProceduresDB;

namespace CSD.Treasury.UI.Areas.BasicInfo.Controllers
{
    [Authorize(Roles = "Admin,Manager,BasicInfo,ReportInApp")]
    public class RefrenceTypeController : BaseController
    {
        public ActionResult Index(string status="")
        {
            ViewBag.status = status;
            return View();
        }
        public ActionResult List()
        {
            int pageNum = 1;
            int pageSize = 1000;
            string orderByFieldName = "referenceTypeID";
            bool isAscendingOrder = true;
            string OrderByfeildtype = "int";
            int totalRows;

            Icls_ReferenceTypeClient obj = new Icls_ReferenceTypeClient();
            var query = obj.SelectAll(pageNum, pageSize, orderByFieldName, OrderByfeildtype, isAscendingOrder, out totalRows);
            return PartialView(query);
        }
        #region Create
        public ActionResult Create()
        {
            return PartialView();
        }
        [HttpPost]
        public ActionResult Create(ReferenceType obj)
        {
            try
            {
                string message = "";
                Icls_ReferenceTypeClient RP = new Icls_ReferenceTypeClient();
                var result = RP.Insert(obj, out message);
                return JsonMessage(result, message);
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
            ViewBag.Branches = Utility.GetBranches();
            Icls_ReferenceTypeClient obj = new Icls_ReferenceTypeClient();
            return PartialView(obj.SelectRow(id).SingleOrDefault());
        }

        [HttpPost]
        public ActionResult Edit(ReferenceType obj)
        {
            try
            {
                string message = "";
                Icls_ReferenceTypeClient RP = new Icls_ReferenceTypeClient();
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

        [HttpPost]
        public ActionResult Delete(int id)
        {
            try
            {
                string message = "";
                Icls_ReferenceTypeClient RP = new Icls_ReferenceTypeClient();
                var result = RP.Delete(id, out message);
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
        public JsonResult CheckTitle(string referenceTypeDescription,string InitialreferenceTypeDescription)
        {
            Icls_SPsClient db = new Icls_SPsClient();
            if (referenceTypeDescription == InitialreferenceTypeDescription)
            {
                return Json(true, JsonRequestBehavior.AllowGet);
            }
            if (db.CheckExistValueInTable("ReferenceType", "referenceTypeDescription", referenceTypeDescription))
            {
                return Json(false, JsonRequestBehavior.AllowGet);
            }

            return Json(true, JsonRequestBehavior.AllowGet);
        }
        #endregion
    }
}