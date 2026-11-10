using CSD.Treasury.DataModel;
using CSD.Treasury.UI.Controllers;
using CSD.Treasury.UI.Models;
using Stimulsoft.Report;
using Stimulsoft.Report.Mvc;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace CSD.Treasury.UI.Areas.Report.Controllers
{
    [Authorize(Roles = "Admin,Manager,BasicInfo,ReportInApp")]
    public class DaryaftaniPardakhtaniController : BaseController
    {
        private TreasuryEntities db = new TreasuryEntities();
        public ActionResult DaryaftaniPardakhtaniAti(int Type = 3)
        {
            var query = db.DaryaftaniPardakhtaniAti(Type).OrderBy(d => d.FiscalYear).ToList();
            return View(query);
        }
        public ActionResult IndexPerZinaf(DaryaftaniPardakhtaniAti OBJ)
        {
            return View(OBJ);
        }
        public ActionResult ListPerZinaf(DaryaftaniPardakhtaniAti OBJ)
        {
            int pageNum = OBJ.PageIndex;
            int pageSize = 10;
            int totalRows;

            var query = db.DaryaftaniPardakhtaniAtiPerZinaf(OBJ.Type).OrderBy(d => d.FiscalYear).ToList();
            totalRows = query.Count();

            
            OBJ.TotalPageCount = (int)Math.Ceiling(totalRows / (double)pageSize);
            OBJ.CurrentPage = pageNum;
            OBJ.ListPerZinaf = query;
            OBJ.CurrentPage = OBJ.PageIndex;
            OBJ.TotalItemCount = totalRows;
            return PartialView(OBJ);
        }
        public ActionResult IndexPerZinafSarresid(DaryaftaniPardakhtaniAti OBJ)
        {
            return View(OBJ);
        }
        public ActionResult ListPerZinafSarresid(DaryaftaniPardakhtaniAti OBJ)
        {
            int pageNum = OBJ.PageIndex;
            int pageSize = 10;
            int totalRows;

            var query = db.DaryaftaniPardakhtaniAtiPerZinafSarresid(OBJ.Type).OrderBy(d => d.DueDate).ToList();
            totalRows = query.Count();

            
            OBJ.TotalPageCount = (int)Math.Ceiling(totalRows / (double)pageSize);
            OBJ.CurrentPage = pageNum;
            OBJ.ListPerZinafSaresid = query;
            OBJ.CurrentPage = OBJ.PageIndex;
            OBJ.TotalItemCount = totalRows;
            return PartialView(OBJ);
        }

        public ActionResult ReportDaryaftaniPardakhtani()
        {
            return View();
        }
        #region Report
        [HttpPost]
        public ActionResult ReportDaryaftaniPardakhtani(DaryaftaniPardakhtaniAti OBJ)
        {
            return View(OBJ);
        }
        public ActionResult LoadReportDaryaftaniPardakhtani(DaryaftaniPardakhtaniAti SearchObj)
        {
            StiReport report = new StiReport();
          
            int pageNum = SearchObj.PageIndex;
            
            //if (SearchObj.Type ==)
            //{
            //    ReceiptLs = spsClient.GetRecieptHeaderAndDetail_ByRecieptTypeIdsss("18,19,21,25,28").ToList();
            //}
           
            //var QueryLs = ReceiptLs.OrderBy(d => d.DueDate).ToList();

            //List<CustomReceiptHeaderDetails> CustomLs = new List<CustomReceiptHeaderDetails>();



            //string Path = Server.MapPath("~/Report/ReceiptChecksLs.mrt");
            //report.Load(Path);
            //report.Dictionary.Variables["CompanyName"].Value = ConfigurationManager.AppSettings["CompanyTitle"];
            //report.RegBusinessObject("ReceiptObj", CustomLs);
            //report.Dictionary.SynchronizeBusinessObjects();


            return StiMvcViewer.GetReportSnapshotResult(HttpContext, report);
        }
        //ایجاد پرینت
        public ActionResult PrintReport()
        {
            return StiMvcViewer.PrintReportResult();

        }
        //ایجاد خروجی
        public ActionResult ExportReport()
        {
            return StiMvcViewer.ExportReportResult();
        }

        public ActionResult ViewerEvent()
        {
            return StiMvcViewer.ViewerEventResult();
        }

        public ActionResult Interaction()
        {
            return StiMvcViewer.InteractionResult();
        }

        #endregion
    }
}