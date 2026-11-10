
using System;

namespace CSD.Treasury.Common.Enumeration
{
    public class TreasuryEnums
    {
        public static string GetTitleFromResource(string enumName, string key)
        {
            try
            {
                var s = Resourse.Resources.ResourceManager.GetString(key);
                if (string.IsNullOrEmpty(s))
                    s = key;
                return s;
            }
            catch
            {
                return key;
            }
        }
    }
    ///// <summary>
    ///// برنامه ریزی 
    ///// </summary>
    //public enum ReceiptState:byte
    //{
    //    //واگذاری 
    //    Assignment=1,
    //    //وصول -برگشت- عودت 
    //    ReceiptReturn=2,
    //    //خرج 
    //    Spend=3,
    //    //عودت
    //    ReturnSpent=4,
    //    //   واگذار شده 
    //    DoneAssignment = 5,
    //    //   وصول -برگشت- عودت شده
    //    DoneReceiptReturn = 6,
    //    //خرج شده
    //    DoneSpend = 7,
    //    //عودت شده
    //    DoneReturnSpent = 8
    //}
    public enum Account_No_Type : byte
    {
        Account_No=1,
        Assign_Account_No=2,
        Returns_Account_No=3
    };
    public enum Priority : byte
    {
        High = 1,
        Normal = 2,
        Low = 3
    };
    public enum Source_DestinationType : byte
    {
        BankCheque = 1,
        Cash = 2,
        CardBank = 3,
 
    };
    public enum BehalfGroupType : byte
    {
        Receipt = 1,
        Pay = 2
    };
    public enum DefualtLanguage : byte
    {
        FA = 1,
        ENG = 2
    };
    public enum FormatDescription : byte
    {
        NewestLarge = 1,
        NewLarge = 2,
        OldLarge = 3,
        NewestSmall = 4,
        NewSmall = 5,
        Small = 6
    };
    /// <summary>
    /// وضعیت درخواست وجه 
    /// </summary>
    public enum RequestStatusTypes : short
    {
        //ابطال چک 
        CancelCheque=-2,
        //ابطال
        Cancellation = -1,
        //نامشخص
        Unknown = 0,
        //تایید درخواست 
        Confirmed = 1,
        //تایید پرداخت
        ConfirmPayment = 2,
        //پرداخت شده
        Paid = 3,
        //تحویل شده
        Delivered = 4,
        //pendding
        Pendding = 5,
        //تحویل لاشه چک
        DeleiverdLasheCheque=6,
       //چک تبدیل شده
        CovertedCheque=7,
       //تمدیدسررسید
       ExtendDuedate=8

    };

    public enum RequestDescription : byte
    {
        ResultOfIntegration = 1,//مشخص کننده درخواستی است که در نتیحه تجمیع چند درخواست دیگر ایجاد شده است //درصورتیست که درخواست ها در ذینفع و گروه بابت باهم اشتراک داشته باشند
        Duplication = 2,//مشخص کننده درخواست وجه ای که تکثیر شده است
        Integration = 3,//درصورتیست که درخواست در ذینفع و گروه بابت با هم اشتراک داشته باشند// مشخص کننده درخواست وجه ای است که با چندین درخواست وجه دیگه تجمیع شده است 
        ResultOfDuplication = 4,//نتیجه درخواست وجه های تکثیر شده است 
        TearingCheque = 5, //پاره شدن چک 
        ResultOfConvertCheque=6 ,// نتیجه تبدیل چک انتظامی به نقد
        IntegrationRequest=7,//درخواست وجه هایی که با هم تجمیع شده اند و تا تایید پرداخت رفته بودند
        ResultOfIntegrationRequest = 8 ,  //حاصل درخواست وجه هایی که تا تایید پرداخت رفته اند وهیچ اشتراکی نداشته اند و نکثیر شدند
        ResultOfExtendDueDate=9
    }
    public enum planingType : int
    {
        NazdeSandogh=1,
        AsnadDrayaftaniTazmini=18,
        TavizchequeDaryafti = 21,
        TanzilAsnadDryafti=22,
        MogodiNaghdNazdeBank=17,
    };
}
