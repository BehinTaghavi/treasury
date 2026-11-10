using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using CSD.Treasury.DataModel;

namespace CSD.Treasury.UI.Models
{
    public class CustomPrincipalSerializeModel
    {
        public int Id { get; set; }
        public string UserFullName { get; set; }
        public string LocationName { get; set; }
        public List<string> Roles { get; set; }
        public string UserName { get; set; }
        public int BranchId { get; set; }
        public string BranchName { get; set; }
    }
}