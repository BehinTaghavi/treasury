using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Tresury_wcfService
{
    public class cls_Bank : Icls_Bank
    {
        TreasuryEntities1 Treasury = new TreasuryEntities1();

        public bool Insert(Bank BankObj, out string Output_Message)
        {

            Output_Message = "insert_success";

            string validated_messageName = ValidateObject(Output_Message, BankObj);
            if (Output_Message != validated_messageName)
            {
                Output_Message = validated_messageName;
                return false;
            }

            try
            {
                Treasury.Banks.Add(BankObj);
                Treasury.SaveChanges();
                return true;
            }
            catch
            {
                Output_Message = "insert_error";
                return false;
            }



        }

        public bool Update(Bank BankObj, out string Output_Message)
        {

            Output_Message = "update_success";

            string validated_messageName = ValidateObject(Output_Message, BankObj);
            if (Output_Message != validated_messageName)
            {
                Output_Message = validated_messageName;
                return false;
            }

            try
            {
                Bank obj = Treasury.Banks.First(p => p.BankID == BankObj.BankID);
                obj.BasicBankID = BankObj.BasicBankID;
                obj.Bank_AccountNumber = BankObj.Bank_AccountNumber;
                obj.BranchCode = BankObj.BranchCode;
                obj.BranchName = BankObj.BranchName;
                obj.BranchName2 = BankObj.BranchName2;
                obj.OpeningDate = BankObj.OpeningDate;
                obj.HasExchangeTransaction = BankObj.HasExchangeTransaction;
                obj.ExchangeTypeID = BankObj.ExchangeTypeID;
                obj.IsActive = BankObj.IsActive;


                Treasury.SaveChanges();
                return true;
            }
            catch
            {
                Output_Message = "update_error";
                return false;
            }



        }

        public bool Delete(int Bankid, out string Output_Message)
        {

            Output_Message = "delete_success";
            try
            {
                Bank obj = Treasury.Banks.First(p => p.BankID == Bankid);
                Treasury.Banks.Remove(obj);
                Treasury.SaveChanges();
                return true;
            }
            catch
            {
                Output_Message = "delete_error";
                return false;
            }



        }

        public IQueryable<Bank> SelectAll(int pageNum, int pageSize, string orderByFieldName, string orderByFieldType, bool isAscendingOrder, out int totalRows)
        {
           
            Treasury.Configuration.ProxyCreationEnabled = false;
            Treasury.Configuration.LazyLoadingEnabled = false;
            IQueryable<Bank> articles = from obj in Treasury.Banks select obj;            
            return ClsPaging.PagedResult<Bank, IQueryable<Bank>>(articles, pageNum, pageSize, orderByFieldName, isAscendingOrder, out totalRows, orderByFieldType);
        }

        public IQueryable<Bank> SelectRow(int BankID)
        {
            Treasury.Configuration.ProxyCreationEnabled = false;
            Treasury.Configuration.LazyLoadingEnabled = false;
            IQueryable<Bank> articles = Treasury.Banks.Where(p => p.BankID == BankID);  
            return articles;
        }

        public IQueryable<Bank> SelectBy_DynamicWhereClause(string WhereClause, int pageNum, int pageSize, string orderByFieldName, string orderByFieldType, bool isAscendingOrder, out int totalRows)
        {
            Treasury.Configuration.ProxyCreationEnabled = false;
            Treasury.Configuration.LazyLoadingEnabled = false;
            IQueryable<Bank> articles = Treasury.Banks.Where(WhereClause);            
            return ClsPaging.PagedResult<Bank, IQueryable<Bank>>(articles, pageNum, pageSize, orderByFieldName, isAscendingOrder, out totalRows, orderByFieldType);
        }

        private string ValidateObject(string messageName, Bank BankObj)
        {
            string err = messageName;

            if (BankObj == null)
            {
                err = "error_nullobject";
                return err;
            }

            else if (BankObj.BasicBankID.ToString().Trim() == "0" )
            {
                err = "error_emptydata_BasicBankID";
                return err;
            }
            else if (BankObj.Bank_AccountNumber.Trim() == "" || BankObj.Bank_AccountNumber == null)
            {
                err = "error_emptydata_Bank_AccountNumber";
                return err;
            }
            else if (BankObj.BranchCode.Trim() == "" || BankObj.BranchCode == null)
            {
                err = "error_emptydata_BranchCode";
                return err;
            }
            else if (BankObj.BranchName.Trim() == "" || BankObj.BranchName == null)
            {
                err = "error_emptydata_BranchName";
                return err;
            }
            else if (BankObj.OpeningDate.Trim() == "" || BankObj.OpeningDate == null)
            {
                err = "error_emptydata_OpeningDate";
                return err;
            }
 

            return err;

        }
    }
}
