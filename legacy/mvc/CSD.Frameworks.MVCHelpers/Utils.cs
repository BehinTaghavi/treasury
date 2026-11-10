using System;
using System.Collections.Generic;
using System.Data.Linq.Mapping;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Web;
using System.Web.Mvc;
using System.Web.Mvc.Html;

namespace CSD.MVC.Helpers
{
    public class Utils
    {
        public object GetItemIdValue<T>(GridCommandColumn cmd, T item) where T : class
        {
            if (string.IsNullOrEmpty(cmd.IdFieldName))
            {
                PropertyInfo[] infos = typeof(T).GetProperties();
                PropertyInfo PKProperty = null;
                foreach (PropertyInfo info in infos)
                {
                    var column = info.GetCustomAttributes(false).Where(x => x.GetType() == typeof(ColumnAttribute))
                                    .FirstOrDefault(x => ((ColumnAttribute)x).IsPrimaryKey && ((ColumnAttribute)x).DbType.Contains("NOT NULL"));
                    if (column != null)
                    {
                        PKProperty = info;
                        break;
                    }
                }
                if (PKProperty != null)
                {
                    cmd.IdFieldName = PKProperty.Name;
                }
            }
            var idValue = typeof(T).GetProperty(cmd.IdFieldName).GetValue(item, null);
            return idValue;
        }

        public ImageLink MakeCmdImageLink(HtmlHelper _helper, GridCommandColumn cmd, object idValue)
        {
            var imglink = new ImageLink(_helper).Action(cmd.Action).Controller(cmd.Controller).Src(cmd.ImageSrc)
                .LinkAttribute(cmd.LinkAttributes).ImageAttribute(cmd.ImageAttributes);
            if (!string.IsNullOrEmpty(cmd.Area))
            {
                imglink = imglink.RouteValue(new { Id = idValue, area = cmd.Area });
            }
            else
            {
                imglink = imglink.RouteValue(new { Id = idValue });
            }
            return imglink;
        }

    }

    //public class RenderUtility<T>
    //{
    //    public MvcHtmlString RenderObj<T, TValue>(HtmlHelper<T> html, Expression<Func<T, TValue>> expression)
    //    {
    //        return html.DisplayFor(expression);
    //    }
    //}

    public static class StaticUtils
    {
        public static object GetItemIdValue<T>(this GridCommandColumn cmd, T item) where T : class
        {
            var u = new Utils();
            return u.GetItemIdValue<T>(cmd, item);
        }

        public static ImageLink MakeCmdImageLink(this GridCommandColumn cmd, HtmlHelper _helper, object idValue)
        {
            var imglink = new ImageLink(_helper).Action(cmd.Action).Controller(cmd.Controller).Src(cmd.ImageSrc)
                .LinkAttribute(cmd.LinkAttributes).ImageAttribute(cmd.ImageAttributes);
            if (!string.IsNullOrEmpty(cmd.Area))
            {
                imglink = imglink.RouteValue(new { Id = idValue, area = cmd.Area });
            }
            else
            {
                imglink = imglink.RouteValue(new { Id = idValue });
            }
            return imglink;
        }

        public static MvcHtmlString RenderFor<TModel, TValue>(this HtmlHelper<TModel> html, TValue item)
        {
            return html.DisplayFor(modelitem => item); 
        }
    }
}