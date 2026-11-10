using CSD.Treasury.Resourse;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;


namespace CSD.Treasury.Service.ExchangeTypesService
{
    [MetadataType(typeof(ExchangeType_Validation))]
    public partial class ExchangeType
    {

    }
    public partial class ExchangeType_Validation
    {
        [Remote("CheckTitle", "ExchangeType", "BasicInfo", ErrorMessageResourceType = typeof(Resources), ErrorMessageResourceName = "Repetitive", HttpMethod = "Post", AdditionalFields = "InitialExchangeTypeDescription")]
        [Display(ResourceType = typeof(Resources), Name = "ExchangeType")]
        [Required(ErrorMessageResourceType = typeof(Resources), ErrorMessageResourceName = "Required")]
        public int ExchangeTypeDescription { get; set; }

        [Display(ResourceType = typeof(Resources), Name = "ExchangeType")]
       
        [Required(ErrorMessageResourceType = typeof(Resources), ErrorMessageResourceName = "Required")]
        public int ExchangeTypeDescription2 { get; set; }
    }
}
