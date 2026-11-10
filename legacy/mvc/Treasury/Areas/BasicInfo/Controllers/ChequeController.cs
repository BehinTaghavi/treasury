using CSD.Treasury.UI.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using CSD.Treasury.Service.ChequeFormatTypeDetailsService;
using CSD.Treasury.Service.ChequeFormatTypesService;
using CSD.Treasury.UI.Models;
using Newtonsoft.Json;
using System.Text;
using CSD.Treasury.DataModel;
using System.IO;
using System.Drawing;
using CSD.Treasury.UI.Areas.BasicInfo.Models;    

namespace CSD.Treasury.UI.Areas.BasicInfo.Controllers
{
    [Authorize(Roles = "Admin,Manager,BasicInfo,ReportInApp")]
    public class ChequeController : BaseController
    {
        // GET: BasicInfo/Cheque
        public ActionResult Index()
        {
            return View();
        }
        public PartialViewResult List()
        {
            int pageNum = 1;
            int pageSize = 1000;
            string orderByFieldName = "FormatID";
            bool isAscendingOrder = true;
            string OrderByfeildtype = "int";
            int totalRows;

            Icls_ChequeFormatTypeDetailsClient obj = new Icls_ChequeFormatTypeDetailsClient();
            var query = obj.SelectAll(pageNum, pageSize, orderByFieldName, OrderByfeildtype, isAscendingOrder, out totalRows);
            return PartialView(query);
        }
        #region Create

        public ActionResult Create(int id,int BankId,int BasicBankId)
        {
            Icls_ChequeFormatTypesClient objs = new Icls_ChequeFormatTypesClient();

            string whereclouse = "ChequeFormatID =" + id;
            int totalRows;
            var query = objs.SelectBy_DynamicWhereClause(whereclouse, 1, 100, "ChequeFormatID", "int", false, out totalRows).FirstOrDefault();
            ViewBag.ChequeFormatID = query.ChequeFormatID;
            ViewBag.BasicBankID = BasicBankId;
            ViewBag.BankId = BankId;
            ViewBag.ChequeFormatTypes = Utility.GetChequeFormatTypeLs(BasicBankId);
            ViewBag.BasicBanks = Utility.GetBasicBanks();
            ViewBag.BasicChequeLables = Utility.GetBasicChequeLableLS();
            var db = new TreasuryFilesEntities();
            {
                CSD.Treasury.DataModel.File obj = new CSD.Treasury.DataModel.File();
                obj = db.Files.Where(u => u.tableName == "ChequeFormatTypes" && u.relatedID == query.ChequeFormatID).FirstOrDefault();
                if (obj != null)
                {
                    FileContentResult result = new FileContentResult(obj.fileContent.ToArray(), "application/octet-stream")
                    {
                        FileDownloadName = obj.fileName,
                    };
                    MemoryStream myMemStream = new System.IO.MemoryStream(result.FileContents);
                  
                    return View(new CustomImage { Image = result.FileContents, Extension = obj.fileType.Split('/')[1], Height = Image.FromStream(myMemStream).Height, Width = Image.FromStream(myMemStream).Width });
                }
                return View();
            }
        }

        public string GetChequeFormatType(int id)
        {
            var res = new StringBuilder();
            foreach (var i in Utility.GetChequeFormatTypeLs(id))
            {
                res.Append("<option value='" + i.ChequeFormatID + "'>" + Utility.GetChequeDesc(i.FormatDescriptionID_Main) + "</option>");
            }
            return res.ToString();

        }

        public JsonResult GetChequeFormatTypeImage(string tableName, decimal relatedID)
        {
            var db = new TreasuryFilesEntities();
            {
                CSD.Treasury.DataModel.File obj = new CSD.Treasury.DataModel.File();
                obj = db.Files.Where(u => u.tableName == tableName && u.relatedID == relatedID).FirstOrDefault();
                if (obj != null)
                {
                    FileContentResult result = new FileContentResult(obj.fileContent.ToArray(), "application/octet-stream")
                    {
                        FileDownloadName = obj.fileName,
                    };
                    MemoryStream myMemStream = new System.IO.MemoryStream(result.FileContents);
                    var jsonResult = Json(new { Image = Convert.ToBase64String(result.FileContents), Extension = obj.fileType.Split('/')[1], Height = Image.FromStream(myMemStream).Height, Width = Image.FromStream(myMemStream).Width }, JsonRequestBehavior.AllowGet);
                    jsonResult.MaxJsonLength = int.MaxValue;
                    return jsonResult;
                }
                return Json(false, JsonRequestBehavior.AllowGet);
            }
        }
        [HttpPost, ActionName("Create")]
        public ActionResult CreateChequeFormatTypeDetails()
        {
            try
            {
                string json = Request.Form["CheckFormatType"];
                var ObjLS = JsonConvert.DeserializeObject<List<Service.ChequeFormatTypeDetailsService.ChequeFormatTypeDetail>>(json);
                string message = "";
                foreach (var item in ObjLS)
                {
                    Icls_ChequeFormatTypeDetailsClient RP = new Icls_ChequeFormatTypeDetailsClient();
                    var result = RP.Insert(item, out message);
                }
                return JsonMessage(true, message);
            }
            catch
            {
                return PartialView();
            }
        }
        #endregion
        #region Edit
        public ActionResult Edit(int id, int BankId, int BasicBankId)
        {
            Icls_ChequeFormatTypesClient chequeFormatTypesClient = new Icls_ChequeFormatTypesClient();
            int totalRowsOut;
            var chequeFormatTypeList = chequeFormatTypesClient.SelectBy_DynamicWhereClause("ChequeFormatID =" + id, 1, 1000, "ChequeFormatID", "int", true, out totalRowsOut).FirstOrDefault();
            ViewBag.ChequeFormatID = chequeFormatTypeList.ChequeFormatID;
            ViewBag.BasicBankID = BasicBankId;
            ViewBag.BankId = BankId;
            ViewBag.ChequeFormatTypes = Utility.GetChequeFormatTypeLs(BasicBankId);
            ViewBag.BasicBanks = Utility.GetBasicBanks();
            ViewBag.BasicChequeLables = Utility.GetBasicChequeLableLS();

            Icls_ChequeFormatTypeDetailsClient obj = new Icls_ChequeFormatTypeDetailsClient();

            int pageNum = 1;
            int pageSize = 1000;
            string orderByFieldName = "FormatID";
            bool isAscendingOrder = true;
            string OrderByfeildtype = "int";
            string whereclouse = "ChequeFormatID =" + id;
            int totalRows;

            var db = new TreasuryFilesEntities();
            {
                CSD.Treasury.DataModel.File objd = new CSD.Treasury.DataModel.File();
                objd = db.Files.Where(u => u.tableName == "ChequeFormatTypes" && u.relatedID == id).FirstOrDefault();
                if (objd != null)
                {
                    FileContentResult result = new FileContentResult(objd.fileContent.ToArray(), "application/octet-stream")
                    {
                        FileDownloadName = objd.fileName,
                    };
                    MemoryStream myMemStream = new System.IO.MemoryStream(result.FileContents);
                    //var jsonResult = Json(new { Image = Convert.ToBase64String(result.FileContents), Extension = objd.fileType.Split('/')[1], Height = Image.FromStream(myMemStream).Height, Width = Image.FromStream(myMemStream).Width }, JsonRequestBehavior.AllowGet);
                    //jsonResult.MaxJsonLength = int.MaxValue;
                    ViewBag.Image = Convert.ToBase64String(result.FileContents).Trim();
                    ViewBag.Extension = objd.fileType.Split('/')[1].Trim();
                    ViewBag.Height = Image.FromStream(myMemStream).Height;
                    ViewBag.Width = Image.FromStream(myMemStream).Width;
                }
            }

            var query = obj.SelectBy_DynamicWhereClause(whereclouse, pageNum, pageSize, orderByFieldName, OrderByfeildtype, isAscendingOrder, out totalRows).ToList();

            return View(query);
        }
        [HttpPost, ActionName("Edit")]
        public ActionResult EditChequeFormatTypeDetails()
        {
            try
            {
                string json = Request.Form["CheckFormatType"];
                Icls_ChequeFormatTypeDetailsClient RP = new Icls_ChequeFormatTypeDetailsClient();
                var ObjLS = JsonConvert.DeserializeObject<List<Service.ChequeFormatTypeDetailsService.ChequeFormatTypeDetail>>(json);
                string message = "";
                DeleteAll(ObjLS.FirstOrDefault().ChequeFormatID);
                foreach (var item in ObjLS)
                {
                    item.m_FontName = item.m_FontName.Replace("\"", string.Empty);
                    var result = RP.Insert(item, out message);
                }
                return JsonMessage(true, message);
            }
            catch
            {
                return PartialView();
            }


            //try
            //{
            //    int pageNum = 1;
            //    int pageSize = 1000;
            //    string orderByFieldName = "FormatID";
            //    bool isAscendingOrder = true;
            //    string OrderByfeildtype = "int";
            //    int totalRows;
            //    string message = "";

            //    Icls_ChequeFormatTypeDetailsClient obj = new Icls_ChequeFormatTypeDetailsClient();
            //    var query = obj.SelectAll(pageNum, pageSize, orderByFieldName, OrderByfeildtype, isAscendingOrder, out totalRows).Where(d=>d.ChequeFormatID==ChequeFormatID);
            //    if(query!=null)
            //    {
            //        foreach(var item in query)
            //        {
            //            var result = obj.Delete(item.FormatID, out message);
            //        }
            //    }

            //    string json = Request.Form["CheckFormatType"];
            //    var ObjLS = JsonConvert.DeserializeObject<List<Service.ChequeFormatTypeDetailsService.ChequeFormatTypeDetail>>(json);
            //    foreach (var item in ObjLS)
            //    {
            //        Icls_ChequeFormatTypeDetailsClient RP = new Icls_ChequeFormatTypeDetailsClient();
            //        var result = RP.Update(item, out message);
            //        return JsonMessage(result, message);
            //    }
            //    return PartialView();
            //}
            //catch
            //{
            //    return PartialView();
            //}
        }
        #endregion
        #region Delete
        // GET: BasicInfo/ExchangeType/Delete/5
        public ActionResult Delete(int id)
        {
            Icls_ChequeFormatTypeDetailsClient obj = new Icls_ChequeFormatTypeDetailsClient();
            return PartialView(obj.SelectRow(id).SingleOrDefault());
        }

        // POST: BasicInfo/ExchangeType/Delete/5
        [HttpPost]
        public ActionResult Delete(Service.ChequeFormatTypeDetailsService.ChequeFormatTypeDetail obj)
        {
            try
            {
                string message = "";
                Icls_ChequeFormatTypeDetailsClient RP = new Icls_ChequeFormatTypeDetailsClient();
                var result = RP.Delete(obj.FormatID, out message);
                return JsonMessage(result, message);
            }
            catch
            {
                return PartialView();
            }
        }
        //حذف دسته جمعی رکورد ها
        public bool DeleteAll(int ChequeFormatID)
        {
            try
            {           
                int totalRows;
                string message = "";
                Icls_ChequeFormatTypeDetailsClient obj = new Icls_ChequeFormatTypeDetailsClient();
                var ls = obj.SelectBy_DynamicWhereClause("ChequeFormatID=" + ChequeFormatID, 1, 1000, "FormatID", "int", true, out totalRows).ToList();

                foreach (var item in ls)
                {
                    obj.Delete(item.FormatID, out message);
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        #endregion

    }
}