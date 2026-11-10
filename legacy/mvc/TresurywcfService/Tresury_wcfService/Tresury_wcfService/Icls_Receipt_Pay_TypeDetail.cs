using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Tresury_wcfService
{
    [ServiceContract]
    public interface Icls_Receipt_Pay_TypeDetail
    {
        [OperationContract]
        bool Insert(Receipt_Pay_TypeDetail Receipt_Pay_TypeDetailObj, out string Output_Message);

        [OperationContract]
        bool Update(Receipt_Pay_TypeDetail Receipt_Pay_TypeDetailObj, out string Output_Message);

        [OperationContract]
        bool Delete(int Receipt_Pay_TypeDetailID, out string Output_Message);

        [OperationContract]
        IQueryable<Receipt_Pay_TypeDetail> SelectAll(int pageNum, int pageSize, string orderByFieldName, string orderByFieldType, bool isAscendingOrder, out int totalRows);

        [OperationContract]
        IQueryable<Receipt_Pay_TypeDetail> SelectRow(int Receipt_Pay_TypeDetailID);

        [OperationContract]
        IQueryable<Receipt_Pay_TypeDetail> SelectBy_DynamicWhereClause(string WhereClause, int pageNum, int pageSize, string orderByFieldName, string orderByFieldType, bool isAscendingOrder, out int totalRows);

    }
}
