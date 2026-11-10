using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Tresury_wcfService
{

    public class cls_ExchangeTypes : Icls_ExchangeTypes
    {
        TreasuryEntities1 Treasury = new TreasuryEntities1();

        public bool Insert(ExchangeType ExchangeTypeObj, out string Output_Message)
        {

            Output_Message = "insert_success";

            string validated_messageName = ValidateObject(Output_Message, ExchangeTypeObj);
            if (Output_Message != validated_messageName)
            {
                Output_Message = validated_messageName;
                return false;
            }

            try
            {
                 
                Treasury.ExchangeTypes.Add(ExchangeTypeObj);
                Treasury.SaveChanges();
                return true;
            }
            catch
            {
                Output_Message = "insert_error";
                return false;
            }



        }

        public bool Update(ExchangeType ExchangeTypeObj, out string Output_Message)
        {

            Output_Message = "update_success";

            string validated_messageName = ValidateObject(Output_Message, ExchangeTypeObj);
            if (Output_Message != validated_messageName)
            {
                Output_Message = validated_messageName;
                return false;
            }

            try
            {
                ExchangeType obj = Treasury.ExchangeTypes.First(p => p.ExchangeTypeID == ExchangeTypeObj.ExchangeTypeID);
                obj.ExchangeTypeDescription = ExchangeTypeObj.ExchangeTypeDescription;
                obj.ExchangeTypeDescription2 = ExchangeTypeObj.ExchangeTypeDescription2;
 

                Treasury.SaveChanges();
                return true;
            }
            catch
            {
                Output_Message = "update_error";
                return false;
            }



        }

        public bool Delete(int ExchangeTypeID, out string Output_Message)
        {

            Output_Message = "delete_success";
            try
            {
                ExchangeType obj = Treasury.ExchangeTypes.First(p => p.ExchangeTypeID == ExchangeTypeID);
                Treasury.ExchangeTypes.Remove(obj);
                Treasury.SaveChanges();
                return true;
            }
            catch
            {
                Output_Message = "delete_error";
                return false;
            }



        }

        public IQueryable<ExchangeType> SelectAll(int pageNum, int pageSize, string orderByFieldName, string orderByFieldType, bool isAscendingOrder, out int totalRows)
        {
            Treasury.Configuration.ProxyCreationEnabled = false;
            Treasury.Configuration.LazyLoadingEnabled = false;
            IQueryable<ExchangeType> articles = from obj in Treasury.ExchangeTypes select obj;
            return ClsPaging.PagedResult<ExchangeType, IQueryable<ExchangeType>>(articles, pageNum, pageSize, orderByFieldName, isAscendingOrder, out totalRows, orderByFieldType);
        }

        public IQueryable<ExchangeType> SelectRow(int ExchangeTypeID)
        {
            Treasury.Configuration.ProxyCreationEnabled = false;
            Treasury.Configuration.LazyLoadingEnabled = false;
            IQueryable<ExchangeType> articles = Treasury.ExchangeTypes.Where(p => p.ExchangeTypeID == ExchangeTypeID);
            return articles;
        }

        public IQueryable<ExchangeType> SelectBy_DynamicWhereClause(string WhereClause, int pageNum, int pageSize, string orderByFieldName, string orderByFieldType, bool isAscendingOrder, out int totalRows)
        {
            Treasury.Configuration.ProxyCreationEnabled = false;
            Treasury.Configuration.LazyLoadingEnabled = false;
            IQueryable<ExchangeType> articles = Treasury.ExchangeTypes.Where(WhereClause);
            return ClsPaging.PagedResult<ExchangeType, IQueryable<ExchangeType>>(articles, pageNum, pageSize, orderByFieldName, isAscendingOrder, out totalRows, orderByFieldType);
        }

        private string ValidateObject(string messageName, ExchangeType ExchangeTypeObj)
        {
            string err = messageName;

            if (ExchangeTypeObj == null)
            {
                err = "error_nullobject";
                return err;
            }
 
            else if (ExchangeTypeObj.ExchangeTypeDescription.Trim() == "" || ExchangeTypeObj.ExchangeTypeDescription == null)
            {
                err = "error_emptydata_ExchangeTypeDescription";
                return err;
            }
 


            return err;

        }
    }
}
