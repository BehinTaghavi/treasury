using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Tresury_wcfService
{
    [ServiceContract]
    public interface Icls_ReferenceType
    {
        [OperationContract]
        bool Insert(ReferenceType ReferenceTypeObj, out string Output_Message);

        [OperationContract]
        bool Update(ReferenceType ReferenceTypeObj, out string Output_Message);

        [OperationContract]
        bool Delete(int referenceTypeID, out string Output_Message);

        [OperationContract]
        IQueryable<ReferenceType> SelectAll(int pageNum, int pageSize, string orderByFieldName, string orderByFieldType, bool isAscendingOrder, out int totalRows);

        [OperationContract]
        IQueryable<ReferenceType> SelectRow(int referenceTypeID);

        [OperationContract]
        IQueryable<ReferenceType> SelectBy_DynamicWhereClause(string WhereClause, int pageNum, int pageSize, string orderByFieldName, string orderByFieldType, bool isAscendingOrder, out int totalRows);

    }
}
