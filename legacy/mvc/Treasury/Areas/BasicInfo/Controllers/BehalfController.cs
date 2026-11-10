using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using CSD.Treasury.Service.BehalfsService;
using CSD.Treasury.UI.Controllers;
using CSD.Treasury.Service.ProceduresDB;
using CSD.Treasury.UI.Models;

namespace CSD.Treasury.UI.Areas.BasicInfo.Controllers
{
    [Authorize(Roles = "Admin,Manager,BasicInfo,ReportInApp")]
    public class BehalfController : BaseController
    {
        // GET: BasicInfo/Behalf
        public ActionResult Index(SearchModelBehalf obj)
        {
            ViewBag.FromCreateRequest = obj.FromCreateRequest;
            ViewBag.BehalfGroupID = obj.BehalfGroupID;
            if(obj.BehalfGroupDescription == null)
            {
                ViewBag.BehalfGroupDescription = Utility.GetBehalfGroup(obj.BehalfGroupID);
            }
            else
            {
                ViewBag.BehalfGroupDescription = obj.BehalfGroupDescription;
            }
           
            return PartialView();
        }
        public ActionResult List(SearchModelBehalf obj)
        {
            int pageNum = obj.PageIndex;
            int pageSize = 10;
            int totalRows;

            ViewBag.BehalfGroupDescription = obj.BehalfGroupDescription;
            string whereclouse = "BehalfGroupID =" + obj.BehalfGroupID;
            Icls_BehalfsClient BehalfClient = new Icls_BehalfsClient();
            var query = BehalfClient.SelectBy_DynamicWhereClause(whereclouse, 1, 100, "BehalfID", "int", false, out totalRows).ToList();

            if (!string.IsNullOrEmpty(obj.BehalfDescription))
            {
                query = query.Where(d => d.BehalfDescription.Contains(obj.BehalfDescription.Trim())).ToList();
            }

          
            query = query.OrderBy(d => d.BehalfID).Skip(pageSize * pageNum).Take(pageSize).ToList();
            obj.TotalPageCount = (int)Math.Ceiling(totalRows / (double)pageSize);
            obj.CurrentPage = pageNum;
            obj.List = query;
            obj.CurrentPage = obj.PageIndex;
            obj.TotalItemCount = totalRows;

            return PartialView(obj);
            //return PartialView(query);
        }

        #region Create
        // GET: BasicInfo/Behalf/Create
        public ActionResult Create(int BehalfGroupID, string BehalfGroupDescription, bool FromCreateRequest = false)
        {
            ViewBag.BehalfGroupDescription = BehalfGroupDescription;
            if (FromCreateRequest)
                return PartialView("FromCreateRequest", new Behalf { BehalfGroupID = BehalfGroupID });
            return  PartialView(new Behalf { BehalfGroupID=BehalfGroupID});
        }

        [HttpPost]
        public JsonResult ReceiptBehalfCreate(int BehalfGroupID, string BehalfDescription, string BehalfDescription2)
        {
            try
            {
                var behalf = new Behalf {
                    BehalfDescription = BehalfDescription,
                    BehalfGroupID = BehalfGroupID,
                    BehalfDescription2 = BehalfDescription2,
                    CreateDateTime = DateTime.Now
                };
                string message = "";
                Icls_BehalfsClient RP = new Icls_BehalfsClient();

                var result = RP.Insert(behalf, out message);
                if (result)
                {
                    int totalRows;
                    string whereclouse = "BehalfGroupID =" + BehalfGroupID;
                    var behalfs = RP.SelectBy_DynamicWhereClause(whereclouse, 1, 100, "BehalfID", "int", false, out totalRows);
                    return Json(behalfs.OrderByDescending(q => q.CreateDateTime).Select(q => new { BehalfDescription = q.BehalfDescription, BehalfID = q.BehalfID, BehalfGroupID = q.BehalfGroupID }), JsonRequestBehavior.AllowGet);
                }
                else
                {
                    return Json(false, JsonRequestBehavior.AllowGet);
                }
            }
            catch
            {
                return Json(false, JsonRequestBehavior.AllowGet);
            }
        }

        // POST: BasicInfo/Behalf/Create
        [HttpPost]
        public ActionResult Create(Behalf obj )
        {
            try
            {
                string message = "";
                obj.CreateDateTime = DateTime.Now;
                Icls_BehalfsClient RP = new Icls_BehalfsClient();

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

        // GET: BasicInfo/Behalf/Edit/5
        public ActionResult Edit(int id, string BehalfGroupDescription)
        {
            ViewBag.BehalfGroupDescription = BehalfGroupDescription;
            Icls_BehalfsClient obj = new Icls_BehalfsClient();
            return PartialView(obj.SelectRow(id).SingleOrDefault());
        }

        // POST: BasicInfo/Behalf/Edit/5
        [HttpPost]
        public ActionResult Edit(Behalf obj)
        {
            try
            {
                string message = "";
                Icls_BehalfsClient RP = new Icls_BehalfsClient();
                var result = RP.Update(obj, out message);
                return JsonMessage(result, message);
            }
            catch
            {
                return PartialView();
            }
        }

        #endregion

        #region Details
        public ActionResult Details(int BehalfId)
        {
           Icls_BehalfsClient  BehalClient = new Icls_BehalfsClient();
            var behalf= BehalClient.SelectRow(BehalfId).SingleOrDefault();
            return PartialView(behalf);
        }
        #endregion

        #region Delete
        // GET: BasicInfo/Behalf/Delete/5
        //public ActionResult ShowDeleteConfirm(int id)
        //{
        //    Icls_BehalfsClient obj = new Icls_BehalfsClient();
        //    return PartialView(obj.SelectRow(id).SingleOrDefault());
        //}

        // POST: BasicInfo/Behalf/Delete/5
        [HttpPost]
        public ActionResult Delete(int id)
        {
            try
            {
                string message = "";
                Icls_BehalfsClient RP = new Icls_BehalfsClient();
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
        /// <param name="BehalfDescription"></param>
        /// <returns></returns>
        #region CheckTitle
        public JsonResult CheckTitle(string BehalfDescription,string InitialBehalfDescription,int BehalfGroupID)
        {
            Icls_SPsClient db = new Icls_SPsClient();
            int totalrow;
            Icls_BehalfsClient BehalfClient = new Icls_BehalfsClient();
            if (BehalfDescription == InitialBehalfDescription)
            {
                return Json(true, JsonRequestBehavior.AllowGet);
            }
            var ifExit = BehalfClient.SelectBy_DynamicWhereClause("BehalfDescription =\"" + BehalfDescription + "\" and BehalfGroupID =" + BehalfGroupID, 1, 1000, "BehalfID", "int", false, out totalrow).Any();
            if (ifExit)
            {
                return Json(false, JsonRequestBehavior.AllowGet);
            }
        
            return Json(true, JsonRequestBehavior.AllowGet);
        }
        #endregion
    }
}
