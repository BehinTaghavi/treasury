using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Tresury_wcfService
{
    [ServiceContract]
    public interface Icls_BehalfGroup
    {
        [OperationContract]
        bool Insert(BehalfGroup BehalfGroupObj, out string Output_Message);

        [OperationContract]
        bool Update(BehalfGroup BehalfGroupObj, out string Output_Message);

        [OperationContract]
        bool Delete(int BehalfGroupID, out string Output_Message);

        [OperationContract]
        IQueryable<BehalfGroup> SelectAll(int pageNum, int pageSize, string orderByFieldName, string orderByFieldType, bool isAscendingOrder, out int totalRows);

        [OperationContract]
        IQueryable<BehalfGroup> SelectRow(int BehalfGroupID);

        [OperationContract]
        IQueryable<BehalfGroup> SelectBy_DynamicWhereClause(string WhereClause, int pageNum, int pageSize, string orderByFieldName, string orderByFieldType, bool isAscendingOrder, out int totalRows);

    }
}
