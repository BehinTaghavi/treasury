using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace APS.MVC.Helpers
{
    public class PagedList<T> : List<T>
    {
        public PagedList(IEnumerable<T> items, int pageIndex, int pageSize, int totalItemCount, string sortExpression)
        {
            this.AddRange(items);
            this.PageIndex = pageIndex;
            this.PageSize = pageSize;
            this.SortExpression = sortExpression;
            this.TotalItemCount = totalItemCount;
            this.TotalPageCount = (int)Math.Ceiling(totalItemCount / (double)pageSize);
        }

        public int PageIndex { get; set; }
        public int PageSize { get; set; }
        public string SortExpression { get; set; }
        public int TotalItemCount { get; set; }
        public int TotalPageCount { get; private set; }

    }

    public static class PageLinqExtensions
    {
        public static PagedList<T> ToPagedList<T>
            (
                this IQueryable<T> allItems,
                int? pageIndex,
                int pageSize
            )
        {
            return ToPagedList<T>(allItems, pageIndex, pageSize, String.Empty);

        }

        public static PagedList<T> ToPagedList<T>
            (
                this IQueryable<T> allItems,
                int? pageIndex,
                int pageSize,
                string sort
            )
        {
            var truePageIndex = pageIndex ?? 0;
            var itemIndex = truePageIndex * pageSize;
            var pageOfItems = allItems.Skip(itemIndex).Take(pageSize);
            var totalItemCount = allItems.Count();
            return new PagedList<T>(pageOfItems, truePageIndex, pageSize, totalItemCount, sort);

        }
    }
}