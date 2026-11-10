using CSD.Treasury.DataModel;
using CSD.Treasury.UI.Controllers;
using CSD.Treasury.UI.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using CSD.Treasury.Service.PermissiveSignaturesService;
using CSD.Treasury.Service.PermissiveSignatureForEachBankAccountService;
using System.Data.Entity.Validation;
using CSD.Treasury.Service.ProceduresDB;
using System.Configuration;

namespace CSD.Treasury.UI.Areas.Management.Controllers
{
    [Authorize(Roles = "Admin,Manager")]
    public class UserController : BaseController
    {
        public PartialViewResult Register()

        {
            //لیستی از مراکز هزینه
            ViewBag.Location = Utility.GetLocationLs();
            //لیستی از نقش ها ی سیتسم 
            ViewBag.Roles = Utility.GetRolesLS();
            return PartialView();
        }
        [HttpPost]
        public ActionResult Register(DataModel.UserInfo User)
        {
            try
            {
                //if (User.LocationID != 0 && User.LocationByCostCenterID != null)
                //if (User.LocationID != 0)
                //{
                    //string[] rolelist = RoleIDs.Split(',');
                    User.CreatedTime = DateTime.Now;
                    User.UserPassword = CSD.Common.Encryption.Hash.HashPassword(User.UserPassword);
                    User.DefualtLanguage = "1";
                    if (ConfigurationManager.AppSettings["bilingual"] != "True")
                    {
                        User.User_FullName2 = "-";
                    }
                    var IP = Utility.GetLocalIPAddress();
                    if (IP != "")
                    {
                        User.LastIPAddress = IP;
                    }
                    string message = "insert_success";
                    var db = new TreasuryEntities();
                    db.UserInfoes.Add(User);
                    try
                    {
                        db.SaveChanges();
                    }


                    catch (DbEntityValidationException dbEx)
                    {
                        var s = "";
                        foreach (var validationErrors in dbEx.EntityValidationErrors)
                        {
                            foreach (var validationError in validationErrors.ValidationErrors)
                            {
                                s = s + "Property: {0} Error: {1}" + validationError.PropertyName + validationError.ErrorMessage;
                            }
                        }
                    }
                        if (Request.Form["RoleIds"] != null)
                        {
                            var RoleIdLs = Request.Form["RoleIds"].Split(',');
                            foreach (var item in RoleIdLs)
                            {

                                DataModel.UserInRole userrole = new DataModel.UserInRole();
                                userrole.RoleId = int.Parse(item);
                                userrole.UserID = User.UserID;
                                db.UserInRoles.Add(userrole);
                                db.SaveChanges();
                            }
                        }
                        if (User.HasPermissiveSignature)
                        {
                            Icls_PermissiveSignaturesClient ps = new Icls_PermissiveSignaturesClient();
                            Service.PermissiveSignaturesService.PermissiveSignature psObj = new Service.PermissiveSignaturesService.PermissiveSignature();
                            psObj.UserID = User.UserID;
                            ps.Insert(psObj, out message);
                            db.SaveChanges();
                        }

                        return JsonMessage(true, message);
                //    }
                //else
                //    return JsonMessage(false, "insert_error");

                }
            catch (DbEntityValidationException dbEx)
            {
                var s = "";
                foreach (var validationErrors in dbEx.EntityValidationErrors)
                {
                    foreach (var validationError in validationErrors.ValidationErrors)
                    {
                        s = s + "Property: {0} Error: {1}" + validationError.PropertyName + validationError.ErrorMessage;
                    }
                }



                return PartialView();
            }
        }
        /// <summary>
        /// بر گرداندن مراکز هزینه با توجه به واحد سازمانی
        /// </summary>
        /// <param name="LocationID"></param>
        /// <returns></returns>
        public string GetCostCenter(int LocationID)
        {
            var res = new StringBuilder();
            var db = new TreasuryEntities();
            var list = db.LocationByCostCenters.Where(d => d.LocationId == LocationID);
            if (list.Count() != 0)
            {
                foreach (var i in list)
                {
                    res.Append("<option value='" + i.LocationByCostCenterID + "'>" + Utility.GetUniqueAccount(i.Unique_Account_No) + "</option>");
                }
            }
            return res.ToString();
        }
        #region UserList
        public ActionResult Index(SearchModelUser obj)
        {
            return View(obj);
        }
        public ActionResult List(SearchModelUser obj)
        {
            int pageNum = obj.PageIndex;
            int pageSize = 2;
            var db = new TreasuryEntities();
            var query = db.UserInfoes.ToList();
            int totalRows = query.Count();
            obj.TotalPageCount = (int)Math.Ceiling(totalRows / (double)pageSize);
            obj.CurrentPage = pageNum;
            obj.List = query;
            obj.CurrentPage = obj.PageIndex;
            obj.TotalItemCount = totalRows;

            return PartialView(obj);
        }
        #endregion
        #region Edit
        public ActionResult Edit(int id)
        {
            var db = new TreasuryEntities();
            //لیستی از مراکز هزینه
            ViewBag.Location = Utility.GetLocationLs();

            //لیستی از نقش های کاربر انتخاب شده
            var userInRole = db.UserInRoles.Where(d => d.UserID == id).Select(d => d.RoleId);
            var SelectedRoles = (from p in db.RolesInfoes
                                 where userInRole.Contains(p.RoleID)
                                 select p).ToList();
            var query = Utility.GetRolesLS();
            ViewBag.Roles = query.Select(q => new SelectListItem { Value = q.RoleID.ToString(), Text = q.RoleName, Selected = (SelectedRoles.Select(d => d.RoleID).Contains(q.RoleID) ? true : false) }).ToList();
            int totalRow;
            Icls_PermissiveSignaturesClient ps = new Icls_PermissiveSignaturesClient();
            if (ps.SelectBy_DynamicWhereClause(" UserID =" + id, 1, 100, "PermissiveSignatureID", "int", true, out totalRow).Any())
            {
                ViewBag.HasPermissiveSignature = true;
            }
            else
                ViewBag.HasPermissiveSignature = false;

            //غیر فعال کردن چک باکس
            //var PermissiveSignature = ps.SelectBy_DynamicWhereClause("UserID =" + id, 1, 100, "PermissiveSignatureID", "int", true, out totalRow).SingleOrDefault();
            //if (PermissiveSignature != null)
            //{
            //    Icls_PermissiveSignatureForEachBankAccountClient PermissiveSignatureForEachBankClient = new Icls_PermissiveSignatureForEachBankAccountClient();
            //    var PermissiveSignatureForEachBank = PermissiveSignatureForEachBankClient.SelectBy_DynamicWhereClause("PermissiveSignatureID=" + PermissiveSignature.PermissiveSignatureID, 1, 100, "PermissiveSignatureForEachBankAccountID", "int", false, out totalRow).Any();
            //    if (PermissiveSignatureForEachBank)
            //    {
            //        ViewBag.Inactive = true;
            //    }
            //}

            //ViewBag.Inactive = false;


            //List<RolesInfo> s1 = (from p in db.RolesInfoes
            //          where userInRole.Contains(p.RoleID)
            //          select p).ToList();
            //List<RolesInfo> s2 = Utility.GetRolesLS();
            //foreach (var item in s2)
            //{
            //       if(s1.Contains(item))
            //        {
            //            Response.Write("yes");
            //        }     
            //}

            return PartialView(db.UserInfoes.SingleOrDefault(d => d.UserID == id));
        }
        [HttpPost]
        public ActionResult Edit(DataModel.UserInfo user)
        {
            try
            {
                var db = new TreasuryEntities();
                var TempUser = db.UserInfoes.SingleOrDefault(d => d.UserID == user.UserID);
                TempUser.UserName = user.UserName;
                //TempUser.User_FullName = user.User_FullName;
                //TempUser.User_FullName2 = user.User_FullName2;
                //TempUser.LocationID = user.LocationID;
                //TempUser.LocationByCostCenterID = user.LocationByCostCenterID;
                TempUser.IsActive = user.IsActive;
                TempUser.IsAdmin = user.IsAdmin;
               
                if (!string.IsNullOrEmpty(user.UserPassword) && TempUser.UserPassword != user.UserPassword)
                {
                    TempUser.UserPassword = CSD.Common.Encryption.Hash.HashPassword(user.UserPassword);
                }
                TempUser.DefualtLanguage = "1";
                db.SaveChanges();
                //حذف نقش های موجود
                var userInRole = db.UserInRoles.Where(d => d.UserID == user.UserID);
                foreach (var item in userInRole)
                {
                    db.UserInRoles.Remove(item);

                }
                db.SaveChanges();
                //اضافه نقش ها برای کاربر
                string[] rolelist = null;
                if (Request.Form["RoleIds"] != null)
                {
                    rolelist = Request.Form["RoleIds"].Split(',');
                    foreach (var item in rolelist)
                    {
                        DataModel.UserInRole userrole = new DataModel.UserInRole();
                        userrole.RoleId = int.Parse(item);
                        userrole.UserID = user.UserID;
                        db.UserInRoles.Add(userrole);
                        db.SaveChanges();
                    }
                }


                Icls_PermissiveSignatureForEachBankAccountClient PermissiveSignatureForEachBankClient = new Icls_PermissiveSignatureForEachBankAccountClient();
                //حذف مجوز های امضا
                Icls_PermissiveSignaturesClient ps = new Icls_PermissiveSignaturesClient();
                int totalRow;
                string message;
                var PermissiveSignature = ps.SelectBy_DynamicWhereClause("UserID =" + user.UserID, 1, 100, "PermissiveSignatureID", "int", true, out totalRow).SingleOrDefault();
                if (PermissiveSignature != null)
                {
                    var PermissiveSignatureForEachBank = PermissiveSignatureForEachBankClient.SelectBy_DynamicWhereClause("PermissiveSignatureID=" + PermissiveSignature.PermissiveSignatureID, 1, 100, "PermissiveSignatureForEachBankAccountID", "int", false, out totalRow).Any();
                    if (!PermissiveSignatureForEachBank)
                    {
                        if (PermissiveSignature != null)
                        {
                            ps.Delete(PermissiveSignature.PermissiveSignatureID, out message);
                            db.SaveChanges();
                        }

                        //افزودن مجدد مجوز امضا          
                        Service.PermissiveSignaturesService.PermissiveSignature psObj = new Service.PermissiveSignaturesService.PermissiveSignature();
                        psObj.UserID = user.UserID;
                        ps.Insert(psObj, out message);
                        db.SaveChanges();
                    }            
                }
                if (user.HasPermissiveSignature)
                {                    
                    Service.PermissiveSignaturesService.PermissiveSignature psObj = new Service.PermissiveSignaturesService.PermissiveSignature();
                    psObj.UserID = user.UserID;
                    ps.Insert(psObj, out message);
                    db.SaveChanges();
                }

                return JsonMessage(true, "update_success");
            }
            catch (Exception ex)
            {
                return JsonMessage(false, "update_error");
            }
        }
        #endregion

        #region Delete
        [HttpPost]
        public ActionResult Delete(int id)
        {
            try
            {
                var db = new TreasuryEntities();

                //حذف رل ها 
                var userInRoleLs = db.UserInRoles.Where(d => d.UserID == id);
                if (userInRoleLs != null)
                {
                    foreach (var item in userInRoleLs)
                    {
                        db.UserInRoles.Remove(item);
                    }
                }

                //حذف مجوز های امضا
                Icls_PermissiveSignaturesClient ps = new Icls_PermissiveSignaturesClient();
                int totalRow;
                string message;
                var PermissiveSignature = ps.SelectBy_DynamicWhereClause(" UserID =" + id, 1, 100, "PermissiveSignatureID", "int", true, out totalRow).SingleOrDefault();
                if (PermissiveSignature != null)
                {
                    ps.Delete(PermissiveSignature.PermissiveSignatureID, out message);
                }

                //حذف کاربر
                var User = db.UserInfoes.SingleOrDefault(d => d.UserID == id);
                db.UserInfoes.Remove(User);
                db.SaveChanges();
                return JsonMessage(true, "delete_success");
            }
            catch
            {
                return JsonMessage(false, "delete_error");
            }
        }
        #endregion

        #region InActive
        public ActionResult InActive(int UserId)
        {
            try
            {
                var db = new TreasuryEntities();
                var TempUser = db.UserInfoes.SingleOrDefault(d => d.UserID == UserId);
                if (TempUser.IsActive)
                    TempUser.IsActive = false;
                else
                    TempUser.IsActive = true;
                db.SaveChanges();
                return JsonMessage(true, "update_success");
            }
            catch
            {
                return JsonMessage(false, "update_error");
            }
        }
        #endregion

        /// <summary>
        /// چک نمودن برای این که عنوان تکراری ثبت نگردد
        /// </summary>
        /// <param name="BehalfGroupDescription"></param>
        /// <returns></returns>
        #region CheckTitle
        public JsonResult CheckTitle(string UserName, string InitialUserName)
        {
            Icls_SPsClient db = new Icls_SPsClient();
            if (UserName == InitialUserName)
            {
                return Json(true, JsonRequestBehavior.AllowGet);
            }
            if (db.CheckExistValueInTable("UserInfo", "UserName", UserName))
            {
                return Json(false, JsonRequestBehavior.AllowGet);
            }

            return Json(true, JsonRequestBehavior.AllowGet);
        }
        #endregion
    }
}