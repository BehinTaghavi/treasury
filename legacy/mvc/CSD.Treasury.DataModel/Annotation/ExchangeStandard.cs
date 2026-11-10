using CSD.Treasury.Resourse;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSD.Treasury.DataModel
{
    [MetadataType(typeof(ExchangeStandard_Validation))]
    public partial class ExchangeStandard
    {

    }
    public partial class ExchangeStandard_Validation
    {
        [Required(ErrorMessageResourceType = typeof(Resources), ErrorMessageResourceName = "Required")]
        [Display(Name = "سال مالی")]
        public int FiscalYear { get; set; }

        [Required(ErrorMessageResourceType = typeof(Resources), ErrorMessageResourceName = "Required")]
        [Display(Name = "نوع ارز")]
        public int ExchangeTypeID { get; set; }

        [Required(ErrorMessageResourceType = typeof(Resources), ErrorMessageResourceName = "Required")]
        [Display(Name = "مبلغ")]
        public int Price { get; set; }
    }
}