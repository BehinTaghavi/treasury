using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CSD.Treasury.UI.Models
{
    public class Custom_Reciept
    {
        public int HeaderId { get; set; }
        public int ReceiptDetailId { get; set; }
        public  string ReceiptDate { get; set; }
        public int ReceiptPayTypeId { get; set; }
        public bool isRiali { get; set; }
        public List<Custom_Details> DetailsLs { get; set; }
        

    }
}