using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using CSD.Treasury.Service.Reciept_ReferenceTypes_ValueService;
using CSD.Treasury.Service.BehalfGroupService;
using CSD.Treasury.Service.ReferenceTypes_By_BehalfGroupService;
using CSD.Treasury.Service.ReferenceTypeService;
using CSD.Treasury.Service.ProceduresDB;
using CSD.Treasury.UI.Controllers;
using CSD.Treasury.UI.Models;
using CSD.Treasury.DataModel;
using System.Text;

namespace CSD.Treasury.UI.Areas.BasicInfo.Controllers
{
    [Authorize(Roles = "Admin,Manager,BasicInfo,ReportInApp")]
    public class BehalfGroupController : BaseController
    {
        // GET: BasicInfo/BehafGroup
        public ActionResult Index(SearchModelBehalfGroup obj)
        { 
            return View(obj);
        }
        public PartialViewResult List(SearchModelBehalfGroup OBJ)
        {
            int pageNum = OBJ.PageIndex;
            int pageSize = 10;
            int totalRows = 0;

            //Icls_BehalfGroupClient obj = new Icls_BehalfGroupClient();
            //var query = obj.SelectAll(1, 1000 * 1000, "BehalfGroupID", "int", true, out totalRows).ToList();

            var db = new TreasuryEntities();
            var query = db.BehalfGroupLists.ToList();
            if (!string.IsNullOrEmpty(OBJ.BehalfGroupDescription))
            {
                if (OBJ.BehalfGroupDescription.Contains('ی'))
                {
                    OBJ.BehalfGroupDescription = OBJ.BehalfGroupDescription.Replace('ی', 'ي');
                }
                if (OBJ.BehalfGroupDescription.Contains('ک'))
                {
                    OBJ.BehalfGroupDescription = OBJ.BehalfGroupDescription.Replace('ک', 'ك');
                }
                query = query.Where(d => d.BehalfGroupDescription.Contains(OBJ.BehalfGroupDescription.Trim())).ToList();
                //whereclouse = whereclouse + " and  LegalAccountName like N\'%" + obj.LegalAccountName+ "%\'";
                totalRows = query.Count();
            }
            if(OBJ.Receipt_Pay_TypeID != 0)
            {
                query = query.Where(d => d.Receipt_Pay_TypeID == OBJ.Receipt_Pay_TypeID).ToList();
                totalRows = query.Count();
            }
            query = query.OrderBy(d => d.Receipt_Pay_TypeID).Skip(pageSize * pageNum).Take(pageSize).ToList();
            OBJ.TotalPageCount = (int)Math.Ceiling(totalRows / (double)pageSize);
            OBJ.CurrentPage = pageNum;
            OBJ.List = query;
            OBJ.CurrentPage = OBJ.PageIndex;
            OBJ.TotalItemCount = totalRows;

            return PartialView(OBJ);
        }

        #region Create
        // GET: BasicInfo/BehafGroup/Create
        public ActionResult Create( bool FromCreateRequest=false)
        {
            var dc = new TreasuryEntities();
            ViewBag.v_accounts = (from a in dc.v_accounts
                                  select new
                                  {
                                      account_code = a.account_code.Trim(),
                                      Account_Name = a.Account_Name.Replace("ي", "ی").Replace("ك", "ک") + "/" + a.account_code

                                 }).ToList();

            int pageNum = 1;
            int pageSize = 1000;
            string orderByFieldName = "referenceTypeID";
            bool isAscendingOrder = true;
            string OrderByfeildtype = "int";
            int totalRows;

            Icls_ReferenceTypeClient obj = new Icls_ReferenceTypeClient();
            var query = obj.SelectAll(pageNum, pageSize, orderByFieldName, OrderByfeildtype, isAscendingOrder, out totalRows);
            ViewBag.Reference = query;

            //انواع
            ViewBag.ReceiptPayTypeLS = Utility.GetReceiptPayTypeLS();

            //گروه بابت 
            ViewBag.BehalfGroup = Utility.GetBehalfsGroupLs();

            //cashFlowGroup
            ViewBag.cashFlowGroup = Utility.CashFollowGroupLs();

            if(FromCreateRequest)
            {
        
                return PartialView("CreateFromRequest");
            }
            return PartialView();
        }

        // POST: BasicInfo/BehafGroup/Create
        [HttpPost]
        public ActionResult Create(Service.BehalfGroupService.BehalfGroup obj)
        {
            try
            {
                string message = "";

                if (ModelState.IsValid)
                {
                    Icls_SPsClient db = new Icls_SPsClient();
                    

                    //ثبت گروه بابت
                    Icls_BehalfGroupClient RP = new Icls_BehalfGroupClient();
                    var result = RP.Insert(obj, out message);

                    //ثبت عطف های بابت
                    if (Request.Form["Reference"] != null)
                    {
                        var referenceValue = Request.Form["Reference"].Split(',');
                        Service.ReferenceTypes_By_BehalfGroupService.ReferenceTypes_By_BehalfGroup robj = new Service.ReferenceTypes_By_BehalfGroupService.ReferenceTypes_By_BehalfGroup();
                        foreach (var item in referenceValue)
                        {
                            robj.referenceTypeID = Int32.Parse(item);
                            robj.BehalfGroupID = int.Parse(message.Split(';')[1]);
                            Icls_ReferenceTypes_By_BehalfGroupClient RBB = new Icls_ReferenceTypes_By_BehalfGroupClient();
                            string messageReferenceTypes_By_BehalfGroup = "";
                            var resultReferenceTypes_By_BehalfGroup = RBB.Insert(robj, out messageReferenceTypes_By_BehalfGroup);
                        }
                    }
                

                
                    return JsonMessage(result, message);
                }
                else
                {
                    return JsonMessage(false, "insert_error");
                }

            }
            catch
            {
                return PartialView();
            }
        }

        
        [HttpPost]
        public JsonResult ReceiptBehalfGrCreate( string BehalfGroupDescription, string account_code, int referenceTypeID=0)
        {
            try
            {
                var BehalfGroup = new Service.BehalfGroupService.BehalfGroup
                {
                    Receipt_Pay_TypeID=2,
                    BehalfGroupDescription=BehalfGroupDescription,
                    BehalfGroup_Account=account_code,
                    CreateDateTime=DateTime.Now
                };
                string message = "";
                Icls_BehalfGroupClient RP = new Icls_BehalfGroupClient();

                var result = RP.Insert(BehalfGroup, out message);
                if (result)
                {
                    int totalRows;
                    var BehalfGroupLs = RP.SelectBy_DynamicWhereClause("1=1", 1, 100, "BehalfGroupID", "int", false, out totalRows);
                    return Json(BehalfGroupLs.OrderByDescending(q => q.CreateDateTime).ToList(), JsonRequestBehavior.AllowGet);
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
        #endregion

        #region Edit
        // GET: BasicInfo/BehafGroup/Edit/5
        public ActionResult Edit(int id)
        {
            var dc = new TreasuryEntities();
            //معین در معین
            ViewBag.v_accounts = (from a in dc.v_accounts
                                  select new
                                  {
                                      account_code = a.account_code,
                                      Account_Name = a.Account_Name + "-" + a.account_code.Trim()

                                  }).ToList();

            //cashFlowGroup
            ViewBag.cashFlowGroup = Utility.CashFollowGroupLs();

            //انواع
            ViewBag.ReceiptPayTypeLS = Utility.GetReceiptPayTypeLS();
            Icls_BehalfGroupClient obj = new Icls_BehalfGroupClient();

            var BehalfGroup = obj.SelectRow(id).SingleOrDefault();
            int totalRows;
            Icls_ReferenceTypes_By_BehalfGroupClient robj = new Icls_ReferenceTypes_By_BehalfGroupClient();
            

            var selected = robj.SelectBy_DynamicWhereClause("BehalfGroupID=" + id, 1, 100, "referenceTypeID", "int", true, out totalRows);

            Icls_ReferenceTypeClient objR = new Icls_ReferenceTypeClient();
            var query = objR.SelectAll(1, 100, "referenceTypeID", "int", true, out totalRows);
       

            ViewBag.Reference = query.Select(q => new SelectListItem { Value = q.referenceTypeID.ToString(), Text = Utility.GetReferenceTitle(q.referenceTypeID), Selected = (selected.Select(d => d.referenceTypeID).Contains(q.referenceTypeID) ? true:false) }).ToList();
            return PartialView(BehalfGroup);
        }

        // POST: BasicInfo/BehafGroup/Edit/5
        [HttpPost]
        public ActionResult Edit(Service.BehalfGroupService.BehalfGroup obj)
        {
            try
            {
                //ویرایش گروه بابت
                string messageReferenceTypes_By_BehalfGroup="", message = "";
                Icls_BehalfGroupClient RP = new Icls_BehalfGroupClient();
                Icls_Reciept_ReferenceTypes_ValueClient Refrenceavlue = new Icls_Reciept_ReferenceTypes_ValueClient();
                var result = RP.Update(obj, out message);
                string WhereClause = "BehalfGroupID="+obj.BehalfGroupID;
                int totalRows;

                Icls_ReferenceTypes_By_BehalfGroupClient RBB = new Icls_ReferenceTypes_By_BehalfGroupClient();
                var query = RBB.SelectBy_DynamicWhereClause(WhereClause, 1, 1000, "referenceTypeID", "int", false, out totalRows);
               
                //حذف عطف ها
                foreach (var item in query)
                {
                    var resultReferenceTypes_By_BehalfGroup = RBB.Delete(item.ReferenceID, out message);
                }
                //ثبت عطف های بابت
                if (Request.Form["Reference"] != null)
                {
                    var referenceValue = Request.Form["Reference"].Split(',');
                    Service.ReferenceTypes_By_BehalfGroupService.ReferenceTypes_By_BehalfGroup robj = new Service.ReferenceTypes_By_BehalfGroupService.ReferenceTypes_By_BehalfGroup();
                    foreach (var item in referenceValue)
                    {
                        robj.referenceTypeID = Int32.Parse(item);
                        robj.BehalfGroupID =obj.BehalfGroupID;
                        
                        var resultReferenceTypes_By_BehalfGroup = RBB.Insert(robj, out messageReferenceTypes_By_BehalfGroup);
                    }

                    //foreach (var item in referenceValue)
                    //{
                    //    Service.ReferenceTypes_By_BehalfGroupService.ReferenceTypes_By_BehalfGroup robj = new Service.ReferenceTypes_By_BehalfGroupService.ReferenceTypes_By_BehalfGroup();
                    //    robj.BehalfGroupID = obj.BehalfGroupID;
                    //    robj.referenceTypeID = Int32.Parse(item);
                    //    var resultReferenceTypes_By_BehalfGroup = RBB.Insert(robj, out message);
                    //}
                }
                return JsonMessage(result, message);
            }
            catch
            {
                return PartialView();
            }
        }

        #endregion

        #region Delete
        // GET: BasicInfo/BehafGroup/Delete/5
        //public ActionResult ShowDeleteConfirm(int id)
        //{
        //    Icls_BehalfGroupClient obj = new Icls_BehalfGroupClient();
        //    return PartialView(obj.SelectRow(id).SingleOrDefault());
        //}

        // POST: BasicInfo/BehafGroup/Delete/5
        [HttpPost]
        public ActionResult Delete(int id)
        {
            try
            {
                int pageNum = 1;
                int pageSize = 1000;
                string orderByFieldName = "referenceTypeID";
                bool isAscendingOrder = true;
                string OrderByfeildtype = "int";
                string message = "";
                string WhereClause = "BehalfGroupID=" + id;
                int totalRows;
               
                //حذف عطف ها
                Icls_ReferenceTypes_By_BehalfGroupClient RBB = new Icls_ReferenceTypes_By_BehalfGroupClient();
                var query = RBB.SelectBy_DynamicWhereClause(WhereClause, pageNum, pageSize, orderByFieldName, OrderByfeildtype, isAscendingOrder, out totalRows);
                foreach (var item in query)
                {
                    var resultReferenceTypes_By_BehalfGroup = RBB.Delete(item.ReferenceID, out message);
                }

                //حذف گروه بابت
                Icls_BehalfGroupClient RP = new Icls_BehalfGroupClient();
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
        public ActionResult Details(int BehalfGroupId)
        {
            Icls_BehalfGroupClient  BehalfGroupClient = new Icls_BehalfGroupClient();
            var behalfGroup = BehalfGroupClient.SelectRow(BehalfGroupId).SingleOrDefault();
            return PartialView(behalfGroup);
        }
        #endregion
        /// <summary>
        /// چک نمودن برای این که عنوان تکراری ثبت نگردد
        /// </summary>
        /// <param name="BehalfGroupDescription"></param>
        /// <returns></returns>
        #region CheckTitle
        public JsonResult CheckTitle(string BehalfGroupDescription, string InitialBehalfGroupDescription)
        {
            Icls_SPsClient db = new Icls_SPsClient();
            if (BehalfGroupDescription == InitialBehalfGroupDescription)
            {
                return Json(true, JsonRequestBehavior.AllowGet);
            }
            if (db.CheckExistValueInTable("BehalfGroup", "BehalfGroupDescription", BehalfGroupDescription))
            {
                return Json(false, JsonRequestBehavior.AllowGet);
            }

            return Json(true, JsonRequestBehavior.AllowGet);
           
        }
        #endregion
        
        
    }
}
