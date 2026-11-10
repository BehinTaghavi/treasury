using CSD.Treasury.Resourse;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace CSD.Treasury.Service.PettyCashService
{
    [MetadataType(typeof(PettyCash_Validation))]
    public partial class PettyCash
    {
        public string UniqueAccountTitle { get; set; }
        public decimal Mande { get; set; }
    }
    public partial class PettyCash_Validation
    {
        [Remote("CheckTitle", "PettyCash", "BasicInfo", ErrorMessageResourceType = typeof(Resources), ErrorMessageResourceName = "Repetitive", HttpMethod = "Post",AdditionalFields = "InitialPettyCashName")]
        [Required(ErrorMessageResourceType = typeof(Resources), ErrorMessageResourceName = "Required")]
        public string PettyCashName { get; set; }

        [Required(ErrorMessageResourceType = typeof(Resources), ErrorMessageResourceName = "Required")]
        public string cmpBranchID { get; set; }

        [Required(ErrorMessageResourceType = typeof(Resources), ErrorMessageResourceName = "Required")]
        [DisplayFormat(DataFormatString = "{0:N0}", ApplyFormatInEditMode = true)]
        [RegularExpression("^[0-9]*$", ErrorMessage = "لطفا عدد وارد نمایید")]
        public int  PettyCash_Ammount { get; set; }

        [DisplayFormat(DataFormatString = "{0:N0}", ApplyFormatInEditMode = true)]
        [RegularExpression("^[0-9]*$", ErrorMessage = "لطفا عدد وارد نمایید")]
        public int Settle_Limit { get; set; }

        [DisplayFormat(DataFormatString = "{0:N0}", ApplyFormatInEditMode = true)]
        [RegularExpression("^[0-9]*$", ErrorMessage = "لطفا عدد وارد نمایید")]
        public int PayCeiling { get; set; }


    }
}
