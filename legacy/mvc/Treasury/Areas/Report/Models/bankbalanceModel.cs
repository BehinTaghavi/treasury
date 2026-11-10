using CSD.Treasury.DataModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using CSD.Treasury.Service.ProceduresDB;

namespace CSD.Treasury.UI.Areas.Report.Models
{
    public class bankbalanceModel
    {
        public string BehalfGroupDescription { get; set; }
        public    List<GetBankBalance_Result> ls { get; set; }
    }
}