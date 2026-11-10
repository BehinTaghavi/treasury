using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Tresury_wcfService
{

    public class cls_cmpBranch_EmailAddress : Icls_cmpBranch_EmailAddress
    {
        TreasuryEntities1 Treasury = new TreasuryEntities1();

        public bool Insert(cmpBranch_EmailAddress cmpBranch_EmailAddressObj, out string Output_Message)
        {

            Output_Message = "insert_success";

            string validated_messageName = ValidateObject(Output_Message, cmpBranch_EmailAddressObj);
            if (Output_Message != validated_messageName)
            {
                Output_Message = validated_messageName;
                return false;
            }

            try
            {

                Treasury.cmpBranch_EmailAddress.Add(cmpBranch_EmailAddressObj);
                Treasury.SaveChanges();
                return true;
            }
            catch
            {
                Output_Message = "insert_error";
                return false;
            }



        }

        public bool Update(cmpBranch_EmailAddress cmpBranch_EmailAddressObj, out string Output_Message)
        {

            Output_Message = "update_success";

            string validated_messageName = ValidateObject(Output_Message, cmpBranch_EmailAddressObj);
            if (Output_Message != validated_messageName)
            {
                Output_Message = validated_messageName;
                return false;
            }

            try
            {
                cmpBranch_EmailAddress obj = Treasury.cmpBranch_EmailAddress.First(p => p.ID == cmpBranch_EmailAddressObj.ID);
                obj.cmpBranchID = cmpBranch_EmailAddressObj.cmpBranchID;
                obj.cmpBranchEmailAddress = cmpBranch_EmailAddressObj.cmpBranchEmailAddress;
                obj.cmpBranchEmailAddress_Description = cmpBranch_EmailAddressObj.cmpBranchEmailAddress_Description;
                obj.cmpBranchEmailAddress_Description2 = cmpBranch_EmailAddressObj.cmpBranchEmailAddress_Description2;



                Treasury.SaveChanges();
                return true;
            }
            catch
            {
                Output_Message = "update_error";
                return false;
            }



        }

        public bool Delete(int ID, out string Output_Message)
        {

            Output_Message = "delete_success";
            try
            {
                cmpBranch_EmailAddress obj = Treasury.cmpBranch_EmailAddress.First(p => p.ID == ID);
                Treasury.cmpBranch_EmailAddress.Remove(obj);
                Treasury.SaveChanges();
                return true;
            }
            catch
            {
                Output_Message = "delete_error";
                return false;
            }



        }

        public IQueryable<cmpBranch_EmailAddress> SelectAll(int pageNum, int pageSize, string orderByFieldName, string orderByFieldType, bool isAscendingOrder, out int totalRows)
        {
            Treasury.Configuration.ProxyCreationEnabled = false;
            Treasury.Configuration.LazyLoadingEnabled = false;
            IQueryable<cmpBranch_EmailAddress> articles = from obj in Treasury.cmpBranch_EmailAddress select obj;
            return ClsPaging.PagedResult<cmpBranch_EmailAddress, IQueryable<cmpBranch_EmailAddress>>(articles, pageNum, pageSize, orderByFieldName, isAscendingOrder, out totalRows, orderByFieldType);
        }

        public IQueryable<cmpBranch_EmailAddress> SelectRow(int ID)
        {
            Treasury.Configuration.ProxyCreationEnabled = false;
            Treasury.Configuration.LazyLoadingEnabled = false;
            IQueryable<cmpBranch_EmailAddress> articles = Treasury.cmpBranch_EmailAddress.Where(p => p.ID == ID);
            return articles;
        }

        public IQueryable<cmpBranch_EmailAddress> SelectBy_DynamicWhereClause(string WhereClause, int pageNum, int pageSize, string orderByFieldName, string orderByFieldType, bool isAscendingOrder, out int totalRows)
        {
            Treasury.Configuration.ProxyCreationEnabled = false;
            Treasury.Configuration.LazyLoadingEnabled = false;
            IQueryable<cmpBranch_EmailAddress> articles = Treasury.cmpBranch_EmailAddress.Where(WhereClause);
            return ClsPaging.PagedResult<cmpBranch_EmailAddress, IQueryable<cmpBranch_EmailAddress>>(articles, pageNum, pageSize, orderByFieldName, isAscendingOrder, out totalRows, orderByFieldType);
        }

        private string ValidateObject(string messageName, cmpBranch_EmailAddress cmpBranch_EmailAddressObj)
        {
            string err = messageName;

            if (cmpBranch_EmailAddressObj == null)
            {
                err = "error_nullobject";
                return err;
            }

            else if (cmpBranch_EmailAddressObj.cmpBranchID.ToString().Trim() == "0")
            {
                err = "error_emptydata_cmpBranchID";
                return err;
            }
            else if (cmpBranch_EmailAddressObj.cmpBranchEmailAddress.Trim() == "" || cmpBranch_EmailAddressObj.cmpBranchEmailAddress == null)
            {
                err = "error_emptydata_cmpBranchEmailAddress";
                return err;
            }


            return err;

        }
    }
}
