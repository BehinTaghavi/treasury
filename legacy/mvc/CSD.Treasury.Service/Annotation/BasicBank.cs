using CSD.Treasury.Resourse;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace  CSD.Treasury.Service.BasicBanksService
{
    [MetadataType(typeof(BasicBank_Validation))]
    public partial class BasicBank
    {

    }
    public partial class BasicBank_Validation
    {
        [Remote("CheckTitle", "BasicBank", "BasicInfo", ErrorMessageResourceType = typeof(Resources), ErrorMessageResourceName = "Repetitive", HttpMethod = "Post",AdditionalFields = "InitialBasicBankDescription")]
        [Required(ErrorMessageResourceType = typeof(Resources), ErrorMessageResourceName = "Required")]
        public string BasicBankDescription { get; set; }

        [Required(ErrorMessageResourceType = typeof(Resources), ErrorMessageResourceName = "Required")]
        public string BasicBankDescription2 { get; set; }

    }
}
