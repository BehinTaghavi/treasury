using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Tresury_wcfService
{
    [ServiceContract]
    public interface Icls_Bank
    {
        [OperationContract]
        bool Insert(Bank BankObj, out string Output_Message);

        [OperationContract]
        bool Update(Bank BankObj, out string Output_Message);

        [OperationContract]
        bool Delete(int BankID, out string Output_Message);

        [OperationContract]
        IQueryable<Bank> SelectAll(int pageNum, int pageSize, string orderByFieldName, string orderByFieldType, bool isAscendingOrder, out int totalRows);

        [OperationContract]
        IQueryable<Bank> SelectRow(int BankID);

        [OperationContract]
        IQueryable<Bank> SelectBy_DynamicWhereClause(string WhereClause, int pageNum, int pageSize, string orderByFieldName, string orderByFieldType, bool isAscendingOrder, out int totalRows);

    }
}
