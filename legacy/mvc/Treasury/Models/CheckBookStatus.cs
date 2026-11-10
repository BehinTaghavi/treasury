using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;


namespace CSD.Treasury.UI.Models
{
    public class CheckBookStatus 
    {
        public int checkID { get; set; }
        public int DueDate { get; set; }
        public int Price { get; set; }
        public int CheckType { get; set; }
        public string Beneficiaries { get; set; }
        public string CauseOfCancellation { get; set; }
    }
}