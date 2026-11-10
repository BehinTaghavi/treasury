using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Linq.Expressions;
using System.Web.Mvc;
using System.Web.UI;
using MvcContrib.FluentHtml.Behaviors;

namespace APS.MVC.Helpers
{
    public interface IViewModelContainer<T> : IViewDataContainer where T : class
    {
        T ViewModel { get; }
        IEnumerable<IBehaviorMarker> Behaviors { get; }
        string HtmlNamePrefix { get; set; }
    }

    public static class Radio
    {
        public static string RadioButtonList<T>(this IViewModelContainer<T> container, Expression<Func<T, object>> expression, IEnumerable<string> items) where T : class
        {
            var func = expression.Compile();
            var result = func(container.ViewModel);
            var selectList = new SelectList(items, result);
            return container.RadioButtonList(expression, selectList);
        }

        public static string RadioButtonList<T>(this IViewModelContainer<T> container, Expression<Func<T, object>> expression, IEnumerable<SelectListItem> items) where T : class
        {
            TagBuilder tableTag = new TagBuilder("table");
            tableTag.AddCssClass("radio-main");

            var trTag = new TagBuilder("tr");
            foreach (var item in items)
            {
                var tdTag = new TagBuilder("td");
                var radioTag = container.RadioButton(expression).Value(item.Value ?? item.Text).Checked(item.Selected).LabelAfter(item.Text);
                tdTag.InnerHtml = radioTag.ToString();

                trTag.InnerHtml += tdTag.ToString();
            }
            tableTag.InnerHtml = trTag.ToString();

            return tableTag.ToString();
        }
    }
}