using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSD.Treasury.Service.Request_PaymentService
{
    public partial class GetAllRequestStatus_Result
    {
        public List<GetAllRequestStatus_Result> ChildList { get; set; }
        public string UniqueAccountTitle { get; set; }
        public string BehalfTitle { get; set; }
        public List<Service.ReferenceTypes_By_BehalfGroupService.ReferenceTypes_By_BehalfGroup> ReferenceTypeLsBYbehalfGroupId { get; set; }
        public List<Service.ReferenceTypeService.ReferenceType> RefrenceTypeLs { get; set; }
        public List<Service.BehalfGroupService.BehalfGroup> BehalfGroupList { get; set; }
        public string BehalfGroupTiltle { get; set; }
        public string BankBalanceStatus  { get; set; }
    }
}
