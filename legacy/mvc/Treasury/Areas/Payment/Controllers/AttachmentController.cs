using CSD.Treasury.DataModel;
using CSD.Treasury.Service.ChequeFormatTypeDetailsService;
using CSD.Treasury.UI.Controllers;
using System;
using System.Collections.Generic;
using System.Data.Entity.Validation;
using System.Data.Linq;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace CSD.Treasury.UI.Areas.Payment.Controllers
{
    [Authorize(Roles = "Admin,Manager,RequestPayment,Reciept,PettyCash,DirectDebit,Payment")]
    public class AttachmentController : BaseController
    {
        //لیستی از فایل ها ضمیمه 
        public ActionResult List(int Id ,string TableName)
        {
            var db = new TreasuryFilesEntities();
            var Files = db.Files.Where(d => d.relatedID == Id && d.tableName==TableName && d.RecordDeleted == false).ToList();
            ViewBag.RelatedId = Id;
            ViewBag.TableName = TableName;
            return PartialView(Files);
        }
        //خواندن فایل از دیتا بیس 
        public ActionResult GetContentAttachedFile(string TableName="",int fileID=0, int relateID=0)
        {
            var db = new TreasuryFilesEntities();
            {
                CSD.Treasury.DataModel.File obj = new CSD.Treasury.DataModel.File();
                if (fileID!=0)
                {
                     obj = db.Files.SingleOrDefault(u => u.fileID == fileID );
                }
                else if(relateID!=0 && string.IsNullOrEmpty(TableName))
                {
                     obj = db.Files.SingleOrDefault(u => u.relatedID == relateID && u.tableName==TableName);
                }
                if (obj != null)
                {
                    FileContentResult result = new FileContentResult(obj.fileContent.ToArray(), "application/octet-stream")
                    {
                        FileDownloadName = obj.fileName,
                    };
                    return result;
                }

            }
            return PartialView();
        }
        /// <summary>
        /// حذف فایل های ضمیمه با ID
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        #region DeleteAttach
        [Authorize]
        public ActionResult Delete(int id ,int ChequeFormatID=0)
        {
            if (Request.IsAuthenticated)
            {
                var db = new TreasuryFilesEntities();
                var obj = db.Files.SingleOrDefault(u => u.fileID == id);
                if (obj != null)
                {
                    try
                    {
                        db.Files.Remove(obj);
                        db.SaveChanges();
                        if (ChequeFormatID != 0)
                        {
                            int totalRows;
                            string message = "";
                            Icls_ChequeFormatTypeDetailsClient ChequeFormatTypeDetailsClient = new Icls_ChequeFormatTypeDetailsClient();
                            var ls = ChequeFormatTypeDetailsClient.SelectBy_DynamicWhereClause("ChequeFormatID=" + ChequeFormatID, 1, 1000, "FormatID", "int", true, out totalRows).ToList();

                            foreach (var item in ls)
                            {
                                ChequeFormatTypeDetailsClient.Delete(item.FormatID, out message);
                            }
                        }
                        return JsonMessage(true, "delete_success");
                    }
                    catch (Exception)
                    {
                        return JsonMessage(true, "delete_error");
                    }
                }
            
               
            }
            return Json(false, JsonRequestBehavior.AllowGet);
        }

        public PartialViewResult ShowDeleteConfirm(int Id)
        {
            ViewBag.AttachmentId = Id;
            return PartialView();
        }


        #endregion
        /// <summary>
        /// ذخیره فایل ضمیمه 
        /// </summary>
        /// <param name="relatedID"></param>
        /// <param name="file"></param>
        /// <returns></returns>
        #region Create 
        public JsonResult Create(int relatedID, string TableName, HttpPostedFileBase file)
        {
            var db = new TreasuryFilesEntities();
            File fileObj = new File();
            if(file!=null)
            {

                fileObj.fileName = file.FileName;
                byte[] fileData = new byte[file.InputStream.Length];


               
                file.InputStream.Read(fileData, 0, Convert.ToInt32(file.InputStream.Length));
                Binary binaryFile = new Binary(fileData);

                fileObj.fileContent = fileData;
                fileObj.fileType = file.ContentType;
                fileObj.fileModifyDate = DateTime.Now;
                fileObj.fileSize = file.ContentLength;
                fileObj.RecordDeleted = false;
                fileObj.tableName = TableName;
            
                fileObj.relatedID = relatedID;
                try
                {
                    db.Files.Add(fileObj);
                    db.SaveChanges();
                    return JsonMessage(true, "insert_success");
                }
                catch (DbEntityValidationException ex)
                {

                    foreach (var eve in ex.EntityValidationErrors)
                    {
                        Console.WriteLine("Entity of type \"{0}\" in state \"{1}\" has the following validation errors:",
                            eve.Entry.Entity.GetType().Name, eve.Entry.State);
                        foreach (var ve in eve.ValidationErrors)
                        {
                            Console.WriteLine("- Property: \"{0}\", Error: \"{1}\"",
                                ve.PropertyName, ve.ErrorMessage);
                        }
                    }
                    return JsonMessage(true, "insert_error");
                }
            }
            return Json(true, JsonRequestBehavior.AllowGet);
        }
        #endregion
    }
}