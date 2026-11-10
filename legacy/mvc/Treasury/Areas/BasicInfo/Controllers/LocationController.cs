using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using CSD.Treasury.Service.LocationsService;
using CSD.Treasury.Service.LocationByCostCenterService;
using CSD.Treasury.UI.Controllers;
using CSD.Treasury.UI.Models;
using CSD.Treasury.DataModel;

namespace CSD.Treasury.UI.Areas.BasicInfo.Controllers
{
    [Authorize(Roles = "Admin,Manager,BasicInfo,ReportInApp")]
    public class LocationController : BaseController
    {
        // GET: BasicInfo/Location
        public ActionResult Index(int BranchId)
        {
            ViewBag.BranchId = BranchId;
            return PartialView();
        }
        public ActionResult List(int BranchId)
        {
            int pageNum = 1;
            int pageSize = 1000;
            string orderByFieldName = "LocationID";
            bool isAscendingOrder = true;
            string OrderByfeildtype = "int";
            int totalRows;
            string whereclouse = "cmpBranchID=" + BranchId;
            Icls_LocationsClient obj = new Icls_LocationsClient();
            var query = obj.SelectBy_DynamicWhereClause(whereclouse, pageNum, pageSize, orderByFieldName, OrderByfeildtype, isAscendingOrder, out totalRows);
            return PartialView(query);
        }
        #region Create
        public ActionResult Create(int BranchId)
        {

            var dc = new TreasuryEntities();
            //نوع ذینفع  
            //var unique_account_type = dc.v_unique_account_type.Select(q => q.Description == "مرکز هزینه");
            //var typeCodeLs = dc.v_unique_account_type.Where(w => w.Description.Contains("هزینه")).Select(d=>d.Code.ToString());
            ViewBag.unique_account = dc.v_unique_accounts.Where(q =>q.Is_Cost_Center==true);
         
            
            //unique_account_ty  pe.Add(new v_unique_account_type { Code = 0, Description = Resourse.Resources.Others });
            //ViewBag.unique_account_type = unique_account_type;
            ViewBag.Branches = Utility.GetBranches();
            return PartialView(new Service.LocationsService.Location() { cmpBranchID = BranchId });
        }

        // POST: BasicInfo/BehafGroup/Create
        [HttpPost]
        public ActionResult Create(Service.LocationsService.Location obj, List<string> Unique_Account_No)
        {
            try
            {
                string message = "";
                Icls_LocationsClient RP = new Icls_LocationsClient();
                var result = RP.Insert(obj, out message);
                var LocationID = message.Split(';')[1];
                Icls_LocationByCostCenterClient RP1 = new Icls_LocationByCostCenterClient();
                if (!string.IsNullOrEmpty(Request.Form["LocationByCostCenters_St"]))
                {
                    foreach (var item in Request.Form["LocationByCostCenters_St"].Split(','))
                    {
                        Service.LocationByCostCenterService.LocationByCostCenter lcc = new Service.LocationByCostCenterService.LocationByCostCenter();
                        lcc.LocationId = Int32.Parse(LocationID);
                        lcc.Unique_Account_No = item;
                        RP1.Insert(lcc, out message);
                    }
                }
                //if(Unique_Account_No!=null)
                //{
                //    foreach (var item in Unique_Account_No)
                //    {
                //        Service.LocationByCostCenterService.LocationByCostCenter lcc = new Service.LocationByCostCenterService.LocationByCostCenter();
                //        lcc.LocationId = Int32.Parse(LocationID);
                //        lcc.Unique_Account_No = item;
                //        RP1.Insert(lcc, out message);
                //    }
                //}

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
            var dc = new TreasuryEntities();
            //نوع ذینفع  
            var unique_account_type = dc.v_unique_account_type.ToList();
            unique_account_type.Add(new v_unique_account_type { Code = 0, Description = Resourse.Resources.Others });
            ViewBag.unique_account_type = unique_account_type;

            ViewBag.Branches = Utility.GetBranches();
            Icls_LocationsClient obj = new Icls_LocationsClient();
            var Location = obj.SelectRow(id).SingleOrDefault();
         
            ViewBag.unique_account = dc.v_unique_accounts.Where(q => q.Is_Cost_Center == true);

            Icls_LocationByCostCenterClient obj1 = new Icls_LocationByCostCenterClient();
            int pageNum = 1;
            int pageSize = 1000;
            string orderByFieldName = "LocationByCostCenterID";
            bool isAscendingOrder = true;
            string OrderByfeildtype = "int";
            int totalRows;
            string whereclouse = "LocationId=" + Location.LocationID;
            ViewBag.CostCenterLs = obj1.SelectBy_DynamicWhereClause(whereclouse, pageNum, pageSize, orderByFieldName, OrderByfeildtype, isAscendingOrder, out totalRows).ToList();


            return PartialView(Location);
        }

        [HttpPost]
        public ActionResult Edit(CSD.Treasury.Service.LocationsService.Location obj, List<string> Unique_Account_No)
        {
            try
            {
                //حذف مراکز هزینه موجود
                Icls_LocationByCostCenterClient RB = new Icls_LocationByCostCenterClient();
                string message = "";
                int pageNum = 1;
                int pageSize = 1000;
                string orderByFieldName = "LocationByCostCenterID";
                bool isAscendingOrder = true;
                string OrderByfeildtype = "int";
                int totalRows;
                string whereclouse = "LocationId=" + obj.LocationID;
                var CostCenterLs = RB.SelectBy_DynamicWhereClause(whereclouse, pageNum, pageSize, orderByFieldName, OrderByfeildtype, isAscendingOrder, out totalRows).ToList();
                if (CostCenterLs != null)
                {
                    foreach (var item in CostCenterLs)
                    {
                        RB.Delete(item.LocationByCostCenterID, out message);
                    }
                }

                //افزودن مراکزهزینه جدید
                //foreach (var item in Unique_Account_No)
                //{
                //    Service.LocationByCostCenterService.LocationByCostCenter lcc = new Service.LocationByCostCenterService.LocationByCostCenter();
                //    lcc.LocationId = obj.LocationID;
                //    lcc.Unique_Account_No = item;
                //    RB.Insert(lcc, out message);
                //}
                if (!string.IsNullOrEmpty(Request.Form["LocationByCostCenters_St"]))
                {
                    foreach (var item in Request.Form["LocationByCostCenters_St"].Split(','))
                    {
                        Service.LocationByCostCenterService.LocationByCostCenter lcc = new Service.LocationByCostCenterService.LocationByCostCenter();
                        lcc.LocationId = obj.LocationID;
                        lcc.Unique_Account_No = item;
                        RB.Insert(lcc, out message);
                    }
                }


                Icls_LocationsClient RP = new Icls_LocationsClient();
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
        public ActionResult DeleteLocation(int LocationID)
        {
            try
            {
                string message = "";
                Icls_LocationsClient RP = new Icls_LocationsClient();
                Icls_LocationByCostCenterClient obj = new Icls_LocationByCostCenterClient();

                int pageNum = 1;
                int pageSize = 1000;
                string orderByFieldName = "LocationByCostCenterID";
                bool isAscendingOrder = true;
                string OrderByfeildtype = "int";
                int totalRows;
                string whereclouse = "LocationId=" + LocationID;
                var CostCenterLs = obj.SelectBy_DynamicWhereClause(whereclouse, pageNum, pageSize, orderByFieldName, OrderByfeildtype, isAscendingOrder, out totalRows).ToList();
                //حذف مراکز هزینه واحد سازمانی
                if (CostCenterLs != null)
                {
                    foreach (var item in CostCenterLs)
                    {
                        obj.Delete(item.LocationByCostCenterID, out message);
                    }
                }

                var result = RP.Delete(LocationID, out message);
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