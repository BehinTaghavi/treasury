using CSD.Treasury.Resourse;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace CSD.Treasury.UI.Areas.Report.Models
{
    public class BehalfModel
    {
        [Required(ErrorMessageResourceType = typeof(Resources), ErrorMessageResourceName = "Required")]
        public string FromDate { get; set; }
        [Required(ErrorMessageResourceType = typeof(Resources), ErrorMessageResourceName = "Required")]
        public string ToDate { get; set; }
        public string BehalfGroupDescription { get; set; }
        public string BehalfDescription { get; set; }
        public int? BehalfGroupId { get; set; }
        public int ExchangeTypeId { get; set; }
        public int type { get; set; }
    }
}