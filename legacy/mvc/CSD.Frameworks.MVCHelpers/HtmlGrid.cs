using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.IO;
using System.Web.Mvc;
using System.Web.Mvc.Html;
using CSD.Common.Models;
using System.Web.Routing;

namespace CSD.MVC.Helpers
{

    public class GridCommandColumn : GridColumn
    {

        public string Action { get; set; }
        public string Controller { get; set; }
        public string Area { get; set; }

        public bool ShowAsImageLink { get; set; }
        public string ImageSrc { get; set; }

        public string LinkTitle { get; set; }
        public string IdFieldName { get; set; }
        public bool ShowInTheFirst { get; set; }

        public object ImageAttributes { get; set; }
        public object LinkAttributes { get; set; }

        public GridCommandColumn()
        {
            ShowAsImageLink = false;
            ShowInTheFirst = true;
        }
    }

    public enum GridColumnType : byte { Data = 1, Command = 2 };

    public class GridColumn
    {
        public string FieldName { get; set; }
        public int Rank { get; set; }
        public GridColumnType ColumnType { get; set; }
        public object HtmlAtttibutes { get; set; }
        public string HeaderTitle { get; set; }
        public object HeaderHtmlAttributes { get; set; }
        public bool AllowSort { get; set; }

        public GridColumn()
        {
            AllowSort = true;
            Rank = 1;
            ColumnType = GridColumnType.Data;
            HtmlAtttibutes = null;
            HeaderHtmlAttributes = null;
            HeaderTitle = "Title";
        }

        public GridColumn(string fieldName)
        {
            AllowSort = true;
            FieldName = fieldName;
            Rank = 1;
            ColumnType = GridColumnType.Data;
            HtmlAtttibutes = null;
            HeaderHtmlAttributes = null;
            HeaderTitle = fieldName;
        }

        public GridColumn(string fieldName, string headerTitle)
        {
            AllowSort = true;
            FieldName = fieldName;
            Rank = 1;
            ColumnType = GridColumnType.Data;
            HtmlAtttibutes = null;
            HeaderHtmlAttributes = null;
            HeaderTitle = headerTitle;
        }

    }

    public class GridColumns : List<GridColumn>
    {
        public GridColumns AddColumn(GridColumn col, bool AutoSetRank = true)
        {
            if (AutoSetRank)
                col.Rank = (this.Count + 1);
            this.Add(col);
            return this;
        }

        public GridColumns AddCommand(GridCommandColumn cmd, bool AutoSetRank = true)
        {
            if (AutoSetRank)
                cmd.Rank = (this.Count + 1);
            this.Add(cmd);
            return this;
        }

        public GridColumns AddDefaultCommands(string controller = "", string area = "")
        {
            this.Add(new GridCommandColumn() { ColumnType = GridColumnType.Command, ShowAsImageLink = true, Action = "Edit", ImageSrc = "~/images/icon/16x16/document-edit.png", LinkTitle = "Edit", Controller = controller, ShowInTheFirst = true, Area = area });
            this.Add(new GridCommandColumn() { ColumnType = GridColumnType.Command, ShowAsImageLink = true, Action = "Delete", ImageSrc = "~/images/icon/16x16/delete.png", LinkTitle = "Delete", Controller = controller, ShowInTheFirst = false, Area = area });
            return this;
        }

        public bool HasRowNumber
        {
            get;
            set;
        }

        public GridColumns DataColumns
        {
            get
            {
                var qry = this.Where(u => u.ColumnType == GridColumnType.Data).ToList<GridColumn>();
                return new GridColumns(qry);
            }
        }
        public GridColumns CommandColumns
        {
            get
            {
                var qry = this.Where(u => u.ColumnType == GridColumnType.Command).ToList<GridColumn>();
                return new GridColumns(qry);
            }
        }

        public GridColumns()
        {

        }

        public GridColumns(List<GridColumn> cols)
        {
            this.AddRange(cols);
        }

    }

    public static class GridColumnHelpers
    {
        public static MvcHtmlString GridColumnHeaderLink(this HtmlHelper helper, GridColumn col)
        {
            var currentAction = (string)helper.ViewContext.RouteData.Values["action"];
            return helper.ActionLink(col.HeaderTitle, currentAction, new { sort = col.FieldName });
        }
    }

    #region HTMLGrid

    public class HtmlGrid
    {
        object _htmlAttribute;
        object _rowAttribute;
        object _altRowAttribute;
        object _headerAttribute;
        bool _addDefaultCommands = false;

        GridColumns _columns;

        #region Grid Properties

        public object TableAttributes
        {
            get
            {
                return _htmlAttribute;
            }
        }
        public object TableHeaderAttributes
        {
            get
            {
                return _headerAttribute;
            }
        }
        public object TableRowAttributes
        {
            get
            {
                return _rowAttribute;
            }
        }
        public object TableAltRowAttributes
        {
            get
            {
                return _altRowAttribute;
            }
        }

        public GridColumns TableColumns
        {
            get
            {
                return _columns;
            }
        }

        public bool AddDefaultCommands
        {
            get
            {
                return _addDefaultCommands;
            }
        }

        #endregion

        public HtmlGrid()
        {
            _columns = new GridColumns();
        }

        #region FluentMethods

        public HtmlGrid ShowRowNumber()
        {
            this._columns.HasRowNumber = true;
            return this;
        }
        public HtmlGrid Columns(GridColumns columns)
        {
            this._columns = columns;
            return this;
        }

        public HtmlGrid AddColumn(GridColumn col, bool AutoSetRank = true)
        {
            this._columns.AddColumn(col, AutoSetRank);
            return this;
        }

        public HtmlGrid AddColumn(string fieldName, string Title)
        {
            this.AddColumn(new GridColumn(fieldName, Title), true);
            return this;
        }

        public HtmlGrid AddColumn(string fieldName)
        {
            this.AddColumn(new GridColumn(fieldName), true);
            return this;
        }

        public HtmlGrid TableAttribute(object attr)
        {
            this._htmlAttribute = attr;
            return this;
        }

        public HtmlGrid AltRowAttribute(object attr)
        {
            this._altRowAttribute = attr;
            return this;
        }

        public HtmlGrid RowAttribute(object attr)
        {
            this._rowAttribute = attr;
            return this;
        }

        public HtmlGrid HeadAttribute(object attr)
        {
            this._headerAttribute = attr;
            return this;
        }

        public HtmlGrid AddCommand(GridCommandColumn cmd, bool AutoSetRank = true)
        {
            cmd.ColumnType = GridColumnType.Command;
            this._columns.AddCommand(cmd, AutoSetRank);
            return this;
        }

        public HtmlGrid AddDefaulrCommands()
        {
            _addDefaultCommands = true;
            return this;
        }
        #endregion

    }

    public class RenderHtmlGridHeader : IHtmlString
    {
        GridColumns columns;
        bool showCommands;
        HtmlHelper helper;
        object headerAttributes;

        public RenderHtmlGridHeader(HtmlHelper _helper, GridColumns _columns, object _headerAttribute)
        {
            helper = _helper;
            columns = _columns;
            showCommands = _columns.CommandColumns.Any();
            headerAttributes = _headerAttribute;
        }

        public string ToHtmlString()
        {
            using (var writer = new HtmlTextWriter(new StringWriter()))
            {
                var attributes = HtmlHelper.AnonymousObjectToHtmlAttributes(this.headerAttributes);
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
                writer.RenderBeginTag(HtmlTextWriterTag.Thead);
                writer.RenderBeginTag(HtmlTextWriterTag.Tr);
                if (columns.HasRowNumber)
                {
                    writer.RenderBeginTag(HtmlTextWriterTag.Th);
                    writer.RenderEndTag();
                }
                if (showCommands)
                    for (int i = 0; i < columns.CommandColumns.Where(u => ((GridCommandColumn)u).ShowInTheFirst).Count(); i++)
                    {
                        writer.RenderBeginTag(HtmlTextWriterTag.Th);
                        writer.RenderEndTag();
                    }

                foreach (var col in columns.DataColumns)
                {
                    writer.RenderBeginTag(HtmlTextWriterTag.Th);
                    var link = helper.GridColumnHeaderLink((GridColumn)col);
                    writer.Write(link);
                    writer.RenderEndTag();
                }
                if (showCommands)
                    for (int i = 0; i < columns.CommandColumns.Where(u => !((GridCommandColumn)u).ShowInTheFirst).Count(); i++)
                    {
                        writer.RenderBeginTag(HtmlTextWriterTag.Th);
                        writer.RenderEndTag();
                    }
                writer.RenderEndTag();
                writer.RenderEndTag();
                return writer.InnerWriter.ToString();
            }
        }
    }

    public class RenderHtmlGridBody<T> : IHtmlString where T : class
    {
        #region private members

        GridColumns columns;
        bool showCommands;
        HtmlHelper helper;
        object AltRowAttribute;
        object RowAttribute;
        PagedList<T> items;
        #endregion

        public RenderHtmlGridBody(HtmlHelper _helper, GridColumns _columns, PagedList<T> _items, object _rowAttribute, object _altRowAttribute)
        {
            helper = _helper;
            columns = _columns;
            showCommands = _columns.CommandColumns.Any();
            RowAttribute = _rowAttribute;
            AltRowAttribute = _altRowAttribute;

            items = _items;
        }

        private void RenderCmd(HtmlTextWriter writer, GridCommandColumn cmd, T item)
        {
            writer.RenderBeginTag(HtmlTextWriterTag.Td);

            var idValue = cmd.GetItemIdValue<T>(item);
            var imglink = cmd.MakeCmdImageLink(helper, idValue);

            writer.Write(imglink.ToString());
            writer.RenderEndTag();
        }

        private void RenderRow(HtmlTextWriter writer, T item, int i)
        {
            writer.RenderBeginTag(HtmlTextWriterTag.Tr);
            if (columns.HasRowNumber)
            {
                writer.RenderBeginTag(HtmlTextWriterTag.Td);
                writer.Write(i);
                writer.RenderEndTag();
            }
            if (showCommands)
                foreach (var cmd in columns.CommandColumns.Where(u => ((GridCommandColumn)u).ShowInTheFirst).OrderBy(u => ((GridColumn)u).Rank))
                    RenderCmd(writer, (GridCommandColumn)cmd, item);

            foreach (var col in columns.DataColumns)
            {
                writer.RenderBeginTag(HtmlTextWriterTag.Td);
                string fieldName = col.FieldName;
                if (fieldName.IndexOf('.') > -1)
                {
                    string[] ff = fieldName.Split('.');
                    
                    var z1 = typeof(T);
                    var z0 = z1.GetNestedTypes();
                    var z2 = z1.GetNestedType(ff[0]);         
                    
                    var z3 = z2.GetProperty(ff[1]);
                    var z = z3.GetValue(z2, null);
                }
                var value = typeof(T).GetProperty(col.FieldName).GetValue(item, null) ?? String.Empty;
                var xr = "";
                try
                {
                    HtmlHelper<T> Thelper = new HtmlHelper<T>(helper.ViewContext, helper.ViewDataContainer);
                    xr = Thelper.RenderFor(value).ToString();
                }
                catch
                {

                }
                if (string.IsNullOrWhiteSpace(xr))
                    writer.Write(value.ToString());
                else writer.Write(xr);
                writer.RenderEndTag();
            }

            if (showCommands)
                foreach (var cmd in columns.Where(u => ((GridColumn)u).ColumnType == GridColumnType.Command && !((GridCommandColumn)u).ShowInTheFirst).OrderBy(u => ((GridColumn)u).Rank))
                    RenderCmd(writer, (GridCommandColumn)cmd, item);

            writer.RenderEndTag();
        }

        public string ToHtmlString()
        {
            using (var writer = new HtmlTextWriter(new StringWriter()))
            {
                writer.RenderBeginTag(HtmlTextWriterTag.Tbody);
                var RowAttr = HtmlHelper.AnonymousObjectToHtmlAttributes(this.RowAttribute);
                var AltRowAttr = HtmlHelper.AnonymousObjectToHtmlAttributes(this.AltRowAttribute);
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
                    i++;

                    RenderRow(writer, item, (items.PagingItem.PageIndex * items.PagingItem.PageSize) + i);
                }
                writer.RenderEndTag();
                return writer.InnerWriter.ToString();
            }
        }
    }

    public class RenderHtmlGridFooter : IHtmlString
    {
        GridColumns columns;
        bool showCommands;
        HtmlHelper helper;
        object footerAttributes;
        PagingItems PagingItem;

        public RenderHtmlGridFooter(HtmlHelper _helper, GridColumns _columns, PagingItems _pagingItem, object _footerAttribute)
        {
            columns = _columns;
            showCommands = _columns.CommandColumns.Any();
            PagingItem = _pagingItem;
            helper = _helper;
            footerAttributes = _footerAttribute;

        }

        public string ToHtmlString()
        {
            // Don't show paging UI for only 1 page
            if (PagingItem.TotalPageCount == 1)
                return "";

            // Render page numbers
            using (var writer = new HtmlTextWriter(new StringWriter()))
            {
                writer.RenderBeginTag(HtmlTextWriterTag.Tfoot);
                writer.RenderBeginTag(HtmlTextWriterTag.Tr);
                if (columns.HasRowNumber)
                {
                    writer.RenderBeginTag(HtmlTextWriterTag.Td);
                    writer.RenderEndTag();
                }
                if (showCommands)
                    writer.AddAttribute(HtmlTextWriterAttribute.Colspan, columns.Count.ToString());
                else
                    writer.AddAttribute(HtmlTextWriterAttribute.Colspan, columns.DataColumns.Count.ToString());

                writer.RenderBeginTag(HtmlTextWriterTag.Td);
                var currentAction = (string)helper.ViewContext.RouteData.Values["action"];
                for (var i = 0; i < PagingItem.TotalPageCount; i++)
                {
                    if (i == PagingItem.PageIndex)
                    {
                        writer.Write(String.Format("<strong>{0}</strong> ", i + 1));
                    }
                    else
                    {
                        var linkText = String.Format("{0}", i + 1);
                        var link = helper.ActionLink(linkText, currentAction, new { page = i, sort = PagingItem.SortExpression });
                        writer.Write(link + " ");
                    }
                }
                writer.RenderEndTag();
                writer.RenderEndTag();
                writer.RenderEndTag();
                return writer.InnerWriter.ToString();
            }
        }
    }

    public class RenderHtmlGrid<T> : IHtmlString where T : class
    {
        HtmlGrid Grid;
        HtmlHelper Helper;
        PagedList<T> Items;

        public RenderHtmlGrid(HtmlHelper helper, HtmlGrid grid, PagedList<T> items)
        {
            Grid = grid;
            Helper = helper;
            Items = items;

            if (Grid.AddDefaultCommands)
            {
                RouteData routeData = helper.ViewContext.RouteData;
                var _controller = routeData.GetRequiredString("controller");
                var area = routeData.DataTokens["area"];
                var areaStr = "";
                if (area != null) areaStr = area.ToString();
                Grid.TableColumns.AddDefaultCommands(_controller, areaStr);
            }
        }

        public string ToHtmlString()
        {
            using (var writer = new HtmlTextWriter(new StringWriter()))
            {

                var attributes = HtmlHelper.AnonymousObjectToHtmlAttributes(Grid.TableAttributes);
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

                RenderHtmlGridHeader rhead = new RenderHtmlGridHeader(Helper, Grid.TableColumns, Grid.TableHeaderAttributes);
                writer.Write(rhead.ToHtmlString());

                RenderHtmlGridBody<T> rbody = new RenderHtmlGridBody<T>(Helper, Grid.TableColumns, Items, Grid.TableRowAttributes, Grid.TableAltRowAttributes);
                writer.Write(rbody.ToHtmlString());

                RenderHtmlGridFooter rfooter = new RenderHtmlGridFooter(Helper, Grid.TableColumns, Items.PagingItem, null);
                writer.Write(rfooter.ToHtmlString());

                writer.RenderEndTag();
                return writer.InnerWriter.ToString();
            }
        }
    }

    #endregion
}