using CSD.Treasury.DataModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;

namespace CSD.Treasury.UI.Models
{
    public class AccountView<T> : WebViewPage<T>
    {
        
        public string UserName { get; set; }
        public string UserFullName { get; set; }
        public override void InitHelpers()
        {
            base.InitHelpers();
            UserName = User.Identity.Name;
        }
      
        public override void Execute()
        {
        }
    }
}