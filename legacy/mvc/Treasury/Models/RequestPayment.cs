using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CSD.Treasury.UI.Models
{
    public class RequestPayment
    {
        public decimal RequestID { get; set; }
        public int BehalfID { get; set; }
        public int Pay_TypeDetailID_source { get; set; }
        public string Unique_Account_No { get; set; }

        public bool Confirmed { get; set; }
        public bool ConfirmPayment { get; set; }
        public bool Paid { get; set; }
        public bool Delivered { get; set; }
    }
}