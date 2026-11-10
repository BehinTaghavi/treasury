using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Tresury_wcfService
{
    public class cls_BasicBanks : Icls_BasicBanks
    {
        TreasuryEntities1 Treasury = new TreasuryEntities1();

        public bool Insert(BasicBank BasicBankObj, out string Output_Message)
        {

            Output_Message = "insert_success";

            string validated_messageName = ValidateObject(Output_Message, BasicBankObj);
            if (Output_Message != validated_messageName)
            {
                Output_Message = validated_messageName;
                return false;
            }

            try
            {                 
                Treasury.BasicBanks.Add(BasicBankObj);
                Treasury.SaveChanges();
                return true;
            }
            catch
            {
                Output_Message = "insert_error";
                return false;
            }



        }

        public bool Update(BasicBank BasicBankObj, out string Output_Message)
        {

            Output_Message = "update_success";

            string validated_messageName = ValidateObject(Output_Message, BasicBankObj);
            if (Output_Message != validated_messageName)
            {
                Output_Message = validated_messageName;
                return false;
            }

            try
            {
                BasicBank obj = Treasury.BasicBanks.First(p => p.BasicBankId == BasicBankObj.BasicBankId);
                obj.BasicBankDescription = BasicBankObj.BasicBankDescription.Trim();
                obj.BasicBankDescription2 = BasicBankObj.BasicBankDescription2;
                obj.BasicBankImage = BasicBankObj.BasicBankImage;

                Treasury.SaveChanges();
                return true;
            }
            catch
            {
                Output_Message = "update_error";
                return false;
            }



        }

        public bool Delete(int BasicBankId, out string Output_Message)
        {

            Output_Message = "delete_success";
            try
            {
                BasicBank obj = Treasury.BasicBanks.First(p => p.BasicBankId == BasicBankId);
                Treasury.BasicBanks.Remove(obj);
                Treasury.SaveChanges();
                return true;
            }
            catch
            {
                Output_Message = "delete_error";
                return false;
            }



        }

        public IQueryable<BasicBank> SelectAll(int pageNum, int pageSize, string orderByFieldName, string orderByFieldType, bool isAscendingOrder, out int totalRows)
        {
            Treasury.Configuration.ProxyCreationEnabled = false;
            Treasury.Configuration.LazyLoadingEnabled = false;
            IQueryable<BasicBank> articles = from obj in Treasury.BasicBanks select obj;
            return ClsPaging.PagedResult<BasicBank, IQueryable<BasicBank>>(articles, pageNum, pageSize, orderByFieldName, isAscendingOrder, out totalRows, orderByFieldType);
        }

        public IQueryable<BasicBank> SelectRow(int BasicBankId)
        {
            Treasury.Configuration.ProxyCreationEnabled = false;
            Treasury.Configuration.LazyLoadingEnabled = false;
            IQueryable<BasicBank> articles = Treasury.BasicBanks.Where(p => p.BasicBankId == BasicBankId);
            return articles;
        }

        public IQueryable<BasicBank> SelectBy_DynamicWhereClause(string WhereClause, int pageNum, int pageSize, string orderByFieldName, string orderByFieldType, bool isAscendingOrder, out int totalRows)
        {
            Treasury.Configuration.ProxyCreationEnabled = false;
            Treasury.Configuration.LazyLoadingEnabled = false;
            IQueryable<BasicBank> articles = Treasury.BasicBanks.Where(WhereClause);
            return ClsPaging.PagedResult<BasicBank, IQueryable<BasicBank>>(articles, pageNum, pageSize, orderByFieldName, isAscendingOrder, out totalRows, orderByFieldType);
        }

        private string ValidateObject(string messageName, BasicBank BankObj)
        {
            string err = messageName;

            if (BankObj == null)
            {
                err = "error_nullobject";
                return err;
            }
 
            else if (BankObj.BasicBankDescription.Trim() == "" || BankObj.BasicBankDescription == null)
            {
                err = "error_emptydata_BasicBankDescription";
                return err;
            }
 


            return err;

        }
    }
}
