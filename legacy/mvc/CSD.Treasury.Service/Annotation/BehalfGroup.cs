using CSD.Treasury.Common.Enumeration;
using CSD.Treasury.Resourse;
using CSD.Treasury.Service.ExchangeTypesService;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace CSD.Treasury.Service.BehalfGroupService
{
    [MetadataType(typeof(BehalfGroup_Validation))]
    public partial class BehalfGroup
    {    
       // [Required(ErrorMessageResourceType = typeof(Resources), ErrorMessageResourceName = "Required")]
        public string Reference { get; set; }
    }
    public partial class BehalfGroup_Validation
    {
        [Display(ResourceType = typeof(Resources), Name = "ReceiptPayType")]
        [Required(ErrorMessageResourceType = typeof(Resources), ErrorMessageResourceName = "Required")]
        public int Receipt_Pay_TypeID { get; set; }

        //[Remote("CheckTitle", "BehalfGroup", "BasicInfo", ErrorMessageResourceType = typeof(Resources), ErrorMessageResourceName = "Repetitive", HttpMethod = "Post",AdditionalFields = "InitialBehalfGroupDescription")]
        [Required(ErrorMessageResourceType = typeof(Resources), ErrorMessageResourceName = "Required")]
        public string BehalfGroupDescription { get; set; }

        //[Required(ErrorMessageResourceType = typeof(Resources), ErrorMessageResourceName = "Required")]
        public string BehalfGroupDescription2 { get; set; }

        [Required(ErrorMessageResourceType = typeof(Resources), ErrorMessageResourceName = "Required")]
        public string BehalfGroup_Account { get; set; }



    }
}
