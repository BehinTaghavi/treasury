using CSD.Treasury.DataModel;
using CSD.Treasury.UI.Controllers;
using System;
using System.Collections.Generic;
using System.Data.Entity.Validation;
using System.Data.Linq;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace CSD.Treasury.UI.Areas.BasicInfo.Controllers
{

    [Authorize(Roles = "Admin,Manager,BasicInfo,ReportInApp")]
    public class ScanController : BaseController
    {
        // GET: BasicInfo/Scan
        public PartialViewResult Index(int id,int BasicBankID,int BankID)
        {
            ViewBag.id = id;
            ViewBag.BasicBankID = BasicBankID;
            ViewBag.BankID = BankID;
            return PartialView();
        }
        [HttpGet]
        public ActionResult SaveScanFileView(int id,int BankID, int BasicBankID)
        {
            ViewBag.BankID = BankID;
            ViewBag.BasicBankID = BasicBankID;
            ViewBag.relatedID = id;
            return PartialView();
        }
        [HttpPost]
        public ActionResult SaveScanFile(int relatedID,int BankID,int BasicBankID)
        {
            var db = new TreasuryFilesEntities();

            var file = Request.Files[0];
            DataModel.File fileObj = new DataModel.File();
            if (file.ContentLength != 0)
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
                fileObj.tableName = "ChequeFormatTypes";

                fileObj.relatedID = relatedID;
                try
                {
                    db.Files.Add(fileObj);
                    db.SaveChanges();
                    return RedirectToAction("Index", "CheckBook", new { Area = "BasicInfo", BankID=BankID,BasicBankID=BasicBankID });
                }
                catch (DbEntityValidationException ex)
                {
                    ViewBag.BankID = BankID;
                    ViewBag.BasicBankID = BasicBankID;
                    ViewBag.relatedID = relatedID;
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
                    return PartialView();
                }
            }
            ViewBag.BankID = BankID;
            ViewBag.BasicBankID = BasicBankID;
            ViewBag.relatedID = relatedID;
            return RedirectToAction("Index", "CheckBook", new  { BankID = BankID, BasicBankID = BasicBankID });
 
        }
        public bool SaveToFile(int id)
        {
            try
            {
                String strImageName;
                HttpFileCollection files = System.Web.HttpContext.Current.Request.Files;
                HttpPostedFile uploadfile = files["RemoteFile"];
                strImageName = uploadfile.FileName;

                if (files != null)
                {
                    //var fileName = Path.GetFileName(fileurl.FileName);
                    //fileurl.SaveAs(Path.Combine(@"c:\projects", fileName));


                    var db = new TreasuryFilesEntities();
                    CSD.Treasury.DataModel.File file = new CSD.Treasury.DataModel.File();
                    file.fileName = uploadfile.FileName;


                    byte[] fileData = new byte[uploadfile.InputStream.Length];
                    uploadfile.InputStream.Read(fileData, 0, Convert.ToInt32(uploadfile.InputStream.Length));
                    System.Data.Linq.Binary binaryFile = new System.Data.Linq.Binary(fileData);



                    //MemoryStream target = new MemoryStream();
                    //uploadfile.InputStream.CopyTo(target);
                    //byte[] data = target.ToArray();


                    //MemoryStream target = new MemoryStream();
                    //uploadfile.InputStream.CopyTo(target);
                    //byte[] data = target.ToArray();



                    file.fileContent = fileData;
                    file.fileModifyDate = DateTime.Now;
                    file.fileSize = uploadfile.ContentLength;
                    file.RecordDeleted = false;
                    file.relatedID = id;
                    file.fileType = "image/jpeg";
                    file.RecordDeleted = false;
                    file.tableName = "ChequeFormatTypes";


                    db.Files.Add(file);
                    db.SaveChanges();
                }
                return true;
                //return JsonMessage(true, "insert_success");
                //uploadfile.SaveAs(Server.MapPath("/") + "UploadedImages\\" + strImageName);

            }
            catch (DbEntityValidationException ex)
            {
                // Retrieve the error messages as a list of strings.
                var errorMessages = ex.EntityValidationErrors
                        .SelectMany(x => x.ValidationErrors)
                        .Select(x => x.ErrorMessage);

                // Join the list to a single string.
                var fullErrorMessage = string.Join("; ", errorMessages);

                // Combine the original exception message with the new one.
                var exceptionMessage = string.Concat(ex.Message, " The validation errors are: ", fullErrorMessage);

                // Throw a new DbEntityValidationException with the improved exception message.
                throw new DbEntityValidationException(exceptionMessage, ex.EntityValidationErrors);
            }


        }
    }

}