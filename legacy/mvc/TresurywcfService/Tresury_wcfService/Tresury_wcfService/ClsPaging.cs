using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Tresury_wcfService
{
    public static class ClsPaging
    {
        public static IQueryable<T> PagedResult<T, TResult>(IQueryable<T> query, int pageNum, int pageSize, string orderByProperty, bool isAscendingOrder, out int rowsCount ,  string FieldType )
        //public static IList<T>   PagedResult<T, TResult>(IQueryable<T> query, int pageNum, int pageSize,  Expression<Func<T, TResult>> orderByProperty, bool isAscendingOrder, out int rowsCount)
        {
            if (pageSize <= 0) pageSize = 20;

            //Total result count
            rowsCount = query.Count();

            //If page number should be > 0 else set to first page
            if (rowsCount <= pageSize || pageNum <= 0) pageNum = 1;

            //Calculate nunber of rows to skip on pagesize
            int excludedRows = (pageNum - 1) * pageSize;

            if (orderByProperty == null || orderByProperty.Trim() == "")
            {
                orderByProperty = "ID";
            }
            if (FieldType.Substring(0, (FieldType).Length).ToLower() == typeof(int).Name.Substring(0, (FieldType).Length).ToLower())
                query = isAscendingOrder ? query.OrderBy<T, int>(orderByProperty) : query.OrderByDescending<T, int>(orderByProperty);

            else if (FieldType.Substring(0, (FieldType).Length).ToLower() == typeof(string).Name.Substring(0, (FieldType).Length).ToLower())
                query = isAscendingOrder ? query.OrderBy<T, string>(orderByProperty) : query.OrderByDescending<T, string>(orderByProperty);

            else if (FieldType.Substring(0, (FieldType).Length).ToLower() == typeof(decimal).Name.Substring(0, (FieldType).Length).ToLower())
                query = isAscendingOrder ? query.OrderBy<T, decimal>(orderByProperty) : query.OrderByDescending<T, decimal>(orderByProperty);

            else if (FieldType.Substring(0, (FieldType).Length).ToLower() == typeof(bool).Name.Substring(0, (FieldType).Length).ToLower())
                query = isAscendingOrder ? query.OrderBy<T, bool>(orderByProperty) : query.OrderByDescending<T, bool>(orderByProperty);


           // var tem = query.ToList();
            var R = query.Skip(excludedRows).Take(pageSize);

            return R;
 
            
        }
    }
}
