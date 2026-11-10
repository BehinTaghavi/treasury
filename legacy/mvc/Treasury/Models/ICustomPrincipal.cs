using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace CSD.Treasury.UI
{
    interface ICustomPrincipal: IPrincipal
    {
        int Id { get; set; }
        string UserFullName { get; set; }
        string UserName { get; set; }
        string LocationName { get; set; }
        List<string> Roles { get; set; }
        int BranchId { get; set; }
        string BranchName { get; set; }
    }
}
