using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using CSD.Treasury.DataModel;
using CSD.Treasury.UI.Controllers;

namespace CSD.Treasury.UI.Areas.BasicInfo.Controllers
{
    [Authorize(Roles = "Admin,Manager,BasicInfo,ReportInApp")]
    public class ExchangeStandardsController : BaseController
    {
        private TreasuryEntities db = new TreasuryEntities();

  
        public ActionResult Index()
        {
            return View();
        }
        public ActionResult List()
        {
            var exchangeStandards = db.ExchangeStandards.Include(e => e.Config).Include(e => e.ExchangeType);
            return PartialView(exchangeStandards.ToList());
        }
        
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            ExchangeStandard exchangeStandard = db.ExchangeStandards.Find(id);
            if (exchangeStandard == null)
            {
                return HttpNotFound();
            }
            return View(exchangeStandard);
        }

        // GET: BasicInfo/ExchangeStandards/Create
        public ActionResult Create()
        {
            ViewBag.FiscalYear = new SelectList(db.Configs, "FiscalYear", "FiscalYear");
            ViewBag.ExchangeTypeID = new SelectList(db.ExchangeTypes, "ExchangeTypeID", "ExchangeTypeDescription");
            return PartialView();
        }

      
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "FiscalYear,ExchangeTypeID,Price")] ExchangeStandard exchangeStandard)
        {
            var message = "";
            try
            {
                if (ModelState.IsValid)
                {
                    db.ExchangeStandards.Add(exchangeStandard);
                    db.SaveChanges();
                    message = "ثبت با موفقیت انجام شد";

                }
                return JsonMessage(true, message, null, false);
            }
            catch (Exception ex)
            {
                message = "با خطا مواجه شد";
                if (ex.InnerException != null)
                {
                    if (ex.InnerException.InnerException.Message.Contains("\r\n"))
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

    
        public ActionResult Edit(int? id, int? id2)
        {
            if (id == null || id2 == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            ExchangeStandard exchangeStandard = db.ExchangeStandards.Find(id2.Value, id.Value);
            if (exchangeStandard == null)
            {
                return HttpNotFound();
            }
            ViewBag.FiscalYear = new SelectList(db.Configs, "FiscalYear", "AccDatabaseName", exchangeStandard.FiscalYear);
            ViewBag.ExchangeTypeID = new SelectList(db.ExchangeTypes, "ExchangeTypeID", "ExchangeTypeDescription", exchangeStandard.ExchangeTypeID);
            return PartialView(exchangeStandard);
        }

    
       
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "FiscalYear,ExchangeTypeID,Price")] ExchangeStandard exchangeStandard)
        {
            var message = "";
            try
            {
                if (ModelState.IsValid)
                {
                    db.Entry(exchangeStandard).State = EntityState.Modified;
                    db.SaveChanges();
                    message = "ثبت با موفقیت انجام شد";
                }
                return JsonMessage(true, message, null, false);
            }
            catch (Exception ex)
            {
                message = "با خطا مواجه شد";
                if (ex.InnerException != null)
                {
                    if (ex.InnerException.InnerException.Message.Contains("\r\n"))
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
        [HttpPost, ActionName("Delete")]

        public ActionResult Delete(int? id, int? id2)
        {
            var message = "";
            try
            {
                if (id == null || id2 == null)
                {
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
                }
                ExchangeStandard exchangeStandard = db.ExchangeStandards.Find(id2, id);
                if (exchangeStandard == null)
                {
                    return HttpNotFound();
                }
                db.ExchangeStandards.Remove(exchangeStandard);
                db.SaveChanges();
                message = "ثبت با موفقیت انجام شد";
                return JsonMessage(true, message, null, false);
            }
            catch (Exception ex)
            {
                message = "با خطا مواجه شد";
                if (ex.InnerException != null)
                {
                    if (ex.InnerException.InnerException.Message.Contains("\r\n"))
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

        // POST: BasicInfo/ExchangeStandards/Delete/5
        //[HttpPost, ActionName("Delete")]
        //[ValidateAntiForgeryToken]
        //public ActionResult DeleteConfirmed(int id)
        //{
        //    ExchangeStandard exchangeStandard = db.ExchangeStandards.Find(id);
        //    db.ExchangeStandards.Remove(exchangeStandard);
        //    db.SaveChanges();
        //    return RedirectToAction("Index");
        //}

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
