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
    public class ExchangeRatesController : BaseController
    {
        private TreasuryEntities db = new TreasuryEntities();

        // GET: BasicInfo/ExchangeRates
        public ActionResult Index()
        {
            var exchangeRates = db.ExchangeRates.Include(e => e.ExchangeType);
            ViewBag.ExchangeTypeID = new SelectList(db.ExchangeTypes, "ExchangeTypeID", "ExchangeTypeDescription");
            return View(exchangeRates.ToList());
        }
        public ActionResult List(int ExchangeTypeID = 0)
        {
            var exchangeRates = db.ExchangeRates.Include(e => e.ExchangeType);
            if(ExchangeTypeID != 0)
            {
                exchangeRates=exchangeRates.Where(d => d.ExchangeTypeID == ExchangeTypeID);
            }
            return PartialView(exchangeRates.ToList());
        }
      
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            ExchangeRate exchangeRate = db.ExchangeRates.Find(id);
            if (exchangeRate == null)
            {
                return HttpNotFound();
            }
            return View(exchangeRate);
        }


        public ActionResult Create()
        {
            ViewBag.ExchangeTypeID = new SelectList(db.ExchangeTypes, "ExchangeTypeID", "ExchangeTypeDescription");
            return PartialView();
        }

        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "ExchangeTypeID,ExchangeDateRate,FormalPrice,InformalPrice")] ExchangeRate exchangeRate)
        {
            var message = "";
            try
            {
                exchangeRate.ExchangeDateRate = exchangeRate.ExchangeDateRate.Replace("/", "");
                if (ModelState.IsValid)
                {
                    db.ExchangeRates.Add(exchangeRate);
                    db.SaveChanges();

                }

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


        public ActionResult Edit(int? id, string id2)
        {
            if (id == null || id2 == null)
            {
                return JsonMessage(false, "خطا" + HttpStatusCode.BadRequest, null, false);
            }
            ExchangeRate exchangeRate = db.ExchangeRates.Find(id, id2);
            if (exchangeRate == null)
            {
                return JsonMessage(false, "خطا" + HttpNotFound(), null, false);

            }
            ViewBag.ExchangeTypeID = new SelectList(db.ExchangeTypes, "ExchangeTypeID", "ExchangeTypeDescription", exchangeRate.ExchangeTypeID);
            return PartialView(exchangeRate);
        }

    
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "ExchangeTypeID,ExchangeDateRate,FormalPrice,InformalPrice")] ExchangeRate exchangeRate)
        {
            var message = "";
            try
            {
                if (ModelState.IsValid)
                {
                    //db.Entry(exchangeRate).State = EntityState.Modified;
                    var  OldExchangeRate = db.ExchangeRates.Find(exchangeRate.ExchangeTypeID, exchangeRate.ExchangeDateRate);
                    if (OldExchangeRate.FormalPrice != exchangeRate.FormalPrice)
                        OldExchangeRate.FormalPrice = exchangeRate.FormalPrice;

                    if (OldExchangeRate.InformalPrice != exchangeRate.InformalPrice)
                        OldExchangeRate.InformalPrice = exchangeRate.InformalPrice;

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

        // GET: BasicInfo/ExchangeRates/Delete/5
        //public ActionResult Delete(int? id)
        //{
        //    if (id == null)
        //    {
        //        return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
        //    }
        //    ExchangeRate exchangeRate = db.ExchangeRates.Find(id);
        //    if (exchangeRate == null)
        //    {
        //        return HttpNotFound();
        //    }
        //    return View(exchangeRate);
        //}


        [HttpPost]
        public ActionResult Delete(int? id, string id2)
        {
            var message = "";
            try
            {
                if (id == null || id2 == null)
                {
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
                }
                ExchangeRate ExchangeRates = db.ExchangeRates.Find(id, id2);
                if (ExchangeRates == null)
                {
                    return HttpNotFound();
                }
                db.ExchangeRates.Remove(ExchangeRates);
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
