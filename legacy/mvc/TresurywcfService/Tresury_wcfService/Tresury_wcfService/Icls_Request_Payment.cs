using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Tresury_wcfService
{
    [ServiceContract]
    public interface Icls_Request_Payment
    {
        [OperationContract]
        bool Insert(Request_Payment Request_PaymentObj, out string Output_Message);

        [OperationContract]
        bool Update(Request_Payment Request_PaymentObj, out string Output_Message);

        [OperationContract]
        bool Delete(int RequestID, out string Output_Message);

        [OperationContract]
        IQueryable<Request_Payment> SelectAll(int pageNum, int pageSize, string orderByFieldName, string orderByFieldType, bool isAscendingOrder, out int totalRows);

        [OperationContract]
        IQueryable<Request_Payment> SelectRow(int RequestID);

        [OperationContract]
        IList<Request_Payment_GetAllInfo_ForSpecialRequestID_Result> SelectRow_GetAllInfo(int RequestID);

        [OperationContract]
        IQueryable<Request_Payment> SelectBy_DynamicWhereClause(string WhereClause, int pageNum, int pageSize, string orderByFieldName, string orderByFieldType, bool isAscendingOrder, out int totalRows);

    }
}
