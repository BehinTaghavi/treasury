using CSD.Treasury.Resourse;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSD.Treasury.DataModel
{
    [MetadataType(typeof(ExchangeRate_Validation))]
    public partial class ExchangeRate
    {

    }
    public partial class ExchangeRate_Validation
    {
        [Required(ErrorMessageResourceType = typeof(Resources), ErrorMessageResourceName = "Required")]
        [Display(Name = "نوع ارز")]
        public int ExchangeTypeID { get; set; }

        [Required(ErrorMessageResourceType = typeof(Resources), ErrorMessageResourceName = "Required")]
        [Display(Name = "تاریخ")]
        public string ExchangeDateRate { get; set; }

        [Required(ErrorMessageResourceType = typeof(Resources), ErrorMessageResourceName = "Required")]
        [Display(Name = "مبلغ رسمی")]
        public int FormalPrice { get; set; }

        [Required(ErrorMessageResourceType = typeof(Resources), ErrorMessageResourceName = "Required")]
        [Display(Name = "مبلغ غیررسمی")]
        public int InformalPrice { get; set; }
    }
}
