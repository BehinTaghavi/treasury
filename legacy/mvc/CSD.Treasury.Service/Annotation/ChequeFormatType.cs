using CSD.Treasury.Resourse;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSD.Treasury.Service.ChequeFormatTypesService
{
    [MetadataType(typeof(ChequeFormatType_Validation))]
    public partial class ChequeFormatType
    {
        public string ChequeFormatType_Description1 { get; set; }
        public string BranchName { get; set; }
        public string BasicBankDescription { get; set; }
        public string BankAccountNumber { get; set; }
    }
    public partial class ChequeFormatType_Validation
    {

        [Required(ErrorMessageResourceType = typeof(Resources), ErrorMessageResourceName = "Required")]
        public string BankID { get; set; }
        [Required(ErrorMessageResourceType = typeof(Resources), ErrorMessageResourceName = "Required")]
        public string FormatDescriptionID_Main { get; set; }
        [Required(ErrorMessageResourceType = typeof(Resources), ErrorMessageResourceName = "Required")]
        public string FormatDescription { get; set; }


    }
}
