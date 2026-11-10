using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CSD.Treasury.UI.Models
{
    public class BankBalanceModel
    {
        public string Date { get; set; }
        public int BankId { get; set; }
        public int showSum { get; set; }
        public bool showInput { get; set; }
        public bool Short { get; set; }

        public BankBalanceModel() {
            showInput = true;
        }
    }
}