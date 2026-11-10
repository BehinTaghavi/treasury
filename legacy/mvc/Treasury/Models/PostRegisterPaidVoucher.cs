using CSD.Treasury.Resourse;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace CSD.Treasury.UI.Models
{
    public class PostRegisterPaidVoucher
    {
        [RegularExpression(@"^[1-4]\d{3}\/((0[1-6]\/((3[0-1])|([1-2][0-9])|(0[1-9])))|([1-6]\/((3[0-1])|([1-2][0-9])|(0[1-9])))|((1[0-2]|(0[7-9])|([1-9])|(0[1-9]))\/(30|([1-2][0-9])|(0[1-9])|([1-9]))))$",ErrorMessage ="تاریخ اشتباه وارد شده است")]
        [Required(ErrorMessageResourceType = typeof(Resources), ErrorMessageResourceName = "Required")]
        public string startDate { get; set; }

        [RegularExpression(@"^[1-4]\d{3}\/((0[1-6]\/((3[0-1])|([1-2][0-9])|(0[1-9])))|([1-6]\/((3[0-1])|([1-2][0-9])|(0[1-9])))|((1[0-2]|(0[7-9])|([1-9])|(0[1-9]))\/(30|([1-2][0-9])|(0[1-9])|([1-9]))))$", ErrorMessage = "تاریخ اشتباه وارد شده است")]
        [Required(ErrorMessageResourceType = typeof(Resources), ErrorMessageResourceName = "Required")]
        public string endDate { get; set; }
        public bool rebuilding { get; set;}
    }
}