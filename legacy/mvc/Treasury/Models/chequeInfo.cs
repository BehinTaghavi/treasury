using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CSD.Treasury.UI.Models
{
    public class chequeInfo
    {
        public int chequeId { get; set; }
        public int ChequeState { get; set; }
        public int ReceiptDetailId { get; set; }
        public string Account_No { get; set; }
      
        public string StateDate { get; set; }
        public int BankId { get; set; }
        public int CashierID { get; set; }
        public string PlaningTypeValue { get; set; }
        /// <summary>
        /// تفصیل
        /// </summary>
        public string unique_account1 { get; set; }
        public string Unique_Account_No_Code { get; set; }
        public string unique_account2 { get; set; }

        public int State { get; set; }
        public string DueDate { get; set; }
        public int BehalfGroupID { get; set; }
        public int BehalfID { get; set; }
    }
}