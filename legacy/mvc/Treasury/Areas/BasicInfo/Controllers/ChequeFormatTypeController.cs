using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using CSD.Treasury.Service.ChequeFormatTypesService;
using CSD.Treasury.Service.ChequesService;
using CSD.Treasury.UI.Controllers;
using CSD.Treasury.UI.Models;
using CSD.Treasury.DataModel;
using System.IO;

namespace CSD.Treasury.UI.Areas.BasicInfo.Controllers
{
    [Authorize(Roles = "Admin,Manager,BasicInfo,ReportInApp")]
    public class ChequeFormatTypeController : BaseController
    {
        // GET: BasicInfo/ChequeFor
        public ActionResult Index()
        {
            return View();
        }
        public PartialViewResult List()
        {
            int pageNum = 1;
            int pageSize = 1000;
            string orderByFieldName = "ChequeFormatID";
            bool isAscendingOrder = true;
            string OrderByfeildtype = "int";
            int totalRows;

            Icls_ChequeFormatTypesClient obj = new Icls_ChequeFormatTypesClient();
            var query = obj.SelectAll(pageNum, pageSize, orderByFieldName, OrderByfeildtype, isAscendingOrder, out totalRows);

            ViewBag.BankList = Utility.GetBasicBanks();
            return PartialView(query);
        }
        public ActionResult GetChequeTypeByBankId(int BankID, int BasicBankID)
        {
            string where = "BankID =" + BasicBankID;
            int totalRows;
            ViewBag.BankID = BankID;
            ViewBag.BasicBankID = BasicBankID;
            Icls_ChequesClient dbcheque = new Icls_ChequesClient();
            string wherecheque = "BankID =" + BankID;
            //لیست دسته چک ها
            var chequeLS = dbcheque.SelectBy_DynamicWhereClause(wherecheque, 1, 1000, "ChequeID", "int", true, out totalRows);

            Icls_ChequeFormatTypesClient obj = new Icls_ChequeFormatTypesClient();
            //لیست قطع چک ها
            var chequeFormatLs = obj.SelectBy_DynamicWhereClause(where, 1, 1000, "ChequeFormatID", "int", true, out totalRows);


            var query = from chf in chequeFormatLs
                        join ch in chequeLS on chf.ChequeFormatID equals ch.ChequeFormatID
                        select chf;

            return View(query);
        }
        #region Create
        public ActionResult Create(int BankID, int BasicBankID)
        {
            ViewBag.BasicBankID = BasicBankID;
            ViewBag.BankID = BankID;
            Service.ChequesService.Cheque obj = new Service.ChequesService.Cheque() { BankID = BankID, BasicBankID = BasicBankID };
            ViewBag.GetChequeFormatTypeLs = Utility.GetFormatTypeDescLs();
            return PartialView(obj);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        //public ActionResult Create(ChequeFormatType obj, HttpPostedFileBase files)
        public ActionResult Create(Service.ChequesService.Cheque obj)
        {
            try
            {
                string message = "";
                Icls_ChequeFormatTypesClient RP = new Icls_ChequeFormatTypesClient();
                Service.ChequeFormatTypesService.ChequeFormatType chequeFormatTYpe = new Service.ChequeFormatTypesService.ChequeFormatType();
                chequeFormatTYpe.BankID = obj.BasicBankID;
                chequeFormatTYpe.FormatDescriptionID_Main = obj.FormatDescriptionID_Main;
                chequeFormatTYpe.FormatDescription = obj.FormatDescription;
                RP.Insert(chequeFormatTYpe, out message);
                var ChequeFormatID = message.Split(';')[1];

                if(ChequeFormatID!=null)
                {
                   Service.ChequesService.Cheque cheque = new Service.ChequesService.Cheque();
                    obj.LastUsedPaperNo = 0;
                    obj.ChequeFormatID = int.Parse(ChequeFormatID);

                    Icls_ChequesClient chequeClient = new Icls_ChequesClient();
                    var result = chequeClient.Insert(obj, out message);
                    return JsonMessage(result, message);
                }
                return JsonMessage(false, "insert_error");

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
            ViewBag.GetChequeFormatTypeLs = Utility.GetFormatTypeDescLs();
            Icls_ChequeFormatTypesClient obj = new Icls_ChequeFormatTypesClient();
            return PartialView(obj.SelectRow(id).SingleOrDefault());
        }


        [HttpPost]
        public ActionResult Edit(Service.ChequeFormatTypesService.ChequeFormatType obj)
        {
            try
            {
                string message = "";
                Icls_ChequeFormatTypesClient RP = new Icls_ChequeFormatTypesClient();
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
        public ActionResult Delete(int id)
        {
            Icls_ChequeFormatTypesClient obj = new Icls_ChequeFormatTypesClient();
            return PartialView(obj.SelectRow(id).SingleOrDefault());
        }

        [HttpPost]
        public ActionResult Delete(Service.ChequeFormatTypesService.ChequeFormatType obj)
        {
            try
            {
                string message = "";
                Icls_ChequeFormatTypesClient RP = new Icls_ChequeFormatTypesClient();
                var result = RP.Delete(obj.ChequeFormatID, out message);

                var db = new TreasuryFilesEntities();
                var objFile = db.Files.SingleOrDefault(u => u.relatedID == obj.ChequeFormatID && u.tableName == "ChequeFormatTypes");
                if (obj != null)
                {
                    try
                    {
                        db.Files.Remove(objFile);
                        db.SaveChanges();
                        return JsonMessage(true, "delete_success");
                    }
                    catch (Exception)
                    {
                        return JsonMessage(true, "delete_error");
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
    }
}