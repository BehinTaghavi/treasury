using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CSD.Treasury.Service.CashiersService;
using System.ComponentModel.DataAnnotations;
using CSD.Treasury.Resourse;
using System.Web.Mvc;

namespace CSD.Treasury.Service.CashiersService
{
    [MetadataType(typeof(Cashier_Validation))]
    public partial class Cashier
    {

    }
    public partial class Cashier_Validation
    {
        [Remote("CheckTitle", "Cashier", "BasicInfo", ErrorMessageResourceType = typeof(Resources), ErrorMessageResourceName = "Repetitive", HttpMethod = "Post", AdditionalFields = "InitialAccount_no")]
        [Required(ErrorMessageResourceType = typeof(Resources), ErrorMessageResourceName = "Required")]
        public string Account_no { get; set; }

        [Required(ErrorMessageResourceType = typeof(Resources), ErrorMessageResourceName = "Required")]
        public string Unique_Account_No { get; set; }

        [Required(ErrorMessageResourceType = typeof(Resources), ErrorMessageResourceName = "Required")]
        public string UserID { get; set; }
        

    }
}
