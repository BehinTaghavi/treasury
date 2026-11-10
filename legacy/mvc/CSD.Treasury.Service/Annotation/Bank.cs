using CSD.Treasury.Resourse;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace CSD.Treasury.Service.BankService
{
    [MetadataType(typeof(Bank_Validation))]
    public partial class Bank
    {

    }
    public partial class Bank_Validation
    {
        [Required(ErrorMessageResourceType = typeof(Resources), ErrorMessageResourceName = "Required")]
        public int BasicBankID { get; set; }

        [Required(ErrorMessageResourceType = typeof(Resources), ErrorMessageResourceName = "Required")]
        public string Bank_AccountNumber { get; set; }

        [Required(ErrorMessageResourceType = typeof(Resources), ErrorMessageResourceName = "Required")]
        [RegularExpression("^[0-9]*$", ErrorMessage = "لطفا عدد وارد نمایید")]
        public decimal OpeningValue { get; set; }

        [Required(ErrorMessageResourceType = typeof(Resources), ErrorMessageResourceName = "Required")]
        public int SortOrder { get; set; }

        [Remote("CheckTitle", "Banks", "BasicInfo", ErrorMessageResourceType = typeof(Resources), ErrorMessageResourceName = "Repetitive", HttpMethod = "Post", AdditionalFields = "InitialBranchCode,BasicBankID")]
        [Required(ErrorMessageResourceType = typeof(Resources), ErrorMessageResourceName = "Required")]
        public string BranchCode { get; set; }

       
        [Required(ErrorMessageResourceType = typeof(Resources), ErrorMessageResourceName = "Required")]
        public string BranchName { get; set; }

        [Required(ErrorMessageResourceType = typeof(Resources), ErrorMessageResourceName = "Required")]
        public string OpeningDate { get; set; }

        [Required(ErrorMessageResourceType = typeof(Resources), ErrorMessageResourceName = "Required")]
        public string Account_No { get; set; }

        [Required(ErrorMessageResourceType = typeof(Resources), ErrorMessageResourceName = "Required")]
        public string Assign_Account_No { get; set; }

        [Required(ErrorMessageResourceType = typeof(Resources), ErrorMessageResourceName = "Required")]
        public string Returns_Account_No { get; set; }

        [Remote("CheckUniqueAcount", "Banks", "BasicInfo", ErrorMessageResourceType = typeof(Resources), ErrorMessageResourceName = "Repetitive", HttpMethod = "Post", AdditionalFields = "InitialUnique_Account_No,BasicBankID")]
        public string Unique_Account_No { get; set; }


        //[Required(ErrorMessageResourceType = typeof(Resources), ErrorMessageResourceName = "Required")]
        //public decimal OpeningValue { get; set; }

    }
}
