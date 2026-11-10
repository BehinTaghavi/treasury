using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using CSD.Treasury.Service.Receipt_Pay_TypeService;
using CSD.Treasury.Service.cmpBranchesService;
using CSD.Treasury.Service.Receipt_Pay_TypeDetailService;

namespace CSD.Treasury.UI.Models
{
    public class Custom_Reciept_Header
    {
        public int ReceiptPayTypeId { get; set; }
        public string ReceiptPayTypeTitle { get; set; }
        public string ReceiptDate { get; set; }
        public decimal ReceiptId { get; set; }
        public bool Arzi { get; set; }
        public int BranchId { get; set; }
        public decimal? CashDeskID { get; set; }
        public CSD.Treasury.DataModel.Bank bank { get; set; }
        public  Service.BasicBanksService.BasicBank basicbank { get; set; }
        public List<Service.cmpBranchesService.cmpBranch> BranchLs { get; set; }
        public List<Service.Receipt_Pay_TypeDetailService.Receipt_Pay_TypeDetail> ReceiptPayTypeDetailLs { get; set; }

    }
}