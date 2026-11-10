using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Tresury_wcfService
{
    [ServiceContract]
    public interface Icls_Behalfs
    {
        [OperationContract]
        bool Insert(Behalf BehalfObj, out string Output_Message);

        [OperationContract]
        bool Update(Behalf BehalfObj, out string Output_Message);

        [OperationContract]
        bool Delete(int BehalfID, out string Output_Message);

        [OperationContract]
        IQueryable<Behalf> SelectAll(int pageNum, int pageSize, string orderByFieldName, string orderByFieldType, bool isAscendingOrder, out int totalRows);

        [OperationContract]
        IQueryable<Behalf> SelectRow(int BehalfID);

        [OperationContract]
        IQueryable<Behalf> SelectBy_DynamicWhereClause(string WhereClause, int pageNum, int pageSize, string orderByFieldName, string orderByFieldType, bool isAscendingOrder, out int totalRows);

    }
}
