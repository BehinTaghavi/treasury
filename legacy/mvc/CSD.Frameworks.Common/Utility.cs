using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Configuration;
using System.Web.Configuration;

namespace CSD.Common.Utility
{

    public class DateUtils
    {
        public DateUtils()
        {
            //
            // TODO: Add constructor logic here
            //
        }

        public static bool ValidateJilaliDate(string input, out DateTime jilaliDate)
        {
            jilaliDate = DateTime.MinValue;
            try
            {
                string[] dateSplits = input.Split(new char[] { '/' }, 3);
                int day, month, year;
                year = int.Parse(dateSplits[0]);
                month = int.Parse(dateSplits[1]);
                day = int.Parse(dateSplits[2]);
                if (day > 31 && year <= 31)
                {
                    int temp = year;
                    year = day;
                    day = temp;
                    string tempSplit = dateSplits[0];
                    dateSplits[0] = dateSplits[2];
                    dateSplits[2] = tempSplit;
                }
                if (dateSplits[0].Length <= 2)
                    year += 1300;

                PersianCalendar pCal = new PersianCalendar();
                DateTime date = pCal.ToDateTime(year, month, day, 0, 0, 0, 0);
                jilaliDate = date;
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static string Gregorian2Jilali(DateTime date)
        {
            if (date != DateTime.MinValue)
            {
                PersianCalendar pCal = new PersianCalendar();
                return String.Format("{0}/{1}/{2}", pCal.GetYear(date),
                    pCal.GetMonth(date), pCal.GetDayOfMonth(date));
            }
            else
                return string.Empty;
        }
        public static string Gregorian2Jilali(DateTime dt, string seperator = "/")
        {
            PersianCalendar pc = new PersianCalendar();
            return pc.GetYear(dt) + seperator + pc.GetMonth(dt) + seperator + pc.GetDayOfMonth(dt);
        }
        public static string JilaliYear(DateTime date)
        {
            if (date != DateTime.MinValue)
            {
                PersianCalendar pCal = new PersianCalendar();
                return String.Format("{0}", pCal.GetYear(date));
            }
            else
                return string.Empty;
        }

        public static int GetCurrentYear()
        {
            int year = Convert.ToInt32(DateUtils.JilaliYear(DateTime.Now));
            if (year > 100) year = year % 100;
            return year;
        }
        /// <summary>
        /// تبدیل تاریخ شمسی به میلادی
        /// </summary>
        /// <param name="date">yyyy/mm/dd</param>
        /// <param name="seperator">seperator default "/"</param>
        /// <returns></returns>
        public static DateTime Jalali2Gregorian(string date, string seperator, string Time)
        {
            try
            {
                int indexFirst = date.IndexOf(seperator);
                int indexLast = date.LastIndexOf(seperator);
                string yy = date.Substring(0, indexFirst);
                string mm = date.Substring(indexFirst + 1, indexLast - (indexFirst + 1));
                string dd = date.Substring(indexLast + 1);
                PersianCalendar pc = new PersianCalendar();
                if (Time != "")
                {
                    var time = Time.Split(':');
                    if (time[0] == "24")
                        time[0] = "0";
                    return pc.ToDateTime(int.Parse(yy), int.Parse(mm), int.Parse(dd), int.Parse(time[0]), int.Parse(time[1]), int.Parse(time[2]), 0);
                }
                return pc.ToDateTime(int.Parse(yy), int.Parse(mm), int.Parse(dd), 0, 0, 0, 0);
            }
            catch (Exception)
            {
                return DateTime.MinValue;
            }
        }

        public static DateTime Jalali2Gregorian(int yy, int mm, int dd)
        {
            try
            {
                PersianCalendar pc = new PersianCalendar();
                return pc.ToDateTime(yy, mm, dd, 0, 0, 0, 0);
            }
            catch (Exception)
            {
                return DateTime.MinValue;
            }
        }
    }

    //public class ExportUtils
    //{
    //    Hashtable htControls = new Hashtable();

    //    private void PrepareGridViewForExport(Control gv)
    //    {

    //        Literal l = new Literal();

    //        for (int i = 0; i < gv.Controls.Count; i++)
    //        {

    //            if ((null != htControls[gv.Controls[i].GetType().Name]) || (null != htControls[gv.Controls[i].GetType

    //               ().BaseType.Name]))
    //            {

    //                l.Text = GetControlPropertyValue(gv.Controls[i]);

    //                gv.Controls.Remove(gv.Controls[i]);

    //                gv.Controls.AddAt(i, l);

    //            }

    //            if (gv.Controls[i].HasControls())
    //            {

    //                PrepareGridViewForExport(gv.Controls[i]);

    //            }

    //        }

    //    }

    //    private string GetControlPropertyValue(Control control)
    //    {

    //        Type controlType = control.GetType();

    //        string strControlType = controlType.Name;

    //        string strReturn = "";

    //        bool bReturn;



    //        PropertyInfo[] ctrlProps = controlType.GetProperties();

    //        string ExcelPropertyName = (string)htControls[strControlType];

    //        if (ExcelPropertyName == null)
    //        {

    //            ExcelPropertyName = (string)htControls[control.GetType().BaseType.Name];

    //            if (ExcelPropertyName == null)

    //                return strReturn;

    //        }

    //        foreach (PropertyInfo ctrlProp in ctrlProps)
    //        {

    //            if (ctrlProp.Name == ExcelPropertyName && ctrlProp.PropertyType == typeof(String))
    //            {

    //                try
    //                {

    //                    strReturn = (string)ctrlProp.GetValue(control, null);


    //                    break;

    //                }

    //                catch
    //                {

    //                    strReturn = "";

    //                }

    //            }

    //            if (ctrlProp.Name == ExcelPropertyName && ctrlProp.PropertyType == typeof(bool))
    //            {
    //                try
    //                {

    //                    bReturn = (bool)ctrlProp.GetValue(control, null);

    //                    strReturn = bReturn ? "درست" : "غلط";

    //                    break;

    //                }

    //                catch
    //                {

    //                    strReturn = "";

    //                }

    //            }

    //            if (ctrlProp.Name == ExcelPropertyName && ctrlProp.PropertyType == typeof(ListItem))
    //            {

    //                try
    //                {

    //                    strReturn = ((ListItem)(ctrlProp.GetValue(control, null))).Text;

    //                    break;

    //                }

    //                catch
    //                {

    //                    strReturn = "";

    //                }
    //            }

    //        }

    //        return strReturn;

    //    }

    //    private void ExportGridView(HttpResponse Response, GridView GridView1, string title)
    //    {
    //        Random rnd = new Random();
    //        int i = rnd.Next(1000);

    //        string attachment = "attachment; filename=File" + i.ToString() + ".xls";

    //        Response.ClearContent();

    //        Response.AddHeader("content-disposition", attachment);

    //        Response.ContentType = "application/ms-excel";

    //        StringWriter sw = new StringWriter();

    //        HtmlTextWriter htw = new HtmlTextWriter(sw);



    //        // Create a form to contain the grid

    //        HtmlForm frm = new HtmlForm();

    //        GridView1.Parent.Controls.Add(frm);

    //        frm.Attributes["runat"] = "server";

    //        frm.Controls.Add(GridView1);

    //        if (!string.IsNullOrEmpty(title))
    //        {
    //            string tmp = string.Format("<b>{0}</b>", title) + "<br/>";
    //            sw.Write(tmp);
    //        }

    //        frm.RenderControl(htw);



    //        Response.Write(sw.ToString());

    //        Response.End();

    //    }

    //    public void ExportToExcel(HttpResponse response, GridView grid, string title)
    //    {



    //        grid.AllowPaging = false;
    //        grid.DataBind();

    //        PrepareGridViewForExport(grid);

    //        ExportGridView(response, grid, title);
    //    }
    //    public void ExportToExcel(HttpResponse response, GridView grid, string title, bool withDataBind)
    //    {

    //        if (withDataBind)
    //        {
    //            grid.AllowPaging = false;
    //            grid.DataBind();
    //        }
    //        PrepareGridViewForExport(grid);

    //        ExportGridView(response, grid, title);
    //    }

    //    public ExportUtils()
    //    {
    //        htControls.Add("LinkButton", "Text");

    //        htControls.Add("HyperLink", "Text");

    //        htControls.Add("DropDownList", "SelectedItem");

    //        htControls.Add("CheckBox", "Checked");
    //        htControls.Add("Image", "ToolTip");
    //        htControls.Add("ImageButton", "");
    //    }
    //}

    //public class GridViewUtils
    //{
    //    public static void DisabledDeleteImageButton(GridViewRowEventArgs e, int deleteCellNo)
    //    {
    //        ImageButton imgdel = (ImageButton)e.Row.Cells[deleteCellNo].FindControl("DeleteImageButton");
    //        if (imgdel != null)
    //        {
    //            imgdel.ImageUrl =UIUtility.IconUrl+ "button/16x16/DeleteGray.gif";
    //            imgdel.Enabled = false;
    //            imgdel.ToolTip = "عدم امکان حذف";
    //        }
    //    }
    //    public static void DisabledEditImageButton(GridViewRowEventArgs e, int EditCellNo)
    //    {
    //        ImageButton imgdel = (ImageButton)e.Row.Cells[EditCellNo].FindControl("EditImageButton");
    //        if (imgdel != null)
    //        {
    //            imgdel.ImageUrl =UIUtility.IconUrl+ "button/16x16/EditGray.png";
    //            imgdel.Enabled = false;
    //            imgdel.ToolTip = "عدم امکان ویرایش";
    //        }
    //    }
    //}

    public class ImageUtils
    {
        public static string[] imageExtention = { ".jpg", ".jpeg", ".bmp", ".png", ".gif" };

        /// <summary>
        /// از عکس موجود نمای کوچک ایجاد کرده و در مسیر جدید ذخیره می نماید .
        /// </summary>
        /// <param name="filePath">مسیری که فایل اصلی در آن قرار دارد به همراه نام و پسوند فایل</param>
        /// <param name="newWith">اندازه فایل کوچک ، ابتدا سعی می کند پهنای فایل را با حفظ مقیاس به این اندازه در بیاورد اگر ارتفاع فایل بیشتر از
        /// پهنا باشد ارتفاع فایل را با حفظ مقیاس به این اندازه در می آورد .</param>
        /// <param name="newPath">مسیری که فایل کوچک را در آنجا ذخیره کند . این مسیر باید به همراه نام و پسوند فایل باشد .</param>
        public static void CreateThumbnail(string filePath, int newWith, string newPath)
        {
            string extension = System.IO.Path.GetExtension(filePath);

            if (imageExtention.Contains(extension) && File.Exists(filePath))
            {
                System.Drawing.Image image = System.Drawing.Image.FromFile(filePath);
                float imgWidth = image.PhysicalDimension.Width;
                float imgHeight = image.PhysicalDimension.Height;
                float imgSize = imgHeight > imgWidth ? imgHeight : imgWidth;

                //the approximately pixel size of thumbnail image is 150 x 150 based on ratio
                float imgResize = imgSize <= newWith ? (float)1.0 : newWith / imgSize;
                imgWidth *= imgResize;
                imgHeight *= imgResize;

                System.Drawing.Image thumb1 = image.GetThumbnailImage((int)imgWidth, (int)imgHeight, delegate () { return false; }, (IntPtr)0);

                string dir = Path.GetDirectoryName(newPath);

                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                thumb1.Save(newPath);
            }
        }

        public static MemoryStream CreateThumbnail(MemoryStream ms, int newWith)
        {

            if (ms != null && ms.Length > 0)
            {
                System.Drawing.Image image = System.Drawing.Image.FromStream(ms);
                float imgWidth = image.PhysicalDimension.Width;
                float imgHeight = image.PhysicalDimension.Height;
                float imgSize = imgHeight > imgWidth ? imgHeight : imgWidth;

                //the approximately pixel size of thumbnail image is 150 x 150 based on ratio
                float imgResize = imgSize <= newWith ? (float)1.0 : newWith / imgSize;
                imgWidth *= imgResize;
                imgHeight *= imgResize;

                System.Drawing.Image thumb1 = image.GetThumbnailImage((int)imgWidth, (int)imgHeight, delegate () { return false; }, (IntPtr)0);

                MemoryStream outms = new MemoryStream();

                thumb1.Save(outms, System.Drawing.Imaging.ImageFormat.Jpeg);

                return outms;
            }
            return null;
        }

        public static bool IsImageFileName(string fileName)
        {
            var ext = System.IO.Path.GetExtension(fileName).ToLower();

            if (imageExtention.Contains(ext))
                return true;

            return false;
        }
    }

    public class FileUtils
    {

        public static string[] DangerousExtensionArray = new string[]{".386",".ADE",".ADP",".ADT",".APP",".ASP",".BAS",
                    ".BAT",".BIN",".BTM",".CBT",".CHM",".CLA",".CLASS",".CMD",".COM",".CPL",".CRT",".CSC",
                    ".CSS",".DLL",".DOT",".EML",".EMAIL",".EXE",".FON",".HLP",".HTA",".HTM",".HTML",
                    ".INF",".INI",".INS",".ISP",".JS",".JSE",".LIB",".LNK",".MDB",".MDE",".MHT",".MHTM",
                    ".MHTML",".MSO",".MSC",".MSI",".MSP",".MST",".OBJ",".OBJ",".OV?",".PCD",".PGM",".PIF",
                    ".PRC",".REG",".SCR",".SCT",".SHB",".SHS",".SMM",".ASM",".C",".CPP",".PAS",".BAS",".FOR",
                    ".ASP",".ASPX",".CS",".SYS",".URL",".VB",".VBE",".VBS",".VXD",".WSC",".WSF",".WSH",
                    ".XL?",".IATA",".IMDG",".h"};

        /// <summary>
        /// یک فایل متنی را کامل خوانده و بازمی گرداند .
        /// </summary>
        /// <param name="strFilePath">مسیر کامل فایل به همراه نام و پسوند فایل</param>
        /// <returns></returns>
        public static string ReadTextFile(string strFilePath)
        {
            StreamReader rd;
            string strOutput = "";

            rd = File.OpenText(strFilePath);

            while (rd.Peek() != -1)
                strOutput += rd.ReadLine();

            rd.Close();

            return strOutput;
        }
    }

    public class StringUtils
    {
        public static string GetSubString(string text, int length)
        {
            text = text.Trim();
            if (string.IsNullOrEmpty(text) || length <= 0)
                return text;
            if (length >= text.Length)
                return text;
            string s = text.Substring(0, length);
            return s;
        }

        public static string StripTags(string HTML)
        {
            // Removes tags from passed HTML           
            System.Text.RegularExpressions.Regex objRegEx = new System.Text.RegularExpressions.Regex("<[^>]*>");

            return objRegEx.Replace(HTML, "");
        }
    }

    public class NumberToPersianText
    {
        //مبلغ به حروف
        // Convert Num To String
        // http://aspcode.ir/Article.aspx?id=20

        private static string[] yekan = new string[10] { "صفر", "یک", "دو", "سه", "چهار", "پنج", "شش", "هفت", "هشت", "نه" };
        private static string[] dahgan = new string[10] { "", "", "بیست", "سی", "چهل", "پنجاه", "شصت", "هفتاد", "هشتاد", "نود" };
        private static string[] dahyek = new string[10] { "ده", "یازده", "دوازده", "سیزده", "چهارده", "پانزده", "شانزده", "هفده", "هجده", "نوزده" };
        //array[10..19]
        private static string[] sadgan = new string[10] { "", "یکصد", "دویست", "سیصد", "چهارصد", "پانصد", "ششصد", "هفتصد", "هشتصد", "نهصد" };
        private static string[] basex = new string[5] { "", "هزار", "میلیون", "میلیارد", "تریلیون" };


        private static string getnum3(int num3)
        {
            string s = "";
            int d3, d12;
            d12 = num3 % 100;
            d3 = num3 / 100;
            if (d3 != 0)
                s = sadgan[d3] + " و ";
            if ((d12 >= 10) && (d12 <= 19))
            {
                s = s + dahyek[d12 - 10];
            }
            else
            {
                int d2 = d12 / 10;
                if (d2 != 0)
                    s = s + dahgan[d2] + " و ";
                int d1 = d12 % 10;
                if (d1 != 0)
                    s = s + yekan[d1] + " و ";
                s = s.Substring(0, s.Length - 3);
            };
            return s;
        }

        public static string num2str(string snum)
        {
            string stotal = "";
            if (snum == "0")
            {
                return yekan[0];
            }
            else
            {
                snum = snum.PadLeft(((snum.Length - 1) / 3 + 1) * 3, '0');
                int L = snum.Length / 3 - 1;
                for (int i = 0; i <= L; i++)
                {
                    int b = int.Parse(snum.Substring(i * 3, 3));
                    if (b != 0)
                        stotal = stotal + getnum3(b) + " " + basex[L - i] + " و ";
                }
                stotal = stotal.Substring(0, stotal.Length - 3);
            }
            return stotal;
        }
    }

    public class ConfigReader
    {
        public static string AppSettingsKey_SiteTitle = "SiteTitle";
        public static string AppSettingsKey_SiteName = "SiteName";
        public static string AppSettingsKey_SiteUrl = "SiteUrl";

        public static string GetAppSettingsValue(string key)
        {
            return ConfigurationManager.AppSettings[key];
        }

        public static string GetConnectionString(string connectionName)
        {
            return ConfigurationManager.ConnectionStrings[connectionName].ConnectionString;
        }

        public static CustomErrorsMode GetCustomeErrorMode()
        {

            System.Configuration.Configuration configuration =
            System.Web.Configuration.WebConfigurationManager.OpenWebConfiguration("/");

            System.Web.Configuration.CustomErrorsSection section =
                (CustomErrorsSection)configuration.GetSection("system.web/customErrors");
            return section.Mode;
        }
    }

    public class JsonMessage
    {
        public dynamic Model { get; set; }
        public bool result { get; set; }
        public string message { get; set; }
    }

}