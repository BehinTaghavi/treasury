using CSD.Treasury.UI.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using CSD.Treasury.Service.ChequesService;
using CSD.Treasury.Service.ChequeFormatTypesService;
using CSD.Treasury.Service.ChequeFormatTypeDetailsService;
using CSD.Treasury.Service.BankService;
using CSD.Treasury.UI.Models;
using System.Text;
using CSD.Treasury.DataModel;
using CSD.Treasury.Service.ProceduresDB;

namespace CSD.Treasury.UI.Areas.BasicInfo.Controllers
{
    [Authorize(Roles = "Admin,Manager,BasicInfo,ReportInApp")]
    public class CheckBookController : BaseController
    {
        // GET: BasicInfo/CheckBook
        public ActionResult Index(int BankID, int BasicBankID)
        {
            ViewBag.BankID = BankID;
            ViewBag.BasicBankID = BasicBankID;
            return View();
        }
        public ActionResult List(int BankID, int BasicBankID)
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
                        select new Service.ChequesService.Cheque
                        {
                            BankID = ch.BankID,
                            FormatDescription = chf.FormatDescription,
                            FormatDescriptionID_Main = chf.FormatDescriptionID_Main,
                            BasicBankID = chf.BankID,
                            ChequeFormatID = chf.ChequeFormatID,
                            IsActive = ch.IsActive,
                            LastPageNumber = ch.LastPageNumber,
                            LastUsedPaperNo = ch.LastUsedPaperNo,
                            FirstPageNumber = ch.FirstPageNumber,
                            TotalPageCount = ch.TotalPageCount,
                            ChequeID= ch.ChequeID
                        };

            return PartialView(query);
        }

        /// <summary>
        /// لیستی از بانک ها  را از دیتا بیس بر می گرداند
        /// </summary>
        /// <returns></returns>
        public string GetBankList(int id)
        {
            var res = new StringBuilder();
            //int pageNum = 1;
            //int pageSize = 1000;
            //string orderByFieldName = "BankID";
            //bool isAscendingOrder = true;
            //string OrderByfeildtype = "int";
            //string whereclouse = "BasicBankID=" + id;
            //int totalRows;
            //Icls_BankClient obj = new Icls_BankClient();
            //var query = obj.SelectBy_DynamicWhereClause(whereclouse, pageNum, pageSize, orderByFieldName, OrderByfeildtype, isAscendingOrder, out totalRows).ToList();
            var db = new TreasuryEntities();
            var query = db.Banks.Where(d => d.BasicBankID == id);
            foreach (var i in query)
            {
                res.Append("<option value='" + i.BankID + "'>" + i.BranchName + "</option>");
            }
            return res.ToString();

        }



        /// <summary>
        /// لیستی از فرمت چک ها  را از دیتا بیس بر می گرداند
        /// </summary>
        /// <returns></returns>
        public string GetChequeFormatType(int id)
        {
            var res = new StringBuilder();
            foreach (var i in Utility.GetChequeFormatTypeLs(id))
            {
                res.Append("<option value='" + i.ChequeFormatID + "'>" + Utility.GetChequeFormatType_Description(i.FormatDescriptionID_Main) + "</option>");
            }
            return res.ToString();

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

                if (ChequeFormatID != null)
                {
                    Service.ChequesService.Cheque cheque = new Service.ChequesService.Cheque();
                    obj.LastUsedPaperNo = obj.FirstPageNumber - 1;
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
        public ActionResult Edit(int ChequeID,int id,int BasicBankID)
        {
            int TotalRow = 0;
            Icls_ChequeFormatTypesClient chequeformatTypeClient = new Icls_ChequeFormatTypesClient();
            var chequeformatType = chequeformatTypeClient.SelectRow(id).SingleOrDefault();
            Icls_ChequesClient obj = new Icls_ChequesClient();
            var cheque = obj.SelectRow(ChequeID).SingleOrDefault();
            //string where = "ChequeFormatID =" + id + " and BankID =" + BankID;
            //var cheque = obj.SelectBy_DynamicWhereClause(where, 1, 100, "ChequeID", "int", true, out TotalRow).FirstOrDefault();
            cheque.FormatDescription = chequeformatType.FormatDescription;
            cheque.FormatDescriptionID_Main = chequeformatType.FormatDescriptionID_Main;
            cheque.BasicBankID = BasicBankID;
            ViewBag.BasicBank = Utility.GetBasicBanks();
            ViewBag.GetChequeFormatTypeLs = Utility.GetFormatTypeDescLs();

            return PartialView(cheque);
        }

        [HttpPost]
        public ActionResult Edit(Service.ChequesService.Cheque obj)
        {
            try
            {
                string message = "";
                Icls_ChequeFormatTypesClient CFType = new Icls_ChequeFormatTypesClient();
                Icls_ChequesClient RP = new Icls_ChequesClient();

                Service.ChequeFormatTypesService.ChequeFormatType chequeType = new Service.ChequeFormatTypesService.ChequeFormatType()
                {
                    BankID = obj.BasicBankID,
                    ChequeFormatID = obj.ChequeFormatID.Value,
                    FormatDescriptionID_Main = obj.FormatDescriptionID_Main,
                    FormatDescription = obj.FormatDescription
                };
                CFType.Update(chequeType, out message);


                //Service.ChequesService.Cheque cheque = new Service.ChequesService.Cheque();
                //cheque.IsActive = obj.IsActive;
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
            try
            {
                string message = "";
                int TotalRow;
                Icls_ChequeFormatTypesClient ChqueFormatTypeDb = new Icls_ChequeFormatTypesClient();
                Icls_ChequesClient ChequeDb = new Icls_ChequesClient();
                var db = new TreasuryFilesEntities();
                var IfExistScanFile = db.Files.SingleOrDefault(d => d.relatedID == id && d.tableName == "ChequeFormatTypes");

                if (IfExistScanFile == null)
                {

                    var ChequeLs = ChequeDb.SelectBy_DynamicWhereClause("ChequeFormatID =" + id, 1, 100, "ChequeID", "int", true, out TotalRow);
                    foreach (var item in ChequeLs)
                    {
                        ChequeDb.Delete(item.ChequeID, out message);
                    }

                    var result = ChqueFormatTypeDb.Delete(id, out message);
                    return JsonMessage(result, message);

                }

                return JsonMessage(false, "لطفا ابتدا تصویرچک حذف شود", null, false);
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
        /// <param name="BehalfGroupDescription"></param>
        /// <returns></returns>
        #region CheckTitle
        public JsonResult CheckTitle(string FormatDescriptionID_Main, string InitialFormatDescriptionID_Main)
        {
            Icls_SPsClient db = new Icls_SPsClient();
            if (FormatDescriptionID_Main == InitialFormatDescriptionID_Main)
            {
                return Json(true, JsonRequestBehavior.AllowGet);
            }
            if (db.CheckExistValueInTable("ChequeFormatTypes", "FormatDescriptionID_Main", FormatDescriptionID_Main))
            {
                return Json(false, JsonRequestBehavior.AllowGet);
            }

            return Json(true, JsonRequestBehavior.AllowGet);

        }
        #endregion

        #region Copy
        public ActionResult Copy(int BasicBankId, int BankId, int ChequeFormatID)
        {
            int TotalRow;
            string Message, message2, message3;
            Icls_ChequeFormatTypeDetailsClient ChequeFormatDetailsClient = new Icls_ChequeFormatTypeDetailsClient();
            Icls_ChequesClient ChequeClient = new Icls_ChequesClient();
            Icls_ChequeFormatTypesClient chequeFormatTypeClient = new Icls_ChequeFormatTypesClient();
            var db = new TreasuryFilesEntities();

            var chequeFormatType = chequeFormatTypeClient.SelectBy_DynamicWhereClause("ChequeFormatID =" + ChequeFormatID, 1, 1000, "ChequeFormatID", "int", false, out TotalRow).SingleOrDefault();
            var Cheque = ChequeClient.SelectBy_DynamicWhereClause("ChequeFormatID =" + ChequeFormatID , 1, 1000, "ChequeID", "int", false, out TotalRow).SingleOrDefault();
            var ChequeFormatDetails = ChequeFormatDetailsClient.SelectBy_DynamicWhereClause("ChequeFormatID =" + chequeFormatType.ChequeFormatID, 1, 1000, "ChequeFormatID", "int", false, out TotalRow);

            //ایجاد قطع چک
            var chequeFormatTypeObj = new Service.ChequeFormatTypesService.ChequeFormatType
            {
                BankID = chequeFormatType.BankID,
                FormatDescriptionID_Main = chequeFormatType.FormatDescriptionID_Main,
                FormatDescription = chequeFormatType.FormatDescription,
            };
            chequeFormatTypeClient.Insert(chequeFormatTypeObj, out message2);
            var newChequeFormatID=int.Parse(message2.Split(';')[1]);

            //ایجاد دسته چک
            var ChequesObj = new Service.ChequesService.Cheque
            {
                BankID = Cheque.BankID,
                TotalPageCount =0,
                FirstPageNumber =0,
                LastPageNumber = 0,
                IsActive = false,
                ChequeFormatID = newChequeFormatID,
                LastUsedPaperNo = 0
            };
            ChequeClient.Insert(ChequesObj, out Message);

            //طراحی چک
            foreach (var item in ChequeFormatDetails)
            {
                var chequeFormatTypeDetailsObj = new Service.ChequeFormatTypeDetailsService.ChequeFormatTypeDetail
                {
                    ChequeFormatID = newChequeFormatID,
                    ChequeLableID = item.ChequeLableID,
                    m_Left = item.m_Left,
                    m_TOP = item.m_TOP,
                    m_FontName = item.m_FontName,
                    m_FontSize = item.m_FontSize,
                    m_fontweight = item.m_fontweight,
                    m_height = item.m_height,
                    m_Width = item.m_Width,
                    m_TextAlign = item.m_TextAlign,
                    m_Direction = item.m_Direction
                };
                ChequeFormatDetailsClient.Insert(chequeFormatTypeDetailsObj, out message3);
            }


            //ذخیره فایل        
            var ScanFile = db.Files.SingleOrDefault(d => d.relatedID == ChequeFormatID && d.tableName == "ChequeFormatTypes");

            var NewFile = new CSD.Treasury.DataModel.File
            {
                fileName =ScanFile.fileName,
                fileModifyDate=ScanFile.fileModifyDate,
                fileContent=ScanFile.fileContent,
                fileSize=ScanFile.fileSize,
                fileType=ScanFile.fileType,
                relatedID=newChequeFormatID,
                tableName= "ChequeFormatTypes",
                RecordDeleted =false,          
            };
            db.Files.Add(NewFile);
            db.SaveChanges();
            return RedirectToAction("Index", "checkBook", new { area = "BasicInfo", BasicBankId = BasicBankId, BankId = BankId, ChequeFormatID = ChequeFormatID });
        }
        #endregion
    }
}