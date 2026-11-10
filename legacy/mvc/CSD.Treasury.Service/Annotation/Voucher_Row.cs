using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSD.Treasury.Service.Vocher_RowService
{
    [MetadataType(typeof(Voucher_Row_Validation))]
    public partial class Voucher_Row
    {
        public string Unique_Account_No_Title { get; set; }
        public string Unique_Account_No_two_Title { get; set; }
    }
    public partial class Voucher_Row_Validation
    {

    }
}
