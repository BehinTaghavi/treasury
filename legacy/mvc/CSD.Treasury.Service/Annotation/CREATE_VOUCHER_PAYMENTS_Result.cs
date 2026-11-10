using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSD.Treasury.Service.ProceduresDB
{
    [MetadataType(typeof(CREATE_VOUCHER_PAYMENTS_Result_Validation))]
    public partial class CREATE_VOUCHER_PAYMENTS_Result
    {
        public string Unique_Account_No_Title { get; set; }
        public string Unique_Account_No_two_Title { get; set; }
        public string Account_Title { get; set; }
    }
    public partial class CREATE_VOUCHER_PAYMENTS_Result_Validation
    {

    }
}
