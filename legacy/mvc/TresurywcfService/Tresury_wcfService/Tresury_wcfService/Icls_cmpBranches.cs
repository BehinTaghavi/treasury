using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Tresury_wcfService
{
    [ServiceContract]
    public interface Icls_cmpBranches
    {
        [OperationContract]
        bool Insert(cmpBranch BranchObj, out string Output_Message);

        [OperationContract]
        bool Update(cmpBranch BranchObj, out string Output_Message);

        [OperationContract]
        bool Delete(int Branchid, out string Output_Message);

        [OperationContract]
        IQueryable<cmpBranch> SelectAll(int pageNum, int pageSize, string orderByFieldName, string orderByFieldType, bool isAscendingOrder, out int totalRows);

        [OperationContract]
        IQueryable<cmpBranch> SelectRow(int BranchID );

        [OperationContract]
        IQueryable<cmpBranch> SelectBy_DynamicWhereClause(string WhereClause, int pageNum, int pageSize, string orderByFieldName, string orderByFieldType, bool isAscendingOrder, out int totalRows);
        
    }
}
