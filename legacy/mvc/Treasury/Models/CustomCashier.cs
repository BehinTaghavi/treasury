using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using CSD.Treasury.Service.CashiersService;

namespace CSD.Treasury.UI.Models
{
    public class CustomCashier: Cashier
    {
        public string CashierName { get; set; }
    }
}