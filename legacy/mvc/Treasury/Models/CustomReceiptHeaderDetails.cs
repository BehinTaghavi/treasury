using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;


namespace CSD.Treasury.UI.Models
{
    public class CustomReceiptHeaderDetails
    {
        public decimal ReceiptId { get; set; }
        public decimal ReceiptDetailId { get; set; }
        public string RecieptTypeTitle { get; set; }
        public decimal Price { get; set; }
        public string ExchangeTypeTitle { get; set; }
        public string ChequeNumber { get; set; }
        public string DueDate { get; set; }
        public string LastStateDate { get; set; }
        public string DueDate_en { get; set; }
        public string AssignmentAccountNum { get; set; }
        public string Unique_Account_No_Code { get; set; }
        public string Unique_Account_No { get; set; }
        public string Unique_Account_No_One { get; set; }
        public string Unique_Account_No_Two { get; set; }
        public string PlaningType_Value { get; set; }
        public int AssignmentReceiptNum { get; set; }
        public string AssignmentDate { get; set; }
        public string Receipt_Pay_TypeTitle { get; set; }
        public string PlaningTypeDesc { get; set; }
        public string BehalfTitle { get; set; }
        public int BehalfGroupID { get; set; }
        public int MyProperty { get; set; }
        public string Refrences { get; set; }
        public string UserName { get; set; }
        public string Bank { get; set; }
        public int? BankId { get; set; }
        public int? CashierId { get; set; }
        public string Cashier { get; set; }
        public int PlaningTypeReferenceID { get; set; }
        public int PlaningTypeID { get; set; }
        public decimal StateID { get; set; }
        public int ExchangeTypeID { get; set; }
        public decimal ExchangeRate { get; set; }
        public string ReceiptDate { get; set; }
    }
}