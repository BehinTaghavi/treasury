using CSD.Treasury.Resourse;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSD.Treasury.DataModel
{
    [MetadataType(typeof(BankCard_Validation))]
    public partial class BankCard
    {

    }
    public partial class BankCard_Validation
    {
        [Required(ErrorMessageResourceType = typeof(Resources), ErrorMessageResourceName = "Required")]      
        public  string Card_AccountNo { get; set; }
        
        [RegularExpression(@"^(?!(\d)\1{9})\d{16}$", ErrorMessage = "حداکثر طول شماره کارت 16 رقم است")]
        [Required(ErrorMessageResourceType = typeof(Resources), ErrorMessageResourceName = "Required")]
        public string Card_NO { get; set; }

        [Required(ErrorMessageResourceType = typeof(Resources), ErrorMessageResourceName = "Required")]
        public string CVV2 { get; set; }

        [Required(ErrorMessageResourceType = typeof(Resources), ErrorMessageResourceName = "Required")]
        public string ExpiryDate { get; set; }

        [Required(ErrorMessageResourceType = typeof(Resources), ErrorMessageResourceName = "Required")]
        public string Unique_Account_No { get; set; }

    }
}
