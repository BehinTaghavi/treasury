using CSD.Treasury.Resourse;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSD.Treasury.Service.Beneficiaries_TELSService
{
    [MetadataType(typeof(Beneficiaries_TELS_Validation))]
    public partial class Beneficiaries_TELS
    {

    }
    public partial class Beneficiaries_TELS_Validation
    {
       
        [Required(ErrorMessageResourceType = typeof(Resources), ErrorMessageResourceName = "Required")]
        [RegularExpression("^0[0-9]{2,4}[0-9]{7,9}$", ErrorMessage = "لطفا عدد وارد نمایید")]
        public string Beneficiar_TEL { get; set; }

    }
}
