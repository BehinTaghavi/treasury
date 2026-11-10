using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Tresury_wcfService
{
    [ServiceContract]
    public interface Icls_Receipt_Pay_Type
    {
        [OperationContract]
        bool Insert(Receipt_Pay_Type Receipt_Pay_TypeObj, out string Output_Message);

        [OperationContract]
        bool Update(Receipt_Pay_Type Receipt_Pay_TypeObj, out string Output_Message);

        [OperationContract]
        bool Delete(int Receipt_Pay_TypeID, out string Output_Message);

        [OperationContract]
        IQueryable<Receipt_Pay_Type> SelectAll(int pageNum, int pageSize, string orderByFieldName, string orderByFieldType, bool isAscendingOrder, out int totalRows);

        [OperationContract]
        IQueryable<Receipt_Pay_Type> SelectRow(int Receipt_Pay_TypeID);

        [OperationContract]
        IQueryable<Receipt_Pay_Type> SelectBy_DynamicWhereClause(string WhereClause, int pageNum, int pageSize, string orderByFieldName, string orderByFieldType, bool isAscendingOrder, out int totalRows);

    }
}
