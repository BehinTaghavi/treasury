using CSD.Common.Models;
using CSD.Treasury.DataModel;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;
using CSD.Treasury.Service.Request_PaymentService;
using CSD.Treasury.Service.PlaningTypeService;
using CSD.Treasury.Service.PettyCash_TransactionHeaderService;
using CSD.Treasury.Common.Enumeration;
using CSD.Treasury.Service.ProceduresDB;
using CSD.Treasury.Service.BankService;
using CSD.Treasury.Service.BeneficiaryService;
using System.Configuration;
using System.ComponentModel.DataAnnotations;

namespace CSD.Treasury.UI.Models
{
    public class SearchModelBeneficiaries : PagingItems
    {
        public string LegalAccountName1 { get; set; }
        public string Unique_Account_No1 { get; set; }
        public List<DataModel.Beneficiary> List { get; set; }
        public SearchModelBeneficiaries()
        {
            PageIndex = 0;
            PageSize = 10;
        }
    }
    public class DaryaftaniPardakhtaniAti  : PagingItems
    {
        public int Type { get; set; }
        public List<CSD.Treasury.DataModel.DaryaftaniPardakhtaniAtiPerZinaf_Result> ListPerZinaf { get; set; }
        public List<CSD.Treasury.DataModel.DaryaftaniPardakhtaniAtiPerZinafSarresid_Result> ListPerZinafSaresid { get; set; }

    }

    public class SearchModelBehalfGroup : PagingItems
    {
        public string BehalfGroupDescription { get; set; }
        public int Receipt_Pay_TypeID { get; set; }
        public bool FromCreateRequest { get; set; }
        public List<CSD.Treasury.DataModel.BehalfGroupList> List { get; set; }
        public SearchModelBehalfGroup()
        {
            PageIndex = 0;
            PageSize = 10;
        }
    }
    public class SearchModelBehalf : PagingItems
    {
        public string BehalfGroupDescription { get; set; }
        public string BehalfDescription { get; set; }
        public int BehalfGroupID { get; set; }
        public bool FromCreateRequest { get; set; }
        public int Receipt_Pay_TypeID { get; set; }
        public List<Service.BehalfsService.Behalf> List { get; set; }
        public SearchModelBehalf()
        {
            PageIndex = 0;
            PageSize = 10;
            FromCreateRequest = false;
        }
    }
    public class SearchModelRequestPayment : PagingItems
    {

        public bool ChequePassed { get; set; }
        public decimal RequsetId { get; set; }
        [RegularExpression(@"/^[1-4]\d{3}\/((0[1-6]\/((3[0-1])|([1-2][0-9])|(0[1-9])))|((1[0-2]|(0[7-9]))\/(30|([1-2][0-9])|(0[1-9]))))$/",
                 ErrorMessage = "Characters are not allowed.")]
        public string DueDate { get; set; }
        public string DueDateF { get; set; }
        public string DueDateT { get; set; }
        public string RequestDateFrom { get; set; }
        public string RequestDateTo { get; set; }
        public decimal PayAmount { get; set; }
        public int Pay_TypeDetailID_source { get; set; }
        public int ExchangeTypeID { get; set; }
        public int BehalfID { get; set; }
        public string FiscalYear { get; set; }
        public bool NotAccepted { get; set; }
        public int[] payTypeLs { get; set; }
        public string Status { get; set; }
        public bool IsPaid { get; set; }
        public List<Treasury.DataModel.GetAllRequestStatus_Result> List { get; set; }

        public List<Course> Courses { get; set; }
        public string UniqueAccountType { get; set; }
        public string Unique_Account_No { get; set; }

        //مشخص کننده نوع پرداخت نقدی یا چک است
        public int PayType { get; set; }
        //مشخص کننده پرداخت شده یا نشده است؟
        public bool ShowConfirmPayment { get; set; }
        public int Source_PaymentValue { get; set; }
        public decimal ChequeNo { get; set; }
        public int BankId { get; set; }
        public SearchModelRequestPayment()
        {
            PersianCalendar pc = new PersianCalendar();
            var Month = pc.GetMonth(DateTime.Now);
            FiscalYear = pc.GetYear(DateTime.Now).ToString();
            if (Month >= 10 && ConfigurationManager.AppSettings["YearPlus"] == "True")
            {
                FiscalYear = (pc.GetYear(DateTime.Now) + 1).ToString();
            }

            PageIndex = 0;
            PageSize = 10;
        }
    }
    public class SearchModelPayment : PagingItems
    {
        public decimal RequsetId { get; set; }
        public string DueDate { get; set; }
        public string RequestDateFrom { get; set; }
        public string RequestDateTo { get; set; }
        public decimal PayAmount { get; set; }
        public int Pay_TypeDetailID_source { get; set; }
        public int ExchangeTypeID { get; set; }
        public int BehalfID { get; set; }
        public string FiscalYear { get; set; }
        public int[] payTypeLs { get; set; }
        public string Status { get; set; }
        public List<DataModel.GetRequestPayment_Payment_WithLastStates_Result> List { get; set; }
        public string UniqueAccountType { get; set; }
        public string Unique_Account_No { get; set; }

        //مشخص کننده نوع پرداخت نقدی یا چک است
        public int PayType { get; set; }


        public SearchModelPayment()
        {
            PersianCalendar pc = new PersianCalendar();
            var Month = pc.GetMonth(DateTime.Now);
            FiscalYear = pc.GetYear(DateTime.Now).ToString();
            if (Month >= 10 && ConfigurationManager.AppSettings["YearPlus"] == "True")
            {
                FiscalYear = (pc.GetYear(DateTime.Now) + 1).ToString();
            }

            PageIndex = 0;
            PageSize = 10;
        }
    }
    public class SearchModelExpiredReceivableCheque : PagingItems
    {
        public decimal ReceiptDetailId { get; set; }
        public string DueDateFrom { get; set; }
        public string ReceiptDateFrom { get; set; }
        public string ReceeiptDateTo { get; set; }
        public string DueDateTo { get; set; }
        public string StatusUnique_Account_No_One { get; set; }
        public decimal PayAmount { get; set; }
        public int Pay_TypeDetailID_source { get; set; }
        public int BehalfID { get; set; }
        public int BehalfGroupID { get; set; }
        public string FiscalYear { get; set; }
        public int[] payTypeLs { get; set; }
        public string Status { get; set; }
        public string ChequeNo { get; set; }
        public int BankId { get; set; }
        public int exchangeTypeId { get; set; }
        public List<Service.ProceduresDB.GetRecieptHeaderAndDetail_ByRecieptTypeId_Result> List { get; set; }
        public string UniqueAccountType { get; set; }
        public string Unique_Account_No { get; set; }

        //مشخص کننده نوع پرداخت نقدی یا چک است
        public int PayType { get; set; }


        public SearchModelExpiredReceivableCheque()
        {
            PersianCalendar pc = new PersianCalendar();
            var Month = pc.GetMonth(DateTime.Now);
            FiscalYear = pc.GetYear(DateTime.Now).ToString();
            if (Month >= 10 && ConfigurationManager.AppSettings["YearPlus"] == "True")
            {
                FiscalYear = (pc.GetYear(DateTime.Now) + 1).ToString();
            }

            PageIndex = 0;
            PageSize = 15;
        }
    }
    public class SearchModelExpiredCheque : PagingItems
    {
        public decimal RequsetId { get; set; }
        public string DueDateFrom { get; set; }
        public string RequestDateFrom { get; set; }
        public string DueDateTo { get; set; }

        public string RequestDateTo { get; set; }
        public decimal PayAmount { get; set; }
        public int Pay_TypeDetailID_source { get; set; }
        public int ExchangeTypeID { get; set; }
        public int BehalfID { get; set; }
        public int BehalfGroupID { get; set; }
        public string FiscalYear { get; set; }
        public int[] payTypeLs { get; set; }
        public string Status { get; set; }
        public decimal ChequeNo { get; set; }
        public int BankId { get; set; }
        public List<Get_Maturity_Cheques_Report_Result> List { get; set; }
        public string UniqueAccountType { get; set; }
        public string Unique_Account_No { get; set; }

        //مشخص کننده نوع پرداخت نقدی یا چک است
        public int PayType { get; set; }


        public SearchModelExpiredCheque()
        {
            PersianCalendar pc = new PersianCalendar();
            var Month = pc.GetMonth(DateTime.Now);
            FiscalYear = pc.GetYear(DateTime.Now).ToString();
            if (Month >= 10 && ConfigurationManager.AppSettings["YearPlus"] == "True")
            {
                FiscalYear = (pc.GetYear(DateTime.Now) + 1).ToString();
            }

            PageIndex = 0;
            PageSize = 15;
        }
    }
    public class SearchModelReceipt : PagingItems
    {
        public List<DataModel.Reciept_Header> List { get; set; }
        public SearchModelReceipt()
        {
            PageIndex = 0;
            PageSize = 10;
        }
    }
    public class Course
    {
        public int Id { get; set; }
        public string CourseCode { get; set; }
        public string CourseName { get; set; }
        public int SchoolId { get; set; }
    }
    public class SearchModelUniqueAccount : PagingItems
    {
        public string UniqueAccountTypeId { get; set; }
        public string Name { get; set; }
        public string Code { get; set; }
        public string NameField { get; set; }
        public string CodeField { get; set; }
        public string UpdateTargetId { get; set; }
        public List<v_unique_accounts> List { get; set; }
        public string Bind { get; set; }
        public SearchModelUniqueAccount()
        {
            UniqueAccountTypeId = "0";
            PageIndex = 1;
            PageSize = 10;
            NameField = "Name";
            CodeField = "Code";
            UpdateTargetId = "UniqueAccountList";
            Name = "";
            Code = "";
        }
    }
    public class SearchModelUser : PagingItems
    {
        public string User_FullName { get; set; }
        public string LocationByCostCenterID { get; set; }
        public List<CSD.Treasury.DataModel.UserInfo> List { get; set; }
        public SearchModelUser()
        {
            PageIndex = 1;
            PageSize = 10;
        }
    }
    public class SearchModelPettyCash : PagingItems
    {
        public int PettyCashID { get; set; }
        public string DateFrom { get; set; }
        public string DateTo { get; set; }
        public List<Service.PettyCash_TransactionHeaderService.PettyCash_TransactionHeader> List { get; set; }

    }
    public class SearchModelPettyCash2 : PagingItems
    {
        public int PettyCashID { get; set; }
        public string DateFrom { get; set; }
        public string DateTo { get; set; }
        public string Unique_Account_No { get; set; }
        public string Account_NO { get; set; }
        public string Description { get; set; }
        public List<Service.ProceduresDB.GetPettyCashHeaderAndDetails_ByPettyCashId_Result> List { get; set; }

    }
    public class SearchModelBranch : PagingItems
    {
        public string cmpBranchName { get; set; }
        public List<CSD.Treasury.Service.cmpBranchesService.cmpBranch> List { get; set; }
        public SearchModelBranch()
        {
            PageIndex = 0;
            PageSize = 10;
        }
    }
    public class ReceiptSearch1 : PagingItems
    {
        public int RecieptTypeId { get; set; }
        public int State { get; set; }
        public List<Service.ProceduresDB.GetRecieptHeaderAndDetail_ByRecieptTypeId_Result> List { get; set; }
        public string DueDateFrom { get; set; }
        public string DueDateTo { get; set; }
        public int ReceiptId { get; set; }
        public int BehalfID { get; set; }
        public string Unique_Account_No { get; set; }
        public int PayAmount { get; set; }
        public string FiscalYear { get; set; }
        public ReceiptSearch1()
        {
            PersianCalendar pc = new PersianCalendar();
            var Month = pc.GetMonth(DateTime.Now);
            // FiscalYear = pc.GetYear(DateTime.Now).ToString();
            //if (Month >= 10 && ConfigurationManager.AppSettings["YearPlus"] == "True")
            //{
            //    FiscalYear = (pc.GetYear(DateTime.Now) + 1).ToString();
            //}

        }
    }
    public class ReceiptSearch : PagingItems
    {
        public int BehalfID { get; set; }
        public int RecieptTypeId { get; set; }
        public string RecieptTypeId2 { get; set; }
        public int State { get; set; }
        public List<CustomReceiptHeaderDetails> List { get; set; }
        public string DueDateFrom { get; set; }
        public string DueDateTo { get; set; }
        public string RecieptDateFrom { get; set; }
        public string RecieptDateTo { get; set; }
        public int ReceiptId { get; set; }
        public int ReceiptDetailId { get; set; }
        public List<Service.PlaningTypeService.PlaningType> PlaningTypeLS { get; set; }
        public List<Custom_Bank> BankAccountLs { get; set; }
        public List<Custom_Bank> BankAccountLsArzi { get; set; }
        public List<Service.BehalfGroupService.BehalfGroup> BehalfGroupLs { get; set; }
        public List<Service.BehalfsService.Behalf> BehlafLS { get; set; }
        public List<CustomCashier> CashierLS { get; set; }

        public List<v_accounts> v_accountslS { get; set; }

        public List<Service.Receipt_Pay_TypeDetailService.Receipt_Pay_TypeDetail> ReceiptPayTypeDetailLs { get; set; }
        public bool isCheck { get; set; }
        public string Unique_Account_No { get; set; }
        public string FiscalYear { get; set; }
        public int ChequeNo { get; set; }
        public decimal Price { get; set; }
        public int PlaningTypeReferenceID { get; set; }
        public string ReferenceValue { get; set; }
        public int ExchangeTypeID { get; set; }//1
        public ReceiptSearch()
        {
            PersianCalendar pc = new PersianCalendar();
            var Month = pc.GetMonth(DateTime.Now);
            FiscalYear = pc.GetYear(DateTime.Now).ToString();
            if (Month >= 10 && ConfigurationManager.AppSettings["YearPlus"] == "True")
            {
                FiscalYear = (pc.GetYear(DateTime.Now) + 1).ToString();
            }

        }

    }
    public class CheckBookStatusSearch : PagingItems
    {
        public decimal ChequeNo { get; set; }
        public string DueDateF { get; set; }
        public string DueDateT { get; set; }
        public int Source_PaymentValue { get; set; }
        public string FiscalYear { get; set; }
        public decimal PayAmount { get; set; }
        public int Pay_TypeDetailID_source { get; set; }
        public int ExchangeTypeID { get; set; }
        public string RequestDateFrom { get; set; }
        public string RequestDateTo { get; set; }
        public decimal RequsetId { get; set; }
        public string BankAccountNumber { get; set; }
        public int BankAccountName { get; set; }
        public int BasicBankId { get; set; }
        public int ChequeFormatType { get; set; }
        public List<CSD.Treasury.DataModel.GetAllRequestStatus_Result> List { get; set; }
        public string DueDateFrom { get; set; }
        public string DueDateTo { get; set; }
        public string UniqueAccountType { get; set; }
        public string Unique_Account_No { get; set; }

        // public List<CSD.Treasury.Service.Request_PaymentService.Request_Payment> List { get; set; }
    }


    public class SearchModel
    {
        public string MenuTitle { get; set; }
        public int PageIndex { get; set; }
        public int PageSize { get; set; }
        public int MenuPosition { get; set; }
        public SearchModel()
        {
            PageSize = 15;
            PageIndex = 0;
        }
    }

    public class MyModel<T> where T : class
    {
        private bool temp;

        public int PageIndex { get; set; }

        public int PageSize { get; set; }

        public int TotalCount { get; set; }

        public int TotalPages { get; set; }

        public bool HasPreviousPage
        {
            get
            {
                return (PageIndex > 0);
            }
            set
            {
                temp = value;
            }
        }


        public bool HasNextPage
        {
            get
            {
                return (PageIndex + 1 < TotalPages);
            }
            set
            {
                temp = value;
            }
        }


        public List<T> MyList { get; set; }

        public MyModel()
        {

        }

        public MyModel(List<T> source, int pageIndex, int pageSize, int totalCount)
        {
            PageIndex = pageIndex;
            PageSize = pageSize;
            TotalCount = totalCount;
            TotalPages = (int)Math.Ceiling(TotalCount / (double)PageSize);
            if (pageIndex > TotalPages) pageIndex = TotalPages;
            if (source != null)
                MyList = source;
        }
    }

}