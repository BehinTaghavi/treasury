using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using CSD.Treasury.Service.PettyCashService;

namespace CSD.Treasury.UI.Models
{
    public class CustomPettyCash_TransactionHeader
    {
        public List<PettyCash> PettyCashLs { get; set; }
        public int SelectedPettyCashId { get; set; }
        public string UniqueAccountTitle1 { get; set; }
        public string UniqueAccountTitle2 { get; set; }
        public int PettyCash_Ammount { get; set; }
        public int Settle_Limit { get; set; }
        public int PayCeiling { get; set; }
        public string PettyCashTransactionDate { get; set; }
        public int lastHeaderId { get; set; }
        public int PettyCashID { get; set; }
        public bool ShowVakhahiBtn { get; set; }
        public decimal Mande { get; set; }

    }
}