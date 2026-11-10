using CSD.Treasury.DataModel;
using CSD.Treasury.Service.Request_PaymentService;
using CSD.Treasury.Service.ChequeFormatTypeDetailsService;
using CSD.Treasury.UI.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using CSD.Treasury.Service.BeneficiaryService;
using CSD.Treasury.Service.BeneficiariesDetailInfoService;
using CSD.Treasury.Service.ProceduresDB;
using CSD.Treasury.Service.PaymentService;
using CSD.Treasury.Service.ChequesService;
using CSD.Treasury.Service.ChequeFormatTypesService;
using CSD.Treasury.Service.BankService;
using CSD.Treasury.UI.Controllers;
using CSD.Treasury.Service.Request_Payment_StatusDetailsService;
using CSD.Treasury.UI.Areas.Payment.Models;
using CSD.Treasury.Common.Enumeration;
using System.Configuration;
using CSD.Treasury.Service.TransferFromBackupBankService;
using System.Text;
using System.Diagnostics;
using System.Drawing.Printing;
using System.Globalization;

namespace CSD.Treasury.UI.Areas.Payment.Controllers
{
    [Authorize(Roles = "Admin,Manager,RequestPayment")]
    public class ChequePaymentController : BaseController
    {

        List<CSD.Treasury.Service.ChequePrintService.ChequeElement> _element = new List<CSD.Treasury.Service.ChequePrintService.ChequeElement>();
        public static ChequeDetails _chequeDetails = new ChequeDetails();
        public static List<CSD.Treasury.Service.ChequePrintService.ChequeElement> elements = new List<Service.ChequePrintService.ChequeElement>();
        public static int width = 0;
        public static int height = 0;
        int Height, Width = 0;
        // GET: Payment/ChequePayment
        public ActionResult Index()
        {
            return View();
        }

        //به دست آوردن آخرین شماره دسته چک
        public JsonResult GetLastChequeNo(int RequestId, int ChequeFormatID)
        {

            int totalRows;
            var Nosheet = false;
            string Message = "";
            var NoPrint = false;
            var BardashtAzGari = false;
            var ExchangeType = 0;
            var dateNow = Utility.ToPersianDate(DateTime.Now);
            //Icls_Request_PaymentClient RP = new Icls_Request_PaymentClient();
            Icls_ChequesClient cc = new Icls_ChequesClient();
            Icls_TransferFromBackupBankClient TransferClient = new Icls_TransferFromBackupBankClient();

            //Icls_BankClient BankClient = new Icls_BankClient();
            var dc = new TreasuryEntities();
            string mess;
            //var Request = RP.SelectRow(RequestId).SingleOrDefault();
            var Request = dc.Request_Payment.Find(RequestId);
            var BankID = Int32.Parse(Request.Source_PaymentValue);

            var ChequePrice = Request.PayAmount;
            if(Request.ExchangeTypeID != null)
            {
                ChequePrice = Request.ExchangeAmount.Value;
            }

            //درصورتی که چک انتظامی باشد نباید موجودی را چک کرد
            //درصورتی که چک بلند مدت باشد و همچنین در کانفیگ مشخص شده باشد که بدون موجودی چک صادر شود 
            //دراین صورت موجودی چک نمی گردد
            var chequeType = Request.Pay_TypeDetailID_source;

            var Cheques = cc.SelectBy_DynamicWhereClause("ChequeFormatID =" + ChequeFormatID + " and BankID = " + BankID + " and IsActive = " + true, 1, 100, "ChequeID", "int", false, out totalRows);
            var LastChequeNo = int.Parse(Cheques.SingleOrDefault().LastUsedPaperNo.ToString()) + 1;
            if (LastChequeNo > Cheques.SingleOrDefault().LastPageNumber)
                Nosheet = true;


            //درصورتیکه دسته چک برگه داشته باشد
            //و کافیگ ست شده باشد که برداشت از حساب زمان صدور چک است
            if (Nosheet == false && ConfigurationManager.AppSettings["PayWhenCheckPrinted"] == "True")
            {
                // بررسی موجودی بانک و بانک پشتیبان
                var Date = Utility.ToPersianDate(DateTime.Now);
                Icls_SPsClient db = new Icls_SPsClient();
                //$Sp_GetBankBalance$
                var Bank = db.Sp_GetBankBalance("13000101", Date, BankID, 1, false).FirstOrDefault();
                //var BackupBank = BankClient.SelectBy_DynamicWhereClause("Backup_BankID =" + Bank.BankID, 1, 10, "BankID", "int", false, out totalRows);
                var BankInfo = dc.Banks.SingleOrDefault(d=>d.BankID == BankID);
                //استاندارد
                if (BankInfo.ExchangeType == 1)
                {
                    PersianCalendar pc = new PersianCalendar();
                    var FiscalYear = pc.GetYear(DateTime.Now);
                    var ExchangeStandards = dc.ExchangeStandards.SingleOrDefault(d => d.FiscalYear == FiscalYear);
                    if (ExchangeStandards != null)
                    {
                        ExchangeType = ExchangeStandards.Price;
                    }
                }
                else if (BankInfo.ExchangeType == 2 || BankInfo.ExchangeType == 3)
                {
                    var exchangeRate = dc.ExchangeRates.SingleOrDefault(d => d.ExchangeDateRate == dateNow);
                    if (exchangeRate != null)
                    {
                        if (BankInfo.ExchangeType == 2)
                        {
                            ExchangeType = exchangeRate.FormalPrice;
                        }
                        else if (BankInfo.ExchangeType == 3)
                        {
                            ExchangeType = exchangeRate.InformalPrice;
                        }

                    }
                }
                //اگرازری نباشد
                //یا ارزی باشد و مقدار نرخ ارز بانک رسمی و غیررسمی و استاندارد تعریف شده باشد
                //یا ارزی باشد ونرخ ارز  بانک واقعی باشد
                if (!BankInfo.HasExchangeTransaction || (BankInfo.HasExchangeTransaction && ExchangeType != 0) || (BankInfo.HasExchangeTransaction && BankInfo.ExchangeType == 4 ))
                {
                    var BackupBank = dc.Banks.Where(d => d.Backup_BankID == Bank.BankID);
                    GetBankBalance_Result Backup_Bank = new GetBankBalance_Result();
                    if (BackupBank.Count() != 0)
                    {
                        if (BackupBank.Count() != 0)
                        {
                            //$Sp_GetBankBalance$
                            var qury = db.Sp_GetBankBalance("13000101", Date, BackupBank.SingleOrDefault().BankID, 1, false);

                            //if (query.Count() == 1)
                            //{
                            Backup_Bank = qury.FirstOrDefault();
                            //}
                            //else
                            //{
                            //    Backup_Bank = qury.LastOrDefault();
                            //}
                        }
                    }
                    //chequeType == 2 چک انتظامی
                    // chequeType == 4 چک بلندمدت

                    if ((ChequePrice <= Bank.balance && (ConfigurationManager.AppSettings["CompletePayBackup"] == "True" && BackupBank.Count() == 0))
                        || (ChequePrice <= Bank.balance && (ConfigurationManager.AppSettings["CompletePayBackup"] == "False"))
                        || chequeType == 2 || (ConfigurationManager.AppSettings["Long-termCheckWithoutCash"] == "True" && chequeType == 4))
                    {
                        Message = "موجودی کافی است";
                    }
                    else if (BackupBank.Count() != 0)
                    {
                        //اگر بانک حساب پشتیبان داشته باشد

                        //var Sumbalance = Bank.balance + Backup_Bank.balance;

                        decimal? remain = 0;
                        //برداشت مبلغ به صورت کامل از پشتیبان
                        if (ConfigurationManager.AppSettings["CompletePayBackup"] == "True")
                        {
                            remain = ChequePrice;
                        }
                        else
                        {
                            remain = ChequePrice - Bank.balance;
                        }
                        //اگر موجودی حساب پشتیبان بیشتر یا مساوی مبلغ کمبود حساب جاری است
                        if (Backup_Bank.balance >= remain)
                        {
                            Message = "موجودی حساب جاری کافی نیست .درصورت 'تایید نقل و انتقال از پشتیبان' مبلغ از حساب پشتیبان کسر می گردد";
                            NoPrint = true;
                            var Transferobj = TransferClient.SelectBy_DynamicWhereClause("RequestID=" + RequestId, 1, 10, "TransferId", "int64", false, out totalRows);
                            if (Transferobj.Count() != 0)
                            {
                                if (Transferobj.SingleOrDefault().IsOk && Bank.balance >= ChequePrice)
                                {

                                    NoPrint = false;
                                    Message = "";
                                }
                                if (Bank.balance <= ChequePrice)
                                {
                                    Message = "مبلغ موجودی به علاوه مبلغ انتقال از پشتیبان کمتر از مبلغ چک است";
                                }
                            }
                            if (!Transferobj.Any())
                            {
                                //پرداخت از پشتیبان
                                //دریافت به حساب جاری
                                Service.TransferFromBackupBankService.TransferFromBackupBank TransferFromBackupBankOBJ = new Service.TransferFromBackupBankService.TransferFromBackupBank();
                                TransferFromBackupBankOBJ.TransferDate = Utility.ToPersianDate(DateTime.Now);
                                TransferFromBackupBankOBJ.From_BankID = Backup_Bank.BankID;
                                TransferFromBackupBankOBJ.To_BankID = Bank.BankID;
                                TransferFromBackupBankOBJ.Price = (long)remain;
                                TransferFromBackupBankOBJ.RequestID = RequestId;
                                TransferClient.Insert(TransferFromBackupBankOBJ, out mess);

                            }


                        }
                        else
                        {

                            if (ConfigurationManager.AppSettings["CompletePayBackup"] == "True" && ChequePrice <= Bank.balance)
                            {
                                Message = "موجودی حساب پشتیبان کافی نیست";
                                BardashtAzGari = true;

                            }
                            else
                            {

                                Message = "موجودی حساب جاری و پشتیبان کافی نیست.امکان صدور چک نیست";
                                NoPrint = true;
                            }
                        }
                    }
                    else
                    {
                        Message = "موجودی حساب جاری کافی نیست ";
                        NoPrint = true;
                    }
                }
                else
                {
                    var desc = "";
                    if(BankInfo.ExchangeType == 1)
                    {
                        desc = "نرخ استاندارد";
                    }
                    else if(BankInfo.ExchangeType == 2)
                    {
                        desc = " نرخ رسمی ";
                    }
                    else if (BankInfo.ExchangeType == 3)
                    {
                        desc = " نرخ غیررسمی ";
                    }
                    
                   Message = desc + "تعریف نشده است " ;
                    NoPrint = true;
                }
            }

            return Json(new { LastChequeNo, Nosheet, Message, NoPrint, BardashtAzGari }, JsonRequestBehavior.AllowGet);
        }
        public ActionResult Issuance(ChequeDetails chequeDetails)
        {
            _chequeDetails = chequeDetails;
            Icls_ChequeFormatTypeDetailsClient CD = new Icls_ChequeFormatTypeDetailsClient();
            Icls_ChequesClient cc = new Icls_ChequesClient();
            //Icls_Request_PaymentClient RP = new Icls_Request_PaymentClient();
            Icls_BeneficiaryClient BeneficiaryClient = new Icls_BeneficiaryClient();
            Icls_BeneficiariesDetailInfoClient BeneficiaryDetailsClient = new Icls_BeneficiariesDetailInfoClient();
            //Icls_BankClient BankClient = new Icls_BankClient();
            var dc = new TreasuryEntities();
            string mess;

            //var Bank = RP.SelectRow((int)chequeDetails.RequestId).SingleOrDefault();
            //var RePa = RP.SelectRow((int)chequeDetails.RequestId).SingleOrDefault();
            var RePa = dc.Request_Payment.Find((int)chequeDetails.RequestId);
            var BankID = RePa.Source_PaymentValue;

            string whereclouse = "ChequeFormatID=" + chequeDetails.ChequeFormatID;
            int totalRows;
            //لیستی از المان ها ی چک 
            //_element = new List<checkElement>();
            //آخرین شماره چک 


            var Cheques = cc.SelectBy_DynamicWhereClause("ChequeFormatID =" + chequeDetails.ChequeFormatID + " and BankID = " + BankID + " and IsActive = " + true, 1, 100, "ChequeID", "int", false, out totalRows);
            ViewBag.ChequeNo = Cheques.SingleOrDefault().LastUsedPaperNo + 1;
            ViewBag.RequestId = chequeDetails.RequestId;
            ViewBag.TableName = chequeDetails.TableName;
            ViewBag.ChequeFormatID = chequeDetails.ChequeFormatID;


            if (chequeDetails.DueDate != null)
            {
                chequeDetails.DueDate = chequeDetails.DueDate.Replace("/", string.Empty);
                ViewBag.ExchangeRate = chequeDetails.ExchangeRate;
                ViewBag.DueDate = chequeDetails.DueDate.Substring(0, 4) + "/" + chequeDetails.DueDate.Substring(4, 2) + "/" + chequeDetails.DueDate.Substring(6, 2);

            }
            //لیستی از لیبل ها بر اساس نوع چک
            var lables = CD.SelectBy_DynamicWhereClause(whereclouse, 1, 100, "FormatID", "int", false, out totalRows).ToList();


            //درخواست وجه را به همراه وضعیت هایش می دهد 
            //var objs = Utility.GetRequest_Payment_WithLastStates((int)chequeDetails.RequestId);
            var codeMeli = Utility.GetmeliCode(RePa.Unique_Account_No);

            //درصورتی که بخواهد جک را دروجه حساب جاری یا شبا چاپ کند
            //var BeneficiaryObj = BeneficiaryClient.SelectBy_DynamicWhereClause("Unique_Account_No=\"" + objs.Unique_Account_No + "\"", 1, 10, "BeneficiarID", "int", false, out totalRows);

            //if (BeneficiaryObj.Count() != 0)
            //{
            //    var BeneficiarID = BeneficiaryObj.SingleOrDefault().BeneficiarID;
            //    var BeneficiaryDetailsObj = BeneficiaryDetailsClient.SelectBy_DynamicWhereClause("BeneficiarID=" + BeneficiaryObj.SingleOrDefault().BeneficiarID, 1, 10, "BeneficiarDetailID", "int", false, out totalRows);
            //    if (BeneficiaryDetailsObj.Count() != 0)
            //    {
            //        chequeDetails.BeneficiarDetailID = BeneficiaryDetailsObj.SingleOrDefault().BeneficiarDetailID;
            //    }
            //}

            foreach (var item in lables)
            {
                var value = "";
                string mapToField = Utility.GetLable(item.ChequeLableID).MapToField;
                if (chequeDetails.DueDate != null)
                {
                    chequeDetails.DueDate = chequeDetails.DueDate.Replace("/", string.Empty);
                }
                switch (mapToField)
                {
                    case "chequeDate_n":
                        if (chequeDetails.DueDate != null)
                        {
                            value = chequeDetails.DueDate.Substring(0, 4) + "/" + chequeDetails.DueDate.Substring(4, 2) + "/" + chequeDetails.DueDate.Substring(6, 2);
                        }
                        break;
                    case "chequeDate_w":
                        if (chequeDetails.DueDate != null)
                        {
                            value = Utility.num2str(chequeDetails.DueDate.Substring(6, 2)) + " " + Utility.mountToW(chequeDetails.DueDate.Substring(4, 2)) + " " + Utility.num2str(chequeDetails.DueDate.Substring(0, 4));
                        }
                        break;
                    case "day_n":
                        if (chequeDetails.DueDate != null)
                        {
                            value = chequeDetails.DueDate.Substring(6, 2);
                        }
                        break;
                    case "day_w":
                        if (chequeDetails.DueDate != null)
                        {
                            value = Utility.num2str(chequeDetails.DueDate.Substring(6, 2));
                        }
                        break;
                    case "month_n":
                        if (chequeDetails.DueDate != null)
                        {
                            value = chequeDetails.DueDate.Substring(5, 2);
                        }
                        break;
                    case "month_w":
                        if (chequeDetails.DueDate != null)
                        {
                            value = Utility.num2str(chequeDetails.DueDate.Substring(4, 2));
                        }
                        break;
                    case "year_n":
                        if (chequeDetails.DueDate != null)
                        {
                            value = chequeDetails.DueDate.Substring(0, 4);
                        }
                        break;
                    case "year_w":
                        if (chequeDetails.DueDate != null)
                        {
                            value = Utility.num2str(chequeDetails.DueDate.Substring(0, 4));
                        }
                        break;
                    case "price_n":
                        value = Convert.ToInt64(RePa.PayAmount).ToString("n0");
                        break;
                    case "price_w":
                        value = Utility.num2str(((Int64)RePa.PayAmount).ToString());
                        break;
                    case "beneficiary":
                        if (!string.IsNullOrEmpty(chequeDetails.PayTo))
                        {
                            value = chequeDetails.PayTo;
                            RePa.Main_Unique_Account_No = chequeDetails.PayTo;
                            dc.SaveChanges();
                        }
                        else
                        {
                            value = Utility.GetAccountNumIfExist(RePa.Unique_Account_No);
                        }
                        break;
                    case "beneficiaryandbehalf":
                        if (chequeDetails.CurrentAccount)
                        {
                            if (chequeDetails.Unique_Account_No2)
                            {
                                value += Utility.GetAccountNumIfExist(RePa.Main_Unique_Account_No, true) + " " + @"جهت واریز به حساب جاری
" + Utility.GetAccountNum(chequeDetails.CurrentAccountId, true) + " ";
                            }
                            else
                            {
                                value += @"جهت واریز به حساب جاری
" + Utility.GetAccountNum(chequeDetails.CurrentAccountId) + " ";
                            }
                        }
                        else
                        {
                            if (chequeDetails.ShebaAccount)
                            {
                                if (chequeDetails.Unique_Account_No2)
                                {
                                    value += Utility.GetAccountNumIfExist(RePa.Main_Unique_Account_No, true) + " " + Utility.GetShebaNum(chequeDetails.ShebaAccountId, true) + " ";
                                }
                                else
                                {
                                    value += Utility.GetShebaNum(chequeDetails.ShebaAccountId) + " ";
                                }
                            }
                        }
                        if (chequeDetails.Unique_Account_No2)
                        {
                            if (chequeDetails.RemoveOrdersFor)
                            {
                                if (!chequeDetails.ShebaAccount && !chequeDetails.CurrentAccount)
                                {
                                    value += Utility.GetAccountNumIfExist(RePa.Main_Unique_Account_No, true) + " ";
                                }
                            }
                            else
                            {
                                if (!chequeDetails.ShebaAccount && !chequeDetails.CurrentAccount)
                                {
                                    value += Utility.GetAccountNumIfExist(RePa.Main_Unique_Account_No, true) + " " + ((ConfigurationManager.AppSettings["ShowBehalfTextInCheque"] == "True") ? " بابت " : " ") + " " + Utility.GetBehalf(RePa.BehalfID) + " ";
                                }
                            }
                        }
                        else
                        {
                            if (chequeDetails.RemoveOrdersFor)
                            {
                                value += Utility.GetAccountNumIfExist(RePa.Unique_Account_No) + " ";
                            }
                            else
                            {
                                value += Utility.GetAccountNumIfExist(RePa.Unique_Account_No) + " " + ((ConfigurationManager.AppSettings["ShowBehalfTextInCheque"] == "True") ? " بابت " : " ") + " " + Utility.GetBehalf(RePa.BehalfID) + " ";
                            }
                        }
                        //var value = item.labelValue;

                        if (chequeDetails.ShowDescription)
                        {
                            var dec = Utility.GetChequeDescription(chequeDetails.RequestId);
                            _chequeDetails.Description = dec;
                            value += @"
" + dec;
                        }
                        break;
                    case "behalf":
                        value = Utility.GetBehalf(RePa.BehalfID);
                        break;
                    case "lineDraft":
                        value = "";
                        break;
                    case "price_e_n":
                        value = RePa.PayAmount.ToString();
                        break;
                    case "price_e_w":
                        value = RePa.PayAmount.ToString();
                        break;
                    case "chequeDate_e_n":
                        if (chequeDetails.DueDate != null)
                        {
                            value = chequeDetails.DueDate.Substring(0, 4) + "/" + chequeDetails.DueDate.Substring(4, 2) + "/" + chequeDetails.DueDate.Substring(6, 2);
                        }
                        break;
                    case "chequeDate_e_w":
                        if (chequeDetails.DueDate != null)
                        {
                            value = chequeDetails.DueDate.Substring(0, 4) + "/" + chequeDetails.DueDate.Substring(4, 2) + "/" + chequeDetails.DueDate.Substring(6, 2);
                        }
                        break;
                    case "meliCode":
                        if (!string.IsNullOrEmpty(codeMeli))
                        {
                            value = codeMeli;
                        }
                        break;
                        //case "shebaAccount":
                        //    value = chequeDetails.DueDate;
                        //    break;
                        //case "currentAccount":
                        //    value = Utility.GetUniqueAccount(objs.Unique_Account_No). chequeDetails.DueDate;
                        //    break;
                }
                Service.ChequePrintService.ChequeElement RPW = new Service.ChequePrintService.ChequeElement();
                RPW.m_TOP = item.m_TOP.Value;
                RPW.m_Left = item.m_Left.Value;
                RPW.m_FontName = item.m_FontName;
                RPW.m_FontSize = item.m_FontSize.Value;
                RPW.m_TextAlign = item.m_TextAlign;
                RPW.m_fontweight = item.m_fontweight;
                RPW.m_Width = item.m_Width.Value;
                RPW.m_height = item.m_height.Value;
                RPW.m_Direction = item.m_Direction;
                RPW.ChequeLable = Utility.GetLable(item.ChequeLableID).ChequeLableDescription;
                RPW.labelValue = value;
                RPW.labelMapToField = mapToField;
                _element.Add(RPW);
            }



            var db = new TreasuryFilesEntities();
            {
                CSD.Treasury.DataModel.File obj = new CSD.Treasury.DataModel.File();
                obj = db.Files.Where(u => u.tableName == chequeDetails.TableName && u.relatedID == chequeDetails.ChequeFormatID).FirstOrDefault();
                if (obj != null)
                {
                    FileContentResult result = new FileContentResult(obj.fileContent.ToArray(), "application/octet-stream")
                    {
                        FileDownloadName = obj.fileName,
                    };
                    MemoryStream myMemStream = new System.IO.MemoryStream(result.FileContents);

                    using (FileStream fs = new FileStream(Server.MapPath("~/chequeprint.html"), FileMode.Create))
                    {
                        using (StreamWriter w = new StreamWriter(fs, Encoding.UTF8))
                        {
                            w.WriteLine(Request.Url.GetLeftPart(UriPartial.Authority) + "/Content/css/print.css");
                            var hasAttr = chequeDetails.ChequeFormatID + "," + (string)ViewBag.DueDate + "," + (string)ViewBag.TableName + ",";
                            foreach (var item in _element)
                            {
                                switch (item.labelMapToField)
                                {
                                    //حذف حواله کرد
                                    case "lineDraft":
                                        if (chequeDetails.RemoveRemit)
                                        {
                                            hasAttr += "RemoveRemit,";
                                            w.WriteLine("<div style = 'position: absolute; top: " + item.m_TOP + "px; left: " + item.m_Left + "px; font-family: " + item.m_FontName + "; font-size: " + item.m_FontSize + "px; text-align: " + item.m_TextAlign + "; font-weight: " + item.m_fontweight + "; width: " + (int)item.m_Width + "px; height: " + (int)item.m_height + "px; line-height: " + (int)item.m_height + "px; direction: " + item.m_Direction + "; z-index: 2;'></div>");
                                            w.WriteLine("<div style = 'position: absolute; top: " + item.m_TOP + "px; left: " + item.m_Left + "px; font-family: " + item.m_FontName + "; font-size: " + item.m_FontSize + "px; text-align: " + item.m_TextAlign + "; font-weight: " + item.m_fontweight + "; width: " + (int)item.m_Width + "px; height: " + (int)item.m_height + "px; line-height: " + (int)item.m_height + "px; direction: " + item.m_Direction + "; z-index: 2;' ></div>");
                                        }
                                        else
                                        {
                                            w.WriteLine("<div style = 'position: absolute; top: " + item.m_TOP + "px; left: " + item.m_Left + "px; font-family: " + item.m_FontName + "; font-size: " + item.m_FontSize + "px; text-align: " + item.m_TextAlign + "; font-weight: " + item.m_fontweight + "; width: " + (int)item.m_Width + "px; height: " + (int)item.m_height + "px; line-height: " + (int)item.m_height + "px; direction: " + item.m_Direction + "; z-index: 2;' > " + item.labelValue + " </div>");
                                        }
                                        break;
                                    // حدف حواله "بابت"
                                    case "behalf":
                                        if (!chequeDetails.RemoveOrdersFor)
                                        {
                                            hasAttr += "RemoveOrdersFor,";
                                            w.WriteLine("<div style = 'position: absolute; top: " + item.m_TOP + "px; left: " + item.m_Left + "px; font-family: " + item.m_FontName + "; font-size: " + item.m_FontSize + "px; text-align: " + item.m_TextAlign + "; font-weight: " + item.m_fontweight + "; width: " + (int)item.m_Width + "px; height: " + (int)item.m_height + "px; line-height: " + (int)item.m_height + "px; direction: " + item.m_Direction + "; z-index: 2;' > " + item.labelValue + " </div>");
                                        }
                                        break;
                                    // در وجه حامل
                                    case "beneficiary":
                                        if (chequeDetails.Bearer)
                                        {
                                            hasAttr += "Bearer,";
                                            w.WriteLine("<div style = 'position: absolute; top: " + item.m_TOP + "px; left: " + item.m_Left + "px; font-family: " + item.m_FontName + "; font-size: " + item.m_FontSize + "px; text-align: " + item.m_TextAlign + "; font-weight: " + item.m_fontweight + "; width: " + (int)item.m_Width + "px; height: " + (int)item.m_height + "px; line-height: " + (int)item.m_height + "px; direction: " + item.m_Direction + "; z-index: 2;' > حامل </div>");
                                        }
                                        else if (chequeDetails.Unique_Account_No2)
                                        {
                                            chequeDetails.Unique_Account_No2Val = Utility.GetAccountNumIfExist(RePa.Main_Unique_Account_No);
                                            chequeDetails.Unique_Account_No2Val = Utility.GetAccountNumIfExist(RePa.Main_Unique_Account_No);
                                            hasAttr += "Unique_Account_No2,";
                                            w.WriteLine("<div style = 'position: absolute; top: " + item.m_TOP + "px; left: " + item.m_Left + "px; font-family: " + item.m_FontName + "; font-size: " + item.m_FontSize + "px; text-align: " + item.m_TextAlign + "; font-weight: " + item.m_fontweight + "; width: " + (int)item.m_Width + "px; height: " + (int)item.m_height + "px; line-height: " + (int)item.m_height + "px; direction: " + item.m_Direction + "; z-index: 2;' >" + chequeDetails.Unique_Account_No2Val + "</div>");
                                        }
                                        else
                                        {
                                            w.WriteLine("<div style = 'position: absolute; top: " + item.m_TOP + "px; left: " + item.m_Left + "px; font-family: " + item.m_FontName + "; font-size: " + item.m_FontSize + "px; text-align: " + item.m_TextAlign + "; font-weight: " + item.m_fontweight + "; width: " + (int)item.m_Width + "px; height: " + (int)item.m_height + "px; line-height: " + (int)item.m_height + "px; direction: " + item.m_Direction + "; z-index: 2;' > " + item.labelValue + " </div>");
                                        }
                                        break;
                                    case "beneficiaryandbehalf":
                                        if (chequeDetails.Bearer)
                                        {
                                            hasAttr += "Bearer,";
                                            w.WriteLine("<div style = 'position: absolute; top: " + item.m_TOP + "px; left: " + item.m_Left + "px; font-family: " + item.m_FontName + "; font-size: " + item.m_FontSize + "px; text-align: " + item.m_TextAlign + "; font-weight: " + item.m_fontweight + "; width: " + (int)item.m_Width + "px; height: " + (int)item.m_height + "px; line-height: " + (int)item.m_height + "px; direction: " + item.m_Direction + "; z-index: 2;' > حامل </div>");
                                        }
                                        else if (chequeDetails.Unique_Account_No2)
                                        {
                                            chequeDetails.Unique_Account_No2Val = Utility.GetAccountNumIfExist(RePa.Main_Unique_Account_No);// + ((!chequeDetails.RemoveOrdersFor) ? (" بابت " + Utility.GetBehalf(objs.BehalfID)) : "");
                                            chequeDetails.behalfValue = Utility.GetBehalf(RePa.BehalfID);
                                            hasAttr += "Unique_Account_No2,";
                                            w.WriteLine("<div style = 'position: absolute; top: " + item.m_TOP + "px; left: " + item.m_Left + "px; font-family: " + item.m_FontName + "; font-size: " + item.m_FontSize + "px; text-align: " + item.m_TextAlign + "; font-weight: " + item.m_fontweight + "; width: " + (int)item.m_Width + "px; height: " + (int)item.m_height + "px; line-height: " + (int)item.m_height + "px; direction: " + item.m_Direction + "; z-index: 2;' >" + chequeDetails.Unique_Account_No2Val + "</div>");
                                        }
                                        else
                                        {
                                            w.WriteLine("<div style = 'position: absolute; top: " + item.m_TOP + "px; left: " + item.m_Left + "px; font-family: " + item.m_FontName + "; font-size: " + item.m_FontSize + "px; text-align: " + item.m_TextAlign + "; font-weight: " + item.m_fontweight + "; width: " + (int)item.m_Width + "px; height: " + (int)item.m_height + "px; line-height: " + (int)item.m_height + "px; direction: " + item.m_Direction + "; z-index: 2;' > " + item.labelValue + " </div>");
                                        }
                                        break;
                                    // چک فاقد هر گونه قلم خوردگی است
                                    case "withoutCrossing":
                                        if (chequeDetails.WithoutCrossing)
                                        {
                                            hasAttr += "WithoutCrossing,";
                                            w.WriteLine("<div style = 'position: absolute; top: " + item.m_TOP + "px; left: " + item.m_Left + "px; font-family: " + item.m_FontName + "; font-size: " + item.m_FontSize + "px; text-align: " + item.m_TextAlign + "; font-weight: " + item.m_fontweight + "; width: " + (int)item.m_Width + "px; height: " + (int)item.m_height + "px; line-height: " + (int)item.m_height + "px; direction: " + item.m_Direction + "; z-index: 2;' > " + item.ChequeLable + " </div>");
                                        }
                                        break;
                                    // چک فاقد ظهر نویسی است
                                    case "checkIsUnderwritten":
                                        if (chequeDetails.checkIsUnderwritten)
                                        {
                                            hasAttr += "checkIsUnderwritten,";
                                            w.WriteLine("<div style = 'position: absolute; top: " + item.m_TOP + "px; left: " + item.m_Left + "px; font-family: " + item.m_FontName + "; font-size: " + item.m_FontSize + "px; text-align: " + item.m_TextAlign + "; font-weight: " + item.m_fontweight + "; width: " + (int)item.m_Width + "px; height: " + (int)item.m_height + "px; line-height: " + (int)item.m_height + "px; direction: " + item.m_Direction + "; z-index: 2;' > " + item.ChequeLable + " </div>");
                                        }
                                        break;
                                    // چک غیر قابل انتقال می باشد
                                    case "nonTransferable":
                                        if (chequeDetails.NonTransferable)
                                        {
                                            hasAttr += "NonTransferable,";
                                            w.WriteLine("<div style = 'position: absolute; top: " + item.m_TOP + "px; left: " + item.m_Left + "px; font-family: " + item.m_FontName + "; font-size: " + item.m_FontSize + "px; text-align: " + item.m_TextAlign + "; font-weight: " + item.m_fontweight + "; width: " + (int)item.m_Width + "px; height: " + (int)item.m_height + "px; line-height: " + (int)item.m_height + "px; direction: " + item.m_Direction + "; z-index: 2;' > " + item.ChequeLable + " </div>");
                                        }
                                        break;
                                    // چک در وجه حساب جاری
                                    case "currentAccount":
                                        if (chequeDetails.CurrentAccount)
                                        {
                                            chequeDetails.BeneficiarDetailID = chequeDetails.CurrentAccountId;
                                            hasAttr += "CurrentAccount,";
                                            w.WriteLine("<div style = 'position: absolute; top: " + item.m_TOP + "px; left: " + item.m_Left + "px; font-family: " + item.m_FontName + "; font-size: " + item.m_FontSize + "px; text-align: " + item.m_TextAlign + "; font-weight: " + item.m_fontweight + "; width: " + (int)item.m_Width + "px; height: " + (int)item.m_height + "px; line-height: " + (int)item.m_height + "px; direction: " + item.m_Direction + "; z-index: 2;' >" + Utility.GetAccountNum(chequeDetails.BeneficiarDetailID) + " </div>");
                                        }
                                        break;
                                    // چک در وجه حساب شبا
                                    case "shebaAccount":
                                        if (chequeDetails.ShebaAccount)
                                        {
                                            chequeDetails.BeneficiarDetailID = chequeDetails.ShebaAccountId;
                                            hasAttr += "ShebaAccount,";
                                            w.WriteLine("<div style = 'position: absolute; top: " + item.m_TOP + "px; left: " + item.m_Left + "px; font-family: " + item.m_FontName + "; font-size: " + item.m_FontSize + "px; text-align: " + item.m_TextAlign + "; font-weight: " + item.m_fontweight + "; width: " + (int)item.m_Width + "px; height: " + (int)item.m_height + "px; line-height: " + (int)item.m_height + "px; direction: " + item.m_Direction + "; z-index: 2;' >" + Utility.GetShebaNum(chequeDetails.BeneficiarDetailID) + " </div>");
                                        }
                                        break;
                                    // در شعبه صادر کننده قابل وصول است
                                    case "branchExporterReceivable":
                                        if (chequeDetails.BranchExporterReceivable)
                                        {
                                            hasAttr += "BranchExporterReceivable,";
                                            w.WriteLine("<div style = 'position: absolute; top: " + item.m_TOP + "px; left: " + item.m_Left + "px; font-family: " + item.m_FontName + "; font-size: " + item.m_FontSize + "px; text-align: " + item.m_TextAlign + "; font-weight: " + item.m_fontweight + "; width: " + (int)item.m_Width + "px; height: " + (int)item.m_height + "px; line-height: " + (int)item.m_height + "px; direction: " + item.m_Direction + "; z-index: 2;' > " + item.ChequeLable + " </div>");
                                        }
                                        break;
                                    // تایید صدور چک
                                    case "approveIssuance":
                                        if (chequeDetails.ApproveIssuance)
                                        {
                                            hasAttr += "ApproveIssuance,";
                                            w.WriteLine("<div style = 'position: absolute; top: " + item.m_TOP + "px; left: " + item.m_Left + "px; font-family: " + item.m_FontName + "; font-size: " + item.m_FontSize + "px; text-align: " + item.m_TextAlign + "; font-weight: " + item.m_fontweight + "; width: " + (int)item.m_Width + "px; height: " + (int)item.m_height + "px; line-height: " + (int)item.m_height + "px; direction: " + item.m_Direction + "; z-index: 2;' > " + item.ChequeLable + " </div>");
                                        }
                                        break;
                                    default:
                                        w.WriteLine("<div style = 'position: absolute; top: " + item.m_TOP + "px; left: " + item.m_Left + "px; font-family: " + item.m_FontName + "; font-size: " + item.m_FontSize + "px; text-align: " + item.m_TextAlign + "; font-weight: " + item.m_fontweight + "; width: " + (int)item.m_Width + "px; height: " + (int)item.m_height + "px; line-height: " + (int)item.m_height + "px; direction: " + item.m_Direction + "; z-index: 2;' > " + item.labelValue + " </div>");
                                        break;
                                }
                            }


                        }
                    }
                    height = Image.FromStream(myMemStream).Height;
                    width = Image.FromStream(myMemStream).Width;
                    //printcheque(_element);
                    elements = _element;
                    return PartialView(new Issuance
                    {
                        Image = Convert.ToBase64String(result.FileContents),
                        Extension = obj.fileType.Split('/')[1],
                        Height = Image.FromStream(myMemStream).Height,
                        Width = Image.FromStream(myMemStream).Width,
                        Elements = _element,
                        ChequeDetails = chequeDetails
                    });
                }
                return PartialView();
            }



        }
        public ActionResult ShowChequePrint(ChequeDetails chequeDetails)
        {
            Icls_ChequeFormatTypeDetailsClient CD = new Icls_ChequeFormatTypeDetailsClient();
            Icls_ChequesClient cc = new Icls_ChequesClient();
            //Icls_Request_PaymentClient RP = new Icls_Request_PaymentClient();
            var dc = new TreasuryEntities();

            //var request = RP.SelectRow((int)chequeDetails.RequestId).SingleOrDefault();
            var request = dc.Request_Payment.Find((int)chequeDetails.RequestId);
            var BankID = request.Source_PaymentValue;
            string whereclouse = "ChequeFormatID=" + chequeDetails.ChequeFormatID;
            int totalRows;

            var cd = cc.SelectBy_DynamicWhereClause("ChequeFormatID =" + chequeDetails.ChequeFormatID + " and BankID = " + BankID + " and IsActive = " + true, 1, 100, "ChequeID", "int", false, out totalRows);
            if (cd.Count() != 0)
            {
                //آخرین شماره چک 
                ViewBag.ChequeNo = cd.SingleOrDefault().LastUsedPaperNo + 1;
            }

            ViewBag.RequestId = chequeDetails.RequestId;
            if (chequeDetails.DueDate != null)
            {
                ViewBag.DueDate = chequeDetails.DueDate.Substring(0, 4) + "/" + chequeDetails.DueDate.Substring(4, 2) + "/" + chequeDetails.DueDate.Substring(6, 2);
            }
            ViewBag.ChequeFormatID = chequeDetails.ChequeFormatID;

            //لیستی از لیبل ها بر اساس نوع چک
            var lables = CD.SelectBy_DynamicWhereClause(whereclouse, 1, 100, "FormatID", "int", false, out totalRows).ToList();
            //لیستی از المان ها ی چک 
            var _element = new List<Service.ChequePrintService.ChequeElement>();

            //درخواست وجه را به همراه وضعیت هایش می دهد 
            //var objs = Utility.GetRequest_Payment_WithLastStates((int)chequeDetails.RequestId);
            //var objs = dc.Request_Payment_StatusDetails.Where(d => d.RequestID == (int)chequeDetails.RequestId).OrderByDescending(d => d.Request_Payment_StatusDetailsID).FirstOrDefault();
            foreach (var item in lables)
            {
                var value = "";
                string mapToField = Utility.GetLable(item.ChequeLableID).MapToField;
                switch (mapToField)
                {
                    case "chequeDate_n":
                        value = request.RequestDate;
                        break;
                    case "chequeDate_w":
                        value = request.RequestDate;
                        break;
                    case "day_n":
                        value = request.RequestDate.Substring(6, 2);
                        break;
                    case "day_w":
                        value = request.RequestDate.Substring(6, 2);
                        break;
                    case "month_n":
                        value = request.RequestDate.Substring(4, 2);
                        break;
                    case "month_w":
                        value = request.RequestDate.Substring(4, 2);
                        break;
                    case "year_n":
                        value = request.RequestDate.Substring(0, 4);
                        break;
                    case "year_w":
                        value = request.RequestDate.Substring(0, 4);
                        break;
                    case "price_n":
                        value = request.PayAmount.ToString();
                        break;
                    case "price_w":
                        value = request.PayAmount.ToString();
                        break;
                    case "beneficiary":
                        if (!string.IsNullOrEmpty(chequeDetails.PayTo))
                        {
                            value = request.Main_Unique_Account_No;
                        }
                        else
                        {
                            value = Utility.GetAccountNumIfExist(request.Unique_Account_No);
                        }
                        break;
                    case "behalf":
                        value = Utility.GetBehalf(request.BehalfID);
                        break;
                    case "lineDraft":
                        value = "";
                        break;
                    case "price_e_n":
                        value = request.PayAmount.ToString();
                        break;
                    case "price_e_w":
                        value = request.PayAmount.ToString();
                        break;
                    case "chequeDate_e_n":
                        if (chequeDetails.DueDate != null)
                        {
                            value = chequeDetails.DueDate.Substring(0, 4) + "/" + chequeDetails.DueDate.Substring(4, 2) + "/" + chequeDetails.DueDate.Substring(6, 2);
                        }
                        break;
                    case "chequeDate_e_w":
                        if (chequeDetails.DueDate != null)
                        {
                            value = chequeDetails.DueDate.Substring(0, 4) + "/" + chequeDetails.DueDate.Substring(4, 2) + "/" + chequeDetails.DueDate.Substring(6, 2);
                        }
                        break;
                }
                Service.ChequePrintService.ChequeElement RPW = new Service.ChequePrintService.ChequeElement();
                RPW.m_TOP = item.m_TOP.Value;
                RPW.m_Left = item.m_Left.Value;
                RPW.m_FontName = item.m_FontName;
                RPW.m_FontSize = item.m_FontSize.Value;
                RPW.m_TextAlign = item.m_TextAlign;
                RPW.m_fontweight = item.m_fontweight;
                RPW.m_Width = item.m_Width.Value;
                RPW.m_height = item.m_height.Value;
                RPW.m_Direction = item.m_Direction;
                RPW.ChequeLable = Utility.GetLable(item.ChequeLableID).ChequeLableDescription;
                RPW.labelValue = value;
                RPW.labelMapToField = mapToField;
                _element.Add(RPW);
            }

            var db = new TreasuryFilesEntities();
            {
                CSD.Treasury.DataModel.File obj = new CSD.Treasury.DataModel.File();
                obj = db.Files.Where(u => u.tableName == chequeDetails.TableName && u.relatedID == chequeDetails.ChequeFormatID).FirstOrDefault();
                if (obj != null)
                {
                    FileContentResult result = new FileContentResult(obj.fileContent.ToArray(), "application/octet-stream")
                    {
                        FileDownloadName = obj.fileName,
                    };
                    MemoryStream myMemStream = new System.IO.MemoryStream(result.FileContents);
                    return PartialView(new Issuance
                    {
                        Image = Convert.ToBase64String(result.FileContents),
                        Extension = obj.fileType.Split('/')[1],
                        Height = Image.FromStream(myMemStream).Height,
                        Width = Image.FromStream(myMemStream).Width,
                        Elements = _element,
                        ChequeDetails = chequeDetails
                    });
                }
                return PartialView();
            }

        }
        public JsonResult Testprintcheque()
        {
            Service.ChequePrintService.PrintServiceClient print = new Service.ChequePrintService.PrintServiceClient();
            string ipAddress = ConfigurationManager.AppSettings["ipAddress"].ToString();
            string result = print.TestPrint(ipAddress);
            return Json(result, JsonRequestBehavior.AllowGet);
        }
        public JsonResult printcheque()
        {
            try
            {

                Service.ChequePrintService.PrintServiceClient print = new Service.ChequePrintService.PrintServiceClient();
                string ipAddress = ConfigurationManager.AppSettings["ipAddress"].ToString();
                var chequeService = new Service.ChequePrintService.Cheque();
                chequeService.ChequeElements = elements.ToArray();// = {
                                                                  //ChequeElements = elements.ToArray(),
                chequeService.ChequeDetails = new Service.ChequePrintService.ChequeDetails();
                {
                    chequeService.ChequeDetails.ApproveIssuance = _chequeDetails.ApproveIssuance;
                    chequeService.ChequeDetails.Bearer = _chequeDetails.Bearer;
                    chequeService.ChequeDetails.Unique_Account_No2 = _chequeDetails.Unique_Account_No2;
                    chequeService.ChequeDetails.Unique_Account_No2Val = _chequeDetails.Unique_Account_No2Val;
                    chequeService.ChequeDetails.BehalfValue = _chequeDetails.behalfValue;
                    chequeService.ChequeDetails.BranchExporterReceivable = _chequeDetails.BranchExporterReceivable;
                    chequeService.ChequeDetails.ChequeFormatID = _chequeDetails.ChequeFormatID;
                    chequeService.ChequeDetails.CurrentAccount = _chequeDetails.CurrentAccount;
                    chequeService.ChequeDetails.CurrentAccountId = _chequeDetails.CurrentAccountId;
                    chequeService.ChequeDetails.DueDate = _chequeDetails.DueDate;
                    chequeService.ChequeDetails.NonTransferable = _chequeDetails.NonTransferable;
                    chequeService.ChequeDetails.RemoveOrdersFor = _chequeDetails.RemoveOrdersFor;
                    chequeService.ChequeDetails.RemoveRemit = _chequeDetails.RemoveRemit;
                    chequeService.ChequeDetails.RequestId = _chequeDetails.RequestId;
                    chequeService.ChequeDetails.ShebaAccount = _chequeDetails.ShebaAccount;
                    chequeService.ChequeDetails.ShowDescription = _chequeDetails.ShowDescription;
                    chequeService.ChequeDetails.Description = _chequeDetails.Description;
                    chequeService.ChequeDetails.ShebaAccountId = _chequeDetails.ShebaAccountId;
                    chequeService.ChequeDetails.TableName = _chequeDetails.TableName;
                    chequeService.ChequeDetails.WithoutCrossing = _chequeDetails.WithoutCrossing;
                    chequeService.ChequeDetails.checkIsUnderwritten = _chequeDetails.checkIsUnderwritten;
                    if (_chequeDetails.BeneficiarDetailID != 0)
                    {
                        chequeService.ChequeDetails.BeneficiarDetail = Utility.GetAccountNum(_chequeDetails.BeneficiarDetailID);
                        if (_chequeDetails.ShebaAccount)
                        {
                            chequeService.ChequeDetails.ShebaValue = Utility.GetShebaNum(_chequeDetails.BeneficiarDetailID);
                        }
                        if (_chequeDetails.CurrentAccount)
                        {
                            chequeService.ChequeDetails.CurrentAccountValue = Utility.GetAccountNum(_chequeDetails.BeneficiarDetailID);
                        }
                    }

                }
                // };
                //bool result = print.PrintCheque(chequeService, ipAddress);
                if (true)
                {
                    return Json(true, JsonRequestBehavior.AllowGet);
                }
                else
                {
                    return Json(false, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception e)
            {
                return Json(false, JsonRequestBehavior.AllowGet);
            }
        }

        //[HttpPost]
        //public ActionResult Issuance(string tableName, decimal ChequeFormatID, int requestId, string dueDate)
        //{
        //    return View();
        //}

        public JsonResult GetChequeFormatTypeImage(string tableName, int requestId, decimal relatedID = 1)
        {


            Icls_ChequeFormatTypeDetailsClient CD = new Icls_ChequeFormatTypeDetailsClient();
            string whereclouse = "ChequeFormatID=" + relatedID;
            int totalRows;
            //لیستی از لیبل ها بر اساس نوع چک
            var lables = CD.SelectBy_DynamicWhereClause(whereclouse, 1, 100, "FormatID", "int", false, out totalRows).ToList();
            var _element = new List<dynamic>();

            foreach (var item in lables)
            {
                _element.Add(new
                {
                    m_TOP = item.m_TOP,
                    m_Left = item.m_Left,
                    m_FontName = item.m_FontName,
                    m_FontSize = item.m_FontSize,
                    m_TextAlign = item.m_TextAlign,
                    m_fontweight = item.m_fontweight,
                    m_Width = item.m_Width,
                    m_height = item.m_height,
                    m_Direction = item.m_Direction,
                    ChequeLable = Utility.GetLable(item.ChequeLableID).ChequeLableDescription
                });
            }
            var db = new TreasuryFilesEntities();
            {
                CSD.Treasury.DataModel.File obj = new CSD.Treasury.DataModel.File();
                obj = db.Files.Where(u => u.tableName == tableName && u.relatedID == relatedID).FirstOrDefault();
                if (obj != null)
                {
                    FileContentResult result = new FileContentResult(obj.fileContent.ToArray(), "application/octet-stream")
                    {
                        FileDownloadName = obj.fileName,
                    };
                    MemoryStream myMemStream = new System.IO.MemoryStream(result.FileContents);
                    return Json(new
                    {
                        Image = Convert.ToBase64String(result.FileContents),
                        Extension = obj.fileType.Split('/')[1],
                        Height = Image.FromStream(myMemStream).Height,
                        Width = Image.FromStream(myMemStream).Width,
                        Elements = _element
                    }, JsonRequestBehavior.AllowGet);
                }
                return Json(false, JsonRequestBehavior.AllowGet);
            }
        }
        /// <summary>
        /// save in Payment 
        /// save dueDate in RequsetPayment
        /// </summary>
        public bool SavePaymentDueDate(string DueDate, int ChequeNo, int RequestId, decimal ChequeFormatID, string hasAttr,decimal ExchangeRate = 0)
        {
            try
            {
                Icls_Request_PaymentClient RP = new Icls_Request_PaymentClient();
                Icls_TransferFromBackupBankClient TransferClient = new Icls_TransferFromBackupBankClient();
                //Icls_BankClient BankClient = new Icls_BankClient();
                Icls_SPsClient db = new Icls_SPsClient();
                var dc = new TreasuryEntities();
                var dateNow = Utility.ToPersianDate(DateTime.Now);

                string message = "";
                int totalRow;
                //var RequestObj = RP.SelectRow(RequestId).SingleOrDefault();
                var RequestObj = dc.Request_Payment.Find(RequestId);
                if (DueDate != null)
                {
                    //update RequsetPayment                                   
                    RequestObj.ChequeNo = ChequeNo;
                    RequestObj.DueDate = DueDate.Replace("/", String.Empty);
                    RequestObj.ChequeElements_PrintLayout = hasAttr;
                    if(ExchangeRate != 0)
                    {
                        RequestObj.ExchangeRate = ExchangeRate;
                        RequestObj.PayAmount = RequestObj.ExchangeRate * RequestObj.ExchangeAmount.Value;
                    }
                    
                    dc.SaveChanges();
                    //RP.Update(RequestObj, out message);
                }
                //درصورتی که درخواست وجه ای باشد که از تجمیع درخواست وجه های تایید پرداخت شده حاصل شده باشد
                //دراین وضعیت باید تمام درخواست وجه هایی که با هم تجمیع شده اند را تاریخ سررسید و شماره چک بزنیم
                //وضعیت پرداخت را نیز باید ست کنیم در صورتی که پرداخت زمان چاپ چک باشد
                //ثبت پرداخت در پایین نوشته شده
                //string whereclouse = "AggrigateOrDuplicationID=" + RequestId + " and AggrigateOrDuplicationStatusID=7 and Paid=NULL and Delivered=NULL";
                //var queryls = RP.GetAllRequestStatus_DynamicWhereClause(dateNow, whereclouse, 1, 200, "RequestID", "dec", false, out totalRows).ToList();
                var queryls = dc.GetAllRequestStatus(dateNow).Where(d => d.AggrigateOrDuplicationID == RequestId && d.AggrigateOrDuplicationStatusID == 7 && d.Paid == null && d.Delivered == null).ToList();
                foreach (var item in queryls)
                {
                    if (DueDate != null)
                    {
                        //var Requsetp = RP.SelectRow(item.RequestID).SingleOrDefault();
                        var Requsetp = dc.Request_Payment.Find(item.RequestID);
                        Requsetp.ChequeNo = ChequeNo;
                        Requsetp.ChequeElements_PrintLayout = hasAttr;
                        Requsetp.DueDate = DueDate.Replace("/", String.Empty);
                        dc.SaveChanges();
                        //RP.Update(Requsetp, out message2);
                    }
                }


                DueDate = DueDate.Replace("/", string.Empty);
                //درصورتیکه تاریخ سررسید چک تاریخ روز باشد از حساب باید کم گردد 
                //ذخیره در جدول payment به عنوان پرداخت شده
                //2 چک انتظامی 
                //و درصورتی که در کانفیگ پرداخت در زمان چاپ باشد 

                if (DueDate == dateNow && RequestObj.Pay_TypeDetailID_source != 2 && ConfigurationManager.AppSettings["PayWhenCheckPrinted"] == "True")
                {
                    //save payment
                    Icls_PaymentClient paymentClient = new Icls_PaymentClient();

                    var PAymentOBj = paymentClient.SelectBy_DynamicWhereClause("RequestID =" + RequestId, 1, 10, "PaymentID", "dec", false, out totalRow).Any();

                    // برای تجمیع درخواست 
                    // queryls.Count()
                    // مقداری بیش از صفر بر میگرداند و باعث میشد که در جدول پیمنت رکوردی ثبت نشود
                    // در نتیجه در گزارش مغایرت بانکی تجمیع ها دیده نمیشد
                    // برای رفع این نقیصه شرط 
                    // queryls.Count() == 0
                    // برداشته شد
                    if (!PAymentOBj) // & queryls.Count() == 0)
                    {
                        Service.PaymentService.Payment payment = new Service.PaymentService.Payment();
                        payment.RequestID = RequestId;
                        payment.PaymentDate = Utility.ToPersianDate(DateTime.Now);
                        payment.HostName = System.Net.Dns.GetHostName();
                        payment.UserID = User.Id;
                        payment.Effective = true;
                        var Result = paymentClient.Insert(payment, out message);
                    }

                    //درصورتی که درخواست وجه ای باشد که از تجمیع درخواست وجه های تایید پرداخت شده حاصل شده باشد
                    foreach (var item in queryls)
                    {
                        var PymentOBj = paymentClient.SelectBy_DynamicWhereClause("RequestID =" + item.RequestID, 1, 10, "PaymentID", "dec", false, out totalRow);
                        if (PymentOBj == null)
                        {
                            Service.PaymentService.Payment payment = new Service.PaymentService.Payment();
                            payment.RequestID = item.RequestID;
                            payment.PaymentDate = Utility.ToPersianDate(DateTime.Now);
                            payment.HostName = System.Net.Dns.GetHostName();
                            payment.UserID = User.Id;
                            payment.Effective = true;
                            var Result = paymentClient.Insert(payment, out message);
                        }
                    }
                    //var Request = RP.SelectRow(RequestId).SingleOrDefault();
                    var Request = dc.Request_Payment.Find(RequestId);
                    var BankID = Request.Source_PaymentValue;
                    var Date = Utility.ToPersianDate(DateTime.Now);


                    var Bank = new GetBankBalance_Result();
                    //Bank = query.FirstOrDefault();

                    //var ChequePrice = Request.PayAmount;
                    //var BackupBank = BankClient.SelectBy_DynamicWhereClause("Backup_BankID =" + Bank.BankID, 1, 10, "BankID", "int", false, out totalRow);



                }

                var Source_PaymentValue = int.Parse(RequestObj.Source_PaymentValue);
                var BankObj = dc.Banks.SingleOrDefault(d => d.BankID == Source_PaymentValue);
                //Icls_BankClient B = new Icls_BankClient();
                //var BankObj = B.SelectRow(int.Parse(RequestObj.Source_PaymentValue)).SingleOrDefault();
                //BankObj.OpeningValue = BankObj.OpeningValue - RequestObj.PayAmount;
                //B.Update(BankObj, out message);

                // اضافه نمودن شماره چک
                Icls_ChequesClient ch = new Icls_ChequesClient();
                var cheque = ch.SelectBy_DynamicWhereClause("BankID=" + BankObj.BankID + "and  ChequeFormatID = " + ChequeFormatID + " and IsActive=true", 1, 100, "ChequeID", "int", true, out totalRow).SingleOrDefault();
                cheque.LastUsedPaperNo = cheque.LastUsedPaperNo + 1;
                var Result1 = ch.Update(cheque, out message);



                return Result1;
            }
            catch (Exception e)
            {
                throw;
            }


        }
        /// <summary>
        ///دراین مورد دیگر برگه چک قابل استفاده نیست وابطال می گردد. درصورتی که هنگام پرینت چک عملیات با مشکل  روبرو شود درخواست وجه کنسل می گردد و درخواست جدیدی زده می شود
        /// </summary>
        /// <returns></returns>
        public ActionResult CancelRequestForCancelCheque(int RequestId, decimal ChequeFormatID)
        {
            try
            {


                int totalRows;
                Icls_Request_PaymentClient RP = new Icls_Request_PaymentClient();
                Icls_ChequesClient cc = new Icls_ChequesClient();
                string messageUpdate, messageRequest, message1;
                var db = new TreasuryEntities();

                //باطل کردن شماره چکی که برگه آن خراب شده است
                //var BankID = RP.SelectRow(RequestId).SingleOrDefault().Source_PaymentValue;
                var request = db.Request_Payment.Find(RequestId);
                var Cheques = cc.SelectBy_DynamicWhereClause("ChequeFormatID =" + ChequeFormatID + " and BankID = " + request.Source_PaymentValue + " and IsActive = " + true, 1, 100, "ChequeID", "int", false, out totalRows).SingleOrDefault();
                Cheques.LastUsedPaperNo = Cheques.LastUsedPaperNo + 1;

                cc.Update(Cheques, out messageUpdate);



                //var request = RP.SelectRow(RequestId).SingleOrDefault();


                //cancel RequestPayment
                Icls_Request_Payment_StatusDetailsClient RPS = new Icls_Request_Payment_StatusDetailsClient();
                Icls_Request_PaymentClient RequestClient = new Icls_Request_PaymentClient();
                var persianDate = Utility.ToPersianDate(DateTime.Now);
                string message = "";
                //Service.Request_Payment_StatusDetailsService.Request_Payment_StatusDetails objSts = new Service.Request_Payment_StatusDetailsService.Request_Payment_StatusDetails();
                DataModel.Request_Payment_StatusDetails objSts = new DataModel.Request_Payment_StatusDetails();
                objSts.ActionDate = persianDate;
                objSts.UserID = User.Id;
                objSts.ActionTime = DateTime.Now.TimeOfDay;
                objSts.RequestStatusTypeID = (decimal)Common.Enumeration.RequestStatusTypes.Cancellation;
                objSts.RequestID = RequestId;
                objSts.HostName = System.Net.Dns.GetHostName();
                db.Request_Payment_StatusDetails.Add(objSts);
                //var result = RPS.Insert(objSts, out message);

                //ذکرعلت کنسل شدن به دلیل پاره شدن
                var RequsetObj = RequestClient.SelectRow(RequestId).SingleOrDefault();
                if (RequsetObj.Description == null)
                {
                    //درصورتی که شماره حساب و شبا رانداشته باشد
                    //چون شماره حساب و شبا ی ذینفع در این فیلد ذخیره می شود بعدازآن سمیکالون میآید
                    RequsetObj.Description = ";" + Common.Enumeration.RequestDescription.TearingCheque.ToString();
                }
                else
                {
                    RequsetObj.Description = Common.Enumeration.RequestDescription.TearingCheque.ToString();
                }
                RequestClient.Update(RequsetObj, out message);

                //insert new Request that is result of cancel cheque
                Treasury.DataModel.Request_Payment obj = new Treasury.DataModel.Request_Payment();
                var dateNow = DateTime.Now;
                obj.UserID = request.UserID;
                obj.FiscalYear = request.FiscalYear;
                obj.RequestDate = Utility.ToPersianDate(dateNow);
                obj.RequestCreatedTime = dateNow.TimeOfDay;
                obj.Priority = request.Priority;
                obj.Unique_Account_No = request.Unique_Account_No;
                obj.BehalfID = request.BehalfID;
                obj.ExchangeTypeID = request.ExchangeTypeID.Value;
                obj.PayAmount = request.PayAmount;
                obj.Main_Unique_Account_No = request.Main_Unique_Account_No;
                obj.Destination_PaymentValue = request.Destination_PaymentValue;
                obj.Destination_TypesID = request.Destination_TypesID;
                obj.EquivalencePrice = request.EquivalencePrice;
                obj.ExchangeRate = request.ExchangeRate;
                obj.Payment_ReferenceTypes_Value = request.Payment_ReferenceTypes_Value;
                obj.Pay_TypeDetailID_source = request.Pay_TypeDetailID_source;
                obj.Source_PaymentValue = request.Source_PaymentValue;
                //obj.UniqueAccountType = request.UniqueAccountType;
                obj.Unique_Account_No = request.Unique_Account_No;
                db.Request_Payment.Add(obj);
                db.SaveChanges();
                //var result1 = RP.Insert(obj, out messageRequest);


                // درصورتی که درخواست وجه ای باشد که از تجمیع درخواست های تایید پرداخت شده ایجاد شده باشد
                //درصورت کنسل شدن باید child ها هم تغییر کند
                //یعنی زیرمجموعه درخواست وجه کنسل شده هم مرتبط شوند به درخواست وچه ای که جدید ایجاد می شود

                if (request.AggrigateOrDuplicationID == null && request.AggrigateOrDuplicationStatusID.Value == (int)RequestDescription.ResultOfIntegrationRequest)
                {
                    //درخواست جدیدایجادشده باید نتیجه تاکثیرباشد
                    //var NewRequest = RP.SelectRow(decimal.Parse(messageRequest.Split(';')[1])).SingleOrDefault();
                    var NewRequest = db.Request_Payment.Find(obj.RequestID);
                    NewRequest.AggrigateOrDuplicationStatusID = (int)RequestDescription.ResultOfIntegrationRequest;
                    //RP.Update(NewRequest, out message1);

                    db.SaveChanges();


                    //child list of Requst
                    var ChildRequsetLs = RP.SelectBy_DynamicWhereClause("AggrigateOrDuplicationID =" + request.RequestID, 1, 2000, "RequestID", "dec", false, out totalRows);
                    //اپدیت نمودن شماره والد به شماره درخواست وجه جدید ایجاد شده
                    foreach (var item in ChildRequsetLs)
                    {
                        item.AggrigateOrDuplicationID = obj.RequestID;
                        RP.Update(item, out message1);
                    }
                }





                //ثبت نامشخص در جدول وضعیت ها
                //Service.Request_Payment_StatusDetailsService.Request_Payment_StatusDetails objSts1 = new Service.Request_Payment_StatusDetailsService.Request_Payment_StatusDetails();
                DataModel.Request_Payment_StatusDetails objSts1 = new DataModel.Request_Payment_StatusDetails();
                objSts1.ActionDate = persianDate;
                objSts1.ActionTime = DateTime.Now.TimeOfDay;
                objSts1.UserID = User.Id;
                objSts1.RequestStatusTypeID = (decimal)Common.Enumeration.RequestStatusTypes.Unknown;
                objSts1.RequestID = obj.RequestID;
                objSts1.HostName = System.Net.Dns.GetHostName();
                db.Request_Payment_StatusDetails.Add(objSts1);
                //RPS.Insert(objSts1, out message);

                //ثبت تایید درخواست در جدول وضعیت ها
                //Request_Payment_StatusDetails objSts2 = new Request_Payment_StatusDetails();
                //objSts2.ActionDate = persianDate;
                //objSts2.ActionTime = DateTime.Now.TimeOfDay;
                //objSts2.RequestID = decimal.Parse(message.Split(';')[1]); 
                //objSts2.HostName = System.Net.Dns.GetHostName();

                DataModel.Request_Payment_StatusDetails objSts2 = new DataModel.Request_Payment_StatusDetails();
                objSts2.ActionDate = persianDate;
                objSts2.ActionTime = DateTime.Now.TimeOfDay;
                objSts2.UserID = User.Id;
                objSts2.RequestStatusTypeID = (decimal)Common.Enumeration.RequestStatusTypes.Confirmed;
                objSts2.RequestID = obj.RequestID;
                objSts2.HostName = System.Net.Dns.GetHostName();
                db.Request_Payment_StatusDetails.Add(objSts2);


                //ثبت تایید برای پرداخت در جدول وضعیت ها
                //Request_Payment_StatusDetails objSts3 = new Request_Payment_StatusDetails();
                //objSts3.ActionDate = persianDate;
                //objSts3.ActionTime = DateTime.Now.TimeOfDay;              
                //objSts3.RequestID = decimal.Parse(message.Split(';')[1]); 
                //objSts3.HostName = System.Net.Dns.GetHostName();

                DataModel.Request_Payment_StatusDetails objSts3 = new DataModel.Request_Payment_StatusDetails();
                objSts3.ActionDate = persianDate;
                objSts3.ActionTime = DateTime.Now.TimeOfDay;
                objSts3.UserID = User.Id;
                objSts3.RequestStatusTypeID = (decimal)Common.Enumeration.RequestStatusTypes.ConfirmPayment;
                objSts3.RequestID = obj.RequestID;
                objSts3.HostName = System.Net.Dns.GetHostName();
                db.Request_Payment_StatusDetails.Add(objSts3);


                db.SaveChanges();
                return JsonMessage(true, message);
            }
            catch (Exception)
            {
                return JsonMessage(false, "با خطا مواجه شد");
                throw;
            }

            //return Json(false, JsonRequestBehavior.AllowGet);

        }
    }
}