using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace CSD.MVC.Helpers
{
    public static class SubmitButtonHelper
    {
        /// <summary>
        /// Renders an HTML form submit button
        /// </summary>
        public static string SubmitButton(this HtmlHelper helper, string buttonText)
        {
            return String.Format("<input type=\"submit\" value=\"{0}\" />", buttonText);
        }

    }
}