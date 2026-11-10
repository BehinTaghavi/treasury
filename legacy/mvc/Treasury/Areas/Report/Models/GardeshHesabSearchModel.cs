using CSD.Treasury.Resourse;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace CSD.Treasury.UI.Areas.Report.Models
{
    public class GardeshHesabSearchModel
    {
        public int BasicBank { get; set; }
        public int BankId { get; set; }
        public int BankType { get; set; }
        //public List<int> BasicBank { get; set; }
        //public List<int> BankId { get; set; }
        [Required(ErrorMessageResourceType = typeof(Resources), ErrorMessageResourceName = "Required")]
        public string BankIdList { get; set; }
        public string UniqueAccountNO_Ls { get; set; }
        public string UniqueAccountNO { get; set; }
        [Required(ErrorMessageResourceType = typeof(Resources), ErrorMessageResourceName = "Required")]
        public  string FromDate { get; set; }
        [Required(ErrorMessageResourceType = typeof(Resources), ErrorMessageResourceName = "Required")]
        public string ToDate { get; set; }
        public bool IsRial { get; set; }
        public bool ShowSum { get; set; }
        public bool Seporde { get; set; }
       
    }
}