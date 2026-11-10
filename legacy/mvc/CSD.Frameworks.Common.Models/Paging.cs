using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using System.ComponentModel;

namespace CSD.Common.Models
{
    public static class Constants
    {
        public const int PageIndexDefault = 0;
        public const int PageSizeDefault = 12;
    }

    public class PagingItems
    {
        [HideInForm]
        [DisplayName("شماره صفحه")]
        public int PageIndex { get; set; }

        [HideInForm]
        [DisplayName("شماره صفحه جاری")]
        public int CurrentPage { get; set; }

        [HideInForm]
        [DisplayName("تعداد ردیف در هر صفحه")]
        public int PageSize { get; set; }

        [HideInForm]
        public string SortExpression { get; set; }

        [HideInForm]
        [DisplayName("تعداد کل")]
        public int TotalItemCount { get; set; }
        
        [HideInForm]
        [DisplayName("تعداد کل صفحات")]
        public int TotalPageCount { get; set; }

        [HideInForm]
        public string Area { get; set; }

        public PagingItems()
        {
            PageIndex = Constants.PageIndexDefault;
            PageSize = Constants.PageSizeDefault;
            SortExpression = string.Empty;
            Area = string.Empty;
        }

        public PagingItems(int pageIndex, int pageSize)
        {
            this.PageIndex = pageIndex;
            this.PageSize = pageSize;
            SortExpression = string.Empty;
        }

        public PagingItems(int totalItemCount)
            : this()
        {
            this.TotalItemCount = totalItemCount;
        }

        public void Update(int pageSize, int totalItemCount)
        {
            this.PageSize = pageSize;
            this.TotalItemCount = totalItemCount;
            this.TotalPageCount = (int)Math.Ceiling(totalItemCount / (double)pageSize);
        }

        public PagingItems(int pageIndex, int pageSize, int totalItemCount, string sortExpression)
        {
            this.PageIndex = pageIndex;
            this.PageSize = pageSize;
            this.SortExpression = sortExpression;
            this.TotalItemCount = totalItemCount;
            this.TotalPageCount = (int)Math.Ceiling(totalItemCount / (double)pageSize);
        }
    }

    public class PagedList<T> : List<T> where T : class
    {
        public T ModelInstance { get; set; }
        public IList<object> ExtraData { get; set; }

        public PagedList(List<T> items, PagingItems pagingItem)
        {
            this.AddRange(items);
            this.PagingItem = pagingItem;
            ExtraData = new List<object>();
        }

   
        public PagedList(IEnumerable<T> items, PagingItems pagingItem)
        {
            if (pagingItem == null) pagingItem = new PagingItems();
            pagingItem.Update(pagingItem.PageSize, items.Count());
            var ls = items.Skip(pagingItem.PageSize * pagingItem.PageIndex).Take(pagingItem.PageSize).ToList();
            this.AddRange(ls);
            this.PagingItem = pagingItem; 
            ExtraData = new List<object>();
        }

        public PagedList(IEnumerable<T> items, int pageIndex, int pageSize, int totalItemCount, string sortExpression)
        {
            this.PagingItem = new PagingItems(pageIndex, pageSize, totalItemCount, sortExpression);
            var ls = items.Skip(pageSize * pageIndex).Take(pageSize).ToList();
            this.AddRange(ls);
            ExtraData = new List<object>();
        }
        

        public PagingItems PagingItem { get; set; }


    }
}
