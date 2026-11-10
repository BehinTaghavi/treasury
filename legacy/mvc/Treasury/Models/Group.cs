using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CSD.Treasury.UI.Models
{
    public class Group
    {
        public int Key { get; set; }
        public List<RequestPayment> Values;
    }
}