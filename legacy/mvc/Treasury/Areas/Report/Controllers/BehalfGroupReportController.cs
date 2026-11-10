using CSD.Treasury.Service.ProceduresDB;
using CSD.Treasury.UI.Areas.Report.Models;
using CSD.Treasury.UI.Controllers;
using CSD.Treasury.UI.Models;
using Stimulsoft.Report;
using Stimulsoft.Report.Mvc;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using CSD.Treasury.Service.BehalfGroupService;
using CSD.Treasury.DataModel;

namespace CSD.Treasury.UI.Areas.Report.Controllers
{
    [Authorize(Roles = "Admin,Manager,Report,ReportInApp")]
    public class BehalfGroupReportController : BaseController
    {
        // GET: Report/BehlalfsReport
        public ActionResult SearchView()
        {
            return View();
        }
        
        public ActionResult Index(BehalfModel SearchObj)
        {
            return View(SearchObj);
        }
        public ActionResult List(BehalfModel SearchObj)
        {
            StiReport report = new StiReport();
            Icls_SPsClient ProceduresDB = new Icls_SPsClient();
            string Path = Server.MapPath("~/Report/GetAllPaymentReceiptByBehalfGroup.mrt");
            report.Load(Path);

            var ls =ProceduresDB.GetAllPaymentReceiptByBehalfGroup(SearchObj.FromDate.Replace(@"/", ""), SearchObj.ToDate.Replace(@"/", ""),SearchObj.ExchangeTypeId).ToList();
            if (SearchObj.ExchangeTypeId == 1)
            {
                ls = ls.Where(d => d.ExchangeTypeID == 1).ToList();
            }
            else
            {
                ls = ls.Where(d => d.ExchangeTypeID != 1).ToList();
            }
            if (SearchObj.BehalfGroupId!=null)
            {
                ls = ls.Where(d => d.BehalfGroupID==SearchObj.BehalfGroupId).ToList();
            }
            if (SearchObj.type == 0)
            {
                report.RegBusinessObject("Receipt", ls.Where(d => d.RecordNo == 2));
                report.RegBusinessObject("Payment", ls.Where(d => d.RecordNo == 3));
            }
            else if (SearchObj.type == 1)
            {
                report.RegBusinessObject("Receipt", ls.Where(d => d.RecordNo == 2));              
            }
            else if (SearchObj.type == 2)
            {
                report.RegBusinessObject("Payment", ls.Where(d => d.RecordNo == 3));
            }

            report.Dictionary.Variables["CompanyName"].Value = ConfigurationManager.AppSettings["CompanyTitle"];
            report.Dictionary.Variables["DateNow"].Value = Utility.GetPersianDate(Utility.ToPersianDate(DateTime.Now));
            report.Dictionary.Variables["DateFrom"].Value = SearchObj.FromDate;
            report.Dictionary.Variables["DateTO"].Value = SearchObj.ToDate;
            report.Dictionary.Databases.Clear();
            report.Dictionary.SynchronizeBusinessObjects();
            return StiMvcViewer.GetReportSnapshotResult(HttpContext, report);
        }
        public string GetBehalfGroup(int id)
        {
            var res = new StringBuilder();
            string whereclouse = "Receipt_Pay_TypeID=" + id;
            int totalRows;
            Icls_BehalfGroupClient obj = new Icls_BehalfGroupClient();
            var query = obj.SelectBy_DynamicWhereClause(whereclouse, 1, 10000, "BehalfGroupID", "int", true, out totalRows).ToList();
            res.Append("<option value='0'>---</option>");
            foreach (var i in query)
            {            
                res.Append("<option value='" + i.BehalfGroupID + "'>" + i.BehalfGroupDescription + "</option>");
            }
            return res.ToString();
        }

        #region BehalfGroupListprint
        public ActionResult BehalfGroupIndex()
        {
            return View();
        }
        public ActionResult BehalfGroupList()
        {
            var db = new TreasuryEntities();
            StiReport report = new StiReport();
            //Icls_SPsClient ProceduresDB = new Icls_SPsClient();
            string Path = Server.MapPath("~/Report/BehalfGroupList.mrt");
            report.Load(Path);
            var ls = db.BehalfList().ToList();
            report.Dictionary.Variables["DateNow"].Value = Utility.GetPersianDate(Utility.ToPersianDate(DateTime.Now));
            report.Dictionary.Variables["CompanyName"].Value = ConfigurationManager.AppSettings["CompanyTitle"];
            report.RegBusinessObject("BehalfGroupList", ls);
            report.Dictionary.Databases.Clear();
            report.Dictionary.SynchronizeBusinessObjects();
            return StiMvcViewer.GetReportSnapshotResult(HttpContext, report);
        }
        #endregion
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

    }
}