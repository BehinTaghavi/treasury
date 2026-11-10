using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.IO;
using System.Web.Mvc;
using System.Web.Mvc.Html;
using System.Web.Mvc.Ajax;
using System.Web.Routing;
using System.Linq.Expressions;
using CSD.Common.Models;


namespace CSD.MVC.Helpers
{ 
    public class DataGrid<T> : IHtmlString where T : class
    {
        object _htmlAttribute;
        object _rowAttribute;
        object _altRowAttribute;
        object _headerAttribute;

        string _controller = "";
        string _area = "";
        GridColumns _columns;
        PagedList<T> _items = null;
        HtmlHelper<T> _helper;

        bool _showCommands = false;

        #region constructors

        public DataGrid()
        {
        }

        public DataGrid(HtmlHelper<T> helper)
        {
            _helper = helper;
            _columns = new GridColumns();
            RouteData routeData = _helper.ViewContext.RouteData;
            _controller = routeData.GetRequiredString("controller");
        }

        public DataGrid(HtmlHelper<T> helper, PagedList<T> items)
        {
            _items = items;
            _helper = helper;
            _columns = new GridColumns();
            RouteData routeData = _helper.ViewContext.RouteData;
            _controller = routeData.GetRequiredString("controller");
        }

        public DataGrid(HtmlHelper<T> helper, PagedList<T> items, GridColumns columns)
        {
            _helper = helper;
            _items = items;
            _columns = columns;
            RouteData routeData = _helper.ViewContext.RouteData;
            _controller = routeData.GetRequiredString("controller");
            var area = routeData.DataTokens["area"];
            if (area != null)
                _area = area.ToString();
        }
        #endregion

        #region FluentMethods

        public DataGrid<T> Columns(GridColumns columns)
        {
            this._columns = columns;
            return this;
        }

        public DataGrid<T> AddColumn(GridColumn col, bool AutoSetRank = true)
        {
            this._columns.AddColumn(col, AutoSetRank);
            return this;
        }

        public DataGrid<T> AddColumn(string fieldName, string Title)
        {
            this.AddColumn(new GridColumn(fieldName, Title), true);
            return this;
        }

        public DataGrid<T> AddColumn(string fieldName)
        {
            this.AddColumn(new GridColumn(fieldName), true);
            return this;
        }

        public DataGrid<T> TableAttribute(object attr)
        {
            this._htmlAttribute = attr;
            return this;
        }

        public DataGrid<T> AltRowAttribute(object attr)
        {
            this._altRowAttribute = attr;
            return this;
        }

        public DataGrid<T> RowAttribute(object attr)
        {
            this._rowAttribute = attr;
            return this;
        }

        public DataGrid<T> HeadAttribute(object attr)
        {
            this._headerAttribute = attr;
            return this;
        }

        public DataGrid<T> ShowCommands(bool addDefaults = true)
        {
            this._showCommands = true;
            if (addDefaults)
                _columns.AddDefaultCommands(this._controller, this._area);
            return this;
        }

        public DataGrid<T> BindData(PagedList<T> items)
        {
            this._items = items;
            return this;
        }

        public DataGrid<T> AddCommand(GridCommandColumn cmd, bool AutoSetRank = true)
        {
            cmd.ColumnType = GridColumnType.Command;
            this._columns.AddCommand(cmd, AutoSetRank);
            return this;
        }


        #endregion

        public string ToHtmlString()
        {
            // Get items           
            var items = _items;

            // Get column names
            if (_columns == null)
            {
                _columns = new GridColumns();
                foreach (var p in typeof(T).GetProperties().Where(p => p.PropertyType.Namespace == "System").Select(p => p.Name))
                    _columns.AddColumn(new GridColumn(p));
            }
            // Create HtmlTextWriter
            var writer = new HtmlTextWriter(new StringWriter());


            var attributes = HtmlHelper.AnonymousObjectToHtmlAttributes(this._htmlAttribute);
            foreach (var key in attributes.Keys)
            {
                var value = attributes[key];
                string valueAsString = null;
                if (value != null)
                {
                    valueAsString = value.ToString();
                    writer.AddAttribute(key, valueAsString);
                }
            }
            // Open table tag
            writer.RenderBeginTag(HtmlTextWriterTag.Table);

            attributes = HtmlHelper.AnonymousObjectToHtmlAttributes(this._headerAttribute);
            foreach (var key in attributes.Keys)
            {
                var value = attributes[key];
                string valueAsString = null;
                if (value != null)
                {
                    valueAsString = value.ToString();
                    writer.AddAttribute(key, valueAsString);
                }
            }
            // Render table header
            writer.RenderBeginTag(HtmlTextWriterTag.Thead);
            RenderHeader(writer);
            writer.RenderEndTag();

            // Render table body
            writer.RenderBeginTag(HtmlTextWriterTag.Tbody);

            var RowAttr = HtmlHelper.AnonymousObjectToHtmlAttributes(this._rowAttribute);
            var AltRowAttr = HtmlHelper.AnonymousObjectToHtmlAttributes(this._altRowAttribute);
            int i = 0;
            foreach (var item in items)
            {
                if (i % 2 == 0)
                {
                    foreach (var key in RowAttr.Keys)
                    {
                        var value = RowAttr[key];
                        string valueAsString = null;
                        if (value != null)
                        {
                            valueAsString = value.ToString();
                            writer.AddAttribute(key, valueAsString);
                        }
                    }
                }
                else
                {
                    foreach (var key in AltRowAttr.Keys)
                    {
                        var value = AltRowAttr[key];
                        string valueAsString = null;
                        if (value != null)
                        {
                            valueAsString = value.ToString();
                            writer.AddAttribute(key, valueAsString);
                        }
                    }
                }
                RenderRow(writer, item);
                i++;
            }
            writer.RenderEndTag();

            writer.RenderBeginTag(HtmlTextWriterTag.Tfoot);
            RenderPagerRow(writer);
            writer.RenderEndTag();
            // Close table tag
            writer.RenderEndTag();

            // Return the string
            return writer.InnerWriter.ToString();

        }

        #region private Methods

        private void RenderHeader(HtmlTextWriter writer)
        {
            writer.RenderBeginTag(HtmlTextWriterTag.Tr);
            if (_showCommands)
                for (int i = 0; i < _columns.CommandColumns.Where(u => ((GridCommandColumn)u).ShowInTheFirst).Count(); i++)
                {
                    writer.RenderBeginTag(HtmlTextWriterTag.Th);
                    writer.RenderEndTag();
                }

            foreach (var col in _columns.DataColumns)
            {
                writer.RenderBeginTag(HtmlTextWriterTag.Th);
                var link = _helper.GridColumnHeaderLink((GridColumn)col);
                writer.Write(link);
                writer.RenderEndTag();
            }
            if (_showCommands)
                for (int i = 0; i < _columns.CommandColumns.Where(u => !((GridCommandColumn)u).ShowInTheFirst).Count(); i++)
                {
                    writer.RenderBeginTag(HtmlTextWriterTag.Th);
                    writer.RenderEndTag();
                }
            writer.RenderEndTag();
        }

        private void RenderCmd(HtmlTextWriter writer, GridCommandColumn cmd, T item)
        {
            writer.RenderBeginTag(HtmlTextWriterTag.Td);

            var idValue = cmd.GetItemIdValue<T>(item);
            var imglink = cmd.MakeCmdImageLink(_helper, idValue);

            writer.Write(imglink.ToString());
            writer.RenderEndTag();
        }

        private void RenderRow(HtmlTextWriter writer, T item)
        {
            writer.RenderBeginTag(HtmlTextWriterTag.Tr);

            if (_showCommands)
                foreach (var cmd in _columns.CommandColumns.Where(u => ((GridCommandColumn)u).ShowInTheFirst).OrderBy(u => ((GridColumn)u).Rank))
                    RenderCmd(writer, (GridCommandColumn)cmd, item);


            foreach (var col in _columns.DataColumns)
            {
                writer.RenderBeginTag(HtmlTextWriterTag.Td);
                var value = typeof(T).GetProperty(col.FieldName).GetValue(item, null) ?? String.Empty;
                var xr = _helper.RenderFor(value).ToString();
                if (string.IsNullOrWhiteSpace(xr))
                    writer.Write(value.ToString());
                else writer.Write(xr);
                writer.RenderEndTag();
            }

            if (_showCommands)
                foreach (var cmd in _columns.Where(u => ((GridColumn)u).ColumnType == GridColumnType.Command && !((GridCommandColumn)u).ShowInTheFirst).OrderBy(u => ((GridColumn)u).Rank))
                    RenderCmd(writer, (GridCommandColumn)cmd, item);

            writer.RenderEndTag();
        }

        private void RenderPagerRow(HtmlTextWriter writer)
        {
            // Don't show paging UI for only 1 page
            if (_items.PagingItem.TotalPageCount == 1)
                return;

            // Render page numbers
            writer.RenderBeginTag(HtmlTextWriterTag.Tr);
            writer.AddAttribute(HtmlTextWriterAttribute.Colspan, _columns.Count.ToString());
            writer.RenderBeginTag(HtmlTextWriterTag.Td);
            var currentAction = (string)_helper.ViewContext.RouteData.Values["action"];
            for (var i = 0; i < _items.PagingItem.TotalPageCount; i++)
            {
                if (i == _items.PagingItem.PageIndex)
                {
                    writer.Write(String.Format("<strong>{0}</strong> ", i + 1));
                }
                else
                {
                    var linkText = String.Format("{0}", i + 1);
                    var link = _helper.ActionLink(linkText, currentAction, new { page = i, sort = _items.PagingItem.SortExpression });
                    writer.Write(link + " ");
                }
            }
            writer.RenderEndTag();
            writer.RenderEndTag();
        }

        #endregion
    }

    public static class DataGridHelper
    {

        public static DataGrid<T> DataGrid<T>(this HtmlHelper<T> helper) where T : class
        {
            return new DataGrid<T>(helper);
        }

        public static HtmlString MyLabel<T, TProperty>(this HtmlHelper<T> helper, Expression<Func<T, TProperty>> expression)
        {
            var member = ModelMetadata.FromLambdaExpression(expression, helper.ViewData);
            return new HtmlString(member.DisplayName);
        }

        public static RenderHtmlGrid<T> DataGrid<T>(this HtmlHelper helper , HtmlGrid Grid , PagedList<T> Items) where T:class
        {
            return new RenderHtmlGrid<T>(helper,Grid,Items);
        }
    }

}