using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Text;
using CSD.Treasury.Service.ExchangeTypesService;
using CSD.Treasury.Service.BehalfGroupService;
using CSD.Treasury.Service.BehalfsService;
using CSD.Treasury.Service.ReferenceTypeService;
using CSD.Treasury.Service.Receipt_Pay_TypeDetailService;
using CSD.Treasury.Service.Source_DestinationTypesService;
using CSD.Treasury.Service.CityService;
using CSD.Treasury.Service.ProvinceService;
using CSD.Treasury.Service.LocationsService;
using CSD.Treasury.Service.AccountsService;
using CSD.Treasury.Service.ConfigService;
using CSD.Treasury.Service.BankService;
using CSD.Treasury.Service.BankCardsService;
using CSD.Treasury.Service.Receipt_Pay_TypeService;
using CSD.Treasury.Service.BasicBanksService;
using CSD.Treasury.Service.cmpBranchesService;
using CSD.Treasury.Service.ChequesService;
using CSD.Treasury.Service.ChequeFormatTypesService;
using CSD.Treasury.Service.BasicChequeLablesService;
using CSD.Treasury.Service.ChequeFormatType_DescriptionService;
using CSD.Treasury.Service.ChequeFormatTypeDetailsService;
using CSD.Treasury.Service.ReferenceTypes_By_BehalfGroupService;
using CSD.Treasury.Service.Payment_ReferenceTypes_ValueService;
using CSD.Treasury.Service.PermissiveSignaturesService;
using CSD.Treasury.Service.PettyCashService;
using CSD.Treasury.Service.cashFlowGroupService;
using CSD.Treasury.Service.Reciept_OtherProperyService;
using CSD.Treasury.Service.BeneficiariesDetailInfoService;
using CSD.Treasury.Service.BeneficiaryService;
using CSD.Treasury.Service.CashiersService;
using CSD.Treasury.Service.PermissiveSignatureForEachBankAccountService;
using CSD.Treasury.Service.ProceduresDB;
using CSD.Treasury.Service.PettyCash_TransactionHeaderService;
using CSD.Treasury.Service.Reciept_ReferenceTypes_ValueService;
using CSD.Treasury.Service.Reciept_OtherPropery_BY_Receipt_Pay_TypeDetailService;
using CSD.Treasury.Service.Vocher_RowService;
using CSD.Treasury.Service.PlaningTypeService;
using CSD.Treasury.DataModel;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using CSD.Treasury.Service.Request_PaymentService;
using System.Web.Mvc;
using CSD.Treasury.Service.PermissiveSignature_CompleteProccessService;
using CSD.Treasury.Service.PaymentService;
using System.Text.RegularExpressions;
using CSD.Treasury.Service.PettyCash_TransactionDetailService;
using CSD.Treasury.Service.Request_Payment_StatusDetailsService;
using CSD.Treasury.Service.CashDeskService;
using CSD.Treasury.Service.Receipt_Detail_PlanStateService;

namespace CSD.Treasury.UI.Models
{
    public class Utility
    {
        private static string[] yakan = new string[10] { "صفر", "یک", "دو", "سه", "چهار", "پنج", "شش", "هفت", "هشت", "نه" };
        private static string[] dahgan = new string[10] { "", "", "بیست", "سی", "چهل", "پنجاه", "شصت", "هفتاد", "هشتاد", "نود" };
        private static string[] dahyek = new string[10] { "ده", "یازده", "دوازده", "سیزده", "چهارده", "پانزده", "شانزده", "هفده", "هجده", "نوزده" };
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
                    s = s + yakan[d1] + " و ";
                s = s.Substring(0, s.Length - 3);
            };
            return s;
        }
        public static string num2str(string snum)
        {
            string stotal = "";
            //snum = ((Int64)Math.Ceiling(float.Parse(snum))).ToString();
            if (snum == "") return "صفر";
            if (snum == "0")
            {
                return yakan[0];
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
        #region Date
        public static string mountToW(string mount)
        {
            string mTxt = "";
            switch (mount)
            {
                case "01":
                    mTxt = "فروردین";
                    break;
                case "02":
                    mTxt = "اردیبهشت";
                    break;
                case "03":
                    mTxt = "خرداد";
                    break;
                case "04":
                    mTxt = "تیر";
                    break;
                case "05":
                    mTxt = "مرداد";
                    break;
                case "06":
                    mTxt = "شهریور";
                    break;
                case "07":
                    mTxt = "مهر";
                    break;
                case "08":
                    mTxt = "آبان";
                    break;
                case "09":
                    mTxt = "آذر";
                    break;
                case "10":
                    mTxt = "دی";
                    break;
                case "11":
                    mTxt = "بهمن";
                    break;
                case "12":
                    mTxt = "اسفند";
                    break;
            }
            return mTxt;
        }
        public static string ToPersianDate(DateTime dt)
        {
            PersianCalendar pc = new PersianCalendar();
            string sy = pc.GetYear(dt).ToString();
            string sm = pc.GetMonth(dt).ToString("00");
            string sd = pc.GetDayOfMonth(dt).ToString("00");

            return sy + sm + sd;
        }
        public static string DateStructure(string S)
        {
            if (!String.IsNullOrEmpty(S))
            {
                var year = S.Substring(0, 4);
                var Month = S.Substring(4, 2);
                var Day = S.Substring(6, 2);
                return year + "/" + Month + "/" + Day;
            }
            return "";
        }
        public static int? HasVoucher(int ReceiptDetailId, int StateId = 0)
        {
            var db = new TreasuryEntities();
            if (ReceiptDetailId != 0)
            {
                if (StateId == 0)
                {
                    var CountVoucher = db.Voucher_Row_Reciept.Where(d => d.ReceiptDetailId == ReceiptDetailId && d.AccVoucherNo != null);
                    if (CountVoucher != null)
                    {
                        if (CountVoucher.Count() > 1)
                        {
                            return CountVoucher.FirstOrDefault().AccVoucherNo;
                        }
                    }
                }
                else
                {
                    var CountVoucher = db.Voucher_Row_Reciept.Where(d => d.ReceiptDetailId == ReceiptDetailId && d.StateId == StateId && d.AccVoucherNo != null);
                    if (CountVoucher != null)
                    {
                        if (CountVoucher.Count() > 1)
                        {
                            var voucherno = CountVoucher.FirstOrDefault().AccVoucherNo;
                            return CountVoucher.FirstOrDefault().AccVoucherNo;

                        }
                    }
                }

            }

            return 0;
        }
        //public static giveLastStatus(int ReceiptDetailId)
        //{

        //}
        /// <summary>
        /// تاریخ را با توجه به رشته ای که در دیتا بیس ذخیره شده است می دهد
        /// </summary>
        /// <returns></returns>
        public static string GetPersianDate(string RequestDate)
        {
                //if (!string.IsNullOrEmpty(RequestDate))
                if (!string.IsNullOrEmpty(RequestDate.Trim()))
            {
                if (RequestDate.Count() != 2)
                {
                    string date = "";
                    string year = RequestDate.Trim().Substring(0, 4);
                    string Mounth = RequestDate.Trim().Substring(4, 2);
                    string Day = RequestDate.Trim().Substring(6, 2);
                    date = year + "/" + Mounth + "/" + Day;
                    return date;
                }
                return "";
            }
            else
            {
                return "";
            }


        }
        public static DateTime? GetMiladiDate(string Date)
        {
            if (Date != null)
            {
                string year = Date.Substring(0, 4);
                string Mounth = Date.Substring(4, 2);
                string Day = Date.Substring(6, 2);
                PersianCalendar pc = new PersianCalendar();
                DateTime dt2 = pc.ToDateTime(int.Parse(year), int.Parse(Mounth), int.Parse(Day), 0, 0, 0, 0);
                return dt2;
            }
            return null;
        }
        /// <summary>
        /// تاریخ را با توجه به رشته ای که در دیتا بیس ذخیره شده است به میلادی می دهد
        /// </summary>
        /// <returns></returns>
        public static string GetGregorianDate(string RequestDate)
        {
            if ((RequestDate != null && RequestDate != "" && RequestDate.Count() != 2))
            {
                string year = RequestDate.Substring(0, 4);
                string Mounth = RequestDate.Substring(4, 2);
                string Day = RequestDate.Substring(6, 2);
                //date = year + "/" + Mounth + "/" + Day;
                //string date2 = Regex.Replace(date, "[۰-۹]", x => ((char)(x.Value[0] - '۰' + '0')).ToString());              
                //DateTime dt = DateTime.ParseExact(date, "yyyy/MM/dd", CultureInfo.InvariantCulture);
                PersianCalendar pc = new PersianCalendar();
                //DateTime dt2 = pc.ToDateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second, dt.Millisecond);
                DateTime dt2 = pc.ToDateTime(int.Parse(year), int.Parse(Mounth), int.Parse(Day), 0, 0, 0, 0);
                return dt2.Year + "/" + dt2.Month + "/" + dt2.Day;
            }
            else
            {
                return "";
            }


        }

        /// <summary>
        /// لیستی از  سالی مالی را بر می گرداند
        /// </summary>
        /// <returns></returns>
        public static List<DataModel.Config> GetFiscalYear()
        {
            //int pageNum = 1;
            //int pageSize = 1000;
            //string orderByFieldName = "FiscalYear";
            //bool isAscendingOrder = true;
            //string OrderByfeildtype = "str";
            //int totalRows;

            //Icls_ConfigClient obj = new Icls_ConfigClient();
            //List<Service.ConfigService.Config> ls = new List<Service.ConfigService.Config>();
            //var query = obj.SelectAll(pageNum, pageSize, orderByFieldName, OrderByfeildtype, isAscendingOrder, out totalRows).ToList();
            var db = new TreasuryEntities();
            var query =db.Configs.ToList();
            //ls.AddRange(query);
            return query;
        }
        #endregion

        #region Behalf
        /// <summary>
        /// لیستی از گروه بابت ها  را از دیتا بیس بر می گرداند
        /// </summary>
        /// <returns></returns>
        public static List<Service.BehalfGroupService.BehalfGroup> GetBehalfsGroupLs(int BehalfsGroupId = 0)
        {
            int totalRows;
            Icls_BehalfGroupClient obj = new Icls_BehalfGroupClient();
            List<Service.BehalfGroupService.BehalfGroup> ls = new List<Service.BehalfGroupService.BehalfGroup>();
            if (BehalfsGroupId != 0)
            {
                string whereclouse = "BehalfGroupId=" + BehalfsGroupId;
                var query = obj.SelectBy_DynamicWhereClause(whereclouse, 1, 1000, "BehalfGroupID", "int", true, out totalRows);
                foreach (var item in query)
                {
                    if (item.BehalfGroupDescription.Contains('ي'))
                    {
                        item.BehalfGroupDescription = item.BehalfGroupDescription.Replace('ي', 'ی');
                    }
                    if (item.BehalfGroupDescription.Contains('ك'))
                    {
                        item.BehalfGroupDescription = item.BehalfGroupDescription.Replace('ك', 'ک');
                    }
                    ls.Add(item);
                }
                // ls.AddRange(query);
                return ls;
            }
            else
            {
                var query = obj.SelectAll(1, 1000, "BehalfGroupID", "int", true, out totalRows);
                foreach (var item in query)
                {
                    if (item.BehalfGroupDescription.Contains('ي'))
                    {
                        item.BehalfGroupDescription = item.BehalfGroupDescription.Replace('ي', 'ی');
                    }
                    if (item.BehalfGroupDescription.Contains('ك'))
                    {
                        item.BehalfGroupDescription = item.BehalfGroupDescription.Replace('ك', 'ک');
                    }
                    ls.Add(item);
                }
                // ls.AddRange(query);
                return ls;
            }


        }
        /// <summary>
        /// لیست بابت ها
        /// </summary>
        /// <param name="BehalfsGroupId"></param>
        /// <returns></returns>
        public static List<Service.BehalfsService.Behalf> GetBehalfLs(int BehalfsGroupId = 0)
        {
            Icls_BehalfsClient BehalfClient = new Icls_BehalfsClient();
            var whereclouse = "";
            if (BehalfsGroupId == 0)
            {

                int totalRows;
                var query = BehalfClient.SelectAll(1, 1000 * 1000, "BehalfGroupID", "int", true, out totalRows).ToList();
                return query;
            }
            else
            {
                int totalRows;

                List<Service.BehalfsService.Behalf> ls = new List<Service.BehalfsService.Behalf>();
                whereclouse = " BehalfGroupId=" + BehalfsGroupId.ToString();
                var query = BehalfClient.SelectBy_DynamicWhereClause(whereclouse, 1, 1000 * 1000, "BehalfGroupID", "int", true, out totalRows);
                ls.AddRange(query);
                return ls;
            }

        }


        public static string GetBehalf(int BehalfID)
        {
            //Service.BehalfsService.Icls_BehalfsClient obj = new Service.BehalfsService.Icls_BehalfsClient();
            //var Behalf = obj.SelectRow(BehalfID);
            //return Behalf.FirstOrDefault().BehalfDescription;
            var db = new TreasuryEntities();
            var Behalf = db.Behalfs.SingleOrDefault(d => d.BehalfID == BehalfID);
            return Behalf.BehalfDescription;
        }
        public static string GetBehalfGroup(int BehalfGroupId)
        {
            Service.BehalfGroupService.Icls_BehalfGroupClient obj = new Service.BehalfGroupService.Icls_BehalfGroupClient();
            var BehalfGroup = obj.SelectRow(BehalfGroupId);
            return BehalfGroup.SingleOrDefault().BehalfGroupDescription;
        }
        public static CSD.Treasury.DataModel.BehalfGroup GetBehalfGroupBybehalfId(int BehalfID)
        {
            try
            {
                //Service.BehalfsService.Icls_BehalfsClient obj = new Service.BehalfsService.Icls_BehalfsClient();
                //var Behalf = obj.SelectRow(BehalfID);
                //var BehalfGroupID = Behalf.SingleOrDefault().BehalfGroupID;
                //Service.BehalfGroupService.Icls_BehalfGroupClient obj1 = new Service.BehalfGroupService.Icls_BehalfGroupClient();
                //var BehalfGroup = obj1.SelectRow(BehalfGroupID);
                //return BehalfGroup.SingleOrDefault();

                var db = new TreasuryEntities();
                var BehalfGroup = db.Behalfs.SingleOrDefault(d=>d.BehalfID == BehalfID).BehalfGroup;
                return BehalfGroup;
            }
            catch (Exception ex)
            {
                throw;
            }
        }
        #endregion

        #region UniqueAccount

        /// <summary>
        /// ذیفع 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static string GetUniqueAccount(string id)
        {
            if (string.IsNullOrEmpty(id) || id == "0")
            {
                //return Resourse.Resources.Others;
                return "";
            }
            var dc = new TreasuryEntities();
            var query = dc.v_unique_accounts.SingleOrDefault(d => d.Account_Code == id);
            if (query != null)
                return query.Account_Name;
            else
                return "";
        }



        /// <summary>
        /// ذیفع 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static v_unique_accounts GetUniqueAccountObj(string id)
        {
            var dc = new TreasuryEntities();
            var query = dc.v_unique_accounts.SingleOrDefault(d => d.Account_Code == id);
            if (query != null)
                return query;
            else
                return null;
        }

        /// <summary>
        ///ذینفع اصلی 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        //public static string GetMainUniqueAccount(string id)
        //{
        //    var dc = new TreasuryEntities();
        //    var query = dc.v_accounts.SingleOrDefault(d => d.account_code ==id.Trim());
        //    if (query != null)
        //        return query.Account_Name;
        //    else
        //        return "";
        //}
        /// <summary>
        /// حساب معین درمعین
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static List<v_accounts> v_accountsLS()
        {
            var dc = new TreasuryEntities();
            var ls = dc.v_accounts.ToList();
            foreach (var item in ls)
            {
                item.account_code = item.account_code;
                item.Has_Date_Field = item.Has_Date_Field;
                item.Has_Value_Field = item.Has_Value_Field;
            }
            return ls;
        }
        public static string Getaccounts(string Account_No)
        {
            var dc = new TreasuryEntities();
            var query = dc.v_accounts.Where(d => d.account_code == Account_No);
            if (query.Count() != 0)
                return query.SingleOrDefault().Account_Name;
            else
                return "";

        }
        public static bool[] HasUniqueAccount(string Account_no)
        {
            var dc = new TreasuryEntities();
            var Vaccount = dc.v_accounts.Where(d => d.account_code == Account_no);
            var Has_Unique_Account = false;
            var Has_Unique_Account_Two = false;
            var Has_Date_Field = false;
            var Has_Value_Field = false;
            if (Vaccount != null)
            {
                Has_Unique_Account = Vaccount.SingleOrDefault().Has_Unique_Account;
                Has_Unique_Account_Two = Vaccount.SingleOrDefault().Has_Unique_Account_Two;
                Has_Date_Field = Vaccount.SingleOrDefault().Has_Date_Field;
                Has_Value_Field = Vaccount.SingleOrDefault().Has_Value_Field;
            }
            bool[] result = new bool[4];
            result[0] = Has_Unique_Account;
            result[1] = Has_Unique_Account_Two;
            result[2] = Has_Date_Field ;
            result[3] = Has_Value_Field;
            return result;
        }
        public static bool HasUniqueAccountTwo(string Account_no)
        {

            var dc = new TreasuryEntities();
            var Vaccount = dc.v_accounts.Where(d => d.account_code == Account_no);
            var Has_Unique_Account_Two = false;
            if (Vaccount != null)
            {
                Has_Unique_Account_Two = Vaccount.SingleOrDefault().Has_Unique_Account_Two;
            }

            return Has_Unique_Account_Two;
        }
        #endregion

        #region Bank

        public static int HasBackUp(int BankId)
        {
            int TotalRow;
            var db = new TreasuryEntities();
            //Icls_BankClient BankClient = new Icls_BankClient();
            //var BackupBank = BankClient.SelectBy_DynamicWhereClause("Backup_BankID =" + BankId, 1, 10, "BankID", "int", false, out TotalRow);
            var BackupBank = db.Banks.Where(d => d.Backup_BankID == BankId);
            if (BackupBank.Any())
            {
                return BackupBank.SingleOrDefault().BankID;
            }
            return 0;
        }

        /// <summary>
        /// عنوان بانک را می دهد
        /// </summary>
        /// <param name="BasicBankId"></param>
        /// <returns></returns>
        public static string GetBasicBankInfo(int BasicBankId)
        {
            Icls_BasicBanksClient obj = new Icls_BasicBanksClient();
            return obj.SelectRow(BasicBankId).FirstOrDefault().BasicBankDescription;
        }


        /// <summary>
        /// عنوان بانک را می دهد
        /// </summary>
        /// <param name="BasicBankId"></param>
        /// <returns></returns>
        public static string GetBasicBankInfoWithBankId(int BankId)
        {
            //Icls_BankClient obj = new Icls_BankClient();
            var db = new TreasuryEntities();
            var bank = db.Banks.SingleOrDefault(d => d.BankID == BankId);
            //var bank = obj.SelectRow(BankId).SingleOrDefault();
            if (bank != null)
            {
                return GetBasicBankInfo(bank.BasicBankID);
            }
            return "";
        }

        /// <summary>
        /// عنوان شعبه بانک را می دهد
        /// </summary>
        /// <param name="BasicBankId"></param>
        /// <returns></returns>
        public static CSD.Treasury.DataModel.Bank GetBankInfoBankId(int BankId)
        {
            //Icls_BankClient obj = new Icls_BankClient();
            var db = new TreasuryEntities();
            var bank = db.Banks.SingleOrDefault(d=>d.BankID == BankId);
            //var bank = obj.SelectRow(BankId).SingleOrDefault();
            if (bank != null)
                return  bank;
            else
                return null;
        }


        /// <summary>
        /// لیستی از  شعب بانک ها را بر می گرداند
        /// </summary>
        /// <returns></returns>
        public static List<Custom_Bank> GetBanks(int BasicBankId = 0 ,bool arzi=false)
        {
            //int totalRows;
            //Icls_BankClient BankClient = new Icls_BankClient();
            var db = new TreasuryEntities();
            List<Custom_Bank> ls = new List<Custom_Bank>();
          
            if (BasicBankId != 0)
            {
                //var query = BankClient.SelectBy_DynamicWhereClause("BasicBankId=" + c, 1, 1000, "BankID", "int", true, out totalRows).OrderBy(d => d.SortOrder);
                var query = db.Banks.Where(d=>d.BasicBankID == BasicBankId).OrderBy(d => d.SortOrder);
                foreach (var item in query)
                {
                    Custom_Bank customBank = new Custom_Bank();
                    customBank.BankID = item.BankID;
                    customBank.Bank_AccountNumber = "<div class='clearfix'>" + "<div class='pull-right pl15'>" + item.Bank_AccountNumber + "</div><div class='pull-right'>" + GetBasicBankInfo(item.BasicBankID) + "</div><div class='pull-right'>" + item.BranchName + "</div>";

                    ls.Add(customBank);
                }

                return ls;
            }
            else
            {
                //var query = BankClient.SelectAll(1, 1000, "BankID", "int", true, out totalRows).OrderBy(d => d.SortOrder);
                var query = db.Banks.OrderBy(d => d.SortOrder);
                if (arzi)
                {
                    query = query.Where(d=>d.ExchangeTypeID!= null).OrderBy(d => d.SortOrder);
                }
                else {
                    query = query.Where(d => d.ExchangeTypeID == null).OrderBy(d => d.SortOrder);
                }
                foreach (var item in query)
                {
                    Custom_Bank customBank = new Custom_Bank();
                    customBank.BasicBankID = item.BasicBankID;
                    customBank.BankID = item.BankID;
                    customBank.Bank_AccountNumber = item.Bank_AccountNumber + GetBasicBankInfo(item.BasicBankID) + " " + item.BranchName;
                    customBank.AccountNumber = item.Bank_AccountNumber;
                    customBank.Account_No = item.Account_No + "/" + Getaccounts(item.Account_No);
                    customBank.UniqueAccountNo = item.Unique_Account_No;
                    customBank.BranchCode = item.BranchCode;
                    customBank.BranchName = item.BranchName;
                    if (item.CityID != null)
                    {
                        customBank.CityID = item.CityID.Value;
                        customBank.CityName = Utility.GetCity(item.CityID.Value).CityDescription;
                    }

                    if (item.Unique_Account_No != null)
                    {
                        customBank.Unique_Account_No = item.Unique_Account_No + "/" + GetUniqueAccount(item.Unique_Account_No);
                    }
                    if (item.Unique_Account_No_two != null)
                    {
                        customBank.Unique_Account_No_two = item.Unique_Account_No_two + "/" + GetUniqueAccount(item.Unique_Account_No_two);
                    }

                    ls.Add(customBank);
                }

                return ls;
            }
        }

        /// <summary>
        /// لیستی از   بانک ها را بر می گرداند
        /// </summary>
        /// <returns></returns>
        public static List<Service.BasicBanksService.BasicBank> GetBasicBanks()
        {
            int pageNum = 1;
            int pageSize = 1000;
            string orderByFieldName = "BasicBankId";
            bool isAscendingOrder = true;
            string OrderByfeildtype = "int";
            int totalRows;

            Icls_BasicBanksClient obj = new Icls_BasicBanksClient();
            List<Service.BasicBanksService.BasicBank> ls = new List<Service.BasicBanksService.BasicBank>();
            var query = obj.SelectAll(pageNum, pageSize, orderByFieldName, OrderByfeildtype, isAscendingOrder, out totalRows).ToList();
            ls.AddRange(query);
            return ls;
        }

        /// <summary>
        /// لیستی از  کارت ها را بر می گرداند
        /// </summary>
        /// <returns></returns>
        public static List<Service.BankCardsService.BankCard> GetCardBanks()
        {
            int pageNum = 1;
            int pageSize = 1000;
            string orderByFieldName = "CardID";
            bool isAscendingOrder = true;
            string OrderByfeildtype = "int";
            int totalRows;

            Icls_BankCardsClient obj = new Icls_BankCardsClient();
            List<Service.BankCardsService.BankCard> ls = new List<Service.BankCardsService.BankCard>();
            var query = obj.SelectAll(pageNum, pageSize, orderByFieldName, OrderByfeildtype, isAscendingOrder, out totalRows).ToList();
            ls.AddRange(query);
            return ls;
        }
        /// <summary>
        /// بررسی می کند آیا امضا کننده های مجاز برای برگه پرداخت مشخص شده است یا نه؟ برای نمایش دکمه
        /// </summary>
        /// <param name="RequestId"></param>
        /// <returns></returns>
        public static bool IfSavepermisiveForRequestIsTrue(decimal RequestId)
        {
            Icls_PermissiveSignature_CompleteProccessClient psc = new Icls_PermissiveSignature_CompleteProccessClient();
            int totalrow;

            if (psc.SelectBy_DynamicWhereClause("RequestID =" + RequestId, 1, 1000, "PermissiveSignature_CompleteProccessID", "int", true, out totalrow).Any())
                return true;

            return false;
        }


        #endregion

        #region User
        /// <summary>
        /// واحد درخواست کننده کاربر
        /// </summary>
        /// <returns></returns>
        public static string GetUserLocation(int UserId = 0)
        {
            var locationName = "";
            int locationId = 0;
            Icls_LocationsClient obj = new Icls_LocationsClient();
            //دیتا کانتکس برای دسترسی به جدول یوزر که در دیتا سرویس نیست
            var dc = new TreasuryEntities();
            if (UserId != 0)
            {
                locationId = dc.UserInfoes.SingleOrDefault(d => d.UserID == UserId && d.IsActive).LocationID;
                locationName = obj.SelectRow(locationId).SingleOrDefault().LocationName;
            }
            else
            {

            }

            return locationName;
        }

        /// <summary>
        /// نام درخواست کننده 
        /// </summary>
        /// <returns></returns>
        public static string GetUserName(int UserId = 0)
        {
            var dc = new TreasuryEntities();
            var User = dc.UserInfoes.SingleOrDefault(d => d.UserID == UserId);
            if (User != null)
                return User.User_FullName;
            else
                return "";
        }

        public static List<Service.LocationsService.Location> GetLocationLs()
        {
            int pageNum = 1;
            int pageSize = 1000;
            string orderByFieldName = "LocationID";
            bool isAscendingOrder = true;
            string OrderByfeildtype = "int";
            int totalRows;
            Icls_LocationsClient obj = new Icls_LocationsClient();
            List<Service.LocationsService.Location> ls = new List<Service.LocationsService.Location>();

            var extention = new Service.LocationsService.Location() { LocationID = 0, LocationName = "--------" };
            var query = obj.SelectAll(pageNum, pageSize, orderByFieldName, OrderByfeildtype, isAscendingOrder, out totalRows);
            ls.Add(extention);
            ls.AddRange(query);
            return ls;
        }
        public static string GetUserByPermissiveSignatureID(int PermissiveSignatureID)
        {
            int TotalRow;
            Icls_PermissiveSignaturesClient psClient = new Icls_PermissiveSignaturesClient();
            var dc = new TreasuryEntities();
            var PermissiveSignature = psClient.SelectRow(PermissiveSignatureID);
            if (PermissiveSignature != null)
            {
                var UserId = PermissiveSignature.SingleOrDefault().UserID;
                var User = dc.UserInfoes.SingleOrDefault(d => d.UserID == UserId);
                return User.User_FullName;
            }
            return "";
        }

        #endregion

        #region ReceiptPay
        public static List<Service.Receipt_Pay_TypeService.Receipt_Pay_Type> GetReceiptPayTypeLS()
        {
            int pageNum = 1;
            int pageSize = 1000;
            string orderByFieldName = "Receipt_Pay_TypeID";
            bool isAscendingOrder = true;
            string OrderByfeildtype = "int";
            int totalRows;
            Icls_Receipt_Pay_TypeClient obj = new Icls_Receipt_Pay_TypeClient();
            List<Service.Receipt_Pay_TypeService.Receipt_Pay_Type> ls = new List<Service.Receipt_Pay_TypeService.Receipt_Pay_Type>();
            var query = obj.SelectAll(pageNum, pageSize, orderByFieldName, OrderByfeildtype, isAscendingOrder, out totalRows).ToList();
            ls.AddRange(query);
            return ls;
        }

        /// <summary>
        /// لیستی از انواع پرداخت را برمی گرداند
        /// </summary>
        /// <returns></returns>
        public static List<Service.Receipt_Pay_TypeDetailService.Receipt_Pay_TypeDetail> GetReceipt_Pay_TypeDetailLs()
        {
            int pageNum = 1;
            int pageSize = 1000;
            string orderByFieldName = "Receipt_Pay_TypeDetailID";
            bool isAscendingOrder = true;
            string OrderByfeildtype = "int";
            int totalRows;
            Icls_Receipt_Pay_TypeDetailClient obj = new Icls_Receipt_Pay_TypeDetailClient();
            List<Service.Receipt_Pay_TypeDetailService.Receipt_Pay_TypeDetail> ls = new List<Service.Receipt_Pay_TypeDetailService.Receipt_Pay_TypeDetail>();
            //ls.Add(new Receipt_Pay_TypeDetailService.Receipt_Pay_TypeDetail() {  Receipt_Pay_TypeDetailDescription = "لطفا انتخاب نمایید" });
            var query = obj.SelectAll(pageNum, pageSize, orderByFieldName, OrderByfeildtype, isAscendingOrder, out totalRows).ToList();
            ls.AddRange(query);
            return ls;
        }

        public static string GetReceipt_Pay_TypeDetailTitle(int ID)
        {
            Icls_Receipt_Pay_TypeDetailClient obj = new Icls_Receipt_Pay_TypeDetailClient();
            var qry = obj.SelectRow(ID);
            if (qry.Count() != 0 || qry == null)
            {
                return qry.SingleOrDefault().Receipt_Pay_TypeDetailDescription;
            }
            return "";
        }
        #endregion

        #region Branches
        /// <summary>
        /// لیستی از شعبه ها  را از دیتا بیس بر می گرداند
        /// </summary>
        /// <returns></returns>
        public static List<Service.cmpBranchesService.cmpBranch> GetBranches()
        {
            int totalRows;
            Icls_cmpBranchesClient obj = new Icls_cmpBranchesClient();
            //List<Service.BankCardsService.cmpBranch> ls = new List<Service.BankCardsService.cmpBranch>();
            var query = obj.SelectAll(1, 1000, "cmpBranchID", "int", false, out totalRows).ToList();
            return query;
        }

        #endregion

        #region region
        /// <summary>
        /// لیستی از شهرها  را بر اساس کد استان می دهد  از دیتا بیس بر می گرداند
        /// </summary>
        /// <returns></returns>
        public static List<Service.CityService.City> GetCityLs(int provinceID = 0)
        {
            string whereclouse = "provinceID=" + provinceID;
            int totalRows;
            var query = new List<Service.CityService.City>();
            Icls_CityClient obj = new Icls_CityClient();
            List<Service.CityService.City> ls = new List<Service.CityService.City>();
            if (provinceID == 0)
            {
                query = obj.SelectBy_DynamicWhereClause("1=1", 1, 1000, "CityID", "int", true, out totalRows).ToList();
            }
            else
            {
                query = obj.SelectBy_DynamicWhereClause(whereclouse, 1, 1000, "CityID", "int", true, out totalRows).ToList();
            }

            return query;
        }
        /// <summary>
        /// لیستی از استان ها  را از دیتا بیس بر می گرداند
        /// </summary>
        /// <returns></returns>
        public static List<Service.ProvinceService.Province> GetProvinceLs()
        {
            int pageNum = 1;
            int pageSize = 1000;
            string orderByFieldName = "provinceID";
            bool isAscendingOrder = true;
            string OrderByfeildtype = "int";
            int totalRows;
            Icls_ProvinceClient obj = new Icls_ProvinceClient();
            List<Service.ProvinceService.Province> ls = new List<Service.ProvinceService.Province>();
            var query = obj.SelectAll(pageNum, pageSize, orderByFieldName, OrderByfeildtype, isAscendingOrder, out totalRows).ToList();
            return query;
        }
        /// <summary>
        /// نام شهر را بر اساس CityID
        /// </summary>
        /// <param name="CityId"></param>
        /// <returns></returns>
        //public static string GetCityName(int CityId)
        //{
        //    Icls_CityClient obj = new Icls_CityClient();
        //    var City = obj.SelectRow(CityId);
        //    return City.SingleOrDefault().CityDescription;
        //}
        public static Service.CityService.City GetCity(int CityId)
        {
            Icls_CityClient obj = new Icls_CityClient();
            var City = obj.SelectRow(CityId);
            return City.SingleOrDefault();
        }
        /// <summary>
        /// نام استان بر اساس ProvinceId
        /// </summary>
        /// <param name="ProvinceId"></param>
        /// <returns></returns>
        public static string GetProvinceName(int ProvinceId)
        {
            Icls_ProvinceClient obj = new Icls_ProvinceClient();
            var Behalf = obj.SelectRow(ProvinceId);
            return Behalf.SingleOrDefault().ProvinceDescription;
        }
        #endregion

        #region Cheque

        public static string GetChequeDesc(int ChequeFormatType_DescriptionID)
        {
            Icls_ChequeFormatType_DescriptionClient obj = new Icls_ChequeFormatType_DescriptionClient();
            var ChequeFormatType_Description = obj.SelectRow(ChequeFormatType_DescriptionID);
            return ChequeFormatType_Description.SingleOrDefault().ChequeFormatType_Description1;
        }
        /// <summary>
        /// لیستی از دسته چک ها
        /// </summary>
        /// <returns></returns>
        public static List<Service.ChequesService.Cheque> GetChequeLs()
        {

            int totalRows;
            Icls_ChequesClient obj = new Icls_ChequesClient();
            List<Service.ChequesService.Cheque> ls = new List<Service.ChequesService.Cheque>();
            var query = obj.SelectAll(1, 1000, "ChequeID", "int", true, out totalRows).ToList();
            return query;
        }
        /// <summary>
        /// لیستی از نوع چک ها
        /// </summary>
        /// <returns></returns>
        public static List<Service.ChequeFormatTypesService.ChequeFormatType> GetChequeFormatTypeLs(int BasicBankId = 0, bool SendBankId = false)
        {
            int totalRows;
            Icls_ChequeFormatTypesClient obj = new Icls_ChequeFormatTypesClient();
            List<Service.ChequeFormatTypesService.ChequeFormatType> ls = new List<Service.ChequeFormatTypesService.ChequeFormatType>();
            var db = new TreasuryEntities();
            if (BasicBankId != 0)
            {
                //Icls_BankClient B = new Icls_BankClient();
                
                string whereclouse = "";
                //dar sorati ke bankid ersal shade bashad 
                if (SendBankId)
                    whereclouse = "BankID=" + db.Banks.SingleOrDefault(d => d.BankID == BasicBankId).BasicBankID;
                //whereclouse = "BankID=" + B.SelectRow(BasicBankId).SingleOrDefault().BasicBankID;
                else
                    whereclouse = "BankID=" + BasicBankId;

                var query = obj.SelectBy_DynamicWhereClause(whereclouse, 1, 1000, "ChequeFormatID", "int", true, out totalRows);
                ls.AddRange(query);
                if (SendBankId)
                {
                    Icls_ChequeFormatTypeDetailsClient CFTD = new Icls_ChequeFormatTypeDetailsClient();
                    var ChequeFormatTypeDetailsls = CFTD.SelectAll(1, 100000, "FormatID", "int", true, out totalRows);
                    Icls_ChequesClient ch = new Icls_ChequesClient();
                    var cheque = ch.SelectAll(1, 1000, "ChequeID", "int", true, out totalRows).Where(d => d.LastUsedPaperNo < d.LastPageNumber && d.BankID == BasicBankId && d.IsActive).Select(d => d.ChequeFormatID);
                    return ls.Where(q => ChequeFormatTypeDetailsls.Select(d => d.ChequeFormatID).Contains(q.ChequeFormatID) && cheque.Contains(q.ChequeFormatID)).ToList();

                }
                return ls;
            }
            else
            {
                var query = obj.SelectAll(1, 1000, "ChequeFormatID", "int", true, out totalRows);
                ls.AddRange(query);
                return ls;
            }
        }
        /// <summary>
        /// برگردان لیستی از نوع چک براساس شماره درخواست وجه  
        /// </summary>
        /// <param name="requestId"></param>
        /// <returns></returns>
        public static List<Service.ChequeFormatTypesService.ChequeFormatType> GetchequeFormatTypeLsByRequestID(int requestId)
        {
            Icls_Request_PaymentClient RP = new Icls_Request_PaymentClient();
            var db = new TreasuryEntities();
            //var RequestPayment = RP.SelectRow((int)requestId).SingleOrDefault();
            var RequestPayment = db.Request_Payment.Find(requestId);
            //بدست آوردن نوع چک و قطع های چک براساس بانکی که ذخیره شده در درخواست وجه
            var BankId = int.Parse(RequestPayment.Source_PaymentValue);
            //Icls_BankClient B = new Icls_BankClient();
            
            //var BasicBankId = B.SelectRow(BankId).SingleOrDefault().BasicBankID;
            var BasicBankId = db.Banks.SingleOrDefault(d => d.BankID == BankId).BasicBankID;
            Icls_ChequeFormatTypesClient CFT = new Icls_ChequeFormatTypesClient();

            string where = "BankID =" + BasicBankId;
            int totalRows;
            var ls = CFT.SelectBy_DynamicWhereClause(where, 1, 1000, "ChequeFormatID", "int", true, out totalRows).ToList();

            return ls;
        }

        /// <summary>
        /// چک نمودن وجود نوع چک
        /// </summary>
        /// <returns></returns>
        public static bool CheckIfExistChequeType(int ChequeFormatID)
        {
            int totalRows;
            Icls_ChequeFormatTypeDetailsClient obj = new Icls_ChequeFormatTypeDetailsClient();
            var query = obj.SelectAll(1, 1000, "FormatID", "int", true, out totalRows);

            if (query.Where(d => d.ChequeFormatID == ChequeFormatID).Any())
                return true;
            else
                return false;
        }

        /// <summary>
        /// چک نمودن وجود دسته چک
        /// </summary>
        /// <returns></returns>
        public static bool CheckIfExistCheque(int ChequeFormatID)
        {
            int totalRows;
            Icls_ChequesClient obj = new Icls_ChequesClient();
            var query = obj.SelectAll(1, 1000, "ChequeID", "int", true, out totalRows);
            if (query.Where(d => d.ChequeFormatID == ChequeFormatID).Any())
                return true;
            else
                return false;
        }
        public static string GetChequeFormatType(int ChequeFormatID)
        {
            Icls_ChequeFormatTypesClient obj = new Icls_ChequeFormatTypesClient();
            var CheckFormat = obj.SelectRow(ChequeFormatID);
            if (CheckFormat != null)
                return CheckFormat.SingleOrDefault().FormatDescription;
            else
                return "";
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="FormatDescriptionID_Main"></param>
        /// <returns></returns>
        public static string GetChequeFormatType_Description(int ChequeFormatType_DescriptionID)
        {
            Icls_ChequeFormatType_DescriptionClient obj = new Icls_ChequeFormatType_DescriptionClient();
            var ChequeFormatType_Description = obj.SelectRow(ChequeFormatType_DescriptionID);
            if (ChequeFormatType_Description != null)
                return ChequeFormatType_Description.SingleOrDefault().ChequeFormatType_Description1;
            else
                return "";
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static List<CSD.Treasury.Service.ChequeFormatType_DescriptionService.ChequeFormatType_Description> GetFormatTypeDescLs()
        {
            int totalRows;
            Icls_ChequeFormatType_DescriptionClient obj = new Icls_ChequeFormatType_DescriptionClient();
            List<CSD.Treasury.Service.ChequeFormatType_DescriptionService.ChequeFormatType_Description> ls = new List<CSD.Treasury.Service.ChequeFormatType_DescriptionService.ChequeFormatType_Description>();
            var query = obj.SelectAll(1, 1000, "ChequeFormatType_DescriptionID", "int", false, out totalRows).ToList();

            return query;
        }

        /// <summary>
        /// لیستی از برچسب های فرم چک را بر می گرداند
        /// </summary>
        /// <returns></returns>
        public static List<Service.BasicChequeLablesService.BasicChequeLable> GetBasicChequeLableLS()
        {
            int pageNum = 1;
            int pageSize = 1000;
            string orderByFieldName = "ChequeLableID";
            bool isAscendingOrder = true;
            string OrderByfeildtype = "int";
            int totalRows;

            Icls_BasicChequeLablesClient obj = new Icls_BasicChequeLablesClient();
            var query = obj.SelectAll(pageNum, pageSize, orderByFieldName, OrderByfeildtype, isAscendingOrder, out totalRows);
            return query.ToList();
        }

        /// <summary>
        /// چک نمودن وجود فایل ضمیمه
        /// </summary>
        /// <returns></returns>
        public static decimal CheckIfHasFile(int Id, string TableName)
        {
            var db = new TreasuryFilesEntities();
            var file = db.Files.Where(d => d.relatedID == Id && d.tableName == TableName);
            if (file.Count() != 0)
                return file.SingleOrDefault().fileID;
            else
                return 0;
        }

        /// <summary>
        /// چک نمودن وجود دسته چک
        /// </summary>
        /// <returns></returns>
        public static bool CheckIfHasCheques(int BankID)
        {
            //Icls_BankClient B = new Icls_BankClient();
            //var Bank = B.SelectRow(BankID);

            Icls_ChequesClient obj = new Icls_ChequesClient();
            int totalRow;
            var Cheque = obj.SelectBy_DynamicWhereClause("BankID =" + BankID + " and LastUsedPaperNo< LastPageNumber", 1, 1000, "ChequeID", "int", true, out totalRow);
            if (Cheque.Count() != 0)
            {
                return true;
            }
            return false;
        }
        #endregion

        #region HasPalnState
        public static bool HasPlanState(decimal ReceiptDetailId)
        {
            int TotalRow;
            Icls_Receipt_Detail_PlanStateClient PlanStateClient = new Icls_Receipt_Detail_PlanStateClient();
            //درصورتی که در مرحله اول از برنامه ریزی باشد اجازه حذف دریافت رادارد
            var query = PlanStateClient.SelectBy_DynamicWhereClause("ReceiptDetailId =" + ReceiptDetailId + "and PlaningTypeID != 1 and PlaningTypeID !=17  and PlaningTypeID !=18  and PlaningTypeID !=19 ", 1, 1000, "StateID", "dec", false, out TotalRow).Any();
            if (query)
                return true;
            else
                return false;

        }

        public static bool CanEditReceipt(decimal ReceiptDetailId, decimal ReceiptId = 0)
        {
            
            var db = new TreasuryEntities();
            //درصورتی که دریافت نزد صندوق باشد
            var query = db.Receipt_Detail_PlanState.Where(d => d.ReceiptDetailId == ReceiptDetailId);
            var ISok = true;
            if (ReceiptId != 0)
            {
                //درصورتی که برای یک هدر چندین دریافت ثبت شده باشد
                var receiptdetails = db.Receipt_Detail.Where(d => d.ReceiptId == ReceiptId);
                if (receiptdetails.Count() > 1)
                {
                    var palncount = receiptdetails.Join(
                        db.Receipt_Detail_PlanState,
                        r => r.ReceiptDetailId,
                        d => d.ReceiptDetailId,
                        (r, d) => new { ReceiptDetailId = r.ReceiptDetailId }).Count();
                    if (palncount > receiptdetails.Count())
                    {
                        ISok = false;
                    }
                }
            }
            if (query.Any() && query.Count() == 1 && ISok)
                return true;
            else
                return false;

        }

        #endregion

        #region PlanType
        public static string GetPlanType(int PlaningTypeID)
        {
            Icls_PlaningTypeClient planingTypeClient = new Icls_PlaningTypeClient();
            var PlaningType = planingTypeClient.SelectRow(PlaningTypeID).SingleOrDefault();
            if (PlaningType != null)
                return PlaningType.PlaningTypeDesc;
            else
                return "";
        }
        #endregion
        /// <summary>
        /// لیست عطف های بابت
        /// </summary>
        /// <returns></returns>
        #region Reference
        public static List<Service.ReferenceTypeService.ReferenceType> GetReferenceTypeLs()
        {
            int totalRows;
            Icls_ReferenceTypeClient obj = new Icls_ReferenceTypeClient();
            List<Service.ReferenceTypeService.ReferenceType> ls = new List<Service.ReferenceTypeService.ReferenceType>();
            //ls.Add(new ReferenceTypeService.ReferenceType() { referenceTypeDescription = "لطفا انتخاب نمایید" });
            var query = obj.SelectAll(1, 1000, "referenceTypeID", "int", true, out totalRows);
            ls.AddRange(query);
            return ls;
        }

        public static string GetReferenceTitle(int ReferenceID)
        {
            Icls_ReferenceTypeClient obj = new Icls_ReferenceTypeClient();
            return obj.SelectRow(ReferenceID).SingleOrDefault().referenceTypeDescription;
        }

        public static string GetReferenceTitleByRefrenceId(int ReferenceID)
        {
            int totalRow;
            Icls_ReferenceTypes_By_BehalfGroupClient RefrenceByBehalfGroupClint = new Icls_ReferenceTypes_By_BehalfGroupClient();
            Icls_ReferenceTypeClient ReferenceTypeClient = new Icls_ReferenceTypeClient();
            var obj = RefrenceByBehalfGroupClint.SelectBy_DynamicWhereClause("ReferenceID=" + ReferenceID, 1, 10, "ReferenceID", "int", false, out totalRow).SingleOrDefault();
            if (obj != null)
            {
                return ReferenceTypeClient.SelectRow(obj.referenceTypeID).SingleOrDefault().referenceTypeDescription;
            }

            return "";
        }

        public static IEnumerable<int> GetReferenceTypeByBehalfGroup(int BehalfGroupID)
        {
            string whereclouse = "BehalfGroupID=" + BehalfGroupID;
            int totalRows;
            Icls_ReferenceTypes_By_BehalfGroupClient obj = new Icls_ReferenceTypes_By_BehalfGroupClient();

            var query = obj.SelectBy_DynamicWhereClause(whereclouse, 1, 1000, "ReferenceID", "int", true, out totalRows).ToList();
            return query.Select(d => d.referenceTypeID);
        }
        public static List<Service.ReferenceTypes_By_BehalfGroupService.ReferenceTypes_By_BehalfGroup> GetReferenceTypeByBehalfGroupLs(int BehalfGroupID)
        {
            string whereclouse = "BehalfGroupID=" + BehalfGroupID;
            int totalRows;
            Icls_ReferenceTypes_By_BehalfGroupClient obj = new Icls_ReferenceTypes_By_BehalfGroupClient();

            var query = obj.SelectBy_DynamicWhereClause(whereclouse, 1, 1000, "ReferenceID", "int", true, out totalRows).ToList();
            return query;
        }
        #endregion


        /// <summary>
        /// لیستی از نوع ارزها را از دیتا بیس   بر می گرداند
        /// </summary>
        /// <returns></returns>
        public static List<Service.ExchangeTypesService.ExchangeType> GetExchangeLs()
        {
            int pageNum = 1;
            int pageSize = 1000;
            string orderByFieldName = "ExchangeTypeID";
            bool isAscendingOrder = true;
            string OrderByfeildtype = "int";
            int totalRows;

            Icls_ExchangeTypesClient obj = new Icls_ExchangeTypesClient();
            var query = obj.SelectAll(pageNum, pageSize, orderByFieldName, OrderByfeildtype, isAscendingOrder, out totalRows);
            return query.ToList();
        }



        /// <summary>
        /// لیستی از مبدا و مقصد را بر می گرداند
        /// </summary>
        /// <returns></returns>
        public static List<Service.Source_DestinationTypesService.Source_DestinationTypes> GetSourceDestinationTypesLs()
        {
            int pageNum = 1;
            int pageSize = 1000;
            string orderByFieldName = "Source_DestinationTypeID";
            bool isAscendingOrder = true;
            string OrderByfeildtype = "int";
            int totalRows;

            Icls_Source_DestinationTypesClient obj = new Icls_Source_DestinationTypesClient();
            List<Service.Source_DestinationTypesService.Source_DestinationTypes> ls = new List<Service.Source_DestinationTypesService.Source_DestinationTypes>();
            var query = obj.SelectAll(pageNum, pageSize, orderByFieldName, OrderByfeildtype, isAscendingOrder, out totalRows).ToList();
            ls.AddRange(query);
            return ls;
        }


        /// <summary>
        /// پیدا نمودن آی پی کاربر
        /// </summary>
        /// <returns></returns>
        public static string GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }

            }

            return "";
        }



        /// <summary>
        /// لیستی از نقش ها را بر می گرداند
        /// </summary>
        /// <returns></returns>
        public static List<CSD.Treasury.DataModel.RolesInfo> GetRolesLS()
        {
            var db = new TreasuryEntities();
            var Ls = db.RolesInfoes.Where(d => d.RecordDeleted == false).ToList();
            return Ls;
        }

        public static List<string> GetUserRolesLS(CSD.Treasury.DataModel.UserInfo user)
        {
            var db = new TreasuryEntities();
            IEnumerable<int> userRoleId = user.UserInRoles.Select(q => q.RoleId);
            var Ls = db.RolesInfoes.Where(d => userRoleId.Contains(d.RoleID) && d.RecordDeleted == false).Select(q => q.RoleName1).ToList();
            return Ls;
        }

        /// <summary>
        /// return label with lableid
        /// </summary>
        /// <returns></returns>
        public static CSD.Treasury.Service.BasicChequeLablesService.BasicChequeLable GetLable(int ChequeLableID)
        {
            Icls_BasicChequeLablesClient obj = new Icls_BasicChequeLablesClient();
            var BasicChequeLable = obj.SelectRow(ChequeLableID);
            if (BasicChequeLable != null)
                return BasicChequeLable.SingleOrDefault();
            else
                return null;

        }

        /// <summary>
        /// آخرین وضعیت درخواست وجه  باتوجه به RequestId 
        /// </summary>
        /// <returns></returns>
        public static DataModel.GetRequestPayment_Payment_WithLastStates_Result GetRequest_Payment_WithLastStates(int requestId)
        {
            //Icls_SPsClient db = new Icls_SPsClient();
            var db = new TreasuryEntities();
            var result = db.GetRequestPayment_Payment_WithLastStates("rp.RequestID=" + requestId, "Request_Payment_Status_ActionTime").SingleOrDefault();
            return db.GetRequestPayment_Payment_WithLastStates("rp.RequestID=" + requestId, "Request_Payment_Status_ActionTime").SingleOrDefault();
            //return db.GetRequestPayment_Payment_WithLastStates("rp.RequestID=" + requestId, " ").orde(d => d.Request_Payment_Status_ActionTime).LastOrDefault();

        }
        public static string GetRequest_Payment_BardashtTime(int requestId)
        {
            int TotalRow;
            Icls_PaymentClient db = new Icls_PaymentClient();
            var qry = db.SelectBy_DynamicWhereClause("RequestID =" + requestId, 1, 1, "PaymentID", "dec", false, out TotalRow);
            if (qry.Count() != 0)
            {
                return GetPersianDate(qry.SingleOrDefault().PaymentDate);
            }
            return "";
        }
        public static string GetRequest_Payment_DeliveryTime(int requestId)
        {
            int TotalRow;
            Icls_Request_Payment_StatusDetailsClient db = new Icls_Request_Payment_StatusDetailsClient();
            var Request_Payment_Status = db.SelectBy_DynamicWhereClause("RequestID =" + requestId + " and  RequestStatusTypeID=" + 4, 1, 10, "Request_Payment_StatusDetailsID", "int", false, out TotalRow);
            if (Request_Payment_Status.Count() != 0)
            {
                return GetPersianDate(Request_Payment_Status.SingleOrDefault().ActionDate);
            }
            return "";
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="BehalfGroupID"></param>
        /// <returns></returns>
        public static string GetReferenceTypeValue(decimal RequestId, bool Show = false)
        {
            string whereclouse = "RequestId=" + RequestId;
            int totalRows;
            Payment_ReferenceTypes_ValueClient PRV = new Payment_ReferenceTypes_ValueClient();
            Icls_Request_PaymentClient RPclient = new Icls_Request_PaymentClient();
            Icls_ReferenceTypes_By_BehalfGroupClient RB = new Icls_ReferenceTypes_By_BehalfGroupClient();
            Icls_ReferenceTypeClient RT = new Icls_ReferenceTypeClient();
            Icls_BehalfsClient BClient = new Icls_BehalfsClient();
            var res = new StringBuilder();
            var db = new TreasuryEntities();
            var query1 = PRV.SelectBy_DynamicWhereClause(whereclouse, 1, 10000, "ReferenceTypeValueID", "int", true, out totalRows).ToList();

            if (query1.Count() != 0)
            {
                var ReferenceID = query1.FirstOrDefault().ReferenceID;
                var BehalfGroupID = RB.SelectRow(ReferenceID).SingleOrDefault().BehalfGroupID;
                var query = RB.SelectBy_DynamicWhereClause("BehalfGroupID=" + BehalfGroupID, 1, 10000, "ReferenceID", "int", true, out totalRows).ToList();
                if (query != null)
                {
                    //درصورتی که بخواهد عطف ها را در صفحه نمایش دهد
                    if (Show)
                    {
                        foreach (var i in query1)
                        {
                            var referenceTypeID = RB.SelectRow(i.ReferenceID).SingleOrDefault().referenceTypeID;
                            res.Append("<label>" + GetReferenceTitle(referenceTypeID) + "</label><span> " + i.ReferenceValue + "</span> </br> ");
                        }
                        return res.ToString();
                    }
                    if (query.Count() != query1.Count())
                    {
                        foreach (var i in query)
                        {
                            foreach (var j in query1)
                            {

                                if (i.ReferenceID == j.ReferenceID)
                                {
                                    res.Append("<div class='col-sm-4 child' ><input class='form-control floating-label' placeholder='" + Utility.GetReferenceTitle(i.referenceTypeID) + "' value='" + j.ReferenceValue + "' type='text'  id = 'Reference_" + i.ReferenceID + "' name = 'Reference_" + i.ReferenceID + "'/></div>");
                                }
                                else
                                {
                                    res.Append("<div class='col-sm-4 child' ><input class='form-control floating-label' placeholder='" + Utility.GetReferenceTitle(i.referenceTypeID) + "' type='text' id='Reference_" + i.ReferenceID + "' name='Reference_" + i.ReferenceID + "' /></div>");
                                }

                            }
                        }
                        return res.ToString();
                    }
                    else
                    {
                        foreach (var i in query1)
                        {
                            var referenceTypeID = RB.SelectRow(i.ReferenceID).SingleOrDefault().referenceTypeID;
                            res.Append("<label class='control-label col-sm-2' for='BehalfGropId'>" + Utility.GetReferenceTitle(referenceTypeID) + "</label><div class='col-sm-10' ><input class='form-control floating-label' required='required'  placeholder='" + Utility.GetReferenceTitle(referenceTypeID) + "' value='" + i.ReferenceValue + "' type='text'  id = 'Reference_" + i.ReferenceID + "' name = 'Reference_" + i.ReferenceID + "'/><span class='field - validation - error' data-valmsg-for='Reference_" + i.ReferenceID + "' data-valmsg-replace='true'><span for='Reference_" + i.ReferenceID + "'>این فیلد اجباری است.</span></div>");
                        }
                        return res.ToString();
                    }
                }
            }
            else
            {
                //var behalfID = RPclient.SelectRow(int.Parse(RequestId.ToString())).SingleOrDefault().BehalfID;
                var behalfID = db.Request_Payment.SingleOrDefault(d=>d.RequestID == RequestId).BehalfID;
                var BehalfGroupID = BClient.SelectRow(behalfID).SingleOrDefault().BehalfGroupID;
                var query = RB.SelectBy_DynamicWhereClause("BehalfGroupID=" + BehalfGroupID, 1, 10000, "ReferenceID", "int", true, out totalRows).ToList();

                if (query != null && query1.Count() != 0)
                {
                    foreach (var i in query)
                    {
                        res.Append("<div class='col-sm-4' id='child'><input class='form-control floating-label' placeholder='" + Utility.GetReferenceTitle(i.referenceTypeID) + "' type='text' id='Reference_" + i.ReferenceID + "' name='Reference_" + i.ReferenceID + "' /></div>");
                    }
                    return res.ToString();
                }
            }


            return "";

            //if (query != null)
            //{
            //    foreach (var i in query)
            //    {
            //        var referenceTypeID = RB.SelectRow(i.ReferenceID).SingleOrDefault().referenceTypeID;
            //        res.Append("<div class='col-sm-4 child' ><div class='form-group'><label>" + GetReferenceTitle(referenceTypeID) + "</label><input class='form-control' placeholder='" + i.ReferenceValue + "' type='text'  id = 'Reference_" + i.ReferenceID + "' name = 'Reference_" + i.ReferenceID + "'/></div></div>");
            //    }
            //    return res.ToString();
            //}
            //return "";
        }
        public static string GetRecieveReferenceTypeValue(decimal ReceiptDetailId, bool Show = false, bool fromReport = false)
        {
            Icls_ReferenceTypes_By_BehalfGroupClient RB = new Icls_ReferenceTypes_By_BehalfGroupClient();
            Icls_ReferenceTypeClient RT = new Icls_ReferenceTypeClient();
            Icls_Reciept_ReferenceTypes_ValueClient RefrenceTypeVlueClient = new Icls_Reciept_ReferenceTypes_ValueClient();
            int totalRows;
            var query1 = RefrenceTypeVlueClient.SelectBy_DynamicWhereClause("ReceiptDetailId=" + ReceiptDetailId, 1, 1000, "ReferenceTypeValueID", "int", true, out totalRows).ToList();
            var res = new StringBuilder();
            if (query1.Count() != 0)
            {
                if (Show)
                {
                    foreach (var i in query1)
                    {
                        var referenceTypeID = RB.SelectRow(i.ReferenceID).SingleOrDefault().referenceTypeID;
                        if (fromReport)
                        {
                            res.Append("<span> " + i.ReferenceValue + "</span> </br> ");
                        }
                        else
                        {
                            res.Append("<span>" + GetReferenceTitle(referenceTypeID) + "</span><span> " + i.ReferenceValue + "</span> </br> ");
                        }

                    }
                    return res.ToString();
                }
            }
            return "";
        }


        #region Receipt
        public static string GetReceipt_Pay_Type(int ReceipTTypeId)
        {
            Icls_Receipt_Pay_TypeClient RePaytypeClient = new Icls_Receipt_Pay_TypeClient();
            var obj = RePaytypeClient.SelectRow(ReceipTTypeId).SingleOrDefault();
            return obj.Receipt_Pay_TypeDescription;
        }
        #endregion

        #region CashFlowGroup
        public static List<Service.cashFlowGroupService.cashFlowGroup> CashFollowGroupLs()
        {
            int totalRow;
            Icls_cashFlowGroupClient cashflowGroupClient = new Icls_cashFlowGroupClient();
            var Ls = cashflowGroupClient.SelectAll(1, 1000, "cashFlowGroupID", "int", false, out totalRow).ToList();
            return Ls;
        }
        #endregion

        #region Reciept_OtherPropery
        public static List<Service.Reciept_OtherProperyService.Reciept_OtherPropery> GetOtherPropertyLS(int Receipt_Pay_TypeDetailID)
        {
            int totalRows;
            Icls_Reciept_OtherProperyClient PropertyClient = new Icls_Reciept_OtherProperyClient();
            return PropertyClient.SelectAll(1, 10, "Reciept_OtherProperyID", "int", false, Receipt_Pay_TypeDetailID, out totalRows).ToList();
        }


        #endregion

        #region Reciept_otherPropertyDetails
        public static List<Service.Reciept_OtherPropery_BY_Receipt_Pay_TypeDetailService.Reciept_OtherPropery_BY_Receipt_Pay_TypeDetail> GetOTherPeropertyDetails(int Receipt_Pay_TypeDetailID)
        {
            int totalRows;
            Icls_Reciept_OtherPropery_BY_Receipt_Pay_TypeDetailClient PropertyClient = new Icls_Reciept_OtherPropery_BY_Receipt_Pay_TypeDetailClient();
            return PropertyClient.SelectBy_DynamicWhereClause("Receipt_Pay_TypeDetailID=" + Receipt_Pay_TypeDetailID, 1, 1000, "Reciept_OtherPropery_BY_Receipt_Pay_TypeDetailID", "int", false, out totalRows).ToList();


        }


        #endregion

        #region exchange
        public static string GetExchaneName(int ExchangeId)
        {
            Icls_ExchangeTypesClient ExchangeClient = new Icls_ExchangeTypesClient();
            if (ExchangeId != 1 && ExchangeId!= null)
            {
                return ExchangeClient.SelectRow(ExchangeId).SingleOrDefault().ExchangeTypeDescription;
            }
            return "";
        }
        #endregion

        #region HasRelation
        public static bool HasRelation(string FirstTable, string FirstFeildName, string FirstFeildValue, string SecondTable, string secondFeildName)
        {
            Icls_SPsClient db = new Icls_SPsClient();
            var query = db.HasRelationBetweenToTable(FirstTable, FirstFeildName, FirstFeildValue, SecondTable, secondFeildName);
            return query;
        }
        public static bool CardIsUsedInRequest(int CardId)
        {
            int TotalRow;
            Icls_Request_PaymentClient RequestClient = new Icls_Request_PaymentClient();
            var qry = RequestClient.SelectBy_DynamicWhereClause("Source_TypeID=3 and Source_PaymentValue =\"" + CardId + "\"", 1, 1000, "RequestID", "dec", false, out TotalRow);
            if (qry.Count() != 0)
            {
                return true;
            }
            return false;
        }
        /// <summary>
        /// برگرداندن مقدار true در صورتی که از عطفی در درخواست وجه استفاده شده باشد
        /// </summary>
        /// <param name="BehalfGroupID"></param>
        /// <returns></returns>
        public static bool IfRefrenceUsed(int BehalfGroupID)
        {
            int TotalRow;
            Icls_SPsClient db = new Icls_SPsClient();
            Icls_ReferenceTypes_By_BehalfGroupClient referenceBehalfGroup = new Icls_ReferenceTypes_By_BehalfGroupClient();
            var ls = referenceBehalfGroup.SelectBy_DynamicWhereClause("BehalfGroupID=" + BehalfGroupID, 1, 1000, "ReferenceID", "int", false, out TotalRow);

            foreach (var item in ls)
            {
                var query = db.HasRelationBetweenToTable("ReferenceTypes_By_BehalfGroup", "ReferenceID", item.ReferenceID.ToString(), "Payment_ReferenceTypes_Value", "ReferenceID");
                if (query)
                    return query;
            }
            return false;
        }
        /// <summary>
        /// درصورتیکه از بابت مربوط به گروه بابت استفاده شده باشد
        /// </summary>
        /// <returns></returns>
        public static bool IfBehlfUsed(int BehalfGroupID)
        {
            int TotalRow;

            Icls_BehalfsClient BehalfClient = new Icls_BehalfsClient();
            Icls_BehalfGroupClient BehlafGroupClient = new Icls_BehalfGroupClient();
            //Icls_Request_PaymentClient RequestClient = new Icls_Request_PaymentClient();
            //Icls_Receipt_DetailClient ReceiptClient = new Icls_Receipt_DetailClient();
            var db = new TreasuryEntities();
            var behalfLs = BehalfClient.SelectBy_DynamicWhereClause(" BehalfGroupID =" + BehalfGroupID, 1, 1000, "BehalfID", "int", false, out TotalRow).Select(d => d.BehalfID);
            var BehalfGroup = BehlafGroupClient.SelectRow(BehalfGroupID);
            if(BehalfGroup != null)
            {
                //پرداخت
                if (BehalfGroup.SingleOrDefault().Receipt_Pay_TypeID == 1)
                {
                    //var RequestLs = RequestClient.SelectBy_DynamicWhereClause("1=1", 1, 10000 * 10000, "RequestID", "dec", false, out TotalRow);
                    var RequestLs = db.Request_Payment;
                    if (behalfLs.Count() != 0)
                    {
                        var BehlafIdUsed = RequestLs.Select(d => behalfLs.Contains(d.BehalfID));
                        if (BehlafIdUsed.Count() != 0)
                        {
                            return true;
                        }
                    }
                }
                //دریافت
                if (BehalfGroup.SingleOrDefault().Receipt_Pay_TypeID == 2)
                {
                    //var ReceiptLs = ReceiptClient.SelectBy_DynamicWhereClause("1=1", 1, 10000 * 10000, "ReceiptDetailId", "dec", false, out TotalRow);
                    var ReceiptLs = db.Receipt_Detail;
                    if (behalfLs.Count() != 0)
                    {
                        var BehalfIdUsed = ReceiptLs.Select(d => behalfLs.Contains(d.BehalfID)).ToList();
                        if (BehalfIdUsed.Count() != 0)
                        {
                            return true;
                        }
                    }

                    return false;
                }
            }            
            return false;
        }

        #endregion



        #region Beneficiaries
        public static string GetmeliCode(string Unique_Account_No)
        {
            try
            {
                int totalrow;
                var codeMeli = "";
                Icls_BeneficiaryClient Beneficiary = new Icls_BeneficiaryClient();
                var bnef = Beneficiary.SelectBy_DynamicWhereClause("Unique_Account_No =\"" + Unique_Account_No + "\"", 1, 10, "BeneficiarID", "int", false, out totalrow);
                if (bnef.Count() != 0)
                {
                    codeMeli = bnef.FirstOrDefault().ShenaseMelli;
                }
                return codeMeli;
            }
            catch (Exception e)
            {
                throw;
            }

        }
        public static string GetAccountNum(int BeneficiarDetailID, bool ShowBeneficiarName = false)
        {
            Icls_BeneficiariesDetailInfoClient bdetailsClinet = new Icls_BeneficiariesDetailInfoClient();
            var obj = bdetailsClinet.SelectRow(BeneficiarDetailID).SingleOrDefault();
            var Bank_AccountNumber = "";
            if (obj != null)
            {
                Bank_AccountNumber = obj.Bank_AccountNumber;
                //if (obj.Bank_BranchName!=null)
                //{
                //    Bank_AccountNumber = Bank_AccountNumber + "/" + obj.Bank_BranchName;
                //}
                if (obj != null)
                {

                    if (ShowBeneficiarName)
                    {
                        Icls_BeneficiaryClient beneficiary = new Icls_BeneficiaryClient();
                        var name = beneficiary.SelectRow(obj.BeneficiarID).FirstOrDefault().LegalAccountName;

                        Bank_AccountNumber = Bank_AccountNumber + " - " + name;

                    }
                    else
                    {
                        Bank_AccountNumber = Bank_AccountNumber + " - " + GetBasicBankInfo(obj.BankID);

                    }
                }
            }
            return Bank_AccountNumber;
        }

        public static string GetAccountNumIfExist(string Unique_Account_No, bool fromAcc = false)
        {
            try
            {
                int totalrow;
                Icls_BeneficiaryClient Beneficiary = new Icls_BeneficiaryClient();
                var bnef = Beneficiary.SelectBy_DynamicWhereClause("Unique_Account_No =\"" + Unique_Account_No + "\"", 1, 10, "BeneficiarID", "int", false, out totalrow);
                string bnefAcc = "";
                if (bnef != null && bnef.Count() != 0 && !fromAcc)
                {
                    bnefAcc = bnef.FirstOrDefault().LegalAccountName;
                }
                else
                {
                    bnefAcc = GetUniqueAccount(Unique_Account_No);
                }
                return bnefAcc;
            }
            catch (Exception e)
            {
                throw;
            }

        }
        public static string GetShebaNum(int BeneficiarDetailID, bool ShowBeneficiarName = false)
        {
            Icls_BeneficiariesDetailInfoClient bdetailsClinet = new Icls_BeneficiariesDetailInfoClient();
            string Bank_ShebaNo = "";
            var qry = bdetailsClinet.SelectRow(BeneficiarDetailID).SingleOrDefault();
            if (qry != null)
            {
                if (ShowBeneficiarName)
                {
                    Icls_BeneficiaryClient beneficiary = new Icls_BeneficiaryClient();
                    var name = beneficiary.SelectRow(qry.BeneficiarID).FirstOrDefault().LegalAccountName;
                    Bank_ShebaNo = @"جهت واریز به حساب شبا 
" + qry.Bank_ShebaNo + " " + "بانک " + GetBasicBankInfo(qry.BankID) + @"
" + name;
                    // Bank_ShebaNo = GetBasicBankInfo(qry.BankID) + " " + qry.Bank_ShebaNo + " - " + name;
                }
                else
                {
                    Bank_ShebaNo = @"جهت واریز به حساب شبا 
" + qry.Bank_ShebaNo + @"
" + GetBasicBankInfo(qry.BankID);

                    // GetBasicBankInfo(qry.BankID) + " " + qry.Bank_ShebaNo;
                }
            }
            return Bank_ShebaNo;
        }

        public static string GetChequeDescription(decimal requestId)
        {
            int totalrow;
            Icls_Request_PaymentClient rp = new Icls_Request_PaymentClient();
            var db = new TreasuryEntities();
            //var request = rp.SelectRow(requestId).FirstOrDefault();
            var request = db.Request_Payment.Find(requestId);
            Icls_BehalfsClient behalfs = new Icls_BehalfsClient();
            var behalfGroupId = behalfs.SelectRow(request.BehalfID).FirstOrDefault().BehalfGroupID;
            Payment_ReferenceTypes_ValueClient prv = new Payment_ReferenceTypes_ValueClient();
            Icls_ReferenceTypes_By_BehalfGroupClient rtbg = new Icls_ReferenceTypes_By_BehalfGroupClient();
            Icls_ReferenceTypeClient rt = new Icls_ReferenceTypeClient();
            var referenceTypeID = rt.SelectBy_DynamicWhereClause("referenceTypeDescription2 =\"" + "Description" + "\"", 1, 10, "referenceTypeID", "int", false, out totalrow).FirstOrDefault().referenceTypeID;
            var referenceId = rtbg.SelectBy_DynamicWhereClause("referenceTypeID = " + referenceTypeID + " and " + "BehalfGroupID = " + behalfGroupId, 1, 10, "ReferenceID", "int", false, out totalrow).FirstOrDefault().ReferenceID;
            var res = prv.SelectBy_DynamicWhereClause("ReferenceID = " + referenceId + " and RequestId = " + requestId, 1, 10, "ReferenceTypeValueID", "int", false, out totalrow).FirstOrDefault().ReferenceValue;
            return res;
        }

        #endregion

        #region Casheir
        /// <summary>
        /// لیست صندوق ها
        /// </summary>
        /// <returns></returns>

        public static List<CustomCashier> CashiersLs(int BehalfsGroupId = 0)
        {
            Icls_CashiersClient CashierClient = new Icls_CashiersClient();
            int totalRows;
            var query = CashierClient.SelectAll(1, 1000, "CashierID", "int", true, out totalRows).ToList();
            List<CustomCashier> CustomCashierLS = new List<Models.CustomCashier>();
            foreach (var item in query)
            {
                CustomCashier CustomCashierObj = new Models.CustomCashier();
                CustomCashierObj.CashierID = item.CashierID;
                CustomCashierObj.CashierName = Utility.GetUserName(item.UserID);
                CustomCashierObj.UserID = item.UserID;
                CustomCashierObj.Unique_Account_No = item.Unique_Account_No;
                CustomCashierObj.Unique_Account_No_two = item.Unique_Account_No_two;
                CustomCashierLS.Add(CustomCashierObj);
            }
            return CustomCashierLS;
        }
        public static string GetCashier(int CAshierId)
        {
            Icls_CashiersClient CashierClient = new Icls_CashiersClient();
            var Cashier = CashierClient.SelectRow(CAshierId);
            string Result = "";
            if (Cashier != null)
            {
                Result = GetUserName(Cashier.SingleOrDefault().UserID) + Cashier.SingleOrDefault().Unique_Account_No;
            }

            return Result;
        }

        #endregion

        #region CashDesk
        public static string GetCashdesk(int cashDeskId)
        {
            Icls_CashDeskClient obj = new Icls_CashDeskClient();
            var Cashdesk = obj.SelectRow(cashDeskId).SingleOrDefault();
            return Cashdesk.CashDeskDescription;
        }
        public static List<Service.CashDeskService.CashDesk> GetCashdeskLs()
        {
            int pageNum = 1;
            int pageSize = 1000;
            string orderByFieldName = "CashDeskID";
            bool isAscendingOrder = true;
            string OrderByfeildtype = "int";
            int totalRows;

            Icls_CashDeskClient obj = new Icls_CashDeskClient();
            var query = obj.SelectAll(pageNum, pageSize, orderByFieldName, OrderByfeildtype, isAscendingOrder, out totalRows);
            return query.ToList();
        }
        #endregion

        #region Payment
        public static bool IfHasPayment(decimal RequestId)
        {
            Icls_PaymentClient paymentClient = new Icls_PaymentClient();
            int totalrow;
            if (paymentClient.SelectBy_DynamicWhereClause("RequestID =" + RequestId, 1, 10, "PaymentID", "dec", false, out totalrow).Any())
                return true;
            return false;
        }
        #endregion
        #region GetPostVoucherType
        public static int GetPostVoucherType(string date)
        {
            var db = new TreasuryEntities();
            var result = 0;
            if (!string.IsNullOrEmpty(date))
            {
                result = db.Database.SqlQuery<int>("SELECT [dbo].[GetPostVoucherType] (" + date + ")").Single();
            }
            return result;
        }
        #endregion
        #region PettyCash
        public static bool HasVakhahiToAddHeader(int HeaderID)
        {
            var db = new TreasuryEntities();
            var result = false;
            if (HeaderID != 0)
            {
                result = db.Database.SqlQuery<bool>("SELECT [dbo].[HasVakhahiToAddHeader] (" + HeaderID + ")")
                    .Single();
            }
            return result;
        }
        public static decimal GetPettyCashBalance(int pettycahId)
        {
            var db = new TreasuryEntities();
            decimal result = 0;
            if (pettycahId != 0)
            {
                result = db.Database.SqlQuery<decimal>("SELECT [dbo].[fnGetPettyCashBalance] (" + pettycahId + ")")
                    .Single();
            }
            return result;
        }
        public static string GetPaettyCashById(int PettyCashID)
        {
            Icls_PettyCashClient p = new Icls_PettyCashClient();
            var pettyCash = p.SelectRow(PettyCashID).SingleOrDefault();
            return GetUserName(pettyCash.UserID);

        }
        public static Service.PettyCashService.PettyCash GetPaettyCash(int PettyCashID)
        {
            Icls_PettyCashClient p = new Icls_PettyCashClient();
            var obj = p.SelectRow(PettyCashID).SingleOrDefault();
            if (obj != null)
            {
                return obj;
            }
            return null;
        }
        public static Service.PettyCashService.PettyCash GetPettyCashByHeaderId(decimal HeaderID)
        {
            Icls_PettyCash_TransactionHeaderClient pH = new Icls_PettyCash_TransactionHeaderClient();
            var PettyCashHeader = pH.SelectRow(HeaderID).SingleOrDefault();

            Icls_PettyCashClient p = new Icls_PettyCashClient();
            var obj = p.SelectRow(PettyCashHeader.PettyCashID).SingleOrDefault();
            if (obj != null)
            {
                return obj;
            }
            return null;
        }
        public static bool HasAcceptOrVakhahi(decimal HeaderId)
        {
            Icls_PettyCash_TransactionDetailClient PettyCashDetailsclient = new Icls_PettyCash_TransactionDetailClient();
            Icls_PettyCash_TransactionHeaderClient pcth = new Icls_PettyCash_TransactionHeaderClient();
            int TotalRow;
            var detailsLs = PettyCashDetailsclient.SelectBy_DynamicWhereClause("PettyCashTransactionHeaderID =" + HeaderId, 1, 1000, "PettyCashTransactionDetailID", "dec", false, out TotalRow);
            var petycash = pcth.SelectRow(HeaderId);
            var IsAcceptOrVakhahi = detailsLs.Any(d => (d.IsAccept == false && d.IsVakhahi == false));
            // درصورتی که تمام جزییات تنخواه تعیین تکلیف شده باشد
            if (!IsAcceptOrVakhahi)
            {
                return true;
            }
            return false;
        }


        ///// <summary>
        ///// چک کردن این که تمام جزییات تن خواه تایید شده باشد
        ///// </summary>
        ///// <returns></returns>
        //public static bool ifAllPettyDetailIsAccepted(decimal HeaderId = 0)
        //{
        //    int TotalRow;
        //    Icls_PettyCash_TransactionDetailClient PettyCashDetailsclient = new Icls_PettyCash_TransactionDetailClient();
        //    var detailsLs = PettyCashDetailsclient.SelectBy_DynamicWhereClause("PettyCashTransactionHeaderID =" + HeaderId, 1, 1000, "PettyCashTransactionDetailID", "dec", false, out TotalRow);
        //    foreach (var item in detailsLs)
        //    {
        //        if (!item.IsAccept)
        //        {
        //            return false;
        //        }
        //    }
        //    return true;
        //}
        #endregion

        #region direect
        public static bool HasAccVoucherNo(decimal ReferenceId)
        {
            int totalrow;
            var db = new TreasuryEntities();
            //Voucher_RowClient voucherClient = new Voucher_RowClient();
            //var voucher = voucherClient.SelectBy_DynamicWhereClause("ReferenceId =" + ReferenceId, 1, 10, "RowId", "dec", false, out totalrow);
            var voucher = db.Voucher_Row.Where(d => d.ReferenceId == ReferenceId);
            if (voucher != null && voucher.Count() != 0)
            {
                var accvoucherno = voucher.FirstOrDefault().AccVoucherNo;
                if (accvoucherno != null)
                {
                    return true;
                }
            }

            return false;
        }
        #endregion

        #region CountOfAsnaPardakhtani
        public static int AsnadPardakhtaniCount()
        {
            string CurrentDate = Utility.ToPersianDate(DateTime.Now);
            Icls_SPsClient db = new Icls_SPsClient();
            var query = db.Get_Maturity_Cheques(CurrentDate).Where(d => d.States == "Decision").ToList();
            return query.Count();
        }

        #endregion

        public static string GetExchangeRateTypeName(int  ExchangeRateTypeId)
        {
            var desc = "";
            if (ExchangeRateTypeId == 1)
            {
                desc = "نرخ استاندارد";
            }
            else if (ExchangeRateTypeId == 2)
            {
                desc = " نرخ رسمی ";
            }
            else if (ExchangeRateTypeId == 3)
            {
                desc = " نرخ غیررسمی ";
            }
            else if (ExchangeRateTypeId == 4)
            {
                desc = " واقعی ";
            }
            else
            {
                desc = "";
            }
            return desc;
        }


    }
}