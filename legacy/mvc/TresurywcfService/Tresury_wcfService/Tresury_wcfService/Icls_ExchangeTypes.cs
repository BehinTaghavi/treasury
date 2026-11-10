using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Tresury_wcfService
{
    [ServiceContract]
    public interface Icls_ExchangeTypes
    {
        [OperationContract]
        bool Insert(ExchangeType ExchangeTypesObj, out string Output_Message);

        [OperationContract]
        bool Update(ExchangeType ExchangeTypesObj, out string Output_Message);

        [OperationContract]
        bool Delete(int ExchangeTypeID, out string Output_Message);

        [OperationContract]
        IQueryable<ExchangeType> SelectAll(int pageNum, int pageSize, string orderByFieldName, string orderByFieldType, bool isAscendingOrder, out int totalRows);

        [OperationContract]
        IQueryable<ExchangeType> SelectRow(int ExchangeTypeID);

        [OperationContract]
        IQueryable<ExchangeType> SelectBy_DynamicWhereClause(string WhereClause, int pageNum, int pageSize, string orderByFieldName, string orderByFieldType, bool isAscendingOrder, out int totalRows);

    }
}
