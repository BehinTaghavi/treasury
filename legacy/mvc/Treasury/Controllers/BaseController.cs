using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Threading;
using CSD.Treasury.UI.Helper;
using CSD.Common.Utility;
using CSD.Treasury.DataModel;
using CSD.Treasury.UI.Models;

namespace CSD.Treasury.UI.Controllers
{
    public class BaseController : Controller
    {
        protected override IAsyncResult BeginExecuteCore(AsyncCallback callback, object state)
        {
            string cultureName = null;

            // Attempt to read the culture cookie from Request
            HttpCookie cultureCookie = Request.Cookies["_culture"];
            if (cultureCookie != null)
                cultureName = cultureCookie.Value;
            else
                //cultureName = Request.UserLanguages != null && Request.UserLanguages.Length > 0 ?
                //        Request.UserLanguages[0] :  // obtain it from HTTP header AcceptLanguages
                //        null;
                cultureName = "fa";
            // Validate culture name
            cultureName = CultureHelper.GetImplementedCulture(cultureName); // This is safe

            // Modify current thread's cultures            
            Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo(cultureName);
            Thread.CurrentThread.CurrentUICulture = Thread.CurrentThread.CurrentCulture;

            return base.BeginExecuteCore(callback, state);
        }

        [HttpPost]
        protected JsonResult JsonMessage(bool actionResult, string message, dynamic model = null, bool DefaultMess = true)
        {
            JsonMessage result = new JsonMessage();
            result.result = actionResult;
            if (DefaultMess)
            {
                result.message = ShowMessage(message);
            }
            else
            {
                result.message = message;
            }
            

            if (model != null)
            {
                result.Model = model;
            }
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// کاربر جاری با توجه به یوزر نیم
        /// </summary>
        /// <returns></returns>
        #region GetUser
        //protected UserInfo GetcurrentUser()
        //{
        //    if (Request.IsAuthenticated)
        //    {
        //        var curUser = GetUser(this.User.Identity.Name);
        //        return curUser;
        //    }
        //    return null;
        //}
        protected UserInfo GetUser(string userName)
        {
            var obj = (UserInfo)GetObjectFromHttpContext(userName);
            if (obj == null)
            {
                var dc = new TreasuryEntities();
                obj = dc.UserInfoes.SingleOrDefault(u => u.UserName == userName & u.IsActive);
                SetObjectToHttpContext(userName, obj);
            }
            return obj;
        }

        #endregion
        /// <summary>
        /// نمایش پیام ها برای کاربر با توجه به پیام هایی که از سرویس دریافت شده است
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        #region ShowMessage
        public string ShowMessage(string message)
        {
            if (message.Contains(';'))
            {
                message = message.Split(';')[0];
            }
            switch (message)
            {
                case "insert_success":
                    return Resourse.Resources.CreateSuccessfully;
                case "insert_success_Confirmed":
                    return Resourse.Resources.Insert_success_Confirmed;
                case "insert_success_ConfirmPayment":
                    return Resourse.Resources.insert_success_ConfirmPayment;
                case "insert_success_Cancellation":
                    return Resourse.Resources.insert_success_Cancellation;
                case "insert_success_CancelCheque":
                    return Resourse.Resources.insert_success_Cancellation;
                case "insert_success_Paid":
                    return Resourse.Resources.insert_success_Paid;
                case "insert_success_Delivered":
                    return Resourse.Resources.insert_success_Delivered;
                case "insert_error":
                    return Resourse.Resources.Error;
                case "update_success":
                    return Resourse.Resources.EditSuccessfully;
                case "update_error":
                    return Resourse.Resources.Error;
                case "delete_success":
                    return Resourse.Resources.DeleteSuccessfully;
                case "delete_error":
                    return Resourse.Resources.Error;
                case "delete_error_For_Relations":
                    return Resourse.Resources.RelationsError;
                case "Error_HasRelation":
                    return Resourse.Resources.RelationsError;
                case "ResetSuccess":
                    return Resourse.Resources.ResetSuccess;
                default:
                    return Resourse.Resources.Anerroroccurred;

            }

        }
        #endregion
        #region base

        protected object GetObjectFromHttpContext(string key)
        {
            if (HttpContext.Items[key] != null)
                return HttpContext.Items[key];
            return null;
        }

        protected void SetObjectToHttpContext<T>(string key, T obj)
        {
            HttpContext.Items.Remove(key);
            HttpContext.Items.Add(key, obj);
        }

        #endregion
        public virtual new CustomPrincipal User
        {
            get { return HttpContext.User as CustomPrincipal; }
        }

    }
}