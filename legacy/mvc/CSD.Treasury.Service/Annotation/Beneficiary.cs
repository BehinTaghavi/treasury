using CSD.Treasury.Resourse;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace  CSD.Treasury.Service.BeneficiaryService
{
    [MetadataType(typeof(Beneficiary_Validation))]
    public partial class Beneficiary
    {
        [RegularExpression(@"^(?!(\d)\1{9})\d{10}$", ErrorMessage = "لطفا کدملی را به درستی وارد نمایید")]
        public string CodeMeli { get; set; }
    }
    public partial class Beneficiary_Validation
    {
      
        [Required(ErrorMessageResourceType = typeof(Resources), ErrorMessageResourceName = "Required")]
        public string Unique_Account_No { get; set; }

        [Remote("CheckTitle", "Beneficiaries", "BasicInfo", ErrorMessageResourceType = typeof(Resources), ErrorMessageResourceName = "Repetitive", HttpMethod = "Post",AdditionalFields = "InitialLegalAccountName")]
        [Required(ErrorMessageResourceType = typeof(Resources), ErrorMessageResourceName = "Required")]
        public string LegalAccountName { get; set; }
        [RegularExpression(@"^(?!(\d)\1{9})\d{11}$", ErrorMessage = "لطفا شناسه ملی را به درستی وارد نمایید")]
        public string ShenaseMelli { get; set; }    

        [RegularExpression(@"[0-9]+", ErrorMessage = "فقط عدد وارد نمایید")]
        public string CodeEghtesadi { get; set; }

        [RegularExpression(@"[0-9]+", ErrorMessage = "فقط عدد وارد نمایید")]
        public string ShomareyeSabt { get; set; }
    }
}
