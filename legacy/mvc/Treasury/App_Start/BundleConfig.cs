using System.Web;
using System.Web.Optimization;

namespace CSD.Treasury.UI
{
    public class BundleConfig
    {
        // For more information on bundling, visit http://go.microsoft.com/fwlink/?LinkId=301862
        public static void RegisterBundles(BundleCollection bundles)
        {
            //BundleTable.EnableOptimizations = true;
            //bundles.UseCdn = true;

            bundles.Add(new ScriptBundle("~/bundles/jquery").Include(
                        "~/Scripts/jquery-{version}.js",
                        "~/Scripts/jquery.unobtrusive-ajax.js"
                        ));

            bundles.Add(new ScriptBundle("~/bundles/jqueryval").Include(
                        "~/Scripts/jquery.validate.js",
                        "~/Scripts/jquery.validate.unobtrusive.js"));

            // Use the development version of Modernizr to develop with and learn from. Then, when you're
            // ready for production, use the build tool at http://modernizr.com to pick only the tests you need.
            bundles.Add(new ScriptBundle("~/bundles/modernizr").Include(
                        "~/Scripts/modernizr-*"));

            bundles.Add(new ScriptBundle("~/bundles/bootstrap").Include(
                      "~/Scripts/moment.min.js",
                      "~/Scripts/moment-with-locales.min.js",
                      "~/Scripts/bootstrap.js",
                      "~/Scripts/bootstrap-datepicker.min.js",
                      "~/Scripts/bootstrap-datepicker.fa.min.js",
                      "~/Scripts/bootstrap-timepicker.js",
                      "~/Scripts/moment-jalaali.js",
                      "~/Scripts/material.min.js",
                      "~/Scripts/ripples.min.js",
                      "~/Scripts/snackbar.min.js",
                      "~/Scripts/respond.js",
                      "~/Scripts/bootstrap-sortable.js",
                        "~/Scripts/alertify.min.js",
                        "~/Scripts/responsive-paginate.js"
                      ));

            bundles.Add(new ScriptBundle("~/bundles/admin").Include(
                      "~/Scripts/metisMenu.js",
                      "~/Scripts/sb-admin-2.js"));

            bundles.Add(new ScriptBundle("~/bundles/bootstrap-rtl").Include(
                      "~/Scripts/bootstrap-rtl.js"));

            bundles.Add(new StyleBundle("~/Content/css").Include(
                      "~/Content/css/spinner.css",
                      "~/Content/css/bootstrap.css",
                      "~/Content/css/bootstrap-datepicker.min.css",
                      "~/Content/css/bootstrap-timepicker.css",
                      "~/Content/css/material.css",
                      "~/Content/css/material-fullpalette.css",
                      "~/Content/css/ripples.css",
                      "~/Content/css/roboto.css",
                      "~/Content/css/snackbar.min.css",
                      "~/Content/css/custom-bootstrap.css",
                      "~/Content/css/material-design-iconic-font.min.css",
                      "~/Content/css/animate.min.css",
                      "~/Content/css/material-design-color-palette.min.css",
                      "~/Content/css/alertify.css",
                      "~/Content/css/alertify.rtl.css",
                      "~/Content/css/style.css"));

            bundles.Add(new StyleBundle("~/Content/admin").Include(
                      "~/Content/css/bootstrap-rtl.css",
                      "~/Content/css/metisMenu.min.css",
                      "~/Content/css/timeline.css",
                      "~/Content/css/sb-admin-2.css",
                      "~/Content/css/morris.css",
                      "~/Content/css/font-awesome.min.css",
                      "~/Content/css/bootstrap-sortable.css"));

            bundles.Add(new StyleBundle("~/Content/css-rtl").Include(
                      "~/Content/css/bootstrap-rtl.css",
                      "~/Content/css/custom-bootstrap.css",
                      "~/Content/css/skins/default.css",
                      "~/Content/css/material-design-iconic-font.min.css",
                      "~/Content/css/animate.min.css",
                      "~/Content/css/material-design-color-palette.min.css",
                      "~/Content/css/style.css"));

            bundles.Add(new StyleBundle("~/Content/account").Include(
                      "~/Content/css/account.css"));

        }
    }
}
