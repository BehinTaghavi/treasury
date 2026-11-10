using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Tresury_wcfService
{

    public class cls_BankCards : Icls_BankCards
    {
        TreasuryEntities1 Treasury = new TreasuryEntities1();

        public bool Insert(BankCard BankCardObj, out string Output_Message)
        {

            Output_Message = "insert_success";

            string validated_messageName = ValidateObject(Output_Message, BankCardObj);
            if (Output_Message != validated_messageName)
            {
                Output_Message = validated_messageName;
                return false;
            }

            try
            {
                BankCardObj.CreateDateTime = DateTime.Now;
                Treasury.BankCards.Add(BankCardObj);
                Treasury.SaveChanges();
                return true;
            }
            catch
            {
                Output_Message = "insert_error";
                return false;
            }



        }

        public bool Update(BankCard BankCardObj, out string Output_Message)
        {

            Output_Message = "update_success";

            string validated_messageName = ValidateObject(Output_Message, BankCardObj);
            if (Output_Message != validated_messageName)
            {
                Output_Message = validated_messageName;
                return false;
            }

            try
            {
                BankCard obj = Treasury.BankCards.First(p => p.CardID == BankCardObj.CardID);
                obj.BankID = BankCardObj.BankID;
                obj.Card_AccountNo = BankCardObj.Card_AccountNo;
                obj.Card_NO = BankCardObj.Card_NO;
                obj.CVV2 = BankCardObj.CVV2;
                obj.ExpiryDate = BankCardObj.ExpiryDate;
                obj.IsActive = BankCardObj.IsActive;


                Treasury.SaveChanges();
                return true;
            }
            catch
            {
                Output_Message = "update_error";
                return false;
            }



        }

        public bool Delete(int CardID, out string Output_Message)
        {

            Output_Message = "delete_success";
            try
            {
                BankCard obj = Treasury.BankCards.First(p => p.CardID == CardID);
                Treasury.BankCards.Remove(obj);
                Treasury.SaveChanges();
                return true;
            }
            catch
            {
                Output_Message = "delete_error";
                return false;
            }



        }

        public IQueryable<BankCard> SelectAll(int pageNum, int pageSize, string orderByFieldName, string orderByFieldType, bool isAscendingOrder, out int totalRows)
        {
            Treasury.Configuration.ProxyCreationEnabled = false;
            Treasury.Configuration.LazyLoadingEnabled = false;
            IQueryable<BankCard> articles = from obj in Treasury.BankCards select obj;
            return ClsPaging.PagedResult<BankCard, IQueryable<BankCard>>(articles, pageNum, pageSize, orderByFieldName, isAscendingOrder, out totalRows, orderByFieldType);
        }

        public IQueryable<BankCard> SelectRow(int CardID)
        {
            Treasury.Configuration.ProxyCreationEnabled = false;
            Treasury.Configuration.LazyLoadingEnabled = false;
            IQueryable<BankCard> articles = Treasury.BankCards.Where(p => p.CardID == CardID);
            return articles;
        }

        public IQueryable<BankCard> SelectBy_DynamicWhereClause(string WhereClause, int pageNum, int pageSize, string orderByFieldName, string orderByFieldType, bool isAscendingOrder, out int totalRows)
        {
            Treasury.Configuration.ProxyCreationEnabled = false;
            Treasury.Configuration.LazyLoadingEnabled = false;
            IQueryable<BankCard> articles = Treasury.BankCards.Where(WhereClause);
            return ClsPaging.PagedResult<BankCard, IQueryable<BankCard>>(articles, pageNum, pageSize, orderByFieldName, isAscendingOrder, out totalRows, orderByFieldType);
        }

        private string ValidateObject(string messageName, BankCard BankCardObj)
        {
            string err = messageName;

            if (BankCardObj == null)
            {
                err = "error_nullobject";
                return err;
            }

            else if (BankCardObj.BankID.ToString().Trim() == "0")
            {
                err = "error_emptydata_BankID";
                return err;
            }
            else if (BankCardObj.Card_AccountNo.Trim() == "" || BankCardObj.Card_AccountNo == null)
            {
                err = "error_emptydata_Card_AccountNo";
                return err;
            }
            else if (BankCardObj.Card_NO.Trim() == "" || BankCardObj.Card_NO == null)
            {
                err = "error_emptydata_Card_NO";
                return err;
            }
            else if (BankCardObj.CVV2.Trim() == "" || BankCardObj.CVV2 == null)
            {
                err = "error_emptydata_CVV2";
                return err;
            }
            else if (BankCardObj.ExpiryDate.Trim() == "" || BankCardObj.ExpiryDate == null)
            {
                err = "error_emptydata_ExpiryDate";
                return err;
            }


            return err;

        }
    }
}
