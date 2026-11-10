using CSD.Treasury.Resourse;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace CSD.Treasury.UI.Areas.Payment.Models
{
    public class ChequeDetails
    {
        public decimal RequestId { get; set; }
        public int ChequeFormatID { get; set; }
        [Required(ErrorMessageResourceType = typeof(Resources), ErrorMessageResourceName = "Required")]
        public string DueDate { get; set; }
        public string TableName { get; set; }
        public bool RemoveRemit { get; set; }
        public bool RemoveOrdersFor { get; set; }
        public bool ShowDescription { get; set; }
        public string Description { get; set; }
        //دروجه حامل
        public bool Bearer { get; set; }
        //دروجه شرکت
        public bool PayToCompany { get; set; }
        public bool Unique_Account_No2 { get; set; }
        public string Unique_Account_No2Val { get; set; }
        public bool WithoutCrossing { get; set; }
        public bool checkIsUnderwritten { get; set; }
        public bool beneficiaryandbehalf { get; set; }
        public string behalfValue { get; set; }
        public bool NonTransferable { get; set; }
        public bool CurrentAccount { get; set; }
        public int CurrentAccountId { get; set; }
        public bool ShebaAccount { get; set; }
        public int ShebaAccountId { get; set; }
        public bool BranchExporterReceivable { get; set; }
        public bool ApproveIssuance { get; set; }
        public int BeneficiarDetailID { get; set; }
        public string meliCode { get; set; }
        public string PayTo { get; set; }
        [Required]       
        public decimal ExchangeRate { get; set; }
        public ChequeDetails(decimal requestId)
        {
            this.RequestId = requestId;
        }
        public ChequeDetails()
        {
        }
    }
}