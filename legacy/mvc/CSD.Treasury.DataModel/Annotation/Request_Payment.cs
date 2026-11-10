using CSD.Treasury.Resourse;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSD.Treasury.DataModel
{
    [MetadataType(typeof(Request_Payment_Validation))]
    public partial class Request_Payment
    {

    }
    public partial class Request_Payment_Validation
    {
       
        [Display(Name = "تاریخ برداشت")]
        public string DueDate { get; set; }

        [Required(ErrorMessageResourceType = typeof(Resources), ErrorMessageResourceName = "Required")]
        public decimal PayAmount { get; set; }

        [Required(ErrorMessageResourceType = typeof(Resources), ErrorMessageResourceName = "Required")]
        public int BehalfID { get; set; }

    }
}
