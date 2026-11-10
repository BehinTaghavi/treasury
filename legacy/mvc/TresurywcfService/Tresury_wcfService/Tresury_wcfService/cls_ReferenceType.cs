using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Tresury_wcfService
{

    public class cls_ReferenceType : Icls_ReferenceType
    {
        TreasuryEntities1 Treasury = new TreasuryEntities1();

        public bool Insert(ReferenceType ReferenceTypeObj, out string Output_Message)
        {

            Output_Message = "insert_success";

            string validated_messageName = ValidateObject(Output_Message, ReferenceTypeObj);
            if (Output_Message != validated_messageName)
            {
                Output_Message = validated_messageName;
                return false;
            }

            try
            {
             
                Treasury.ReferenceTypes.Add(ReferenceTypeObj);
                Treasury.SaveChanges();
                return true;
            }
            catch
            {
                Output_Message = "insert_error";
                return false;
            }



        }

        public bool Update(ReferenceType ReferenceTypeObj, out string Output_Message)
        {

            Output_Message = "update_success";

            string validated_messageName = ValidateObject(Output_Message, ReferenceTypeObj);
            if (Output_Message != validated_messageName)
            {
                Output_Message = validated_messageName;
                return false;
            }

            try
            {
                ReferenceType obj = Treasury.ReferenceTypes.First(p => p.referenceTypeID == ReferenceTypeObj.referenceTypeID);
                obj.referenceTypeDescription = ReferenceTypeObj.referenceTypeDescription.Trim();
                obj.referenceTypeDescription2 = ReferenceTypeObj.referenceTypeDescription2;
 


                Treasury.SaveChanges();
                return true;
            }
            catch
            {
                Output_Message = "update_error";
                return false;
            }



        }

        public bool Delete(int referenceTypeID, out string Output_Message)
        {

            Output_Message = "delete_success";
            try
            {
                ReferenceType obj = Treasury.ReferenceTypes.First(p => p.referenceTypeID == referenceTypeID);
                Treasury.ReferenceTypes.Remove(obj);
                Treasury.SaveChanges();
                return true;
            }
            catch
            {
                Output_Message = "delete_error";
                return false;
            }



        }

        public IQueryable<ReferenceType> SelectAll(int pageNum, int pageSize, string orderByFieldName, string orderByFieldType, bool isAscendingOrder, out int totalRows)
        {
            Treasury.Configuration.ProxyCreationEnabled = false;
            Treasury.Configuration.LazyLoadingEnabled = false;
            IQueryable<ReferenceType> articles = from obj in Treasury.ReferenceTypes select obj;
            return ClsPaging.PagedResult<ReferenceType, IQueryable<ReferenceType>>(articles, pageNum, pageSize, orderByFieldName, isAscendingOrder, out totalRows, orderByFieldType);
        }

        public IQueryable<ReferenceType> SelectRow(int referenceTypeID)
        {
            Treasury.Configuration.ProxyCreationEnabled = false;
            Treasury.Configuration.LazyLoadingEnabled = false;
            IQueryable<ReferenceType> articles = Treasury.ReferenceTypes.Where(p => p.referenceTypeID == referenceTypeID);
            return articles;
        }

        public IQueryable<ReferenceType> SelectBy_DynamicWhereClause(string WhereClause, int pageNum, int pageSize, string orderByFieldName, string orderByFieldType, bool isAscendingOrder, out int totalRows)
        {
            Treasury.Configuration.ProxyCreationEnabled = false;
            Treasury.Configuration.LazyLoadingEnabled = false;
            IQueryable<ReferenceType> articles = Treasury.ReferenceTypes.Where(WhereClause);
            return ClsPaging.PagedResult<ReferenceType, IQueryable<ReferenceType>>(articles, pageNum, pageSize, orderByFieldName, isAscendingOrder, out totalRows, orderByFieldType);
        }

        private string ValidateObject(string messageName, ReferenceType ReferenceTypeObj)
        {
            string err = messageName;

            if (ReferenceTypeObj == null)
            {
                err = "error_nullobject";
                return err;
            }
 
            else if (ReferenceTypeObj.referenceTypeDescription.Trim() == "" || ReferenceTypeObj.referenceTypeDescription == null)
            {
                err = "error_emptydata_referenceTypeDescription";
                return err;
            }
 

            return err;

        }
    }
}
