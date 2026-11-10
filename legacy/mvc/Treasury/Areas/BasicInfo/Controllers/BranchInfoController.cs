using CSD.Treasury.UI.Controllers;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using CSD.Treasury.Service.cmpBranch_TELSService;
using CSD.Treasury.Service.cmpBranch_FAXService;
using CSD.Treasury.Service.cmpBranch_EmailAddressService;

namespace CSD.Treasury.UI.Areas.BasicInfo.Controllers
{
    [Authorize(Roles = "Admin,Manager,BasicInfo,ReportInApp")]
    public class BranchInfoController : BaseController
    {

        public ActionResult TelIndex(int ID) {
            ViewBag.BranchId = ID;
            return PartialView();
        }
        public ActionResult FaxIndex(int ID)
        {
            ViewBag.BranchId = ID;
            return PartialView();
        }
        public ActionResult EmailIndex(int ID)
        {
            ViewBag.BranchId = ID;
            return PartialView();
        }
        /// <summary>
        /// ایجاد تلفن های شعب
        /// </summary>
        /// <returns></returns>
        #region BranchTels
        public ActionResult CreateTel(int ID)
        {
            ViewBag.BranchId = ID;
            return PartialView();
        }

        public PartialViewResult ListTel(int ID)
        {
            int pageNum = 1;
            int pageSize = 1000;
            string orderByFieldName = "ID";
            bool isAscendingOrder = false;
            string OrderByfeildtype = "int";
            int totalRows;
            string whereclouse = "cmpBranchID=" + ID;
            Icls_cmpBranch_TELSClient RP = new Icls_cmpBranch_TELSClient();
            ViewBag.BranchId = ID;
            var query = RP.SelectBy_DynamicWhereClause(whereclouse, pageNum, pageSize, orderByFieldName, OrderByfeildtype, isAscendingOrder, out totalRows).ToList();
            return PartialView(query);
        }
        [HttpPost]
        public ActionResult CreateTel(Service.cmpBranch_TELSService.cmpBranch_TELS obj)
        {
            try
            {
                string message = "";
                Icls_cmpBranch_TELSClient RP = new Icls_cmpBranch_TELSClient();
                var result = RP.Insert(obj, out message);
                return JsonMessage(result, message, obj);
            }
            catch
            {
                return PartialView();
            }
        }
    
        [HttpPost]
        public ActionResult EditTel(Service.cmpBranch_TELSService.cmpBranch_TELS obj)
        {
            try
            {
                string message = "";
                Icls_cmpBranch_TELSClient RP = new Icls_cmpBranch_TELSClient();
                var result = RP.Update(obj, out message);
                return JsonMessage(result, message);
            }
            catch
            {
                return PartialView();
            }
        }

        public ActionResult DeleteTel(int Id)
        {
            try
            {
                string message = "";
                Icls_cmpBranch_TELSClient RP = new Icls_cmpBranch_TELSClient();
                var result = RP.Delete(Id, out message);
                return JsonMessage(result, message);
            }
            catch
            {
                return PartialView();
            }
        }
        #endregion

        #region BranchFax
        public PartialViewResult ListFax(int ID)
        {
            int pageNum = 1;
            int pageSize = 1000;
            string orderByFieldName = "ID";
            bool isAscendingOrder = false;
            string OrderByfeildtype = "int";
            int totalRows;
            string whereclouse = "cmpBranchID=" + ID;
            Icls_cmpBranch_FAXClient RP = new Icls_cmpBranch_FAXClient();
            ViewBag.BranchId = ID;
            var query = RP.SelectBy_DynamicWhereClause(whereclouse, pageNum, pageSize, orderByFieldName, OrderByfeildtype, isAscendingOrder, out totalRows).ToList();
            return PartialView(query);
        }
        public ActionResult CreateFax(int ID)
        {
            ViewBag.BranchId = ID;
            return PartialView();
        }
        [HttpPost]
        public ActionResult CreateFax(Service.cmpBranch_FAXService.cmpBranch_FAX obj)
        {
            try
            {
                string message = "";
                Icls_cmpBranch_FAXClient RP = new Icls_cmpBranch_FAXClient();
                var result = RP.Insert(obj, out message);
                return JsonMessage(result, message);
            }
            catch
            {
                return PartialView();
            }
        }

        [HttpPost]
        public ActionResult EditFax(Service.cmpBranch_FAXService.cmpBranch_FAX obj)
        {
            try
            {
                string message = "";
                Icls_cmpBranch_FAXClient RP = new Icls_cmpBranch_FAXClient();
                var result = RP.Update(obj, out message);
                return JsonMessage(result, message);
            }
            catch
            {
                return PartialView();
            }
        }

        public ActionResult DeleteFax(int Id)
        {
            try
            {
                string message = "";
                Icls_cmpBranch_FAXClient RP = new Icls_cmpBranch_FAXClient();
                var result = RP.Delete(Id, out message);
                return JsonMessage(result, message);
            }
            catch
            {
                return PartialView();
            }
        }
        #endregion

        #region BranchEmail
        public PartialViewResult ListEmail(int ID)
        {
            int pageNum = 1;
            int pageSize = 1000;
            string orderByFieldName = "ID";
            bool isAscendingOrder = false;
            string OrderByfeildtype = "int";
            int totalRows;
            string whereclouse = "cmpBranchID=" + ID;
            Icls_cmpBranch_EmailAddressClient RP = new Icls_cmpBranch_EmailAddressClient();
            ViewBag.BranchId = ID;
            var query = RP.SelectBy_DynamicWhereClause(whereclouse, pageNum, pageSize, orderByFieldName, OrderByfeildtype, isAscendingOrder, out totalRows).ToList();
            return PartialView(query);
        }
        public ActionResult CreateEmail(int ID)
        {
            ViewBag.BranchId = ID;
            return PartialView();
        }
        [HttpPost]
        public ActionResult CreateEmail(Service.cmpBranch_EmailAddressService.cmpBranch_EmailAddress obj)
        {
            try
            {
                string message = "";
                Icls_cmpBranch_EmailAddressClient RP = new Icls_cmpBranch_EmailAddressClient();
                var result = RP.Insert(obj, out message);
                return JsonMessage(result, message);
            }
            catch
            {
                return PartialView();
            }
        }
    
        [HttpPost]
        public ActionResult EditEmail(Service.cmpBranch_EmailAddressService.cmpBranch_EmailAddress obj)
        {
            try
            {
                string message = "";
                Icls_cmpBranch_EmailAddressClient RP = new Icls_cmpBranch_EmailAddressClient();
                var result = RP.Update(obj, out message);
                return JsonMessage(result, message);
            }
            catch
            {
                return PartialView();
            }
        }
        public ActionResult DeleteEmail(int Id)
        {
            try
            {
                string message = "";
                Icls_cmpBranch_EmailAddressClient RP = new Icls_cmpBranch_EmailAddressClient();
                var result = RP.Delete(Id, out message);
                return JsonMessage(result, message);
            }
            catch
            {
                return PartialView();
            }
        }
        #endregion
    }
}