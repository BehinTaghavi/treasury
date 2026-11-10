using CSD.Treasury.UI.Areas.Payment.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CSD.Treasury.UI.Models
{
    public class Issuance
    {
        public string Image { get; set; }
        public string Extension { get; set; }
        public int Height { get; set; }
        public int Width { get; set; }
        public ChequeDetails ChequeDetails { get; set; }
        public List<Service.ChequePrintService.ChequeElement> Elements { get; set; }
    }
}