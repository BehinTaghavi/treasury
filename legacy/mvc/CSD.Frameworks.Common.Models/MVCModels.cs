using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;
using System.ComponentModel;

namespace CSD.Common.Models
{

    public class FormModel<T> where T : class
    {
        public T FormObject { get; set; }
        public IList<object> ExtraData { get; set; }
        public BatchProcessResult_Model BPR { get; set; }

        public FormModel()
        {
            this.BPR = new BatchProcessResult_Model();
            this.ExtraData = new List<object>();
        }

        public FormModel(BatchProcessResult_Model BPR)
        {
            this.BPR = BPR;
            this.ExtraData = new List<object>();
        }

        public FormModel(T FormObject)
        {
            this.FormObject = FormObject;
            this.BPR = new BatchProcessResult_Model();
            this.ExtraData = new List<object>();
        }

    }


    public class FormModel<T, U>
        where T : class
        where U : class
    {
        public T FormObject { get; set; }
        public U ExtraKnownData { get; set; }
        public IList<object> ExtraData { get; set; }
        public BatchProcessResult_Model BPR { get; set; }

        public FormModel()
        {
            this.BPR = new BatchProcessResult_Model();
            this.ExtraData = new List<object>();
        }

        public FormModel(BatchProcessResult_Model BPR)
        {
            this.BPR = BPR;
            this.ExtraData = new List<object>();
        }

        public FormModel(T FormObject)
        {
            this.FormObject = FormObject;
            this.BPR = new BatchProcessResult_Model();
            this.ExtraData = new List<object>();
        }

    }

    public class CustomDropDownModel
    {
        public string Area { get; set; }
        public object SelectedValue { get; set; }
        public string FieldName { get; set; }

        public CustomDropDownModel()
        {
            FieldName = "DowpDown";
            SelectedValue = null;
            Area = "";
        }
    }
    public class CustomDropDownModel<T> where T : class
    {
        public string Area { get; set; }
        public object SelectedValue { get; set; }
        public string FieldName { get; set; }
        public IList<T> Items { get; set; }

        public CustomDropDownModel(CustomDropDownModel obj)
        {
            FieldName = obj.FieldName;
            SelectedValue = obj.SelectedValue;
            Area = obj.Area;
        }

        public CustomDropDownModel()
        {
            FieldName = "DowpDown";
            SelectedValue = null;
            Area = "";
        }
    }

    public class DateModel
    {
        public string Area { get; set; }
        public DateTime? SelectedDate { get; set; }
        public int MaxYear { get; set; }
        public int MinYear { get; set; }

        public string FieldName { get; set; }
        public bool? Validate { get; set; }

        public DateModel()
        {
            Area = "";
            FieldName = "CreateDate";
            MaxYear = 1389;
            MinYear = 1350;
            SelectedDate = DateTime.Now;
        }
    }
    public class BatchProcessResult_Model
    {

        public int Success
        { get; set; }


        public int Failed
        { get; set; }


        public List<MessageModel> Messages
        { get; set; }

        public void AddErrorsToModelState(ModelStateDictionary modelState)
        {
            foreach (var msg in this.Messages.Where(u => u.MessageType == MessageViewType.Failed))
                modelState.AddModelError("", msg.Message);
        }

        public string ErrorCss
        { get; set; }

        public string SuccessCss
        { get; set; }

        public string ClientScript { get; set; }
        public string FailedClientScript { get; set; }
        public string SuccessClientScript { get; set; }
        public void Clear()
        {
            Messages = new List<MessageModel>();
            Failed = 0;
            Success = 0;
        }

        public void AppendMessage(string newMessage, MessageViewType messageType)
        {
            if (!string.IsNullOrEmpty(newMessage))
            {
                Messages.Add(new MessageModel(newMessage, messageType));
            }
        }
        public void AddModelStateErrors(ModelStateDictionary modelState, bool addCssClass = false, bool addLi = true)
        {
            foreach (var v in modelState.Values)
                foreach (var e in v.Errors)
                    this.AddError(e.ErrorMessage);
        }
        public void AddError(string newMessage)
        {
            Failed++;
            AppendMessage(newMessage, MessageViewType.Failed);
        }

        public void AddSuccess(string newMessage)
        {
            Success++;
            AppendMessage(newMessage, MessageViewType.Success);
        }
        public BatchProcessResult_Model()
        {
            Success = 0;
            Failed = 0;
            ErrorCss = "red";
            SuccessCss = "green";
            Messages = new List<MessageModel>();
        }

        public override string ToString()
        {
            string result = string.Empty;
            foreach (var msg in this.Messages)
                result += msg.Message + "\r\n";
            return result;
        }
    }
    public enum MessageViewType : byte { None = 0, Success = 1, Failed = 2, Warning = 3, Notification = 4, Information = 5 };

    [Serializable]
    public class MessageModel
    {
        public MessageModel()
        {

        }
        public MessageModel(string messageText, MessageViewType messageType)
        {
            this.Message = messageText;
            this.MessageType = messageType;
        }

        public string Message { get; set; }
        public MessageViewType MessageType { get; set; }
    }
    public class ListBoxItems
    {
        public string[] SelectedIds { get; set; }
        public IEnumerable<SelectListItem> Items { get; set; }
    }

    public class UserLoginModel
    {
        [Required(ErrorMessage = "لطفا کد پرسنلی خود را وارد نمایید")]
        [StringLength(20, MinimumLength = 2, ErrorMessage = "نام کاربری باید بیش از دو حرف باشد")]
        public string UserName { get; set; }

        [Required(ErrorMessage = "لطفا رمز عبور خود را وارد نمایید")]
        [StringLength(20, MinimumLength = 2, ErrorMessage = "رمز عبور باید بیش از دو حرف باشد")]
        [DataType(DataType.Password)]
        public string Password
        { get; set; }

        [Required(ErrorMessage = "لطفا عبارت نمایش داده شده در تصویر را وارد نمایید")]
        [StringLength(10, MinimumLength = 5, ErrorMessage = "کد امنیتی بیش از چهار حرف است")]
        public string Captcha { get; set; }

    }

    public class PersonLoginModel
    {
        //[Required(ErrorMessage = "لطفا کد ملی خود را وارد نمایید")]
        //[StringLength(12, MinimumLength = 10, ErrorMessage = "نام کاربری باید مطابق کد ملی باشد")]
        //[RegularExpression(APS.Common.RegX.RegXPattern.NationalCode, ErrorMessage = "الگوی ورودی باید مطابق کد ملی باشد")]
        [Display(Name = "نام کاربری")]
        public string UserName { get; set; }

        [Required(ErrorMessage = "لطفا رمز عبور خود را وارد نمایید")]
        [StringLength(15, MinimumLength = 3, ErrorMessage = "رمز عبور حداقل سه حرف و حداکثر پانزده حرف است")]
        [Display(Name = "رمز عبور")]
        [DataType(DataType.Password)]
        public string Password
        { get; set; }

        [Required(ErrorMessage = "لطفا عبارت نمایش داده شده در تصویر را وارد نمایید")]
        [StringLength(10, MinimumLength = 5, ErrorMessage = "کد امنیتی بیش از چهار حرف است")]
        [Display(Name = "کد امنیتی")]
        public string Captcha { get; set; }

        [Display(Name = "مرا به خاطر بسپار")]
        public bool RememberMe { get; set; }
    }

    public class PersonelLoginModel
    {
        [Required(ErrorMessage = "لطفا کد پرسنلی خود را وارد نمایید")]
        [Range(minimum: 1000000, maximum: 9999999999, ErrorMessage = "یک کد پرسنلی معتبر وارد نمایید")]
        public int PersonCode
        {
            get;
            set;
        }

        [Required(ErrorMessage = "لطفا رمز عبور خود را وارد نمایید")]
        [StringLength(20, MinimumLength = 2, ErrorMessage = "رمز عبور باید بیش از دو حرف باشد")]
        [DataType(DataType.Password)]
        public string Password
        { get; set; }

        [Required(ErrorMessage = "لطفا عبارت نمایش داده شده در تصویر را وارد نمایید")]
        [StringLength(10, MinimumLength = 5, ErrorMessage = "کد امنیتی بیش از چهار حرف است")]
        public string Capcha { get; set; }

    }


    public class SessionStateModel
    {

        public bool Authorised { get; set; }

        public string UserName { get; set; }
        public string UserFullName { get; set; }

        public DateTime LoginTime { get; set; }
        public DateTime SessionStartTime { get; set; }

        public int TotalLoginsTry { get; set; }
        public DateTime? LastLoginFailTry { get; set; }

        public SessionStateModel()
        {
            Authorised = false;
            UserFullName = "";
            UserName = "";
            SessionStartTime = DateTime.Now;
            TotalLoginsTry = 0;
            LastLoginFailTry = null;
        }
    }

    public class ApplicationStateModel
    {
        public int TotalOnlineUsers { get; set; }
        public int TotalGuestUsers { get; set; }
        public int TotalAuthorisedUsers { get; set; }

        public ApplicationStateModel()
        {
            TotalAuthorisedUsers = 0;
            TotalGuestUsers = 0;
            TotalOnlineUsers = 0;
        }
    }

}
