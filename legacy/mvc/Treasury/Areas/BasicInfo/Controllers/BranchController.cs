using CSD.Treasury.DataModel;
using CSD.Treasury.Service.ProceduresDB;
using CSD.Treasury.UI.Controllers;
using CSD.Treasury.UI.Models;
using System;
using System.Linq;
using System.Web.Mvc;

namespace CSD.Treasury.UI.Areas.BasicInfo.Controllers
{
    [Authorize(Roles = "Admin,Manager,BasicInfo,ReportInApp")]
    public class BranchController : BaseController
    {
        // GET: BasicInfo/Branch
        public ActionResult Index()
        {
            return View();
        }
        public ActionResult List()
        {
            //int pageNum = 1;
            //int pageSize = 1000;
            //string orderByFieldName = "cmpBranchID";
            //bool isAscendingOrder = false;
            //string OrderByfeildtype = "int";
            //int totalRows;
            //Icls_cmpBranchesClient RP = new Icls_cmpBranchesClient();
            var db = new TreasuryEntities();
            //var query = RP.SelectAll(pageNum, pageSize, orderByFieldName, OrderByfeildtype, isAscendingOrder, out totalRows).ToList();
            var query = db.cmpBranches.ToList();
            return PartialView(query);
        }

        public PartialViewResult BranchInformation(int id)
        {
            //Icls_cmpBranchesClient obj = new Icls_cmpBranchesClient();
            //return PartialView(obj.SelectRow(id).SingleOrDefault());
            var db = new TreasuryEntities();
            return PartialView(db.cmpBranches.SingleOrDefault(d=>d.cmpBranchID == id));
        }

        #region Create
        public ActionResult Create()
        {
            var db = new TreasuryEntities();
            ViewBag.City = Utility.GetCityLs();
            return PartialView();
        }
        [HttpPost]
        public ActionResult Create(DataModel.cmpBranch obj)
        {
            var message = "";
            try
            {
                //string message = "";
                //Icls_cmpBranchesClient RP = new Icls_cmpBranchesClient();
                //var result = RP.Insert(obj, out message);
                var db = new TreasuryEntities();
                db.cmpBranches.Add(obj);
                db.SaveChanges();
                message = "ثبت با موفقیت انجام شد";
                return JsonMessage(true, message, null, false);
            }
            catch(Exception ex)
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
                //return PartialView();
            }
        }
        #endregion

        #region Edit
        public ActionResult Edit(int id)
        {
            //Icls_cmpBranchesClient obj = new Icls_cmpBranchesClient();
            //return PartialView(obj.SelectRow(id).SingleOrDefault());
            var db = new TreasuryEntities();

            ViewBag.City = Utility.GetCityLs();
            return PartialView(db.cmpBranches.SingleOrDefault(d=>d.cmpBranchID==id));
        }
        [HttpPost]
        public ActionResult Edit(DataModel.cmpBranch obj)
        {
            var message = "";
            try
            {
                //string message = "";
                //Icls_cmpBranchesClient RP = new Icls_cmpBranchesClient();
                //var result = RP.Update(obj, out message);
                //return JsonMessage(result, message);

                var db = new TreasuryEntities();
                var branche = db.cmpBranches.SingleOrDefault(d => d.cmpBranchID == obj.cmpBranchID);
                branche.cmpBranchName = obj.cmpBranchName;
                branche.cmpBranchAddress = obj.cmpBranchAddress;
                branche.CityID = obj.CityID;
                branche.IsMainBranch = obj.IsMainBranch;
                db.SaveChanges();
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
                //return PartialView();
            }
        }
        #endregion

        #region Delete
        //public ActionResult Delete(int id)
        //{
        //    Icls_cmpBranchesClient obj = new Icls_cmpBranchesClient();
        //    return PartialView(obj.SelectRow(id).SingleOrDefault());
        //}
        [HttpPost]
        public ActionResult Delete(CSD.Treasury.DataModel.cmpBranch obj)
        {
            string message = "";
            try
            {
                //int pageNum = 1;
                //int pageSize = 1000;
                //string orderByFieldName = "";
                //bool isAscendingOrder = true;
                //string OrderByfeildtype = "int";
                //string whereclouse = "cmpBranchID=" + obj.cmpBranchID;
                //int totalRows;
                var db = new TreasuryEntities();
                //حذف تلفن ها
                //Icls_cmpBranch_TELSClient o1 = new Icls_cmpBranch_TELSClient();
                //var TelLS = o1.SelectBy_DynamicWhereClause(whereclouse, pageNum, pageSize, orderByFieldName, OrderByfeildtype, isAscendingOrder, out totalRows).ToList();
                var TelLS = db.cmpBranch_TELS.Where(d => d.cmpBranchID == obj.cmpBranchID);

                if (TelLS.Count()!=0)
                {
                    db.cmpBranch_TELS.RemoveRange(TelLS);
                    //foreach (var item in TelLS)
                    //{
                    //    o1.Delete(item.ID, out message);
                    //}
                }

                //حذف ایمیل ها  
                //Icls_cmpBranch_EmailAddressClient o2 = new Icls_cmpBranch_EmailAddressClient();
                //var EmailAdress = o1.SelectBy_DynamicWhereClause(whereclouse, pageNum, pageSize, orderByFieldName, OrderByfeildtype, isAscendingOrder, out totalRows).ToList();
                var EmailAdress = db.cmpBranch_EmailAddress.Where(d => d.cmpBranchID == obj.cmpBranchID);

                if (EmailAdress.Count() != 0)
                {
                    db.cmpBranch_EmailAddress.RemoveRange(EmailAdress);
                    //foreach (var item in EmailAdress)
                    //{
                    //    o2.Delete(item.ID, out message);
                    //}
                }

                //حذف فکس ها
                //Icls_cmpBranch_FAXClient o3 = new Icls_cmpBranch_FAXClient();
                //var FaxLS = o1.SelectBy_DynamicWhereClause(whereclouse, pageNum, pageSize, orderByFieldName, OrderByfeildtype, isAscendingOrder, out totalRows).ToList();
                var FaxLS = db.cmpBranch_FAX.Where(d=>d.cmpBranchID == obj.cmpBranchID);

                if (FaxLS.Count() != 0)
                {
                    db.cmpBranch_FAX.RemoveRange(FaxLS);
                    //foreach (var item in FaxLS)
                    //{
                    //    o2.Delete(item.ID, out message);
                    //}
                }

                //حذف شعبه
                //Icls_cmpBranchesClient RP = new Icls_cmpBranchesClient();
                //var result = RP.Delete(obj.cmpBranchID, out message);
                var branch=db.cmpBranches.SingleOrDefault(d=>d.cmpBranchID== obj.cmpBranchID);
                db.cmpBranches.Remove(branch);
                db.SaveChanges();
                message = "حذف با موفقیت انجام شد";
                return JsonMessage(true, message, null, false);
            }
            catch(Exception ex)
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
            }
        }
        #endregion
        /// <summary>
        /// چک نمودن برای این که عنوان تکراری ثبت نگردد
        /// </summary>
        /// <param name="BehalfGroupDescription"></param>
        /// <returns></returns>
        #region CheckTitle
        public JsonResult CheckTitle(string cmpBranchName,string InitialcmpBranchName)
        {
            Icls_SPsClient db = new Icls_SPsClient();
            if (cmpBranchName == InitialcmpBranchName)
            {
                return Json(true, JsonRequestBehavior.AllowGet);
            }
            if (db.CheckExistValueInTable("cmpBranches", "cmpBranchName", cmpBranchName))
            {
                return Json(false, JsonRequestBehavior.AllowGet);
            }

            return Json(true, JsonRequestBehavior.AllowGet);
        }
        #endregion
    }
}