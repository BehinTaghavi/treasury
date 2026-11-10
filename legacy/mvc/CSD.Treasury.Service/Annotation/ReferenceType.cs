using CSD.Treasury.Resourse;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace CSD.Treasury.Service.ReferenceTypeService
{
    [MetadataType(typeof(ReferenceType_Validation))]
    public partial class ReferenceType
    {

    }
    public partial class ReferenceType_Validation
    {
        [Remote("CheckTitle", "RefrenceType", "BasicInfo", ErrorMessageResourceType = typeof(Resources), ErrorMessageResourceName = "Repetitive", HttpMethod = "Post",AdditionalFields = "InitialreferenceTypeDescription")]
        [Required(ErrorMessageResourceType = typeof(Resources), ErrorMessageResourceName = "Required")]
        public string referenceTypeDescription { get; set; }

        [Required(ErrorMessageResourceType = typeof(Resources), ErrorMessageResourceName = "Required")]
        public string referenceTypeDescription2 { get; set; }
    }
}
