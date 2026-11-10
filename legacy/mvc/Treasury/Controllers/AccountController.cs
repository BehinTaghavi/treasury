using System;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using CSD.Treasury.UI.Models;
using System.Web.Security;
using System.Web.Caching;
using CSD.Treasury.DataModel;
using System.Web.Script.Serialization;
using System.Collections.Generic;
using CSD.Treasury.Service.PermissiveSignatureForEachBankAccountService;
using CSD.Treasury.Service.PermissiveSignaturesService;

namespace CSD.Treasury.UI.Controllers
{

    public class AccountController : BaseController
    {
        private ApplicationSignInManager _signInManager;
        private ApplicationUserManager _userManager;

        public AccountController()
        {
        }

        public AccountController(ApplicationUserManager userManager, ApplicationSignInManager signInManager)
        {
            UserManager = userManager;
            SignInManager = signInManager;
        }

        public ApplicationSignInManager SignInManager
        {
            get
            {
                return _signInManager ?? HttpContext.GetOwinContext().Get<ApplicationSignInManager>();
            }
            private set
            {
                _signInManager = value;
            }
        }

        public ApplicationUserManager UserManager
        {
            get
            {
                return _userManager ?? HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>();
            }
            private set
            {
                _userManager = value;
            }
        }
        public ActionResult Login(string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }
        [HttpPost]
        public ActionResult Login(LoginViewModel model, string returnUrl)
        {
            if (ModelState.IsValid)
            {
                if (Membership.ValidateUser(model.UserName, model.Password))
                {
                    FormsAuthentication.SetAuthCookie(model.UserName, model.RememberMe);
                    FormsAuthentication.SetAuthCookie(model.UserName, false);

                    var db = new TreasuryEntities();
                    var user = db.UserInfoes.Where(u => u.UserName == model.UserName && u.IsActive).First();
                    var userRole = db.UserInRoles.Where(d => d.UserID == user.UserID).Select(d => d.RoleId);
                    var Roles = db.RolesInfoes.Where(q => userRole.Contains(q.RoleID)).Select(d => d.RoleName1).ToList();

                    CustomPrincipalSerializeModel serializeModel = new CustomPrincipalSerializeModel();
                    serializeModel.Id = user.UserID;
                    serializeModel.UserFullName = user.User_FullName;
                    serializeModel.LocationName = user.Location.LocationName;
                    serializeModel.Roles = Roles;
                    serializeModel.BranchId = user.Location.cmpBranchID;

                    serializeModel.BranchName = user.Location.cmpBranch.cmpBranchName;

                    JavaScriptSerializer serializer = new JavaScriptSerializer();

                    string userData = serializer.Serialize(serializeModel);
                    FormsAuthenticationTicket authTicket = new FormsAuthenticationTicket(
                                1,
                                model.UserName,
                                DateTime.Now,
                                DateTime.Now.AddMinutes(1500),
                                false,
                                userData);


                    string encTicket = FormsAuthentication.Encrypt(authTicket);
                    HttpCookie faCookie = new HttpCookie(FormsAuthentication.FormsCookieName, encTicket);
                    Response.Cookies.Add(faCookie);
                    var _userRole = Utility.GetUserRolesLS(user);
                    //Session["FiscalYear"]=model.FiscalYear;
                    if (Url.IsLocalUrl(returnUrl) && returnUrl.Length > 1 && returnUrl.StartsWith("/")
                        && !returnUrl.StartsWith("//") && !returnUrl.StartsWith("/\\"))
                    {
                        return Redirect(returnUrl);
                    }
                    else
                    {
                        if (_userRole.Contains("Admin")||_userRole.Contains("Manager"))
                        {
                            return RedirectToAction("Index", "DashBoard", new { area = "Management" });
                        }
                        if (_userRole.Contains("RequestPayment"))
                        {
                            return RedirectToAction("Index", "Request", new { area = "Payment" });
                        }
                        if (_userRole.Contains("PettyCash"))
                        {
                            return RedirectToAction("_Index", "PettyCash", new { area = "Payment" });
                        }
                    }
                }
                else
                {
                    ModelState.AddModelError("", "کلمه عبور یا نام کاربری صحیح نیست.");
                }
            }
            return View(model);
        }

        [Authorize]
        public ActionResult ShowUser(int userId)
        {
            var db = new TreasuryEntities();
            var User = db.UserInfoes.Where(d => d.UserID == userId).SingleOrDefault();
            return View(User);
        }

        #region EditUser
        [Authorize]
        public ActionResult EditProfile(int id)
        {
            var db = new TreasuryEntities();
            var User = db.UserInfoes.SingleOrDefault(d => d.UserID == id);
            //User.UserPassword = CSD.Common.Encryption.MD5Encryption.DecryptString(User.UserPassword, "");
            return PartialView(User);
        }

        [Authorize]
        [HttpPost]
        public ActionResult Edit(DataModel.UserInfo user)
        {
            try
            {
                var db = new TreasuryEntities();
                var TempUser = db.UserInfoes.SingleOrDefault(d => d.UserID == user.UserID);
                var _tmPass = TempUser.UserPassword;
                var _newPAss = CSD.Common.Encryption.Hash.HashPassword(user.UserPassword);
             
                TempUser.User_FullName = user.User_FullName;
                TempUser.User_FullName2 = user.User_FullName2;
                //TempUser.LocationID = user.LocationID;
                //TempUser.LocationByCostCenterID = user.LocationByCostCenterID;
                //TempUser.IsActive = user.IsActive;
                //TempUser.IsAdmin = user.IsAdmin;
                if (!string.IsNullOrEmpty(user.UserPassword) && user.UserPassword!=TempUser.UserPassword)
                {
                    TempUser.UserPassword = _newPAss;
                }
                TempUser.DefualtLanguage = "1";
                db.SaveChanges();
                //حذف نقش های موجود
                //var userInRole = db.UserInRoles.Where(d => d.UserID == user.UserID);
                //foreach (var item in userInRole)
                //{
                //    db.UserInRoles.Remove(item);

                //}
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
                if (_tmPass != _newPAss)
                {
                    return RedirectToAction("LogOff");
                }
                else
                {
                    return RedirectToAction("ShowUser", new { userId = user.UserID });
                }
            }
            catch (Exception e)
            {
                return RedirectToAction("ShowUser", new { userId = user.UserID });
            }
        }
        #endregion


        public ActionResult LogOff()
        {
            FormsAuthentication.SignOut();
            return RedirectToAction("Login", "Account", new { area = "" });
        }

        #region Helpers
        private const string XsrfKey = "XsrfId";

        private IAuthenticationManager AuthenticationManager
        {
            get
            {
                return HttpContext.GetOwinContext().Authentication;
            }
        }


        private ActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction("Index", "Home");
        }

        internal class ChallengeResult : HttpUnauthorizedResult
        {
            public ChallengeResult(string provider, string redirectUri)
                : this(provider, redirectUri, null)
            {
            }

            public ChallengeResult(string provider, string redirectUri, string userId)
            {
                LoginProvider = provider;
                RedirectUri = redirectUri;
                UserId = userId;
            }

            public string LoginProvider { get; set; }
            public string RedirectUri { get; set; }
            public string UserId { get; set; }

            public override void ExecuteResult(ControllerContext context)
            {
                var properties = new AuthenticationProperties { RedirectUri = RedirectUri };
                if (UserId != null)
                {
                    properties.Dictionary[XsrfKey] = UserId;
                }
                context.HttpContext.GetOwinContext().Authentication.Challenge(properties, LoginProvider);
            }
        }
        #endregion
    }
}