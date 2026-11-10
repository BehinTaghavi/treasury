using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Web.Mvc;
using System.Web.Mvc.Ajax;
using System.Web.Mvc.Html;
using System.Web;

namespace CSD.MVC.Helpers
{
    public class IActionLink : IHtmlString
    {
       
        string _innerText = "";
        string _title = "";
        string _controller = "";
        string _action = "";
        string _iclass = "";        
        object _routeValue = null;
        object _linkAttribute = null;       
        HtmlHelper _helper = null;

        public IActionLink(HtmlHelper helper)
        {
            this._helper = helper;
        }       

        public IActionLink Controller(string controllerName)
        {
            this._controller = controllerName;
            return this;
        }

        public IActionLink Action(string actionName)
        {
            this._action = actionName;
            return this;
        }

        public IActionLink RouteValue(object routeValue)
        {
            this._routeValue = routeValue;
            return this;
        }       

        public IActionLink LinkAttribute(object linkAttr)
        {
            this._linkAttribute = linkAttr;
            return this;
        }        

        public IActionLink Title(string titleText)
        {
            this._title = titleText;
            return this;
        }
        public IActionLink iClass(string ClassName)
        {
            this._iclass = ClassName;
            return this;
        }
        public IActionLink InnerText(string Inner)
        {
            this._innerText = Inner;
            return this;
        }

        public override string ToString()
        {           
                      
            //builder.MergeAttribute("alt", this._innerText);
                    
            var link = _helper.ActionLink("[placeholder]", _action, _controller, _routeValue, _linkAttribute);
            var text = link.ToHtmlString();           
            var ibuilder = new TagBuilder("i");
            ibuilder.MergeAttribute("class", this._iclass);
            text = text.Replace("[placeholder]", ibuilder.ToString(TagRenderMode.StartTag)+"</i>"+this._innerText );
            return text;
        }
        string IHtmlString.ToHtmlString()
        {
            return this.ToString();
        }
    }

    public static class IActionLinks
    {
        public static IActionLink IActionLink(this HtmlHelper helper)
        {
            return new IActionLink(helper);
        }


        public static IHtmlString IActionLink(this HtmlHelper helper,           
            string innerText,
            string actionName,
            string controllerName,
            object routeValues,
            object linkHtmlAttributes ,
            string iClassName)
        {
            var a = new IActionLink(helper);
            a
                .LinkAttribute(linkHtmlAttributes)
                .Action(actionName)
                .Controller(controllerName)
                .RouteValue(routeValues)
                .iClass(iClassName)
                .InnerText(innerText);
            return new HtmlString(a.ToString());
        }


        public static IHtmlString IActionLink(this AjaxHelper helper, string innerText
            , string actionName
            , string controllerName
            , object routeValues, AjaxOptions ajaxOptions,
            object linkHtmlAttributes,
            string ClassName)
        {           
            var builder = new TagBuilder("a");                                           
            var link = helper.ActionLink("[replaceme]", actionName, controllerName, routeValues, ajaxOptions, linkHtmlAttributes).ToHtmlString();
            var ibuilder = new TagBuilder("i");
            ibuilder.MergeAttribute("class", ClassName);

            return new HtmlString(link.Replace("[replaceme]", ibuilder.ToString(TagRenderMode.StartTag) + "</i>" + innerText));
        }

    }
}