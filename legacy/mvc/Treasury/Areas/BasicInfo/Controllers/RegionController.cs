using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using CSD.Treasury.Service.CountryService;
using CSD.Treasury.Service.ProvinceService;
using CSD.Treasury.Service.CityService;
using CSD.Treasury.UI.Controllers;
using CSD.Treasury.Service.ProceduresDB;

namespace CSD.Treasury.UI.Areas.BasicInfo.Controllers
{
    [Authorize(Roles = "Admin,Manager,BasicInfo,ReportInApp")]
    public class RegionController : BaseController
    {
        #region Country
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult ListCountry()
        {
            int pageNum = 1;
            int pageSize = 1000;
            string orderByFieldName = "CountryID";
            bool isAscendingOrder = true;
            string OrderByfeildtype = "int";
            int totalRows;

            Icls_CountryClient obj = new Icls_CountryClient();
            var query = obj.SelectAll(pageNum, pageSize, orderByFieldName, OrderByfeildtype, isAscendingOrder, out totalRows);
            return PartialView(query);
        }

        public ActionResult CreateCountry()
        {
            return PartialView();
        }

        [HttpPost]
        public ActionResult CreateCountry(Service.CountryService.Country obj)
        {
            try
            {
                string message = "";
                Icls_CountryClient RP = new Icls_CountryClient();           
                var result = RP.Insert(obj, out message);
                return JsonMessage(result, message);
            }
            catch
            {
                return PartialView();
            }
        }
        public ActionResult EditCountry(int id)
        {
            Icls_CountryClient obj = new Icls_CountryClient();
            return PartialView(obj.SelectRow(id).SingleOrDefault());
        }

        [HttpPost]
        public ActionResult EditCountry(Service.CountryService.Country obj)
        {
            try
            {
                string message = "";
                Icls_CountryClient RP = new Icls_CountryClient();
                var result = RP.Update(obj, out message);
                return JsonMessage(result, message);
            }
            catch
            {
                return PartialView();
            }
        }
        public ActionResult DeleteCountry(int id)
        {
            Icls_CountryClient obj = new Icls_CountryClient();
            return PartialView(obj.SelectRow(id).SingleOrDefault());
        }

        [HttpPost]
        public ActionResult DeleteCountry(Service.CountryService.Country obj)
        {
            try
            {
                string message = "";
                Icls_CountryClient RP = new Icls_CountryClient();
                var result = RP.Delete(obj.CountryID, out message);
                return JsonMessage(result, message);
            }
            catch
            {
                return PartialView();
            }
        }
        #endregion
        #region Province

        public ActionResult ListProvince(int CountryID)
        {
            int pageNum = 1;
            int pageSize = 1000;
            string orderByFieldName = "provinceID";
            bool isAscendingOrder = true;
            string OrderByfeildtype = "int";
            string whereclouse = "CountryID=" + CountryID;
            int totalRows;

            Icls_ProvinceClient obj = new Icls_ProvinceClient();
            var query = obj.SelectBy_DynamicWhereClause(whereclouse,pageNum, pageSize, orderByFieldName, OrderByfeildtype, isAscendingOrder, out totalRows);
            ViewBag.CountryID = CountryID;
            return PartialView(query);
        }

        public ActionResult CreateProvince(int countryID)
        {
            Service.ProvinceService.Province province = new Service.ProvinceService.Province();
            province.CountryID = countryID;
            return PartialView(province);
        }

        [HttpPost]
        public ActionResult CreateProvince(Service.ProvinceService.Province obj)
        {
            try
            {
                string message = "";
                Icls_ProvinceClient RP = new Icls_ProvinceClient();
                var result = RP.Insert(obj, out message);
                return JsonMessage(result, message);
            }
            catch
            {
                return PartialView();
            }
        }
        public ActionResult EditProvince(int id)
        {
            Icls_ProvinceClient obj = new Icls_ProvinceClient();
            return PartialView(obj.SelectRow(id).SingleOrDefault());
        }

        [HttpPost]
        public ActionResult EditProvince(Service.ProvinceService.Province obj)
        {
            try
            {
                string message = "";
                Icls_ProvinceClient RP = new Icls_ProvinceClient();
                var result = RP.Update(obj, out message);
                return JsonMessage(result, message);
            }
            catch
            {
                return PartialView();
            }
        }
        public ActionResult DeleteProvince(int id)
        {
            Icls_CountryClient obj = new Icls_CountryClient();
            return PartialView(obj.SelectRow(id).SingleOrDefault());
        }


        [HttpPost]
        public ActionResult DeleteProvince(Service.ProvinceService.Province obj)
        {
            try
            {
                string message = "";
                Icls_ProvinceClient RP = new Icls_ProvinceClient();
                var result = RP.Delete(obj.provinceID, out message);
                return JsonMessage(result, message);
            }
            catch
            {
                return PartialView();
            }
        }
        #endregion
        #region City
        public ActionResult ListCity(int provinceID, int countryID)
        {
            int pageNum = 1;
            int pageSize = 1000;
            string orderByFieldName = "CityID";
            bool isAscendingOrder = true;
            string OrderByfeildtype = "int";
            string whereclouse = "ProvinceID=" + provinceID;
            int totalRows;

            Icls_CityClient obj = new Icls_CityClient();
            var query = obj.SelectBy_DynamicWhereClause(whereclouse, pageNum, pageSize, orderByFieldName, OrderByfeildtype, isAscendingOrder, out totalRows);
            ViewBag.ProvinceID = provinceID;
            ViewBag.CountryID = countryID;
            return PartialView(query);
        }
        public ActionResult CreateCity(int provinceID, int countryID)
        {
            Service.CityService.City city = new Service.CityService.City();
            city.ProvinceID = provinceID;
            ViewBag.CountryID = countryID;
            return PartialView(city);
        }

        [HttpPost]
        public ActionResult CreateCity(Service.CityService.City obj)
        {
            try
            {
                string message = "";
                Icls_CityClient RP = new Icls_CityClient();
                var result = RP.Insert(obj, out message);
                return JsonMessage(result, message);
            }
            catch
            {
                return PartialView();
            }
        }
        public ActionResult EditCity(int id, int countryID)
        {
            Icls_CityClient obj = new Icls_CityClient();
            ViewBag.CountryID = countryID;
            return PartialView(obj.SelectRow(id).SingleOrDefault());
        }

        [HttpPost]
        public ActionResult EditCity(Service.CityService.City obj)
        {
            try
            {
                string message = "";
                Icls_CityClient RP = new Icls_CityClient();
                var result = RP.Update(obj, out message);
                return JsonMessage(result, message);
            }
            catch
            {
                return PartialView();
            }
        }
        public ActionResult DeleteCity(int id)
        {
            Icls_CountryClient obj = new Icls_CountryClient();
            return PartialView(obj.SelectRow(id).SingleOrDefault());
        }


        [HttpPost]
        public ActionResult DeleteCity(Service.CityService.City obj)
        {
            try
            {
                string message = "";
                Icls_CityClient RP = new Icls_CityClient();
                var result = RP.Delete(obj.CityID, out message);
                return JsonMessage(result, message);
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
        public JsonResult CheckCountryTitle(string CountryDescription,string InitialCountryDescription)
        {
            Icls_SPsClient db = new Icls_SPsClient();
            if (CountryDescription == InitialCountryDescription)
            {
                return Json(true, JsonRequestBehavior.AllowGet);
            }
            if (db.CheckExistValueInTable("Country", "CountryDescription", CountryDescription))
            {
                return Json(false, JsonRequestBehavior.AllowGet);
            }

            return Json(true, JsonRequestBehavior.AllowGet);
        }
      
        public JsonResult CheckProvinceTitle(string ProvinceDescription,string InitialProvinceDescription)
        {
            Icls_SPsClient db = new Icls_SPsClient();
            if (ProvinceDescription == InitialProvinceDescription)
            {
                return Json(true, JsonRequestBehavior.AllowGet);
            }
            if (db.CheckExistValueInTable("Province", "ProvinceDescription", ProvinceDescription))
            {
                return Json(false, JsonRequestBehavior.AllowGet);
            }

            return Json(true, JsonRequestBehavior.AllowGet);
        }
        public JsonResult CheckCityTitle(string CityDescription,string InitialCityDescription)
        {
            Icls_SPsClient db = new Icls_SPsClient();
            if (CityDescription == InitialCityDescription)
            {
                return Json(true, JsonRequestBehavior.AllowGet);
            }
            if (db.CheckExistValueInTable("City", "CityDescription", CityDescription))
            {
                return Json(false, JsonRequestBehavior.AllowGet);
            }

            return Json(true, JsonRequestBehavior.AllowGet);
        }
        #endregion


    }
}