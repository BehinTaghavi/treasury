using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Tresury_wcfService
{

    public class cls_Request_Payment : Icls_Request_Payment
    {
        TreasuryEntities1 Treasury = new TreasuryEntities1();

        public bool Insert(Request_Payment Request_PaymentObj, out string Output_Message)
        {

            Output_Message = "insert_success";

            string validated_messageName = ValidateObject(Output_Message, Request_PaymentObj);
            if (Output_Message != validated_messageName)
            {
                Output_Message = validated_messageName;
                return false;
            }

            try
            {
               
                Treasury.Request_Payment.Add(Request_PaymentObj);
                Treasury.SaveChanges();
                return true;
            }
            catch
            {
                Output_Message = "insert_error";
                return false;
            }



        }

        public bool Update(Request_Payment Request_PaymentObj, out string Output_Message)
        {

            Output_Message = "update_success";

            string validated_messageName = ValidateObject(Output_Message, Request_PaymentObj);
            if (Output_Message != validated_messageName)
            {
                Output_Message = validated_messageName;
                return false;
            }

            try
            {
                Request_Payment obj = Treasury.Request_Payment.First(p => p.RequestID == Request_PaymentObj.RequestID);
                obj.FiscalYear = Request_PaymentObj.FiscalYear;
                obj.UserID = Request_PaymentObj.UserID;
                obj.RequestDate = Request_PaymentObj.RequestDate;
                obj.RequestCreatedTime = Request_PaymentObj.RequestCreatedTime;
                obj.Reference = Request_PaymentObj.Reference;
                obj.Unique_Account_No = Request_PaymentObj.Unique_Account_No;
                obj.Main_Unique_Account_No = Request_PaymentObj.Main_Unique_Account_No;
                obj.PayAmount = Request_PaymentObj.PayAmount;
                obj.ExchangeTypeID = Request_PaymentObj.ExchangeTypeID;
                obj.Priority = Request_PaymentObj.Priority;
                obj.ExchangeRate = Request_PaymentObj.ExchangeRate;
                obj.EquivalencePrice = Request_PaymentObj.EquivalencePrice;
                obj.BehalfID = Request_PaymentObj.BehalfID;
                obj.referenceTypeID = Request_PaymentObj.referenceTypeID;
                obj.referenceTypeValue = Request_PaymentObj.referenceTypeValue;
                obj.Pay_TypeDetailID_source = Request_PaymentObj.Pay_TypeDetailID_source;
                obj.Source_TypeID = Request_PaymentObj.Source_TypeID;
                obj.Source_PaymentValue = Request_PaymentObj.Source_PaymentValue;
                obj.DueDate = Request_PaymentObj.DueDate;
                obj.Destination_TypesID = Request_PaymentObj.Destination_TypesID;
                obj.Destination_PaymentValue = Request_PaymentObj.Destination_PaymentValue;
                obj.Is_Ok = Request_PaymentObj.Is_Ok;
                obj.Is_Paid = Request_PaymentObj.Is_Paid;
                obj.Is_delivered = Request_PaymentObj.Is_delivered;


                Treasury.SaveChanges();
                return true;
            }
            catch
            {
                Output_Message = "update_error";
                return false;
            }



        }

        public bool Delete(int RequestID, out string Output_Message)
        {

            Output_Message = "delete_success";
            try
            {
                Request_Payment obj = Treasury.Request_Payment.First(p => p.RequestID == RequestID);
                Treasury.Request_Payment.Remove(obj);
                Treasury.SaveChanges();
                return true;
            }
            catch
            {
                Output_Message = "delete_error";
                return false;
            }



        }

        public IQueryable<Request_Payment> SelectAll(int pageNum, int pageSize, string orderByFieldName, string orderByFieldType, bool isAscendingOrder, out int totalRows)
        {
            Treasury.Configuration.ProxyCreationEnabled = false;
            Treasury.Configuration.LazyLoadingEnabled = false;
            IQueryable<Request_Payment> articles = from obj in Treasury.Request_Payment select obj;
            return ClsPaging.PagedResult<Request_Payment, IQueryable<Request_Payment>>(articles, pageNum, pageSize, orderByFieldName, isAscendingOrder, out totalRows, orderByFieldType);
        }

        public IQueryable<Request_Payment> SelectRow(int RequestID)
        {
            Treasury.Configuration.ProxyCreationEnabled = false;
            Treasury.Configuration.LazyLoadingEnabled = false;
            IQueryable<Request_Payment> articles = Treasury.Request_Payment.Where(p => p.RequestID == RequestID);
            return articles;
        }

        public IList<Request_Payment_GetAllInfo_ForSpecialRequestID_Result> SelectRow_GetAllInfo(int RequestID)
        {
            Treasury.Configuration.ProxyCreationEnabled = false;
            Treasury.Configuration.LazyLoadingEnabled = false;
            IList<Request_Payment_GetAllInfo_ForSpecialRequestID_Result> articles = Treasury.Request_Payment_GetAllInfo_ForSpecialRequestID(RequestID).ToList() ;
            
            return articles;
        }

        public IQueryable<Request_Payment> SelectBy_DynamicWhereClause(string WhereClause, int pageNum, int pageSize, string orderByFieldName, string orderByFieldType, bool isAscendingOrder, out int totalRows)
        {
            Treasury.Configuration.ProxyCreationEnabled = false;
            Treasury.Configuration.LazyLoadingEnabled = false;
            IQueryable<Request_Payment> articles = Treasury.Request_Payment.Where(WhereClause);
            return ClsPaging.PagedResult<Request_Payment, IQueryable<Request_Payment>>(articles, pageNum, pageSize, orderByFieldName, isAscendingOrder, out totalRows, orderByFieldType);
        }

        private string ValidateObject(string messageName, Request_Payment Request_PaymentObj)
        {
            string err = messageName;

            if (Request_PaymentObj == null)
            {
                err = "error_nullobject";
                return err;
            }

            else if (Request_PaymentObj.UserID.ToString().Trim() == "0")
            {
                err = "error_emptydata_UserID";
                return err;
            }
            else if (Request_PaymentObj.FiscalYear.Trim() == "" || Request_PaymentObj.FiscalYear == null)
            {
                err = "error_emptydata_FiscalYear";
                return err;
            }
            else if (Request_PaymentObj.RequestDate.Trim() == "" || Request_PaymentObj.RequestDate == null)
            {
                err = "error_emptydata_RequestDate";
                return err;
            }
            else if ( Request_PaymentObj.RequestCreatedTime == null)
            {
                err = "error_emptydata_RequestCreatedTime";
                return err;
            }
            //else if (Request_PaymentObj.Reference.Trim() == "" || Request_PaymentObj.Reference == null)
            //{
            //    err = "error_emptydata_Reference";
            //    return err;
            //}
            else if (Request_PaymentObj.Unique_Account_No.Trim() == "" || Request_PaymentObj.Unique_Account_No == null)
            {
                err = "error_emptydata_Unique_Account_No";
                return err;
            }
            else if (Request_PaymentObj.PayAmount == 0  )
            {
                err = "error_emptydata_PayAmount";
                return err;
            }
            else if (Request_PaymentObj.ExchangeTypeID == 0  )
            {
                err = "error_emptydata_ExchangeTypeID";
                return err;
            }
            else if (Request_PaymentObj.BehalfID == 0)
            {
                err = "error_emptydata_BehalfID";
                return err;
            }
 


            return err;

        }


    }
}
