using CSD.Treasury.Resourse;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace CSD.Treasury.Service.BehalfsService
{
    [MetadataType(typeof(Behalf_Validation))]
    public partial class Behalf
    {

    }
    public partial class Behalf_Validation
    {
        [Remote("CheckTitle", "Behalf", "BasicInfo", ErrorMessageResourceType = typeof(Resources), ErrorMessageResourceName = "Repetitive", HttpMethod = "Post",AdditionalFields = "InitialBehalfDescription,BehalfGroupID")]
        [Required(ErrorMessageResourceType = typeof(Resources), ErrorMessageResourceName = "Required")]
        public string BehalfDescription { get; set; }

        [Required(ErrorMessageResourceType = typeof(Resources), ErrorMessageResourceName = "Required")]
        public string BehalfDescription2 { get; set; }
    }
}
