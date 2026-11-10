using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using CSD.Treasury.Service.Receipt_Detail_BreakPriceService;

namespace CSD.Treasury.UI.Models
{
    public class Custom_Details
    {
        public decimal ReceiptDetailsid { get; set; }
        //
        public int? BankID { get; set; }
        public int? mainBankID { get; set; }
        public string Bank_AccountNo { get; set; }
        public string Bank_BranchCode { get; set; }
        public string Bank_BranchDesc { get; set; }
        public int CashDeskID { get; set; }
        public int BehalfGroupId { get; set; }
        public int BehalfID { get; set; }
        public Int64 ChequePrice_Rial { get; set; }
        public decimal ExchangeAmount { get; set; }
        public int? EquivalencePrice { get; set; }
        public string Cheque_Number { get; set; }
        public string Cheque_OwnerName { get; set; }
        public int? CityID { get; set; }
        public string DueDate { get; set; }
        public int? ExchangeRate { get; set; }
        public int? ExchangeTypeID { get; set; }
        public string Unique_Account_no { get; set; }
        public string Unique_Account_no_Two { get; set; }
        public int? provinceID { get; set; }
        public List<Reference> Refrences { get; set; }
        public List<int> OthersPropertyLs { get; set; }
        public List<Custom_Reciept_OtherPropery_Detail> OtherPropertyDetailsList { get; set; }
        public List<CSD.Treasury.DataModel.Receipt_Detail_BreakPrice> BreakPriceLs { get; set; }

    }
}