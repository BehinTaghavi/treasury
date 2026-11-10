using CSD.Treasury.Resourse;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace CSD.Treasury.DataModel
{
    [MetadataType(typeof(CashDesk_Validation))]
    public partial class CashDesk
    {
        public bool arzi { get; set; }
    }
    public partial class CashDesk_Validation
    {
        
        [Required(ErrorMessageResourceType = typeof(Resources), ErrorMessageResourceName = "Required")]
        [Display(Name = "شعبه")]
        public int cmpBranchID { get; set; }

        //[Required(ErrorMessageResourceType = typeof(Resources), ErrorMessageResourceName = "Required")]
        [Display(Name = "ارز")]
        public int? ExchangeTypeID { get; set; }

        [Required(ErrorMessageResourceType = typeof(Resources), ErrorMessageResourceName = "Required")]
        [Display(Name = "حساب معین")]
        public string Account_No { get; set; }

        [Required(ErrorMessageResourceType = typeof(Resources), ErrorMessageResourceName = "Required")]
        [Display(Name = "نام صندوق")]
        public string CashDeskDescription { get; set; }

        //[Remote("CheckUniqueAcount", "CashDesks", "BasicInfo", HttpMethod = "Post", AdditionalFields = "InitialUnique_Account_No",ErrorMessage ="این تفصیل تکراری است")]
        [Required(ErrorMessageResourceType = typeof(Resources), ErrorMessageResourceName = "Required")]
        [Display(Name = "تفصیل اول")]
        public string Unique_Account_No { get; set; }

        [Display(Name = "تفصیل دوم")]
        public string Unique_Account_No_two { get; set; }


        [Display(Name = "فعال")]
        public bool IsActive { get; set; }

        //[Required(ErrorMessageResourceType = typeof(Resources), ErrorMessageResourceName = "Required")]
        public int? ExchangeTypeValues { get; set; }

        [Display(Name = "چک")]
        public bool IsCheque { get; set; }

    }
}
