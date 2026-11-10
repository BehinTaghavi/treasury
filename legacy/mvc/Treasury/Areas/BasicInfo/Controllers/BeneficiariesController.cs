using CSD.Treasury.UI.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using CSD.Treasury.Service.BeneficiaryService;
using CSD.Treasury.Service.Beneficiaries_TELSService;
using CSD.Treasury.Service.BeneficiariesDetailInfoService;
using CSD.Treasury.DataModel;
using CSD.Treasury.UI.Models;
using CSD.Treasury.Service.ProceduresDB;

namespace CSD.Treasury.UI.Areas.BasicInfo.Controllers
{
    [Authorize(Roles = "Admin,Manager,BasicInfo,ReportInApp")]
    public class BeneficiariesController : BaseController
    {
         
        public ActionResult Index(SearchModelBeneficiaries obj)
        {
            return View(obj);
        }
        public ActionResult List(SearchModelBeneficiaries obj)
        {
            try
            {
            int pageNum = obj.PageIndex;
            int pageSize = 10;
            int totalRows = 0;
            //Icls_BeneficiaryClient RP = new Icls_BeneficiaryClient();
            //var query = RP.SelectAll( 1, 10000*10000, "BeneficiarID", "int", false, out totalRows).ToList();
            var db = new TreasuryEntities();
            var query = db.Beneficiaries.ToList();
           
            if(!string.IsNullOrEmpty(obj.LegalAccountName1))
            {
                if (obj.LegalAccountName1.Contains('ی'))
                {
                    obj.LegalAccountName1 = obj.LegalAccountName1.Replace('ی', 'ي');
                }
                if (obj.LegalAccountName1.Contains('ک'))
                {
                    obj.LegalAccountName1 = obj.LegalAccountName1.Replace('ک', 'ك');
                }
                query =query.Where(d => d.LegalAccountName.Contains(obj.LegalAccountName1.Trim())).ToList();
                //whereclouse = whereclouse + " and  LegalAccountName like N\'%" + obj.LegalAccountName+ "%\'";
               
            }
            if (!string.IsNullOrEmpty(obj.Unique_Account_No1))
            {
                query = query.Where(d => d.Unique_Account_No.Contains(obj.Unique_Account_No1.Trim())).ToList();    
            }
            totalRows = query.Count();


            query = query.OrderBy(d=>d.BeneficiarID).Skip(pageSize*pageNum).Take(pageSize).ToList();

           
            obj.TotalPageCount = (int)Math.Ceiling(totalRows / (double)pageSize);
            obj.CurrentPage = pageNum;
            obj.List = query;
            obj.CurrentPage = obj.PageIndex;
            obj.TotalItemCount = totalRows;

            return PartialView(obj);
            }
            catch (Exception ex)
            {

                throw;
            }
        }
        #region AngularForm
        public ActionResult IndexForm()
        {
            return View();
        }
        public JsonResult Show()
        {
            var ls = Utility.GetBasicBanks();
            return Json(new
            {
                BasicBanksLs = ls,
                BankAccountList =new List<Service.BeneficiaryService.Beneficiary>()
            }
            , JsonRequestBehavior.AllowGet);
        }
        public ActionResult getBeneficiariesUC()
        {
            var dc = new TreasuryEntities();
            //نوع ذینفع  
            var unique_account_type = dc.v_unique_account_type.ToList();
            unique_account_type.Add(new v_unique_account_type { Code = 0, Description = Resourse.Resources.Others });
            ViewBag.unique_account_type = unique_account_type;
            return PartialView();
        }
        #endregion
        #region Create
        public ActionResult Create()
        {
            var dc = new TreasuryEntities();
            //نوع   
            var unique_account_type = dc.v_unique_account_type.ToList();
            //unique_account_type.Add(new v_unique_account_type { Code = 0, Description = Resourse.Resources.Others });
            
            ViewBag.unique_account_type = unique_account_type;

            return PartialView();
        }
        [HttpPost]
        public ActionResult Create(DataModel.Beneficiary obj)
        {
            string message = "";
            try
            {                
                //Icls_BeneficiaryClient RP = new Icls_BeneficiaryClient();
                //var result = RP.Insert(obj, out message);

                var db = new TreasuryEntities();
                db.Beneficiaries.Add(obj);
                db.SaveChanges();
                message = "ثبت با موفقیت انجام شد";
                return JsonMessage(true, message,null,false);
            }
            catch
            {
                message = "ثبت با خطا مواجه شد"; 
                return JsonMessage(false, message, null, false);
            }
        }
        #endregion

        #region Edit
        public ActionResult Edit(int id)
        {
            var dc = new TreasuryEntities();
            //Icls_BeneficiaryClient obj = new Icls_BeneficiaryClient();
            ////نوع ذینفع  
            //var unique_account_type = dc.v_unique_account_type.ToList();
            //unique_account_type.Add(new v_unique_account_type { Code = 0, Description = Resourse.Resources.Others });
            //ViewBag.unique_account_type = unique_account_type;


            //نوع ذینفع  

            //var Beneficiaries = obj.SelectRow(id).SingleOrDefault();
            var Beneficiaries = dc.Beneficiaries.SingleOrDefault(d=>d.BeneficiarID == id);
            //if (Beneficiaries == null)
            //{
            //    //درصورتی که نوع ذینفع نداشته باشد
            //    ViewBag.uniqueTypeId = 0;
            //}
            //else
            //{
            //    var Type_Code = dc.v_unique_accounts.SingleOrDefault(d => d.Account_Code == Beneficiaries.Unique_Account_No).Type_Code;
            //    //برگرداندن مقدار نوع ذینفع برای نمایش
            //    ViewBag.uniqueTypeId = Type_Code.Trim();
            //    //ذیفع ها 
            //    ViewBag.unique_accounts = dc.v_unique_accounts.Where(d => d.Type_Code == Type_Code.Trim());
            //}

            //ViewBag.SlectedLs = ((List<v_unique_accounts>)(from p in dc.v_unique_accounts
            //                    //where p.Type_Code == Beneficiaries.Unique_Account_No.Trim()
            //                    select new
            //                    {
            //                        Account_Code = p.Account_Code.Replace(" ", ""),
            //                        Account_Name = p.Account_Name + "/" + p.Account_Code
            //                    }));
            var _a = dc.v_unique_accounts.Where(q => q.Account_Code == Beneficiaries.Unique_Account_No).FirstOrDefault();
            ViewBag.Slected = _a.Account_Code + " " + _a.Account_Name;

            return PartialView(Beneficiaries);
        }


        [HttpPost]
        public ActionResult Edit(DataModel.Beneficiary obj)
        {
            string message = "";
            try
            {
                //Icls_BeneficiaryClient RP = new Icls_BeneficiaryClient();
                //var result = RP.Update(obj, out message);
                var db = new TreasuryEntities();
                var beneficiary = db.Beneficiaries.SingleOrDefault(d=>d.BeneficiarID == obj.BeneficiarID);
                beneficiary.Unique_Account_No = obj.Unique_Account_No;
                beneficiary.LegalPerson = obj.LegalPerson;
                beneficiary.CodeMeli = obj.CodeMeli;
                beneficiary.ShenaseMelli = obj.ShenaseMelli;
                beneficiary.CodeEghtesadi = obj.CodeEghtesadi;
                beneficiary.ShomareyeSabt = obj.ShomareyeSabt;
                beneficiary.AddressePosti = obj.AddressePosti;
                beneficiary.CodeEghtesadi = obj.CodeEghtesadi;
                db.SaveChanges();
                message = "ثبت با موفقیت انجام شد";
                return JsonMessage(true, message, null, false);
            }
            catch
            {
                message = "ثبت با خطا مواجه شد";
                return JsonMessage(false, message, null, false);
            }
        }
        #endregion

        #region Delete
        //public ActionResult Delete(int id)
        //{
        //    Icls_BeneficiaryClient obj = new Icls_BeneficiaryClient();
        //    return PartialView(obj.SelectRow(id).SingleOrDefault());
        //}

        [HttpPost]
        public ActionResult Delete(int id)
        {
            try
            {
                string message = "";
                Icls_BeneficiaryClient RP = new Icls_BeneficiaryClient();
                var result = RP.Delete(id, out message);
                return JsonMessage(result, message);
            }
            catch
            {
                return PartialView();
            }
        }
        #endregion

        #region DetailsList
        public ActionResult DetailsList(int BeneficiarID)
        {
            ViewBag.BeneficiarID = BeneficiarID;
            int totalRows;
            string whereclouse = "BeneficiarID=" + BeneficiarID;
            Icls_BeneficiariesDetailInfoClient DetailsClient = new Icls_BeneficiariesDetailInfoClient();
            Icls_BeneficiaryClient BeneficiaryClient = new Icls_BeneficiaryClient();         
            ViewBag.Unique_Account_No = BeneficiaryClient.SelectRow(BeneficiarID).SingleOrDefault().Unique_Account_No;
      
            var query = DetailsClient.SelectBy_DynamicWhereClause(whereclouse, 1, 1000, "BeneficiarDetailID", "int", false, out totalRows).ToList();
            return PartialView(query);
        }
        #endregion

        #region CreateDetails
        public ActionResult CreateDetails(int BeneficiarID)
        {
            ViewBag.BasicBank = Utility.GetBasicBanks();
            Service.BeneficiariesDetailInfoService.BeneficiariesDetailInfo obj = new Service.BeneficiariesDetailInfoService.BeneficiariesDetailInfo() { BeneficiarID = BeneficiarID };
            return PartialView(obj);
        }
        [HttpPost]
        public ActionResult CreateDetails(Service.BeneficiariesDetailInfoService.BeneficiariesDetailInfo obj)
        {
            try
            {
                string message = "";
                Icls_BeneficiariesDetailInfoClient RP = new Icls_BeneficiariesDetailInfoClient();

                var result = RP.Insert(obj, out message);
                return JsonMessage(result, message);
            }
            catch
            {
                return PartialView();
            }
        }
        #endregion

        #region EditDetails
        public ActionResult EditDetails(int id)
        {
            ViewBag.BasicBank = Utility.GetBasicBanks();
            Icls_BeneficiariesDetailInfoClient obj = new Icls_BeneficiariesDetailInfoClient();
            return PartialView(obj.SelectRow(id).SingleOrDefault());
        }

        [HttpPost]
        public ActionResult EditDetails(Service.BeneficiariesDetailInfoService.BeneficiariesDetailInfo obj)
        {
            try
            {
                string message = "";
                Icls_BeneficiariesDetailInfoClient RP = new Icls_BeneficiariesDetailInfoClient();
                var result = RP.Update(obj, out message);
                return JsonMessage(result, message);
            }
            catch
            {
                return PartialView();
            }
        }
        #endregion

        #region DeleteDetails


        [HttpPost]
        public ActionResult DeleteDetails(Service.BeneficiariesDetailInfoService.BeneficiariesDetailInfo obj)
        {
            try
            {
                string message = "";
                Icls_BeneficiariesDetailInfoClient RP = new Icls_BeneficiariesDetailInfoClient();
                var result = RP.Delete(obj.BeneficiarDetailID, out message);
                return JsonMessage(result, message);
            }
            catch
            {
                return PartialView();
            }
        }
        #endregion

        #region Tel

        public PartialViewResult BeneficiariesDetails(int id)
        {
            Icls_BeneficiaryClient obj = new Icls_BeneficiaryClient();
            return PartialView(obj.SelectRow(id).SingleOrDefault());
        }

        public ActionResult TelIndex(int ID)
        {
            ViewBag.BeneficiarID = ID;
            return PartialView();
        }

        public PartialViewResult ListTel(int ID)
        {
            ViewBag.BeneficiarID = ID;
            int pageNum = 1;
            int pageSize = 1000;
            string orderByFieldName = "BeneficiarID";
            bool isAscendingOrder = false;
            string OrderByfeildtype = "int";
            int totalRows;
            string whereclouse = "BeneficiarID=" + ID;
            Icls_Beneficiaries_TELSClient RP = new Icls_Beneficiaries_TELSClient();
            ViewBag.BranchId = ID;
            var query = RP.SelectBy_DynamicWhereClause(whereclouse, pageNum, pageSize, orderByFieldName, OrderByfeildtype, isAscendingOrder, out totalRows).ToList();
            return PartialView(query);
        }
        public ActionResult CreateTel(int ID)
        {
            ViewBag.BeneficiarID = ID;
            return PartialView();
        }
        [HttpPost]
        public ActionResult CreateTel(Service.Beneficiaries_TELSService.Beneficiaries_TELS obj)
        {
            try
            {
                string message = "";
                Icls_Beneficiaries_TELSClient RP = new Icls_Beneficiaries_TELSClient();
                var result = RP.Insert(obj, out message);
                return JsonMessage(result, message, obj);
            }
            catch
            {
                return PartialView();
            }
        }

        [HttpPost]
        public ActionResult EditTel(Service.Beneficiaries_TELSService.Beneficiaries_TELS obj)
        {
            try
            {
                string message = "";
                Icls_Beneficiaries_TELSClient RP = new Icls_Beneficiaries_TELSClient();
                var result = RP.Update(obj, out message);
                return JsonMessage(result, message);
            }
            catch
            {
                return PartialView();
            }
        }
        public ActionResult DeleteTel(int Id)
        {
            try
            {
                string message = "";
                Icls_Beneficiaries_TELSClient RP = new Icls_Beneficiaries_TELSClient();
                var result = RP.Delete(Id, out message);
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
        public JsonResult CheckTitle(string LegalAccountName,string InitialLegalAccountName)
        {
            Icls_SPsClient db = new Icls_SPsClient();
            if (LegalAccountName == InitialLegalAccountName)
            {
                return Json(true, JsonRequestBehavior.AllowGet);
            }
            if (db.CheckExistValueInTable("Beneficiaries", "LegalAccountName", LegalAccountName))
            {
                return Json(false, JsonRequestBehavior.AllowGet);
            }

            return Json(true, JsonRequestBehavior.AllowGet);
        }
        #endregion
    }
}