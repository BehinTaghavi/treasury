using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSD.Treasury.DataModel
{
    [MetadataType(typeof(Beneficiary_Validation))]
    public partial class Beneficiary
    {

    }
    public partial class Beneficiary_Validation
    {
        [RegularExpression(@"^(?!(\d)\1{9})\d{11}$", ErrorMessage = "شناسه ملی نامعتبر است")]
        public string ShenaseMelli { get; set; }

        [RegularExpression(@"^(?!(\d)\1{9})\d{12}$", ErrorMessage = "کد اقتصادی نامعتبر است")]
        public string CodeEghtesadi { get; set; }
        public string ShomareyeSabt { get; set; }
        public string AddressePosti { get; set; }

        [Display(Name = "کد پستی")]
        [DataType(DataType.PostalCode)]
        [RegularExpression(@"\b(?!(\d)\1{3})[13-9]{4}[1346-9][013-9]{5}\b", ErrorMessage ="کد پستی شما نادرست می‌باشد")]
        public string CodePosti { get; set; }

        [RegularExpression(@"^(?!(\d)\1{9})\d{10}$", ErrorMessage = "کدملی نامعتبر است")]
        public string CodeMeli { get; set; }
    }
}
