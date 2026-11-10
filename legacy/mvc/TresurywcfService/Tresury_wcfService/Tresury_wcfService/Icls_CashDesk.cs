using System;
using System.Collections.Generic;
 
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Tresury_wcfService
{
    [ServiceContract]
    public interface Icls_CashDesk
    {
        [OperationContract]
        
        bool Insert(CashDesk CashDeskObj, out string Output_Message);

        [OperationContract]
        bool Update(CashDesk CashDeskObj, out string Output_Message);

        [OperationContract]
        bool Delete(int CashDeskID, out string Output_Message);

        [OperationContract]
        IQueryable<CashDesk> SelectAll(int pageNum, int pageSize, string orderByFieldName, string orderByFieldType, bool isAscendingOrder, out int totalRows);

        [OperationContract]
        IQueryable<CashDesk> SelectRow(int CashDeskID);

        [OperationContract]
        IQueryable<CashDesk> SelectBy_DynamicWhereClause(string WhereClause, int pageNum, int pageSize, string orderByFieldName, string orderByFieldType, bool isAscendingOrder, out int totalRows);

    }
}
