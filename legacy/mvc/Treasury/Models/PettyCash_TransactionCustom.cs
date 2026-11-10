using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CSD.Treasury.UI.Models
{

    public class PettyCash_TransactionCustom
    {
        List<CustomDetails> details = new List<CustomDetails>();
        public int PettyCashID { get; set; }
        public string PettyCashTransactionDate { get; set; }
        public string  UniqueAccount2 { get; set; }
        public int RequestID { get; set; }
   

        //public PettyCash_TransactionCustom(string json)
        //{


        //    var str = JsonConvert.DeserializeObject<dynamic>(json);
        //    var data = ((JObject)json).Children();
        //    var stuff = data.Select(x => new { AreaCode = x.Path.Split('.')[1], City = x.First()["city"], State = x.Last()["state"] });




        //    JObject jObject = JObject.Parse(json);
        //    JToken JPettyCash = jObject["Pettycash"];
        //    PettyCashID = (int)JPettyCash["PettyCashID"];
        //    PettyCashTransactionDate = (string)JPettyCash["PettyCashTransactionDate"];
        //    UniqueAccount2 = (string)JPettyCash["UniqueAccount2"];
        //   //details =(List<CustomDetails>)JPettyCash["details"].ToList(); 

        //}
    }
}