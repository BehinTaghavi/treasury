using CSD.Treasury.UI.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using CSD.Treasury.Service.ProceduresDB;
using CSD.Treasury.DataModel;

namespace CSD.Treasury.UI.Areas.Management.Controllers
{
    [Authorize(Roles = "Admin,Manager")]
    public class Vocher_RowReceiptController : BaseController
    {
        // GET: Management/Vocher_RowReceipt
        public ActionResult Index(int ReceiptDetailsId,int StateId)
        {
            var db = new TreasuryEntities();
            //db.CreateVoucher(ReceiptDetailsId, StateId);
            return View();
        }
    }
}