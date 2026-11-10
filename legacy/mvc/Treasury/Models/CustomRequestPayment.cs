using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using CSD.Treasury.Service.Request_PaymentService;

namespace CSD.Treasury.UI.Models
{
    public class CustomRequestPayment
    {
        public string UniqAccountTtitle { get; set; }
        public string UniqAccountNum { get; set; }
        public decimal price { get; set; }
        public decimal RequestId { get; set; }
        public string BehlafTitle { get; set; }

    }
}