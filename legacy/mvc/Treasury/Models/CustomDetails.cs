using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CSD.Treasury.UI.Models
{
    public class CustomDetails
    {
        public string PayDate { get; set; }
        public int BehalfGroupID { get; set; }
        public int ReferenceID { get; set; }
        public int value { get; set; }
        public int BehalfID { get; set; }
        public string UniqueAccount1 { get; set; }
        public string UniqueAccount2 { get; set; }
        public string UniqueAccountH2 { get; set; }
        public int Price { get; set; }
        public bool IsAccept { get; set; }
        public bool IsVakhahi { get; set; }
        public string EllateVakhahi { get; set; }
        public bool IsAcceptVakhahi { get; set; }
        public string AcceptVakhahiDate { get; set; }
    }
}