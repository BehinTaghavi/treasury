using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Tresury_wcfService
{
    [ServiceContract]
    public interface Icls_BasicBanks
    {
        [OperationContract]
        bool Insert(BasicBank BasicBanksObj, out string Output_Message);

        [OperationContract]
        bool Update(BasicBank BasicBanksObj, out string Output_Message);

        [OperationContract]
        bool Delete(int BasicBankId, out string Output_Message);

        [OperationContract]
        IQueryable<BasicBank> SelectAll(int pageNum, int pageSize, string orderByFieldName, string orderByFieldType, bool isAscendingOrder, out int totalRows);

        [OperationContract]
        IQueryable<BasicBank> SelectRow(int BankID);

        [OperationContract]
        IQueryable<BasicBank> SelectBy_DynamicWhereClause(string WhereClause, int pageNum, int pageSize, string orderByFieldName, string orderByFieldType, bool isAscendingOrder, out int totalRows);

    }
}
