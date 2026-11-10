using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Tresury_wcfService
{
    [ServiceContract]
    public interface Icls_Source_DestinationTypes
    {
        [OperationContract]
        bool Insert(Source_DestinationTypes Source_DestinationTypesObj, out string Output_Message);

        [OperationContract]
        bool Update(Source_DestinationTypes Source_DestinationTypesObj, out string Output_Message);

        [OperationContract]
        bool Delete(int Source_DestinationTypeID, out string Output_Message);

        [OperationContract]
        IQueryable<Source_DestinationTypes> SelectAll(int pageNum, int pageSize, string orderByFieldName, string orderByFieldType, bool isAscendingOrder, out int totalRows);

        [OperationContract]
        IQueryable<Source_DestinationTypes> SelectRow(int Source_DestinationTypeID);

        [OperationContract]
        IQueryable<Source_DestinationTypes> SelectBy_DynamicWhereClause(string WhereClause, int pageNum, int pageSize, string orderByFieldName, string orderByFieldType, bool isAscendingOrder, out int totalRows);

    }
}
