using CSD.Treasury.Resourse;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace CSD.Treasury.Service.cmpBranchesService
{
    [MetadataType(typeof(cmpBranch_Validation))]
    public partial class cmpBranch
    {

    }
    public partial class cmpBranch_Validation
    {
        [Remote("CheckTitle", "Branch", "BasicInfo", ErrorMessageResourceType = typeof(Resources), ErrorMessageResourceName = "Repetitive", HttpMethod = "Post", AdditionalFields = "InitialcmpBranchName")]
        [Required(ErrorMessageResourceType = typeof(Resources), ErrorMessageResourceName = "Required")]
        public int cmpBranchName { get; set; }

        [Required(ErrorMessageResourceType = typeof(Resources), ErrorMessageResourceName = "Required")]
        public int cmpBranchName2 { get; set; }


        [Required(ErrorMessageResourceType = typeof(Resources), ErrorMessageResourceName = "Required")]
        public int cmpBranchAddress { get; set; }
    }
}
