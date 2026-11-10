using CSD.Treasury.Resourse;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace CSD.Treasury.Service.ProvinceService
{
    [MetadataType(typeof(Province_Validation))]
    public partial class Province
    {

    }
    public partial class Province_Validation
    {
        [Remote("CheckProvinceTitle", "Region", "BasicInfo", ErrorMessageResourceType = typeof(Resources), ErrorMessageResourceName = "Repetitive", HttpMethod = "Post",AdditionalFields = "InitialProvinceDescription")]
        [Required(ErrorMessageResourceType = typeof(Resources), ErrorMessageResourceName = "Required")]
        public string ProvinceDescription { get; set; }

        [Required(ErrorMessageResourceType = typeof(Resources), ErrorMessageResourceName = "Required")]
        public string ProvinceDescription2 { get; set; }
    }
}
