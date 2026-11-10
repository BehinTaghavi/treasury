using CSD.Treasury.UI.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using CSD.Treasury.Service.PettyCashService;
using CSD.Treasury.Service.PettyCash_BehalfGroupsService;
using CSD.Treasury.DataModel;
using CSD.Treasury.UI.Models;
using CSD.Treasury.Service.ProceduresDB;
using System.Configuration;

namespace CSD.Treasury.UI.Areas.BasicInfo.Controllers
{
    [Authorize(Roles = "Admin,Manager,BasicInfo,ReportInApp")]
    public class PettyCashController : BaseController
    {
        // GET: BasicInfo/PettyCash
        public ActionResult Index()
        {
            return View();
        }
        public ActionResult List()
        {
            int totalRows;
            Icls_PettyCashClient RP = new Icls_PettyCashClient();

            var query = RP.SelectAll(1, 1000, "PettyCashID", "int", false, out totalRows).ToList();
            return PartialView(query);
        }
        #region Create
        public ActionResult Create()
        {
            var dc = new TreasuryEntities();
            //معین در معین
            ViewBag.v_accounts = (from a in dc.v_accounts
                                  select new
                                  {
                                      account_code = a.account_code.Replace(" ",""),
                                      Account_Name = a.Account_Name +"-"+ a.account_code.Trim()

                                  }).ToList();

            //نوع  
            var unique_account_type = dc.v_unique_account_type.ToList();
            unique_account_type.Add(new v_unique_account_type { Code = 0, Description = Resourse.Resources.Others });
            ViewBag.unique_account_type = unique_account_type;

           
            ViewBag.BranchLs = Utility.GetBranches();

            //لیست گروه بابت ها
            ViewBag.BehalfGroup = Utility.GetBehalfsGroupLs().Where(d => d.Receipt_Pay_TypeID == int.Parse(ConfigurationManager.AppSettings["Payment_Type_ID"]));


            //لیست کاربران
            ViewBag.UserLs = dc.UserInfoes.Where(d => d.IsActive == true);

            return PartialView();
        }

        [HttpPost]
        public ActionResult Create(Service.PettyCashService.PettyCash obj)
        {
            try
            {
              
                string message = "";
                Icls_PettyCashClient RP = new Icls_PettyCashClient();
                var result = RP.Insert(obj, out message);
                //ثبت گروه بابت
                if (Request.Form["BehalfGroup"] != null)
                {
                    var referenceValue = Request.Form["BehalfGroup"].Split(',');
                    Service.PettyCash_BehalfGroupsService.PettyCash_BehalfGroups Pobj = new Service.PettyCash_BehalfGroupsService.PettyCash_BehalfGroups();
                    foreach (var item in referenceValue)
                    {
                        Pobj.BehalfGroupID = Int32.Parse(item);
                        Pobj.PettyCashID = int.Parse(message.Split(';')[1]);
                        Icls_PettyCash_BehalfGroupsClient  RBB = new Icls_PettyCash_BehalfGroupsClient();
                        string messagePettyCash_BehalfGroups = "";
                        var resultReferenceTypes_By_BehalfGroup = RBB.Insert(Pobj, out messagePettyCash_BehalfGroups);
                    }
                }
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
            Icls_PettyCashClient obj = new Icls_PettyCashClient();

            ViewBag.BranchLs = Utility.GetBranches();
            //لیست حساب معین
            ViewBag.v_accounts = (from a in dc.v_accounts
                                  select new
                                  {
                                      account_code = a.account_code,
                                      Account_Name = a.Account_Name + "-" + a.account_code.Trim()

                                  }).ToList();

            //لیست کاربران
            ViewBag.UserLs = dc.UserInfoes.Where(d => d.IsActive == true);

            //نوع ذینفع  
            var unique_account_type = dc.v_unique_account_type.ToList();
            unique_account_type.Add(new v_unique_account_type { Code = 0, Description = Resourse.Resources.Others });
            ViewBag.unique_account_type = unique_account_type;

            //لیست گروه بابت ها
            string whereclouse = "PettyCashID=" + id;
            int totalRows;
            Icls_PettyCash_BehalfGroupsClient RBB = new Icls_PettyCash_BehalfGroupsClient();
            var selected = RBB.SelectBy_DynamicWhereClause(whereclouse, 1, 100, "PettyCash_BehalfGroupId", "int", true, out totalRows);

            var query =  Utility.GetBehalfsGroupLs().Where(d => d.Receipt_Pay_TypeID == int.Parse(ConfigurationManager.AppSettings["Payment_Type_ID"]));
            ViewBag.BehalfGroup = query.Select(q => new SelectListItem { Value = q.BehalfGroupID.ToString(), Text = q.BehalfGroupDescription, Selected = (selected.Select(d => d.BehalfGroupID).Contains(q.BehalfGroupID) ? true : false) }).ToList();
           

            var PettyCash = obj.SelectRow(id).SingleOrDefault();

            if (PettyCash.Unique_Account_No == null)
            {
                //درصورتی که نوع ذینفع نداشته باشد
                ViewBag.uniqueTypeId =0;
            }
            else
            {
                var unique_account = dc.v_unique_accounts.SingleOrDefault(d => d.Account_Code == PettyCash.Unique_Account_No);
                //برگرداندن مقدار نوع ذینفع برای نمایش
                ViewBag.uniqueTypeId = unique_account.Type_Code.Trim();
                ViewBag.uniqueAccount_Code = unique_account.Account_Code;
                ViewBag.uniqueAccount_Name = unique_account.Account_Name;
                //تفصیل 
                ViewBag.unique_accounts = dc.v_unique_accounts.Where(d => d.Type_Code == unique_account.Type_Code.Trim());
            
               
              
                ViewBag.SlectedLs = (from p in dc.v_unique_accounts
                                     where p.Type_Code == unique_account.Type_Code
                                     select new
                                     {
                                         Account_Code = p.Account_Code,
                                         Account_Name = p.Account_Name + "/" + p.Account_Code
                                     }).ToList();

            }

            return PartialView(PettyCash);
        }
        [HttpPost]
        public ActionResult Edit(Service.PettyCashService.PettyCash obj)
        {
            try
            {
                string message = "", message1 = "";
                Icls_PettyCashClient RP = new Icls_PettyCashClient();
                var result = RP.Update(obj, out message1);
                int totalRows;

                if(result)
                {
                    //حذف گروه های بایت جاری و بعد اضافه کردن مجدد آنها 
                    Icls_PettyCash_BehalfGroupsClient RBB = new Icls_PettyCash_BehalfGroupsClient();
                    var query = RBB.SelectBy_DynamicWhereClause("PettyCashID ="+obj.PettyCashID, 1, 100, "PettyCash_BehalfGroupId", "int", true, out totalRows);
                    foreach (var item in query)
                    {
                        RBB.Delete(item.PettyCash_BehalfGroupId, out message);
                    }

                    //ثبت گروه بابت
                    if (Request.Form["BehalfGroup"] != null)
                    {
                        var referenceValue = Request.Form["BehalfGroup"].Split(',');
                        Service.PettyCash_BehalfGroupsService.PettyCash_BehalfGroups Pobj = new Service.PettyCash_BehalfGroupsService.PettyCash_BehalfGroups();
                        foreach (var item in referenceValue)
                        {
                            Pobj.BehalfGroupID = Int32.Parse(item);
                            Pobj.PettyCashID =obj.PettyCashID;
                            RBB.Insert(Pobj, out message);
                        }
                    }
                }
               
                return JsonMessage(result, message1);
            }
            catch
            {
                return PartialView();
            }
        }
        #endregion

        #region Delete
        [HttpPost]
        public ActionResult Delete(int PettyCashID)
        {
            try
            {
                int totalRows;
                string message = "";
                Icls_PettyCashClient RP = new Icls_PettyCashClient();
                Icls_PettyCash_BehalfGroupsClient pettybehalfClient = new Icls_PettyCash_BehalfGroupsClient();
                var behalfGroupLs=  pettybehalfClient.SelectBy_DynamicWhereClause("PettyCashID= "+PettyCashID, 1, 50, "PettyCash_BehalfGroupId", "int", false,out totalRows);
                foreach(var item in behalfGroupLs)
                {
                    pettybehalfClient.Delete(item.PettyCash_BehalfGroupId, out message);
                }
                var result = RP.Delete(PettyCashID, out message);
                return JsonMessage(result, message);
            }
            catch
            {
                return PartialView();
            }
        }
        #endregion

        #region Details
        public ActionResult Details(int PettyCashID)
        {
            Icls_PettyCashClient RP = new Icls_PettyCashClient();
            var PettyCash = RP.SelectRow(PettyCashID).SingleOrDefault();
            return PartialView(PettyCash);
        }
        #endregion



        /// <summary>
        /// چک نمودن برای این که عنوان تکراری ثبت نگردد
        /// </summary>
        /// <param name="PettyCashName"></param>
        /// <returns></returns>
        #region CheckTitle
        public JsonResult CheckTitle(string PettyCashName,string InitialPettyCashName)
        {
            Icls_SPsClient db = new Icls_SPsClient();
            if (PettyCashName == InitialPettyCashName)
            {
                return Json(true, JsonRequestBehavior.AllowGet);
            }
            if (db.CheckExistValueInTable("PettyCash", "PettyCashName", PettyCashName))
            {
                return Json(false, JsonRequestBehavior.AllowGet);
            }

            return Json(true, JsonRequestBehavior.AllowGet);
        }
        #endregion
    }
}