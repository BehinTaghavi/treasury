using System;
using System.Collections.Generic;
using System.Configuration;
using System.Drawing;
using System.Drawing.Printing;
using System.Linq;
using System.Web;

namespace CSD.Treasury.UI.Models
{
    public class Print
    {
        List<ChequeElement> elements = new List<ChequeElement>();
        ChequeDetails _chequeDetails = new ChequeDetails();
        int Height = 0;
        int Width = 0;
        public  bool PrintCheque(Cheque cheque)
        {
            try
            {
                if (cheque.ChequeElements.Count > 0)
                {
                    elements = cheque.ChequeElements;
                    _chequeDetails = cheque.ChequeDetails;
                    PrintDocument pd = new PrintDocument();
                    pd.PrinterSettings.PrinterName ="\\\\"+ ConfigurationManager.AppSettings["ClientName"] +"\\"+ ConfigurationManager.AppSettings["PrinterName"];
                    pd.PrintPage += new PrintPageEventHandler(PrintPage);

                    Margins margins = new Margins(0, 0, 0, 0);
                    margins.Bottom = 0;
                    margins.Left = 0;
                    margins.Top = 0;
                    margins.Right = 0;
                    pd.DefaultPageSettings.Margins = margins;
                    pd.Print();
                    return true;
                }
                return false;


            }
            catch (Exception e)
            {
                return false;
            }
        }
        private void PrintPage(object sender, PrintPageEventArgs ev)
        {
            foreach (var item in elements)
            {
                int _left = (int)item.m_Left - 17;
                int _top = (int)item.m_TOP - 17;
                switch (item.labelMapToField)
                {

                    //حذف حواله کرد
                    case "lineDraft":
                        if (_chequeDetails.RemoveRemit)
                        {
                            using (Font font = new Font(item.m_FontName, (float)item.m_FontSize))
                            {
                                Rectangle rect = new Rectangle((_left), (_top), ((int)item.m_Width), ((int)item.m_height));

                                StringFormat stringFormat = new StringFormat();
                                stringFormat.Alignment = StringAlignment.Center;
                                stringFormat.LineAlignment = StringAlignment.Center;
                                ev.Graphics.DrawString("*********", font, Brushes.Black, rect, stringFormat);
                            }
                        }
                        else
                        {
                            using (Font font = new Font(item.m_FontName, (float)item.m_FontSize))
                            {
                                Rectangle rect = new Rectangle((_left), (_top), ((int)item.m_Width), ((int)item.m_height));
                                StringFormat stringFormat = new StringFormat();
                                stringFormat.Alignment = StringAlignment.Far;
                                stringFormat.LineAlignment = StringAlignment.Center;
                                ev.Graphics.DrawString(item.labelValue, font, Brushes.Black, rect, stringFormat);
                            }
                        }
                        break;
                    // حدف حواله "بابت"
                    case "behalf":
                        if (!_chequeDetails.RemoveOrdersFor)
                        {
                            using (Font font = new Font(item.m_FontName, (float)item.m_FontSize))
                            {
                                Rectangle rect = new Rectangle((_left), (_top), ((int)item.m_Width), ((int)item.m_height));
                                StringFormat stringFormat = new StringFormat();
                                stringFormat.Alignment = StringAlignment.Far;
                                stringFormat.LineAlignment = StringAlignment.Center;
                                ev.Graphics.DrawString(item.labelValue, font, Brushes.Black, rect, stringFormat);
                            }
                        }
                        break;
                    // در وجه حامل
                    case "beneficiary":
                        if (_chequeDetails.Bearer)
                        {
                            using (Font font = new Font(item.m_FontName, (float)item.m_FontSize))
                            {
                                Rectangle rect = new Rectangle((_left), (_top), ((int)item.m_Width), ((int)item.m_height));
                                StringFormat stringFormat = new StringFormat();
                                stringFormat.Alignment = StringAlignment.Far;
                                stringFormat.LineAlignment = StringAlignment.Center;
                                ev.Graphics.DrawString("حامل", font, Brushes.Black, rect, stringFormat);
                            }
                        }
                        else
                        {
                            using (Font font = new Font(item.m_FontName, (float)item.m_FontSize))
                            {
                                Rectangle rect = new Rectangle((_left), (_top), ((int)item.m_Width), ((int)item.m_height));
                                StringFormat stringFormat = new StringFormat();
                                stringFormat.Alignment = StringAlignment.Far;
                                stringFormat.LineAlignment = StringAlignment.Center;
                                ev.Graphics.DrawString(item.labelValue, font, Brushes.Black, rect, stringFormat);
                            }
                        }
                        break;
                    // چک فاقد هر گونه قلم خوردگی است
                    case "withoutCrossing":
                        if (_chequeDetails.WithoutCrossing)
                        {
                            using (Font font = new Font(item.m_FontName, (float)item.m_FontSize))
                            {
                                Rectangle rect = new Rectangle((_left), (_top), ((int)item.m_Width), ((int)item.m_height));
                                StringFormat stringFormat = new StringFormat();
                                stringFormat.Alignment = StringAlignment.Far;
                                stringFormat.LineAlignment = StringAlignment.Center;
                                ev.Graphics.DrawString(item.ChequeLable, font, Brushes.Black, rect, stringFormat);
                            }
                        }
                        break;
                    // چک غیر قابل انتقال می باشد
                    case "nonTransferable":
                        if (_chequeDetails.NonTransferable)
                        {
                            using (Font font = new Font(item.m_FontName, (float)item.m_FontSize))
                            {
                                Rectangle rect = new Rectangle((_left), (_top), ((int)item.m_Width), ((int)item.m_height));
                                StringFormat stringFormat = new StringFormat();
                                stringFormat.Alignment = StringAlignment.Far;
                                stringFormat.LineAlignment = StringAlignment.Center;
                                ev.Graphics.DrawString(item.ChequeLable, font, Brushes.Black, rect, stringFormat);
                            }
                        }
                        break;
                    // چک در وجه حساب جاری
                    case "currentAccount":
                        if (_chequeDetails.CurrentAccount)
                        {
                            using (Font font = new Font(item.m_FontName, (float)item.m_FontSize))
                            {
                                Rectangle rect = new Rectangle((_left), (_top), ((int)item.m_Width), ((int)item.m_height));
                                StringFormat stringFormat = new StringFormat();
                                stringFormat.Alignment = StringAlignment.Far;
                                stringFormat.LineAlignment = StringAlignment.Center;
                                ev.Graphics.DrawString(_chequeDetails.BeneficiarDetail, font, Brushes.Black, rect, stringFormat);
                            }
                        }
                        break;
                    // چک در وجه حساب شبا
                    case "shebaAccount":
                        if (_chequeDetails.ShebaAccount)
                        {
                            using (Font font = new Font(item.m_FontName, (float)item.m_FontSize))
                            {
                                Rectangle rect = new Rectangle((_left), (_top), ((int)item.m_Width), ((int)item.m_height));
                                StringFormat stringFormat = new StringFormat();
                                stringFormat.Alignment = StringAlignment.Far;
                                stringFormat.LineAlignment = StringAlignment.Center;
                                ev.Graphics.DrawString(_chequeDetails.BeneficiarDetail, font, Brushes.Black, rect, stringFormat);
                            }
                        }
                        break;
                    // در شعبه صادر کننده قابل وصول است
                    case "branchExporterReceivable":
                        if (_chequeDetails.BranchExporterReceivable)
                        {
                            using (Font font = new Font(item.m_FontName, (float)item.m_FontSize))
                            {
                                Rectangle rect = new Rectangle((_left), (_top), ((int)item.m_Width), ((int)item.m_height));
                                StringFormat stringFormat = new StringFormat();
                                stringFormat.Alignment = StringAlignment.Far;
                                stringFormat.LineAlignment = StringAlignment.Center;
                                ev.Graphics.DrawString(item.ChequeLable, font, Brushes.Black, rect, stringFormat);
                            }
                        }
                        break;
                    // تایید صدور چک
                    case "approveIssuance":
                        if (_chequeDetails.ApproveIssuance)
                        {
                            using (Font font = new Font(item.m_FontName, (float)item.m_FontSize))
                            {
                                Rectangle rect = new Rectangle((_left), (_top), ((int)item.m_Width), ((int)item.m_height));
                                StringFormat stringFormat = new StringFormat();
                                stringFormat.Alignment = StringAlignment.Far;
                                stringFormat.LineAlignment = StringAlignment.Center;
                                ev.Graphics.DrawString(item.ChequeLable, font, Brushes.Black, rect, stringFormat);
                            }
                        }
                        break;



                    case "price_n":
                        using (Font font = new Font(item.m_FontName, (float)item.m_FontSize))
                        {
                            Rectangle rect = new Rectangle((_left), (_top), ((int)item.m_Width), ((int)item.m_height));
                            StringFormat stringFormat = new StringFormat();
                            stringFormat.Alignment = StringAlignment.Near;
                            stringFormat.LineAlignment = StringAlignment.Center;
                            ev.Graphics.DrawString(item.labelValue, font, Brushes.Black, rect, stringFormat);
                        }
                        break;
                    default:
                        using (Font font = new Font(item.m_FontName, (float)item.m_FontSize))
                        {
                            Rectangle rect = new Rectangle((_left), (_top), ((int)item.m_Width), ((int)item.m_height));
                            StringFormat stringFormat = new StringFormat();
                            stringFormat.Alignment = StringAlignment.Far;
                            stringFormat.LineAlignment = StringAlignment.Center;
                            ev.Graphics.DrawString(item.labelValue, font, Brushes.Black, rect, stringFormat);
                        }
                        break;
                }
            }
            ev.Graphics.PageScale = 100;
            ev.Graphics.Clip = new Region(new Rectangle(0, 0, Width, Height));
            ev.HasMorePages = false;
        }
        public class Cheque
        {
            public List<ChequeElement> ChequeElements { get; set; }
            public virtual ChequeDetails ChequeDetails { get; set; }
        }
        public class ChequeDetails
        {
            public decimal RequestId { get; set; }
            public int ChequeFormatID { get; set; }
            public string DueDate { get; set; }
            public string TableName { get; set; }
            public bool RemoveRemit { get; set; }
            public bool RemoveOrdersFor { get; set; }
            public bool Bearer { get; set; }
            public bool WithoutCrossing { get; set; }
            public bool NonTransferable { get; set; }
            public bool CurrentAccount { get; set; }
            public int CurrentAccountId { get; set; }
            public bool ShebaAccount { get; set; }
            public int ShebaAccountId { get; set; }
            public bool BranchExporterReceivable { get; set; }
            public bool ApproveIssuance { get; set; }
            public string BeneficiarDetail { get; set; }
        }
        public class ChequeElement
        {
            public decimal m_TOP { get; set; }
            public decimal m_Left { get; set; }
            public string m_FontName { get; set; }
            public int m_FontSize { get; set; }
            public string m_TextAlign { get; set; }
            public string m_fontweight { get; set; }
            public decimal m_Width { get; set; }
            public decimal m_height { get; set; }
            public string m_Direction { get; set; }
            public string ChequeLable { get; set; }
            public string labelValue { get; set; }
            public string labelMapToField { get; set; }
        }
    }
}