using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Tresury_wcfService
{

    public class cls_Accounts : Icls_Accounts
    {
        AccountsEntities Treasury = new AccountsEntities();

        public IQueryable<t_Accounts_Result> SelectAll_t_Accounts(int pageNum, int pageSize, string orderByFieldName, string orderByFieldType, bool isAscendingOrder, out int totalRows)
        {
            Treasury.Configuration.ProxyCreationEnabled = false;
            Treasury.Configuration.LazyLoadingEnabled = false;
            IQueryable<t_Accounts_Result> articles  = Treasury.t_Accounts().AsQueryable();
           // IList<t_Accounts_Result> r = articles.ToList();
            return ClsPaging.PagedResult<t_Accounts_Result, IQueryable<t_Accounts_Result>>(articles, pageNum, pageSize, orderByFieldName, isAscendingOrder, out totalRows, orderByFieldType);
        }


        public IList<t_Accounts_Result> SelectAll_t_Accounts_LIST(int pageNum, int pageSize, string orderByFieldName, string orderByFieldType, bool isAscendingOrder, out int totalRows)
        {
            Treasury.Configuration.ProxyCreationEnabled = false;
            Treasury.Configuration.LazyLoadingEnabled = false;
            IQueryable<t_Accounts_Result> articles = Treasury.t_Accounts().AsQueryable().Take(10).Skip(10);
            IList<t_Accounts_Result> r = articles.ToList();
            totalRows = r.Count();
            return r;
        }

        public IQueryable<t_unique_Accounts_Result> SelectAll_t_unique_Accounts( int uniqueType ,  int pageNum, int pageSize, string orderByFieldName, string orderByFieldType, bool isAscendingOrder, out int totalRows)
        {
            Treasury.Configuration.ProxyCreationEnabled = false;
            Treasury.Configuration.LazyLoadingEnabled = false;
            IQueryable<t_unique_Accounts_Result> articles  = Treasury.t_unique_Accounts(uniqueType).AsQueryable();            
            return ClsPaging.PagedResult<t_unique_Accounts_Result, IQueryable<t_unique_Accounts_Result>>(articles, pageNum, pageSize, orderByFieldName, isAscendingOrder, out totalRows, orderByFieldType);
        }

        public IQueryable<t_unique_account_type_Result> SelectAll_t_unique_account_type(int pageNum, int pageSize, string orderByFieldName, string orderByFieldType, bool isAscendingOrder, out int totalRows)
        {
            Treasury.Configuration.ProxyCreationEnabled = false;
            Treasury.Configuration.LazyLoadingEnabled = false;
            IQueryable<t_unique_account_type_Result> articles  = Treasury.t_unique_account_type().AsQueryable();            
            return ClsPaging.PagedResult<t_unique_account_type_Result, IQueryable<t_unique_account_type_Result>>(articles, pageNum, pageSize, orderByFieldName, isAscendingOrder, out totalRows, orderByFieldType);
        }


        public IList<t_unique_account_type_Result> SelectAll_t_unique_account_type_list(int pageNum, int pageSize, string orderByFieldName, string orderByFieldType, bool isAscendingOrder, out int totalRows)
        {
            Treasury.Configuration.ProxyCreationEnabled = false;
            Treasury.Configuration.LazyLoadingEnabled = false;
            IQueryable<t_unique_account_type_Result> articles = Treasury.t_unique_account_type().AsQueryable();
            return ClsPaging.PagedResult<t_unique_account_type_Result, IQueryable<t_unique_account_type_Result>>(articles, pageNum, pageSize, orderByFieldName, isAscendingOrder, out totalRows, orderByFieldType).ToList();
        }

        public IQueryable<t_unique_Accounts_Result> SelectBy_t_unique_Accounts_DynamicWhereClause(int uniqueType , string WhereClause, int pageNum, int pageSize, string orderByFieldName, string orderByFieldType, bool isAscendingOrder, out int totalRows)
        {
            Treasury.Configuration.ProxyCreationEnabled = false;
            Treasury.Configuration.LazyLoadingEnabled = false;
            IQueryable<t_unique_Accounts_Result> articles = Treasury.t_unique_Accounts(uniqueType).AsQueryable();
            return ClsPaging.PagedResult<t_unique_Accounts_Result, IQueryable<t_unique_Accounts_Result>>(articles, pageNum, pageSize, orderByFieldName, isAscendingOrder, out totalRows, orderByFieldType);
        }

        public IQueryable<t_Accounts_Result> SelectBy_t_Accounts_DynamicWhereClause(string WhereClause, int pageNum, int pageSize, string orderByFieldName, string orderByFieldType, bool isAscendingOrder, out int totalRows)
        {
            Treasury.Configuration.ProxyCreationEnabled = false;
            Treasury.Configuration.LazyLoadingEnabled = false;
            IQueryable<t_Accounts_Result> articles = Treasury.t_Accounts().AsQueryable();
            return ClsPaging.PagedResult<t_Accounts_Result, IQueryable<t_Accounts_Result>>(articles, pageNum, pageSize, orderByFieldName, isAscendingOrder, out totalRows, orderByFieldType);
        }
    }
}
