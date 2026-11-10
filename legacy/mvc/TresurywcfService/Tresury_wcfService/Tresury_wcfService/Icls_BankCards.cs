using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Tresury_wcfService
{
    [ServiceContract]
    public interface Icls_BankCards
    {
        [OperationContract]
        bool Insert(BankCard BankCardsObj, out string Output_Message);

        [OperationContract]
        bool Update(BankCard BankCardsObj, out string Output_Message);

        [OperationContract]
        bool Delete(int CardID, out string Output_Message);

        [OperationContract]
        IQueryable<BankCard> SelectAll(int pageNum, int pageSize, string orderByFieldName, string orderByFieldType, bool isAscendingOrder, out int totalRows);

        [OperationContract]
        IQueryable<BankCard> SelectRow(int CardID);

        [OperationContract]
        IQueryable<BankCard> SelectBy_DynamicWhereClause(string WhereClause, int pageNum, int pageSize, string orderByFieldName, string orderByFieldType, bool isAscendingOrder, out int totalRows);

    }
}
