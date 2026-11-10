using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Tresury_wcfService
{

    public class cls_Source_DestinationTypes : Icls_Source_DestinationTypes
    {
        TreasuryEntities1 Treasury = new TreasuryEntities1();

        public bool Insert(Source_DestinationTypes Source_DestinationTypesObj, out string Output_Message)
        {

            Output_Message = "insert_success";

            string validated_messageName = ValidateObject(Output_Message, Source_DestinationTypesObj);
            if (Output_Message != validated_messageName)
            {
                Output_Message = validated_messageName;
                return false;
            }

            try
            {
 
                Treasury.Source_DestinationTypes.Add(Source_DestinationTypesObj);
                Treasury.SaveChanges();
                return true;
            }
            catch
            {
                Output_Message = "insert_error";
                return false;
            }



        }

        public bool Update(Source_DestinationTypes Source_DestinationTypesObj, out string Output_Message)
        {

            Output_Message = "update_success";

            string validated_messageName = ValidateObject(Output_Message, Source_DestinationTypesObj);
            if (Output_Message != validated_messageName)
            {
                Output_Message = validated_messageName;
                return false;
            }

            try
            {
                Source_DestinationTypes obj = Treasury.Source_DestinationTypes.First(p => p.Source_DestinationTypeID == Source_DestinationTypesObj.Source_DestinationTypeID);
                obj.Source_DestinationTypeDescription = Source_DestinationTypesObj.Source_DestinationTypeDescription.Trim();
                obj.Source_DestinationTypeDescription2 = Source_DestinationTypesObj.Source_DestinationTypeDescription2;
 

                Treasury.SaveChanges();
                return true;
            }
            catch
            {
                Output_Message = "update_error";
                return false;
            }



        }

        public bool Delete(int Source_DestinationTypeID, out string Output_Message)
        {

            Output_Message = "delete_success";
            try
            {
                Source_DestinationTypes obj = Treasury.Source_DestinationTypes.First(p => p.Source_DestinationTypeID == Source_DestinationTypeID);
                Treasury.Source_DestinationTypes.Remove(obj);
                Treasury.SaveChanges();
                return true;
            }
            catch
            {
                Output_Message = "delete_error";
                return false;
            }



        }

        public IQueryable<Source_DestinationTypes> SelectAll(int pageNum, int pageSize, string orderByFieldName, string orderByFieldType, bool isAscendingOrder, out int totalRows)
        {
            Treasury.Configuration.ProxyCreationEnabled = false;
            Treasury.Configuration.LazyLoadingEnabled = false;
            IQueryable<Source_DestinationTypes> articles = from obj in Treasury.Source_DestinationTypes select obj;
            return ClsPaging.PagedResult<Source_DestinationTypes, IQueryable<Source_DestinationTypes>>(articles, pageNum, pageSize, orderByFieldName, isAscendingOrder, out totalRows, orderByFieldType);
        }

        public IQueryable<Source_DestinationTypes> SelectRow(int Source_DestinationTypeID)
        {
            Treasury.Configuration.ProxyCreationEnabled = false;
            Treasury.Configuration.LazyLoadingEnabled = false;
            IQueryable<Source_DestinationTypes> articles = Treasury.Source_DestinationTypes.Where(p => p.Source_DestinationTypeID == Source_DestinationTypeID);
            return articles;
        }

        public IQueryable<Source_DestinationTypes> SelectBy_DynamicWhereClause(string WhereClause, int pageNum, int pageSize, string orderByFieldName, string orderByFieldType, bool isAscendingOrder, out int totalRows)
        {
            Treasury.Configuration.ProxyCreationEnabled = false;
            Treasury.Configuration.LazyLoadingEnabled = false;
            IQueryable<Source_DestinationTypes> articles = Treasury.Source_DestinationTypes.Where(WhereClause);
            return ClsPaging.PagedResult<Source_DestinationTypes, IQueryable<Source_DestinationTypes>>(articles, pageNum, pageSize, orderByFieldName, isAscendingOrder, out totalRows, orderByFieldType);
        }

        private string ValidateObject(string messageName, Source_DestinationTypes Source_DestinationTypesObj)
        {
            string err = messageName;

            if (Source_DestinationTypesObj == null)
            {
                err = "error_nullobject";
                return err;
            }
 
            else if (Source_DestinationTypesObj.Source_DestinationTypeDescription.Trim() == "" || Source_DestinationTypesObj.Source_DestinationTypeDescription == null)
            {
                err = "error_emptydata_Source_DestinationTypeDescription";
                return err;
            }
 

            return err;

        }
    }
}
