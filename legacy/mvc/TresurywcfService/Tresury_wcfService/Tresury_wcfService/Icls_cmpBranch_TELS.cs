using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Tresury_wcfService
{
    [ServiceContract]
    public interface Icls_cmpBranch_TELS
    {
        [OperationContract]
        bool Insert(cmpBranch_TELS cmpBranch_TELSObj, out string Output_Message);

        [OperationContract]
        bool Update(cmpBranch_TELS cmpBranch_TELSObj, out string Output_Message);

        [OperationContract]
        bool Delete(int ID, out string Output_Message);

        [OperationContract]
        IQueryable<cmpBranch_TELS> SelectAll(int pageNum, int pageSize, string orderByFieldName, string orderByFieldType, bool isAscendingOrder, out int totalRows);

        [OperationContract]
        IQueryable<cmpBranch_TELS> SelectRow(int ID);

        [OperationContract]
        IQueryable<cmpBranch_TELS> SelectBy_DynamicWhereClause(string WhereClause, int pageNum, int pageSize, string orderByFieldName, string orderByFieldType, bool isAscendingOrder, out int totalRows);

    }
}
