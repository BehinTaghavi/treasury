using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CSD.Treasury.UI.Models
{
    public class Dupplication
    {
        public int requestId { get; set; }
        public  List<RequestObj> list { get; set; }
    }
    public class RequestObj
    {
        public decimal requestId { get; set; }
        public string uniqueAccountCode { get; set; }
        public decimal price { get; set; }
        public string BehalfTitle { get; set; }
        public string RequestDate { get; set; }
        public int? ExchangeTypeID { get; set; }
        public string Exchange { get; set; }
    }
}