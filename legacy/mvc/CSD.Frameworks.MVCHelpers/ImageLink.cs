using System.Web.Mvc;
using System.Web.Mvc.Ajax;
using System.Web.Mvc.Html;
using System.Web.UI;
using System.IO;

namespace APS.MVC.Helpers
{
    public static class ImageLink
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="helper"></param>
        /// <param name="imageUrl"></param>
        /// <param name="altText"></param>
        /// <param name="actionName"></param>
        /// <param name="controllerName"></param>
        /// <param name="routeValues"></param>
        /// <param name="linkHtmlAttributes"></param>
        /// <param name="imgHtmlAttributes"></param>
        /// <returns></returns>
        public static MvcHtmlString ImageActionLink(this HtmlHelper helper,
            string imageUrl,
            string altText,
            string actionName,
            string controllerName,
            object routeValues,
            object linkHtmlAttributes,
            object imgHtmlAttributes)
        {
            var attributes = HtmlHelper.AnonymousObjectToHtmlAttributes(imgHtmlAttributes);

            using (var sw = new StringWriter())
            {
                using (var htm = new HtmlTextWriter(sw))
                {
                    htm.AddAttribute(HtmlTextWriterAttribute.Src, imageUrl);
                    htm.AddAttribute(HtmlTextWriterAttribute.Alt, altText);

                    foreach (var key in attributes.Keys)
                    {
                        var value = attributes[key];
                        string valueAsString = null;
                        if (value != null)
                        {
                            valueAsString = value.ToString();
                        }
                        htm.AddAttribute(key, valueAsString);
                    }
                    htm.RenderBeginTag(HtmlTextWriterTag.Img);
                    htm.RenderEndTag();

                    var link = helper.ActionLink("[placeholder]", actionName, controllerName, routeValues, linkHtmlAttributes);
                    var text = link.ToHtmlString();
                    text = text.Replace("[placeholder]", sw.ToString());

                    return MvcHtmlString.Create(text);
                }
            }
        }


        public static MvcHtmlString ImageActionLink(this AjaxHelper helper, string imageUrl, string altText
            , string actionName
            , string controllerName
            , object routeValues, AjaxOptions ajaxOptions,
            object linkHtmlAttributes,
            object imgHtmlAttributes)
        {
            var attributes = HtmlHelper.AnonymousObjectToHtmlAttributes(imgHtmlAttributes);

            using (var sw = new StringWriter())
            {
                using (var htm = new HtmlTextWriter(sw))
                {
                    htm.AddAttribute(HtmlTextWriterAttribute.Src, imageUrl);
                    htm.AddAttribute(HtmlTextWriterAttribute.Alt, altText);

                    foreach (var key in attributes.Keys)
                    {
                        var value = attributes[key];
                        string valueAsString = null;
                        if (value != null)
                        {
                            valueAsString = value.ToString();
                        }
                        htm.AddAttribute(key, valueAsString);
                    }
                    htm.RenderBeginTag(HtmlTextWriterTag.Img);
                    htm.RenderEndTag();

                    
                    var link = helper.ActionLink("[replaceme]", actionName, controllerName, routeValues, ajaxOptions, linkHtmlAttributes).ToHtmlString();
                    return MvcHtmlString.Create(link.Replace("[replaceme]", sw.ToString()));
                }
            }
            
        }


    }
}
