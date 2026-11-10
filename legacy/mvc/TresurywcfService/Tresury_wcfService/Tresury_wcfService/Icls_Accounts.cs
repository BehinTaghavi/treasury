using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Tresury_wcfService
{
    [ServiceContract]
    public interface Icls_Accounts
    {
        [OperationContract]

        IQueryable<t_unique_Accounts_Result> SelectAll_t_unique_Accounts(int uniqueType,  int pageNum, int pageSize, string orderByFieldName, string orderByFieldType, bool isAscendingOrder, out int totalRows);

        [OperationContract]
        IList<t_Accounts_Result> SelectAll_t_Accounts_LIST(int pageNum, int pageSize, string orderByFieldName, string orderByFieldType, bool isAscendingOrder, out int totalRows);

        [OperationContract]
        IQueryable<t_Accounts_Result> SelectAll_t_Accounts(int pageNum, int pageSize, string orderByFieldName, string orderByFieldType, bool isAscendingOrder, out int totalRows);

        [OperationContract]
        IQueryable<t_unique_account_type_Result> SelectAll_t_unique_account_type(int pageNum, int pageSize, string orderByFieldName, string orderByFieldType, bool isAscendingOrder, out int totalRows);

        [OperationContract]
        IQueryable<t_unique_Accounts_Result> SelectBy_t_unique_Accounts_DynamicWhereClause(int uniqueType, string WhereClause, int pageNum, int pageSize, string orderByFieldName, string orderByFieldType, bool isAscendingOrder, out int totalRows);


        [OperationContract]
        IQueryable<t_Accounts_Result> SelectBy_t_Accounts_DynamicWhereClause(string WhereClause, int pageNum, int pageSize, string orderByFieldName, string orderByFieldType, bool isAscendingOrder, out int totalRows);

        [OperationContract]
        IList<t_unique_account_type_Result> SelectAll_t_unique_account_type_list(int pageNum, int pageSize, string orderByFieldName, string orderByFieldType, bool isAscendingOrder, out int totalRows);


    }
}
