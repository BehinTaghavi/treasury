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
    [MetadataType(typeof(UserInfo_Validation))]
    public partial class UserInfo
    {
        public bool HasPermissiveSignature { get; set; }       
    }
    public partial class UserInfo_Validation
    {
        //[Required(ErrorMessageResourceType = typeof(Resources), ErrorMessageResourceName = "Required")]
        public string User_FullName { get; set; }

        [Required(ErrorMessageResourceType = typeof(Resources), ErrorMessageResourceName = "Required")]
        public string User_FullName2 { get; set; }

        [Remote("CheckTitle", "User", "Management", ErrorMessageResourceType = typeof(Resources), ErrorMessageResourceName = "Repetitive", HttpMethod = "Post", AdditionalFields = "InitialUserName")]
        [Required(ErrorMessageResourceType = typeof(Resources), ErrorMessageResourceName = "Required")]
        public string UserName { get; set; }

        [Required(ErrorMessageResourceType = typeof(Resources), ErrorMessageResourceName = "Required")]
        public string UserPassword { get; set; }


        //[Required(ErrorMessageResourceType = typeof(Resources), ErrorMessageResourceName = "Required")]
        public int LocationByCostCenterID { get; set; }

        [Required(ErrorMessageResourceType = typeof(Resources), ErrorMessageResourceName = "Required")]
        public int LocationID { get; set; }
        
    }
}
