using CSD.Treasury.UI.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using CSD.Treasury.Service.Vocher_RowService;
using CSD.Treasury.Service.Voucher_Row_RecieptService;
using CSD.Treasury.Service.ProceduresDB;
using CSD.Treasury.UI.Models;
using CSD.Treasury.Service.Receipt_Detail_PlanStateService;
using CSD.Treasury.Service.Request_PaymentService;
using CSD.Treasury.DataModel;
using Voucher_Row = CSD.Treasury.Service.Vocher_RowService.Voucher_Row;
using System.Configuration;

namespace CSD.Treasury.UI.Areas.Management.Controllers
{
    [Authorize(Roles = "Admin,Manager")]
    public class Vocher_RowController : BaseController
    {
        #region Payment
        public ActionResult ShowVocherList(bool IsPayment, int requestId, bool IsReturend, int Receipt_Pay_TypeID, int BehalfGroupID, int StatusID, int ReferenceId)
        {
            ViewBag.IsPayment = IsPayment;
            ViewBag.requestId = requestId;
            ViewBag.IsReturend = IsReturend;
            ViewBag.Receipt_Pay_TypeID = Receipt_Pay_TypeID;
            ViewBag.BehalfGroupID = BehalfGroupID;
            ViewBag.StatusID = StatusID;
            ViewBag.ReferenceId = ReferenceId;

            return PartialView();
        }
        public ActionResult ShowVocherListFrame(bool IsPayment, int requestId, bool IsReturend, int Receipt_Pay_TypeID, int BehalfGroupID, int StatusID, int ReferenceId)
        {
            //Icls_Request_PaymentClient RequestPaymentClient = new Icls_Request_PaymentClient();
            var db = new TreasuryEntities();
            //var Request=RequestPaymentClient.SelectRow(requestId).FirstOrDefault();
            var Request = db.Request_Payment.SingleOrDefault(d => d.RequestID == requestId);
            //var Voucher = db.Voucher_Row.FirstOrDefault(d => d.ReferenceId == requestId);
            ViewBag.IsPayment = IsPayment;
            ViewBag.requestId = requestId;
            ViewBag.IsReturend = IsReturend;
            ViewBag.Receipt_Pay_TypeID = Receipt_Pay_TypeID;
            ViewBag.BehalfGroupID = BehalfGroupID;
            ViewBag.StatusID = StatusID;
            ViewBag.ReferenceId = ReferenceId;
            ViewBag.Date_Field = Request.DueDate;
            ViewBag.Value_Field = Request.ChequeNo;
            ViewBag.VoucherNo = 0;
            //if (Voucher!= null)
            //{
            //    ViewBag.VoucherNo = Voucher.AccVoucherNo;
            //}
            System.Globalization.PersianCalendar pc = new System.Globalization.PersianCalendar();
            ViewBag.maxdate = string.Format("{0:00}/{1:00}/{2:0000}", pc.GetDayOfMonth(DateTime.Now), pc.GetMonth(DateTime.Now), pc.GetYear(DateTime.Now));

            return View();
        }

        public JsonResult VocherList(bool IsPayment, Int32 requestId, bool IsReturend ,int StatusID)
        {
            //Icls_SPsClient spsClient = new Icls_SPsClient();
           //var ls = spsClient.Sp_CREATE_VOUCHER_PAYMENTS(IsPayment, requestId, User.Id, IsReturend, true);
            //Voucher_RowClient VocherClient = new Voucher_RowClient();
            //var ls =       VocherClient.SelectBy_DynamicWhereClause("ReferenceId=" + requestId, 1, 100, "RowId", "dec", false, out TotalRow);
            //var ls = VocherClient.SelectAll(1, 10, "RowId", "dec", true,out TotalRow);
            var db = new TreasuryEntities();
            var ls = db.Show_Voucher_Row(requestId).Where(d=>d.StatusID == StatusID).ToList();
            var AccVoucherNo = 0;
            //var DontSendVoucherDaily = ConfigurationManager.AppSettings["DontSendVoucherDaily"];

           
            foreach (var item in ls)
            {
                item.Date_Field = Utility.DateStructure(item.Date_Field);
                //if (!string.IsNullOrEmpty(item.Unique_Account_No))
                //{
                //    item.Unique_Account_No = item.Unique_Account_No.Trim();
                //    item.Unique_Account_No_Title = Utility.GetUniqueAccount(item.Unique_Account_No);
                //}
                //if (!string.IsNullOrEmpty(item.Unique_Account_No_two))
                //{
                //    item.Unique_Account_No_two_Title = Utility.GetUniqueAccount(item.Unique_Account_No_two);
                //}
                //if(!string.IsNullOrEmpty(item.Account_No))
                //{
                //    item.Account_No = item.Account_No.Trim();
                //}
            }
            var DontSendVoucherDaily = true;
            if (ls.Count() != 0 && Utility.GetPostVoucherType(ls.FirstOrDefault().StateDate) == 2)
            {
                //ارسال اسناد دراین حالت به صورت روزانه است
                DontSendVoucherDaily = false;
            }

            var accountsLS = Utility.v_accountsLS();
            if(ls.Count() != 0 && ls.FirstOrDefault().AccVoucherNo !=null)
            {
                 AccVoucherNo = ls.FirstOrDefault().AccVoucherNo.Value;
            }
            

            return Json(new { VocherLs = ls, AccountNumLs = accountsLS, AccVoucherNo = AccVoucherNo, DontSendVoucherDaily = DontSendVoucherDaily }, JsonRequestBehavior.AllowGet);

        }

        public ActionResult Save(List<CSD.Treasury.DataModel.Voucher_Row> VocherList)
        {
            var message ="";
            try
            {
                var db = new TreasuryEntities();
                
                Voucher_RowClient VocherClient = new Voucher_RowClient();
                
                foreach (var item in VocherList)
                {
                    var statusId = VocherList.FirstOrDefault().StatusID;
                    var List = db.Voucher_Row.Where(d => d.ReferenceId == item.ReferenceId && d.StatusID == statusId).ToList();
                    var deletedList= List.Where(d=> !VocherList.Select(i=>i.RowId).Contains(d.RowId)).ToList();
                    db.Voucher_Row.RemoveRange(deletedList);
                    var hasUniqueAccount = Utility.HasUniqueAccount(item.Account_No);
                    if (item.RowId == 0)
                    {
                        //if (!string.IsNullOrEmpty(item.Date_Field))
                        //{
                        //    item.Date_Field = item.Date_Field.Replace("/", string.Empty);
                        //}
                        //Date_Field
                        if (hasUniqueAccount[2] && item.Date_Field!=null)
                        {
                            item.Date_Field = item.Date_Field.Replace("/", string.Empty);
                        }
                        else
                        {
                            item.Date_Field = null;
                        }
                        //Value_Field
                        if (hasUniqueAccount[3] && item.Value_Field!=null)
                        {
                            item.Value_Field = item.Value_Field;
                        }
                        else
                        {
                            item.Value_Field = null;
                        }
                        //درصورتی که معین تفصیل اول داشته باشد
                        if (hasUniqueAccount[0])
                        {
                            item.Unique_Account_No = item.Unique_Account_No;
                        }
                        else
                        {
                            item.Unique_Account_No = null;
                        }
                        //درصورتی که معین تفصیل دوم داشته باشد
                        if (hasUniqueAccount[1])
                        {
                            item.Unique_Account_No_two = item.Unique_Account_No_two;
                        }
                        else
                        {
                            item.Unique_Account_No_two = null;
                        }
                        db.Voucher_Row.Add(item);
                       
                        //return JsonMessage(result, Message);
                    }
                    else
                    {
                        var selectedVoucher = db.Voucher_Row.SingleOrDefault(d => d.RowId == item.RowId);
                        //Date_Field
                        if (hasUniqueAccount[2] && item.Date_Field!=null)
                        {
                            selectedVoucher.Date_Field = item.Date_Field.Replace("/", string.Empty);
                        }
                        else
                        {
                            selectedVoucher.Date_Field = null;
                        }
                        //Value_Field
                        if (hasUniqueAccount[3] && item.Value_Field!=null)
                        {
                            selectedVoucher.Value_Field = item.Value_Field;
                        }
                        else
                        {
                            selectedVoucher.Value_Field = null;
                        }
                        
                        if (hasUniqueAccount[0])
                        {
                            selectedVoucher.Unique_Account_No = item.Unique_Account_No;
                        }
                        else
                        {
                            selectedVoucher.Unique_Account_No = null;
                        }
                        if (hasUniqueAccount[1])
                        {
                            selectedVoucher.Unique_Account_No_two = item.Unique_Account_No_two;
                        }
                        else
                        {
                            selectedVoucher.Unique_Account_No_two = null;
                        }
                        selectedVoucher.Debit = item.Debit;
                        selectedVoucher.Credit = item.Credit;
                        selectedVoucher.Account_No = item.Account_No;
                    }
                 
                }
                db.SaveChanges();
                return JsonMessage(true, "ذخیره اسناد با موفقیت انجام شد", null, false);
            }
            catch (Exception ex)
            {
                message = "با خطا مواجه شد";
                if (ex.InnerException != null)
                {
                    if (ex.InnerException.InnerException.Message.Contains("Cannot insert duplicate key"))
                    {
                        message = "تفصیل  بانک تکراری است";
                    }
                    else if (ex.InnerException.InnerException.Message.Contains("\r\n"))
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

        public JsonResult Delete(List<Voucher_Row> VocherList) {
            try
            {
                foreach (var item in VocherList)
                {
                    try
                    {
                        //درصورتی که شماره صند حصابداری نداشته باشد و قابل ویرایش باشد اجازه حذف نمودن هست 
                        string Message;
                        Voucher_RowClient VocherClient = new Voucher_RowClient();
                        var Vocher = VocherClient.SelectRow((int)item.RowId).SingleOrDefault();
                        if (Vocher.AccVoucherNo == null && Vocher.Editable)
                        {
                            var result = VocherClient.Delete((int)item.RowId, out Message);
                        }
                    }
                    catch (Exception)
                    {
                        continue;
                    }
                  
                }
                return Json(true, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return Json(true, JsonRequestBehavior.AllowGet);
            }
        }

        public JsonResult DeleteReciept(List<Service.Voucher_Row_RecieptService.Voucher_Row_Reciept> VocherList)
        {
            try
            {
                foreach (var item in VocherList)
                {
                    try
                    {
                        //درصورتی که شماره صند حصابداری نداشته باشد و قابل ویرایش باشد اجازه حذف نمودن هست 
                        string Message;
                        Voucher_Row_RecieptClient VocherClient = new Voucher_Row_RecieptClient();
                        var Vocher = VocherClient.SelectRow((int)item.Voucher_Row_RecieptID).SingleOrDefault();
                        if (Vocher.AccVoucherNo == null && Vocher.Editable)
                        {
                            var result = VocherClient.Delete((int)item.Voucher_Row_RecieptID, out Message);
                        }
                    }
                    catch (Exception)
                    {
                        continue;
                    }

                }
                return Json(true, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return Json(true, JsonRequestBehavior.AllowGet);
            }
        }

        //public ActionResult Delete(int RowId)
        //{
        //    try
        //    {
        //        //درصورتی که شماره صند حصابداری نداشته باشد و قابل ویرایش باشد اجازه حذف نمودن هست 
        //        string Message;
        //        Voucher_RowClient VocherClient = new Voucher_RowClient();
        //        var Vocher = VocherClient.SelectRow(RowId).SingleOrDefault();
        //        if (Vocher.AccVoucherNo == null && Vocher.Editable)
        //        {
        //            var result = VocherClient.Delete(RowId, out Message);
        //            return JsonMessage(result, Message);
        //        }
        //        return JsonMessage(false, "Error");
        //    }
        //    catch
        //    {
        //        return PartialView();
        //    }
        //}
        #endregion

        #region Receipt

        public JsonResult VocherListReceipt(int ReceiptDetailId, int StateId, int PlaningTypeID)
        {
            //int TotalRow;
            //Icls_SPsClient spsClient = new Icls_SPsClient();
            //Icls_Receipt_Detail_PlanStateClient ReceiptPlanClient = new Icls_Receipt_Detail_PlanStateClient();
            TreasuryEntities db = new TreasuryEntities();
            var ls = db.Show_Voucher_Row_Reciept(ReceiptDetailId, StateId).ToList();
            var AccVoucherNo = 0;
            //var DontSendVoucherDaily =  ConfigurationManager.AppSettings["DontSendVoucherDaily"];
            var DontSendVoucherDaily = true;

            if (Utility.GetPostVoucherType(ls.FirstOrDefault().StateDate) == 2)
            {
                //ارسال اسناد دراین حالت به صورت روزانه است
                DontSendVoucherDaily = false;
                
            }
            //foreach (var item in ls)
            //{

            //item.Date_Field = Utility.DateStructure(item.Date_Field);
            //if(item.Unique_Account_No!=null)
            //{
            //    item.Unique_Account_No = item.Unique_Account_No.Trim();
            //}
            //item.Account_No = item.Account_No.Trim();

            //if (item.Unique_Account_No != null)
            //{
            //    item.Unique_Account_No_Title = Utility.GetUniqueAccount(item.Unique_Account_No);
            //}
            //if (item.Unique_Account_No_two != null)
            //{
            //    item.Unique_Account_No_two_Title = Utility.GetUniqueAccount(item.Unique_Account_No_two);
            //}
            //}
            var accountsLS = Utility.v_accountsLS();
            if(ls.Count() != 0 && ls.FirstOrDefault().AccVoucherNo != null)
            {
                 AccVoucherNo = ls.FirstOrDefault().AccVoucherNo.Value;
            }
            
                      
            return Json(new { VocherLs = ls, AccountNumLs = accountsLS, AccVoucherNo = AccVoucherNo, DontSendVoucherDaily = DontSendVoucherDaily }, JsonRequestBehavior.AllowGet);

        }


        public ActionResult ShowVocherListReceipt(int ReceiptDetailId, int StateId,int PlaningTypeID)
        {
            ViewBag.ReceiptDetailId = ReceiptDetailId;         
            ViewBag.StateId = StateId;
            ViewBag.PlaningTypeID = PlaningTypeID;
            return PartialView();
        }
        public ActionResult ShowVocherListFrameReceipt(int ReceiptDetailId, int StateId,int PlaningTypeID)
        {
            //int totalrow;
            //Icls_Receipt_Detail_PlanStateClient ReceiptDetailsplanstateClient = new Icls_Receipt_Detail_PlanStateClient();
            //Icls_Receipt_DetailClient ReceiptDetailsClient = new Icls_Receipt_DetailClient();
            //var receiptPlan = ReceiptDetailsplanstateClient.SelectBy_DynamicWhereClause("ReceiptDetailId =" + ReceiptDetailId + " and StateId=" + StateId + " and PlaningTypeID=" + PlaningTypeID, 1, 10, "ReceiptDetailId", "dec", true, out totalrow).FirstOrDefault();
            //var receiptDetails = ReceiptDetailsClient.SelectRow(ReceiptDetailId).FirstOrDefault();
            //var db = new TreasuryEntities();
            //var receiptDetails = db.Receipt_Detail.Find(ReceiptDetailId);
            //var voucherNo = Utility.HasVoucher(ReceiptDetailId ,StateId);
            //ViewBag.voucherNumber = voucherNo;
            //ViewBag.StateDate = receiptPlan.StateDate;
            //ViewBag.Value_Field = receiptDetails.Cheque_Number;
            ViewBag.ReceiptDetailId = ReceiptDetailId;

            ViewBag.StateId = StateId;
            ViewBag.PlaningTypeID = PlaningTypeID;
            System.Globalization.PersianCalendar pc = new System.Globalization.PersianCalendar();
            ViewBag.maxdate = string.Format("{0:00}/{1:00}/{2:0000}", pc.GetDayOfMonth(DateTime.Now), pc.GetMonth(DateTime.Now), pc.GetYear(DateTime.Now));
            return View();
        }
        public ActionResult SaveReceipt(List<DataModel.Voucher_Row_Reciept> VocherList)
        {
            var message = "";
            try
            {
               
                //string Message;
                //int totalrow;
                //Icls_SPsClient sp = new Icls_SPsClient();
                //sp.SendVoucherAgain
                //Voucher_Row_RecieptClient VocherClient = new Voucher_Row_RecieptClient();
                var db =new TreasuryEntities();
                
                foreach (var item in VocherList)
                {
                    var StateDate = VocherList.Where(d => d.Voucher_Row_RecieptID != 0).FirstOrDefault().StateDate;
                    //if (!string.IsNullOrEmpty(item.Date_Field))
                    //{
                    //    item.Date_Field = item.Date_Field.Replace("/", string.Empty);
                    //}
                    var hasUniqueAccount = Utility.HasUniqueAccount(item.Account_No);
                    //درصورتی که معین تفصیل اول داشته باشد
                    
                    if (item.Voucher_Row_RecieptID == 0)
                    {
                        if (hasUniqueAccount[0])
                        {
                            item.Unique_Account_No = item.Unique_Account_No;
                        }
                        else
                        {
                            item.Unique_Account_No = null;
                        }
                        //درصورتی که معین تفصیل دوم داشته باشد
                        if (hasUniqueAccount[1])
                        {
                            item.Unique_Account_No_two = item.Unique_Account_No_two;
                        }
                        else
                        {
                            item.Unique_Account_No_two = null;
                        }
                        if (hasUniqueAccount[2] && item.Date_Field != null)
                        {
                            item.Date_Field = item.Date_Field.Replace("/", string.Empty);
                        }
                        else
                        {
                            item.Date_Field = null;
                        }
                        //Value_Field
                        if (hasUniqueAccount[3] && item.Value_Field != null)
                        {
                            item.Value_Field = item.Value_Field;
                        }
                        else
                        {
                            item.Value_Field = null;
                        }
                        item.StateDate = StateDate;
                        var result = db.Voucher_Row_Reciept.Add(item);
                        //var result = VocherClient.Insert(item, out Message);
                        //return JsonMessage(result, Message);
                    }
                    else
                    {
                        var selectedReceipt= db.Voucher_Row_Reciept.SingleOrDefault(d => d.Voucher_Row_RecieptID == item.Voucher_Row_RecieptID);
                        if (hasUniqueAccount[0])
                        {
                            selectedReceipt.Unique_Account_No = item.Unique_Account_No;
                        }
                        else
                        {
                            selectedReceipt.Unique_Account_No = null;
                        }
                        //درصورتی که معین تفصیل دوم داشته باشد
                        if (hasUniqueAccount[1])
                        {
                            selectedReceipt.Unique_Account_No_two = item.Unique_Account_No_two;
                        }
                        else
                        {
                            selectedReceipt.Unique_Account_No_two = null;
                        }
                        
                        //Date_Field
                        if (hasUniqueAccount[2] && item.Date_Field != null)
                        {
                            selectedReceipt.Date_Field = item.Date_Field.Replace("/", string.Empty);
                        }
                        else
                        {
                            selectedReceipt.Date_Field = null;
                        }
                        //Value_Field
                        if (hasUniqueAccount[3] && item.Value_Field != null)
                        {
                            selectedReceipt.Value_Field = item.Value_Field;
                        }
                        else
                        {
                            selectedReceipt.Value_Field = null;
                        }
                        selectedReceipt.Debit = item.Debit;
                        selectedReceipt.Credit = item.Credit;
                        selectedReceipt.Account_No = item.Account_No;
                  
                        //var result = db.Voucher_Row_Reciept.Update(item, out Message);
                        //return JsonMessage(result, Message);
                    }
                }

                db.SaveChanges();
                return JsonMessage(true, "ذخیره اسناد با موفقیت انجام شد", null, false); 
            }
            catch (Exception ex)
            {
                message = "با خطا مواجه شد";
                if (ex.InnerException != null)
                {
                    if (ex.InnerException.InnerException.Message.Contains("Cannot insert duplicate key"))
                    {
                        message = "تفصیل  بانک تکراری است";
                    }
                    else if (ex.InnerException.InnerException.Message.Contains("\r\n"))
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
       
       
        /// <summary>
        /// type=1 payment
        /// type=2 receipt
        /// </summary>
        /// <param name="requestIdOrStateId"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public ActionResult SendVoucherAgain(decimal RefId, int TypeId,bool IsReceipt)
        {
            try
            {
                var startDate ="";
                var endDate = "";
                var db = new TreasuryEntities();
                int? voucherNo;
                if (IsReceipt)
                {
                    startDate = db.Receipt_Detail_PlanState.FirstOrDefault(d => d.StateID == TypeId).StateDate;
                }
                else
                {
                    var request = db.Request_Payment.Find(RefId);
                    startDate = request.RequestDate;
                    
                    if(TypeId == 3)
                    {
                        var payment = db.Payments.SingleOrDefault(d=>d.RequestID == RefId);
                        startDate = payment.PaymentDate;
                    }
                }
                endDate = startDate;
                var ls = db.PostVoucher(startDate, endDate, RefId, TypeId, IsReceipt);
                if(IsReceipt)
                {
                    voucherNo = db.Voucher_Row_Reciept.Where(d=>d.ReceiptDetailId == RefId && d.StateId == TypeId).FirstOrDefault().AccVoucherNo;
                }
                else
                {
                    voucherNo = db.Voucher_Row.Where(d => d.ReferenceId == RefId && d.StatusID == TypeId).FirstOrDefault().AccVoucherNo;
                }
                if(ls.Count() == 0)
                {
                    //return Json("ارسال اسناد با موفقیت انجام شد", JsonRequestBehavior.AllowGet);
                    return JsonMessage(true, voucherNo + ";ارسال اسناد با موفقیت انجام شد", null, false);
                }
                else
                {
                    return JsonMessage(false, voucherNo + ";ارسال اسناد با خطا مواجه شد", null, false);
                }

                
            }
            catch
            {
                return JsonMessage(false, "خطا در ارسال اسناد", null, false);
            }
        }
        //public ActionResult sendVoucherReturn(long requestId)
        //{
        //    try
        //    {
        //        string Message;
        //        Icls_SPsClient sp = new Icls_SPsClient();
        //        sp.Send_Canelation_Vocher_ByRequestId(requestId);
        //        return Json("ارسال اسناد با موفقیت انجام شد", JsonRequestBehavior.AllowGet);
        //    }
        //    catch
        //    {
        //        return Json("خطا در ارسال اسناد", JsonRequestBehavior.AllowGet);
        //    }
        //}
        #endregion
        /// <summary>
        /// payment 
        /// Delete From VocherRow and call  Sp_CREATE_VOUCHER_PAYMENTS procedure
        /// </summary>
        /// <param name="ReferenceId"></param>
        /// <param name="IsPayment"></param>
        /// <param name="IsReturend"></param>
        /// <returns></returns>
        #region DeleteAnCallStoredProcedure
        [HttpPost]
        public ActionResult ResetPaymentVocher(int ReferenceId, bool IsPayment, bool IsReturend)
        {
            //int TotalRow;
            //string Mess1;
            var VocherClient = new Voucher_RowClient();
            var ProcedureClient = new Icls_SPsClient();
           // var VocherLs = VocherClient.SelectBy_DynamicWhereClause("ReferenceId=" + ReferenceId, 1, 100, "RowId", "dec", false, out TotalRow);
            //if (VocherLs.Count() != 0)
            //{
            //    foreach (var item in VocherLs)
            //    {
            //       // VocherClient.Delete(int.Parse(item.RowId.ToString()), out Mess1);
            //        item.AccVoucherNo = null;
            //        VocherClient.Update(item, out Mess1);
            //    }
            //}

            //var ls = ProcedureClient.Sp_CREATE_VOUCHER_PAYMENTS(IsPayment, ReferenceId, User.Id, IsReturend, true);
            return JsonMessage(true, "ResetSuccess");
        }

        [HttpPost]
        public ActionResult ResetReceiptVocher(int ReferenceId, int StateId)
        {
            //int TotalRow;
            //string Mess1;
            //var VocherClient = new Voucher_Row_RecieptClient();
            var ProcedureClient = new Icls_SPsClient();
            var db = new TreasuryEntities();
            //var VocherLs = VocherClient.SelectBy_DynamicWhereClause("ReceiptDetailId=" + ReferenceId, 1, 100, "Voucher_Row_RecieptID", "dec", false, out TotalRow);
            //if (VocherLs.Count() != 0)
            //{
            //    foreach (var item in VocherLs)
            //    {
            //        VocherClient.Delete(int.Parse(item.Voucher_Row_RecieptID.ToString()), out Mess1);
            //    }
            //}

            var ls = db.Show_Voucher_Row_Reciept(ReferenceId, StateId).ToList();
            return JsonMessage(true, "ResetSuccess");
        }
        #endregion
        /// <summary>
        /// payment 
        /// Delete From VocherRow and call   procedure
        /// </summary>
        /// <param name="ReferenceId"></param>
        /// <param name="IsPayment"></param>
        /// <param name="IsReturend"></param>  
        /// <returns></returns>
        #region DeleteAnCallStoredProcedure
        public ActionResult ResetreceiptVocher(int ReceiptDetailId, int StateId)
        {
            //int TotalRow;
            var db = new TreasuryEntities();
            //string Mess1;
            Voucher_Row_RecieptClient VocherClient = new Voucher_Row_RecieptClient();
           // var ProcedureClient = new Icls_SPsClient();
          //var VocherLs = VocherClient.SelectBy_DynamicWhereClause("ReferenceId=" + ReceiptDetailId, 1, 100, "RowId", "dec", false, out TotalRow);
            //if (VocherLs.Count() != 0)
            //{
            //    foreach (var item in VocherLs)
            //    {
            //        VocherClient.Delete(int.Parse(item.Voucher_Row_RecieptID.ToString()), out Mess1);
            //    }
            //}

            //var ls = ProcedureClient.Sp_Create_Voucher_reciept(ReceiptDetailId, StateId, User.Id,true,false);
            var ls = db.Show_Voucher_Row_Reciept(ReceiptDetailId, StateId).ToList();
            return JsonMessage(true, "ResetSuccess");
        }
        #endregion



        public ActionResult RegisterPaidVoucher()
        {
            var db = new TreasuryEntities();
            var VoucherDateBoundraies = db.GetPostVoucherDateBoundraies().FirstOrDefault();
            ViewBag.MinDate = VoucherDateBoundraies.MinDate;
            ViewBag.MaxDate = VoucherDateBoundraies.MaxDate;
            return View();
        }

        [HttpPost]
        public ActionResult ShowRegisteredPaidVoucher(PostRegisterPaidVoucher obj)
        {
            try
            {
                TreasuryEntities db = new TreasuryEntities();
               
                ViewBag.startDate = obj.startDate;
                ViewBag.endDate = obj.endDate;
                //var ProcedureClient = new Icls_SPsClient();
                if (!string.IsNullOrEmpty(obj.startDate) && !string.IsNullOrEmpty(obj.endDate))
                {
                    if (int.Parse(obj.startDate) <= int.Parse(obj.endDate))
                    {
                        obj.startDate = obj.startDate.Replace("/", string.Empty).Replace("-", string.Empty);
                        obj.endDate = obj.endDate.Replace("/", string.Empty).Replace("-", string.Empty);
                        //var ls = ProcedureClient.sp_SendVouchers_BetweenTwoDate(obj.startDate, obj.endDate, obj.rebuilding, true).ToList();
                        var ls = db.PostVoucher(obj.startDate, obj.endDate,null,null,null).ToList();
                        //var t  = db.PostVoucher(obj.startDate, obj.endDate);
                        return PartialView(ls);
                    }
                    else
                        return View("RegisterPaidVoucher");
                }
                return View("RegisterPaidVoucher");
            }
            catch (Exception ex)
            {
                ViewBag.Error = ex.InnerException.Message;
                return PartialView();
                throw;
            }
                
        }

        [HttpGet]
        public ActionResult ShowVoucherInfo(string startDate,string endDate, decimal? Credit, decimal? Debit,string Unique_Account_No, string Unique_Account_Two, string Account_No)
        {
            var db = new TreasuryEntities();
            var list = db.PostVoucherErrors(startDate, endDate, Account_No, Unique_Account_No, Unique_Account_Two, Debit, Credit).ToList();
            return PartialView(list);
        }
    }
}