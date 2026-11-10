using CSD.Treasury.Resourse;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using CSD.Treasury.Service.Request_Payment_BreakPriceService;

namespace CSD.Treasury.Service.Request_PaymentService
{
    [MetadataType(typeof(Request_Payment_Validation))]
    public partial class Request_Payment
    {
        [Required(ErrorMessageResourceType = typeof(Resources), ErrorMessageResourceName = "UniqueAccountTypeRequired")]
        public string UniqueAccountType { get; set; }

        [Required(ErrorMessageResourceType = typeof(Resources), ErrorMessageResourceName = "BehalfGroupRequired")]
        public string BehalfGropId { get; set; }

        [Required(ErrorMessageResourceType = typeof(Resources), ErrorMessageResourceName = "UniqueAccountTypeRequired")]
        public int Code { get; set; }
        public IEnumerable<HttpPostedFileBase> Files { get; set; }
        public List<Request_Payment_BreakPrice> Request_Payment_BreakPriceLs { get; set; }
        public string Status { get; set; }

    }
    public partial class Request_Payment_Validation
    { 
        [Required(ErrorMessageResourceType = typeof(Resources), ErrorMessageResourceName = "BehalfRequired")]
        public int BehalfID { get; set; } // 

        [DisplayFormat(DataFormatString = "{#,##0.00}", ApplyFormatInEditMode = true)]

        [Required(ErrorMessageResourceType = typeof(Resources), ErrorMessageResourceName = "PayAmountRequired")]
        public decimal PayAmount { get; set; }
    }
}
