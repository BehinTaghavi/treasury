using CSD.Treasury.DataModel;
using CSD.Treasury.Service.Reciept_OtherPropery_DetailService;
using CSD.Treasury.Service.Receipt_Detail_BreakPriceService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CSD.Treasury.UI.Models
{
    public class Custom_Reciept_Details
    {
        public decimal ReceiptDetailsid { get; set; }
        public int nerkh { get; set; }
        public string Cheque_Number { get; set; }
        public string DueDate { get; set; }
        public string Unique_Account_No { get; set; }
        public string Unique_Account_Title { get; set; }
        public int CashDeskID { get; set; }
        public decimal ChequePrice_Rial { get; set; }
        public string Bank_AccountNo { get; set; }
        public int? BankID { get; set; }
        public string BankName { get; set; }
        public string Cheque_OwnerName { get; set; }
        public string Bank_BranchDesc { get; set; }
        public string Bank_BranchCode { get; set; }
        public int ProvinceID { get; set; }
        public int CityID { get; set; }
        public string CityName { get; set; }
        public List<Service.CityService.City> CityLs { get; set; }
        public decimal ExchangeRate { get; set; }
        public decimal ExchangeAmount { get; set; }
        public int ExchangeTypeID { get; set; }
        public int BehalfID { get; set; }
        public string BehalfName { get; set; }
        //public List<Service.BehalfsService.Behalf> BehalfList { get; set; }
        public List<int> OtherProperyLS { get; set; }
        public List<Custom_Reciept_OtherPropery_Detail> OtherPropertyDetailsList { get; set; }               
        // Rahimi
        //public List<Service.Receipt_Detail_BreakPriceService.Receipt_Detail_BreakPrice> BreakPriceLs { get; set; }
        public List<DataModel.Receipt_Detail_BreakPrice> BreakPriceLs { get; set; }
        public List<int> ProperySelected { get; set; }
        public int BehalfGroupId { get; set; }
        public string BehalfGroupTitle { get; set; }
        public List<Service.BehalfsService.Behalf> BehalfLs { get; set; }
        public List<Reference> Refrences { get; set; }
        public UCItem UniqueAccount { get; set; }
        public UCItem UniqueAccount2 { get; set; }
        //نوع برنامه ریزی
        public int PlaningTypeID { get; set; }
        //نوع دریافت 
        public int RecieptTypeID { get; set; }
        public int BehalfGroupAccount { get; set; }

    }
    public class Reference
    {
        public int Id { get; set; }
        public string Value { get; set; }
        public string Title { get; set; }
    }
}