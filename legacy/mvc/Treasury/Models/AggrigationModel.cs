using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Web;

namespace CSD.Treasury.UI.Models
{
    public class AggrigationModel
    {
        public string FiscalYear { get; set; }
        public int[] requestIds { get; set; }
        public string Unique_Account_No { get; set; }
        public string Source_PaymentValue { get; set; }
        public string Main_Unique_Account_No { get; set; }
        public int BehalfId { get; set; }
        public AggrigationModel()
        {
            PersianCalendar pc = new PersianCalendar();
            var Month = pc.GetMonth(DateTime.Now);
             FiscalYear = pc.GetYear(DateTime.Now).ToString();
            if (Month >= 10 && ConfigurationManager.AppSettings["YearPlus"] == "True")
            {
                FiscalYear = (pc.GetYear(DateTime.Now) + 1).ToString();
            }
       
        }            
    }
}