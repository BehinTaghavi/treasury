using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using CSD.Treasury.Service.BehalfGroupService;
using CSD.Treasury.Service.BehalfsService;
using CSD.Treasury.Service.ReferenceTypes_By_BehalfGroupService;
using CSD.Treasury.DataModel;

namespace CSD.Treasury.UI.Models
{
    public class CustomPettyCash_TransactionDetail
    {
        public string PayDate { get; set; }
        public int PettyCashTransactionDetailID { get; set; }
        public bool IsAccept { get; set; }
        public bool IsVakhahi { get; set; }
        public string EllateVakhahi { get; set; }
        public string Description { get; set; }
        public bool IsAcceptVakhahi { get; set; }
        public string AcceptVakhahiDate { get; set; }
        public int Price { get; set; }
        public string Status { get; set; }
        public int BehalfGroupID { get; set; }
        public int BehalfID { get; set; }
        public string BehalfTitle { get; set; }
        public  string BehalfGroupTiltle { get; set; }
        public string uniqAccountType { get; set; }
        public UCItem UniqueAccount1 { get; set; }
        public UCItem UniqueAccountTitle1 { get; set; }
        public UCItem UniqueAccountTitle2 { get; set; }
        public string PettyCashName { get; set; }
        public UCItem UniqueAccount2 { get; set; }
        public List<Service.BehalfGroupService.BehalfGroup> BehalfGroupList { get; set; }
        public List<Service.BehalfsService.Behalf> BehalfList { get; set; }
        public List<v_accounts> AccountNumLs { get; set; }
        public List<v_unique_accounts> UniqueAccountLs1 { get; set; }
        public List<v_unique_accounts> UniqueAccountLs2 { get; set; }
        public List<Service.ReferenceTypeService.ReferenceType> References { get; set; }
        public List<Service.ReferenceTypeService.ReferenceType> RefrenceTypeLs { get; set; }
        public string SelectedRefrence { get; set; }
        public string RefrenceTitle1 { get; set; }
        public string RefrenceTitle2{ get; set; }
        public string Account_No{ get; set; }
        public string Unique_Account_No1 { get; set; }
        public string Unique_Account_No2 { get; set; }
        public List<Reference> Refrences { get; set; }

    }

    public class UCItem
    {
        public string Code { get; set; }
        public string Name { get; set; }

        public UCItem() {
            this.Code ="";
            this.Name = "";
        }

    }
}