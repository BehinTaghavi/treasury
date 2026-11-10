using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;
using System.Net;
using TreasuryPrintService.Models;
using System.Configuration;
using System.Drawing.Drawing2D;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;

namespace TreasuryPrintService
{
    public class PrintService : IPrintService
    {
        List<ChequeElement> elements = new List<ChequeElement>();
        ChequeDetails _chequeDetails = new ChequeDetails();
        static int resu = 1000;
        static int resud = 100;
        float _fontSize = 13 * (resu / resud);
        float _smallFontSize = 10 * resu / resud;
        float _largFontSize = 18 * resu / resud;
        string _fontName = "B Nazanin";

        bool rotate = true;
        bool isCenter = true;

        public bool PrintCheque(Cheque cheque, string password)
        {
            try
            {
                var s = CalculateMD5Hash(password);
                var s1 = ConfigurationSettings.AppSettings["printhashcode"].ToString();
                if (CalculateMD5Hash(password).ToLower() != ConfigurationSettings.AppSettings["printhashcode"].ToString().ToLower())
                //if (CalculateMD5Hash(password).ToLower() == ConfigurationSettings.AppSettings["printhashcode"].ToString().ToLower())
                //be dalil nadashtan password shart mosavi ra be mikhalef tabdil kardim ke print chek anjam shavad
                    {

                    if (cheque.ChequeElements.Count > 0)
                    {
                        DialogResult ans = MessageBox.Show("پرینت چیک گرفته شود؟", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                        if (ans == DialogResult.Yes)
                        {
                            elements = cheque.ChequeElements;
                            _chequeDetails = cheque.ChequeDetails;
                            PrintDocument pd = new PrintDocument();

                            //pd.PrinterSettings.PrinterName = "\\\\mollarazi\\hp LaserJet 1320 series UPD PCL 5";
                            //pd.PrinterSettings.PrinterName = "hp LaserJet 1320 series UPD PCL 5";
                            //pd.PrinterSettings.PrinterName = "hp LaserJet 1320 PCL 5e (Copy 6)";
                            pd.PrintPage += new PrintPageEventHandler(PrintPage);
                            Margins margins = new Margins(0, 0, 0, 0);
                            margins.Bottom = 0;
                            margins.Left = 0;
                            margins.Top = 0;
                            margins.Right = 0;
                            pd.DefaultPageSettings.Margins = margins;
                            pd.PrinterSettings.DefaultPageSettings.PrinterResolution = new PrinterResolution() { X = 100, Y = 100 };
                            pd.Print();
                            pd.Dispose();
                            return true;
                        }
                        else
                        {
                            return false;
                        }

                    }
                    return false;
                }
                return false;

            }
            catch (Exception e)
            {
                MessageBox.Show(e.InnerException.ToString());
                return false;
            }
        }

        private void PrintPage(object sender, PrintPageEventArgs ev)
        {

            var bitmap = new Bitmap((643 * resu / resud), (321 * resu / resud), PixelFormat.Format32bppArgb);
            bitmap.SetResolution(100, 100);
            using (var g = Graphics.FromImage(bitmap))
            {
                g.Clear(Color.White);
                g.TextRenderingHint = TextRenderingHint.AntiAlias;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                foreach (var item in elements)
                {

                    int _left = (int)item.m_Left * resu / resud;// - 17;
                    int _top = (int)item.m_TOP * resu / resud;// - 17;
                    int _width = (int)item.m_Width * resu / resud;
                    int _height = (int)item.m_height * resu / resud;



                    switch (item.labelMapToField)
                    {

                        //حذف حواله کرد
                        case "lineDraft":
                            if (_chequeDetails.RemoveRemit)
                            {
                                using (Font font = new Font(_fontName, _fontSize, FontStyle.Bold))
                                {
                                    Rectangle rect = new Rectangle((_left), (_top), ((int)_width),
                                        ((int)_height));

                                    StringFormat stringFormat = new StringFormat();
                                    stringFormat.Alignment = StringAlignment.Center;
                                    stringFormat.LineAlignment = StringAlignment.Center;

                                    g.DrawString("*********", font, Brushes.Black, rect, stringFormat);
                                }
                            }
                            else
                            {
                                using (Font font = new Font(_fontName, _fontSize, FontStyle.Bold))
                                {
                                    Rectangle rect = new Rectangle((_left), (_top), ((int)_width),
                                        ((int)_height));
                                    StringFormat stringFormat = new StringFormat();
                                    stringFormat.Alignment = StringAlignment.Far;
                                    stringFormat.LineAlignment = StringAlignment.Center;

                                    g.DrawString(item.labelValue, font, Brushes.Black, rect, stringFormat);
                                }
                            }

                            break;
                        // حدف حواله "بابت"
                        case "behalf":
                            if (!_chequeDetails.RemoveOrdersFor)
                            {
                                using (Font font = new Font(_fontName, _smallFontSize, FontStyle.Bold))
                                {
                                    Rectangle rect = new Rectangle((_left), (_top), ((int)_width),
                                        ((int)_height));
                                    StringFormat stringFormat = new StringFormat();
                                    stringFormat.Alignment = StringAlignment.Far;
                                    stringFormat.LineAlignment = StringAlignment.Center;
                                    g.DrawString("بابت " + item.labelValue, font, Brushes.Black, rect, stringFormat);
                                }
                            }

                            break;
                        // در وجه حامل
                        case "beneficiary":
                            if (_chequeDetails.Bearer)
                            {
                                using (Font font = new Font(_fontName, _smallFontSize, FontStyle.Bold))
                                {
                                    Rectangle rect = new Rectangle((_left), (_top), ((int)_width),
                                        ((int)_height));
                                    StringFormat stringFormat = new StringFormat();
                                    stringFormat.Alignment = StringAlignment.Far;
                                    stringFormat.LineAlignment = StringAlignment.Center;
                                    g.DrawString("حامل", font, Brushes.Black, rect, stringFormat);
                                }
                            }
                            else if (_chequeDetails.Unique_Account_No2)
                            {
                                using (Font font = new Font(_fontName, _smallFontSize, FontStyle.Bold))
                                {
                                    Rectangle rect = new Rectangle((_left), (_top), ((int)_width),
                                        ((int)_height));
                                    StringFormat stringFormat = new StringFormat();
                                    stringFormat.Alignment = StringAlignment.Far;
                                    stringFormat.LineAlignment = StringAlignment.Center;
                                    g.DrawString(_chequeDetails.Unique_Account_No2Val, font, Brushes.Black, rect, stringFormat);
                                }
                            }
                            else
                            {
                                using (Font font = new Font(_fontName, _smallFontSize, FontStyle.Bold))
                                {
                                    Rectangle rect = new Rectangle((_left), (_top), ((int)_width),
                                        ((int)_height));
                                    StringFormat stringFormat = new StringFormat();
                                    stringFormat.Alignment = StringAlignment.Far;
                                    stringFormat.LineAlignment = StringAlignment.Center;
                                    g.DrawString(item.labelValue, font, Brushes.Black, rect, stringFormat);
                                }
                            }

                            break;

                        case "beneficiaryandbehalf":
                            using (Font font = new Font(_fontName, _smallFontSize, FontStyle.Bold))
                            {
                                Rectangle rect = new Rectangle((_left), (_top), ((int)_width),
                                    ((int)_height));
                                StringFormat stringFormat = new StringFormat();
                                stringFormat.Alignment = StringAlignment.Far;
                                stringFormat.LineAlignment = StringAlignment.Center;
                                string value = item.labelValue;
                                //if (_chequeDetails.CurrentAccount || _chequeDetails.ShebaAccount)
                                //{
                                //    value += " \r\n" + _chequeDetails.BeneficiarDetail;
                                //}
                                g.DrawString(value, font, Brushes.Black, rect, stringFormat);
                            }
                            break;
                        // چک فاقد هر گونه قلم خوردگی است
                        case "withoutCrossing":
                            if (_chequeDetails.WithoutCrossing)
                            {
                                using (Font font = new Font(_fontName, _smallFontSize, FontStyle.Bold))
                                {
                                    Rectangle rect = new Rectangle((_left), (_top), ((int)_width),
                                        ((int)_height));
                                    StringFormat stringFormat = new StringFormat();
                                    stringFormat.Alignment = StringAlignment.Far;
                                    stringFormat.LineAlignment = StringAlignment.Center;
                                    g.DrawString(item.ChequeLable, font, Brushes.Black, rect, stringFormat);
                                }
                            }

                            break;
                        // چک غیر قابل انتقال می باشد
                        case "nonTransferable":
                            if (_chequeDetails.NonTransferable)
                            {
                                using (Font font = new Font(_fontName, _smallFontSize, FontStyle.Bold))
                                {
                                    Rectangle rect = new Rectangle((_left), (_top), ((int)_width),
                                        ((int)_height));
                                    StringFormat stringFormat = new StringFormat();
                                    stringFormat.Alignment = StringAlignment.Far;
                                    stringFormat.LineAlignment = StringAlignment.Center;
                                    g.DrawString(item.ChequeLable, font, Brushes.Black, rect, stringFormat);
                                }
                            }

                            break;
                        // چک در وجه حساب جاری
                        case "currentAccount":
                            if (_chequeDetails.CurrentAccount)
                            {
                                using (Font font = new Font(_fontName, _smallFontSize, FontStyle.Bold))
                                {
                                    Rectangle rect = new Rectangle((_left), (_top), ((int)_width),
                                        ((int)_height));
                                    StringFormat stringFormat = new StringFormat();
                                    stringFormat.Alignment = StringAlignment.Far;
                                    stringFormat.LineAlignment = StringAlignment.Center;
                                    g.DrawString(_chequeDetails.BeneficiarDetail, font, Brushes.Black, rect,
                                        stringFormat);
                                }
                            }

                            break;
                        // چک در وجه حساب شبا
                        case "shebaAccount":
                            if (_chequeDetails.ShebaAccount)
                            {
                                using (Font font = new Font(_fontName, _smallFontSize, FontStyle.Bold))
                                {
                                    Rectangle rect = new Rectangle((_left), (_top), ((int)_width),
                                        ((int)_height));
                                    StringFormat stringFormat = new StringFormat();
                                    stringFormat.Alignment = StringAlignment.Far;
                                    stringFormat.LineAlignment = StringAlignment.Center;
                                    g.DrawString(_chequeDetails.BeneficiarDetail, font, Brushes.Black, rect,
                                        stringFormat);
                                }
                            }

                            break;
                        // در شعبه صادر کننده قابل وصول است
                        case "branchExporterReceivable":
                            if (_chequeDetails.BranchExporterReceivable)
                            {
                                using (Font font = new Font(_fontName, _smallFontSize, FontStyle.Bold))
                                {
                                    Rectangle rect = new Rectangle((_left), (_top), ((int)_width),
                                        ((int)_height));
                                    StringFormat stringFormat = new StringFormat();
                                    stringFormat.Alignment = StringAlignment.Far;
                                    stringFormat.LineAlignment = StringAlignment.Center;
                                    g.DrawString(item.ChequeLable, font, Brushes.Black, rect, stringFormat);
                                }
                            }

                            break;
                        // تایید صدور چک
                        case "approveIssuance":
                            if (_chequeDetails.ApproveIssuance)
                            {
                                using (Font font = new Font(_fontName, _smallFontSize, FontStyle.Bold))
                                {
                                    Rectangle rect = new Rectangle((_left), (_top), ((int)_width),
                                        ((int)_height));
                                    StringFormat stringFormat = new StringFormat();
                                    stringFormat.Alignment = StringAlignment.Far;
                                    stringFormat.LineAlignment = StringAlignment.Center;
                                    g.DrawString(item.ChequeLable, font, Brushes.Black, rect, stringFormat);
                                }
                            }

                            break;

                        case "price_n":
                            using (Font font = new Font("B Nazanin", _largFontSize, FontStyle.Bold))
                            {
                                Rectangle rect = new Rectangle((_left), (_top), ((int)_width),
                                    ((int)_height));
                                StringFormat stringFormat = new StringFormat();
                                stringFormat.Alignment = StringAlignment.Far;
                                stringFormat.LineAlignment = StringAlignment.Center;
                                g.DrawString("***/" + item.labelValue + "/***", font, Brushes.Black, rect, stringFormat);
                            }

                            break;

                        case "price_w":
                            using (Font font = new Font(_fontName, _smallFontSize, FontStyle.Bold))
                            {
                                Rectangle rect = new Rectangle((_left), (_top), ((int)_width),
                                    ((int)_height));
                                StringFormat stringFormat = new StringFormat();
                                stringFormat.Alignment = StringAlignment.Far;
                                stringFormat.LineAlignment = StringAlignment.Center;
                                g.DrawString("***/" + item.labelValue + " تمام/***", font, Brushes.Black, rect, stringFormat);
                            }
                            break;
                        case "chequeDate_w":
                            using (Font font = new Font(_fontName, _smallFontSize, FontStyle.Bold))
                            {
                                Rectangle rect = new Rectangle((_left), (_top), ((int)_width),
                                    ((int)_height));
                                StringFormat stringFormat = new StringFormat();
                                stringFormat.Alignment = StringAlignment.Far;
                                stringFormat.LineAlignment = StringAlignment.Center;
                                g.DrawString(item.labelValue, font, Brushes.Black, rect, stringFormat);
                            }
                            break;

                        case "chequeDate_n":
                            using (Font font = new Font("B Nazanin", _fontSize, FontStyle.Bold))
                            {
                                Rectangle rect = new Rectangle((_left), (_top), ((int)_width),
                                    ((int)_height));
                                StringFormat stringFormat = new StringFormat();
                                stringFormat.Alignment = StringAlignment.Far;
                                stringFormat.LineAlignment = StringAlignment.Center;
                                g.DrawString(item.labelValue, font, Brushes.Black, rect, stringFormat);
                            }
                            break;

                        default:
                            using (Font font = new Font(_fontName, _smallFontSize, FontStyle.Bold))
                            {
                                Rectangle rect = new Rectangle((_left), (_top), ((int)_width),
                                    ((int)_height));
                                StringFormat stringFormat = new StringFormat();
                                stringFormat.Alignment = StringAlignment.Far;
                                stringFormat.LineAlignment = StringAlignment.Center;
                                g.DrawString(item.labelValue, font, Brushes.Black, rect, stringFormat);
                            }
                            break;
                    }

                }
            }

            string[] files = Directory.GetFiles("C:/TreasuryPrintService");

            foreach (string file in files)
            {
                FileInfo fi = new FileInfo(file);
                if (fi.LastAccessTime < DateTime.Now.AddDays(-3))
                    fi.Delete();
            }

            string name = DateTime.Now.Ticks.ToString();
            bitmap.Save("C:/TreasuryPrintService/cheque_" + name + ".jpeg", ImageFormat.Jpeg);

            System.Drawing.Image img = (Image)(new Bitmap(System.Drawing.Image.FromFile("C:/TreasuryPrintService/cheque_" + name + ".jpeg"), new Size(643, 321)));
            int x = isCenter ? 240 : 0;
            Point loc = new Point(x, -10);
            img.RotateFlip(RotateFlipType.Rotate90FlipNone);
            ev.Graphics.DrawImage(img, loc);
            ev.Graphics.TextRenderingHint = TextRenderingHint.AntiAlias;
            ev.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            ev.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            ev.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            if (rotate)
            {
                ev.Graphics.Transform.Rotate(90);
            }
            ev.HasMorePages = false;

        }


        public string CalculateMD5Hash(string input)

        {
            MD5 md5 = System.Security.Cryptography.MD5.Create();
            byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
            byte[] hash = md5.ComputeHash(inputBytes);
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < hash.Length; i++)
            {
                sb.Append(hash[i].ToString("X2"));
            }
            return sb.ToString();
        }
    }
}
