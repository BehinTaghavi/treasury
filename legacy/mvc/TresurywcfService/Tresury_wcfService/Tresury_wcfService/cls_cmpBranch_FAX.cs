using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Tresury_wcfService
{

    public class cls_cmpBranch_FAX : Icls_cmpBranch_FAX
    {
        TreasuryEntities1 Treasury = new TreasuryEntities1();

        public bool Insert(cmpBranch_FAX cmpBranch_FAXObj, out string Output_Message)
        {

            Output_Message = "insert_success";

            string validated_messageName = ValidateObject(Output_Message, cmpBranch_FAXObj);
            if (Output_Message != validated_messageName)
            {
                Output_Message = validated_messageName;
                return false;
            }

            try
            {
 
                Treasury.cmpBranch_FAX.Add(cmpBranch_FAXObj);
                Treasury.SaveChanges();
                return true;
            }
            catch
            {
                Output_Message = "insert_error";
                return false;
            }



        }

        public bool Update(cmpBranch_FAX cmpBranch_FAXObj, out string Output_Message)
        {

            Output_Message = "update_success";

            string validated_messageName = ValidateObject(Output_Message, cmpBranch_FAXObj);
            if (Output_Message != validated_messageName)
            {
                Output_Message = validated_messageName;
                return false;
            }

            try
            {
                cmpBranch_FAX obj = Treasury.cmpBranch_FAX.First(p => p.ID == cmpBranch_FAXObj.ID);
                obj.cmpBranchID = cmpBranch_FAXObj.cmpBranchID;
                obj.cmpBranchFax = cmpBranch_FAXObj.cmpBranchFax;
                obj.cmpBranchFax_Description = cmpBranch_FAXObj.cmpBranchFax_Description ;
                obj.cmpBranchFax_Description2 = cmpBranch_FAXObj.cmpBranchFax_Description2;
 


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
                cmpBranch_FAX obj = Treasury.cmpBranch_FAX.First(p => p.ID == ID);
                Treasury.cmpBranch_FAX.Remove(obj);
                Treasury.SaveChanges();
                return true;
            }
            catch
            {
                Output_Message = "delete_error";
                return false;
            }



        }

        public IQueryable<cmpBranch_FAX> SelectAll(int pageNum, int pageSize, string orderByFieldName, string orderByFieldType, bool isAscendingOrder, out int totalRows)
        {
            Treasury.Configuration.ProxyCreationEnabled = false;
            Treasury.Configuration.LazyLoadingEnabled = false;
            IQueryable<cmpBranch_FAX> articles = from obj in Treasury.cmpBranch_FAX select obj;
            return ClsPaging.PagedResult<cmpBranch_FAX, IQueryable<cmpBranch_FAX>>(articles, pageNum, pageSize, orderByFieldName, isAscendingOrder, out totalRows, orderByFieldType);
        }

        public IQueryable<cmpBranch_FAX> SelectRow(int ID)
        {
            Treasury.Configuration.ProxyCreationEnabled = false;
            Treasury.Configuration.LazyLoadingEnabled = false;
            IQueryable<cmpBranch_FAX> articles = Treasury.cmpBranch_FAX.Where(p => p.ID == ID);
            return articles;
        }

        public IQueryable<cmpBranch_FAX> SelectBy_DynamicWhereClause(string WhereClause, int pageNum, int pageSize, string orderByFieldName, string orderByFieldType, bool isAscendingOrder, out int totalRows)
        {
            Treasury.Configuration.ProxyCreationEnabled = false;
            Treasury.Configuration.LazyLoadingEnabled = false;
            IQueryable<cmpBranch_FAX> articles = Treasury.cmpBranch_FAX.Where(WhereClause);
            return ClsPaging.PagedResult<cmpBranch_FAX, IQueryable<cmpBranch_FAX>>(articles, pageNum, pageSize, orderByFieldName, isAscendingOrder, out totalRows, orderByFieldType);
        }

        private string ValidateObject(string messageName, cmpBranch_FAX cmpBranch_FAXObj)
        {
            string err = messageName;

            if (cmpBranch_FAXObj == null)
            {
                err = "error_nullobject";
                return err;
            }

            else if (cmpBranch_FAXObj.cmpBranchID.ToString().Trim() == "0")
            {
                err = "error_emptydata_cmpBranchID";
                return err;
            }
            else if (cmpBranch_FAXObj.cmpBranchFax.Trim() == "" || cmpBranch_FAXObj.cmpBranchFax == null)
            {
                err = "error_emptydata_cmpBranchFax";
                return err;
            }
 

            return err;

        }
    }
}
