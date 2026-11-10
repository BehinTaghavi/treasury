using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSD.Treasury.Service.Annotation
{
    [MetadataType(typeof(ChequeFormatType_Description_Validation))]
    public partial class ChequeFormatType_Description
    {
        public int ChequeFormatID { get; set; }
    }
    public partial class ChequeFormatType_Description_Validation
    {

    }
}
