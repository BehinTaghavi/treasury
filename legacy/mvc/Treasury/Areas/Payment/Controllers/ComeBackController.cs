using CSD.Treasury.Common.Enumeration;
using CSD.Treasury.Service.Request_Payment_StatusDetailsService;
using CSD.Treasury.Service.Request_PaymentService;
using CSD.Treasury.UI.Controllers;
using CSD.Treasury.UI.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace CSD.Treasury.UI.Areas.Payment.Controllers
{
    [Authorize(Roles = "Admin,Manager,RequestPayment")]
    public class ComeBackController : BaseController
    {
        /// <summary>
        /// امکان برگرداندن درخواست های وجه ای که تایید شده اند تا مرحله قبل چاپ چک و پرداخت
        /// بازگشت از وضعیت تایید درخواست
        /// بازگشت از وضعیت تایید برای پرداخت
        /// </summary>
        /// <returns></returns>
        #region ReturnRequest
        public ActionResult ReturnRequest(int RequestId)
        {
            int TotalRow;
            string message, mesRequest, messchild;
            Icls_Request_PaymentClient RequestClient = new Icls_Request_PaymentClient();
            Icls_Request_Payment_StatusDetailsClient statusClient = new Icls_Request_Payment_StatusDetailsClient();
            var Request = RequestClient.SelectRow(RequestId);
            var Childs = RequestClient.SelectBy_DynamicWhereClause("AggrigateOrDuplicationID =" + RequestId, 1, 1000, "RequestID", "dec", false, out TotalRow);
            var statusLs = statusClient.SelectBy_DynamicWhereClause("RequestID =" + RequestId, 1, 1000, "Request_Payment_StatusDetailsID", "int", false, out TotalRow).OrderBy(d => d.ActionTime).ToList();
            var last = statusLs.LastOrDefault();
            //بازگشت تایید درخواست
            if (Request.SingleOrDefault() != null && (statusLs.LastOrDefault().RequestStatusTypeID == (int)RequestStatusTypes.Confirmed|| statusLs.LastOrDefault().RequestStatusTypeID == (int)RequestStatusTypes.ConfirmPayment))
            {
                if (Childs.Count() == 0)
                {
                    //درصورتی که درخواست وجه تجمیع یا تکثیر نشده باشد
                    foreach (var item in statusLs)
                    {
                        if(item.RequestStatusTypeID!=(int) RequestStatusTypes.Unknown)
                        {
                            statusClient.Delete(item.Request_Payment_StatusDetailsID, out message);
                        }
                    }
                }
                else
                {
                    //درصورت تجمیع یا تکثیر 
                    foreach (var item in Childs)
                    {
                        var childstatus = statusClient.SelectBy_DynamicWhereClause("RequestID =" + item.RequestID, 1, 1000, "Request_Payment_StatusDetailsID", "int", false, out TotalRow);
                        foreach (var item1 in childstatus)
                        {
                            if (item1.RequestStatusTypeID != (int)RequestStatusTypes.Unknown)
                            {
                                statusClient.Delete(item1.Request_Payment_StatusDetailsID, out message);
                            }
                        }
                        var rChild = RequestClient.SelectRow(item.RequestID).SingleOrDefault();
                        rChild.AggrigateOrDuplicationID = null;
                        rChild.AggrigateOrDuplicationStatusID = null;
                        RequestClient.Update(rChild, out messchild);
                    }
                }
                var R = Request.SingleOrDefault();
                R.AggrigateOrDuplicationID = null;
                R.AggrigateOrDuplicationStatusID = null;
                RequestClient.Update(R, out mesRequest);
            }
            SearchModelRequestPayment obj = new SearchModelRequestPayment();
            return RedirectToAction("Index", "Request", new { Area = "Payment" });
        }
        #endregion
    }
}