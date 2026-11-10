using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Tresury_wcfService
{
    [ServiceContract]
    public interface Icls_cmpBranch_FAX
    {
        [OperationContract]
        bool Insert(cmpBranch_FAX cmpBranch_FAXObj, out string Output_Message);

        [OperationContract]
        bool Update(cmpBranch_FAX cmpBranch_FAXObj, out string Output_Message);

        [OperationContract]
        bool Delete(int ID, out string Output_Message);

        [OperationContract]
        IQueryable<cmpBranch_FAX> SelectAll(int pageNum, int pageSize, string orderByFieldName, string orderByFieldType, bool isAscendingOrder, out int totalRows);

        [OperationContract]
        IQueryable<cmpBranch_FAX> SelectRow(int ID);

        [OperationContract]
        IQueryable<cmpBranch_FAX> SelectBy_DynamicWhereClause(string WhereClause, int pageNum, int pageSize, string orderByFieldName, string orderByFieldType, bool isAscendingOrder, out int totalRows);

    }
}
