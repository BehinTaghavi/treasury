using CSD.Treasury.Resourse;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSD.Treasury.Service.BeneficiariesDetailInfoService
{
    [MetadataType(typeof(BeneficiariesDetailInfo_Validation))]
    public partial class BeneficiariesDetailInfo
    {

    }
    public partial class BeneficiariesDetailInfo_Validation
    {
        [Required(ErrorMessageResourceType = typeof(Resources), ErrorMessageResourceName = "Required")]
        public int  BankID { get; set; }

        [RegularExpression(@"^IR\d{24}$", ErrorMessage = "شماره شبا با IR شروع شده و شامل 24 عدد می باشد.")]
        //[RegularExpression("[a-zA-Z]{2}[0-9]{2}[a-zA-Z0-9]{4}[0-9]{7}([a-zA-Z0-9]?){0,16}", ErrorMessage = "لطفا درست وارد نمایید")]
        public int Bank_ShebaNo { get; set; }

        [Required(ErrorMessageResourceType = typeof(Resources), ErrorMessageResourceName = "Required")]
        public int Bank_BranchName { get; set; }

 
        

    }
}
