using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using CSD.Treasury.Resourse;

namespace CSD.Treasury.DataModel
{
    [MetadataType(typeof(Bank_Validation))]
    public partial class Bank
    {

    }
    public partial class Bank_Validation
    {
        public bool HasExchangeTransaction { get; set; }

        [Required(ErrorMessageResourceType = typeof(Resources), ErrorMessageResourceName = "Required")]
        [Display(Name = "شماره حساب")]
        public string Bank_AccountNumber { get; set; }

        [Required(ErrorMessageResourceType = typeof(Resources), ErrorMessageResourceName = "Required")]
        public string BranchCode { get; set; }

        [Required(ErrorMessageResourceType = typeof(Resources), ErrorMessageResourceName = "Required")]
        public string BranchName { get; set; }

        public string BranchName2 { get; set; }
        public string OpeningDate { get; set; }
        [Display(Name = "موجودی اول دوره")]
        public decimal OpeningValue { get; set; }

        public Nullable<int> ExchangeTypeID { get; set; }
        public bool IsActive { get; set; }
        public bool HasBackup { get; set; }
        public Nullable<int> Backup_BankID { get; set; }
        public Nullable<byte> ExchangeType { get; set; }
        [Required(ErrorMessageResourceType = typeof(Resources), ErrorMessageResourceName = "Required")]
        [Display(Name = "معین وصول")]
        public string Account_No { get; set; }

        [Required(ErrorMessageResourceType = typeof(Resources), ErrorMessageResourceName = "Required")]
        [Display(Name = "تفصیل")]
        public string Unique_Account_No { get; set; }
        public string Unique_Account_No_two { get; set; }

        [Required(ErrorMessageResourceType = typeof(Resources), ErrorMessageResourceName = "Required")]
        [Display(Name = "معین واگذاری")]
        public string Assign_Account_No { get; set; }

        [Required(ErrorMessageResourceType = typeof(Resources), ErrorMessageResourceName = "Required")]
        [Display(Name = "معین برگشتی")]
        public string Returns_Account_No { get; set; }
        public int SortOrder { get; set; }
        public bool IS_Facilities { get; set; }
        public Nullable<int> CityID { get; set; }
        public int MaxPermissiveSignatureCount { get; set; }
        public int MinPermissiveSignatureCount { get; set; }
        public bool Deposited { get; set; }
        public decimal ExchangeRate { get; set; }
    }
}
