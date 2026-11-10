using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Web;

namespace CSD.Treasury.UI.Models
{
    public class CustomPrincipal: ICustomPrincipal
    {
        public IIdentity Identity { get; private set; }
        public List<string> Roles { get; set; }
        public int Id { get; set; }
        public string UserName { get; set; }
        public string LocationName { get; set; }
        public string UserFullName { get; set; }
        public int BranchId { get; set; }
        public string BranchName { get; set; }
        public bool IsInRole(string role)
        {
            foreach(var item in Roles)
            {
                if (item.ToLower() == role.ToLower())
                    return true;
            }
            return false;
        }

        public CustomPrincipal(string UserName)
        {
            this.Identity = new GenericIdentity(UserName);
        }
    }
}