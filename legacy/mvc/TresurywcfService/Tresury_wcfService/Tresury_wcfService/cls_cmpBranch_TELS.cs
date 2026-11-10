using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Tresury_wcfService
{

    public class cls_cmpBranch_TELS : Icls_cmpBranch_TELS
    {
        TreasuryEntities1 Treasury = new TreasuryEntities1();

        public bool Insert(cmpBranch_TELS cmpBranch_TELSObj, out string Output_Message)
        {

            Output_Message = "insert_success";

            string validated_messageName = ValidateObject(Output_Message, cmpBranch_TELSObj);
            if (Output_Message != validated_messageName)
            {
                Output_Message = validated_messageName;
                return false;
            }

            try
            {

                Treasury.cmpBranch_TELS.Add(cmpBranch_TELSObj);
                Treasury.SaveChanges();
                return true;
            }
            catch
            {
                Output_Message = "insert_error";
                return false;
            }



        }

        public bool Update(cmpBranch_TELS cmpBranch_TELSObj, out string Output_Message)
        {

            Output_Message = "update_success";

            string validated_messageName = ValidateObject(Output_Message, cmpBranch_TELSObj);
            if (Output_Message != validated_messageName)
            {
                Output_Message = validated_messageName;
                return false;
            }

            try
            {
                cmpBranch_TELS obj = Treasury.cmpBranch_TELS.First(p => p.ID == cmpBranch_TELSObj.ID);
                obj.cmpBranchID = cmpBranch_TELSObj.cmpBranchID;
                obj.cmpBranchTel = cmpBranch_TELSObj.cmpBranchTel;
                obj.cmpBranchTel_Description = cmpBranch_TELSObj.cmpBranchTel_Description;
                obj.cmpBranchTel_Description2 = cmpBranch_TELSObj.cmpBranchTel_Description2;



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
                cmpBranch_TELS obj = Treasury.cmpBranch_TELS.First(p => p.ID == ID);
                Treasury.cmpBranch_TELS.Remove(obj);
                Treasury.SaveChanges();
                return true;
            }
            catch
            {
                Output_Message = "delete_error";
                return false;
            }



        }

        public IQueryable<cmpBranch_TELS> SelectAll(int pageNum, int pageSize, string orderByFieldName, string orderByFieldType, bool isAscendingOrder, out int totalRows)
        {
            Treasury.Configuration.ProxyCreationEnabled = false;
            Treasury.Configuration.LazyLoadingEnabled = false;
            IQueryable<cmpBranch_TELS> articles = from obj in Treasury.cmpBranch_TELS select obj;
            return ClsPaging.PagedResult<cmpBranch_TELS, IQueryable<cmpBranch_TELS>>(articles, pageNum, pageSize, orderByFieldName, isAscendingOrder, out totalRows, orderByFieldType);
        }

        public IQueryable<cmpBranch_TELS> SelectRow(int ID)
        {
            Treasury.Configuration.ProxyCreationEnabled = false;
            Treasury.Configuration.LazyLoadingEnabled = false;
            IQueryable<cmpBranch_TELS> articles = Treasury.cmpBranch_TELS.Where(p => p.ID == ID);
            return articles;
        }

        public IQueryable<cmpBranch_TELS> SelectBy_DynamicWhereClause(string WhereClause, int pageNum, int pageSize, string orderByFieldName, string orderByFieldType, bool isAscendingOrder, out int totalRows)
        {
            Treasury.Configuration.ProxyCreationEnabled = false;
            Treasury.Configuration.LazyLoadingEnabled = false;
            IQueryable<cmpBranch_TELS> articles = Treasury.cmpBranch_TELS.Where(WhereClause);
            return ClsPaging.PagedResult<cmpBranch_TELS, IQueryable<cmpBranch_TELS>>(articles, pageNum, pageSize, orderByFieldName, isAscendingOrder, out totalRows, orderByFieldType);
        }

        private string ValidateObject(string messageName, cmpBranch_TELS cmpBranch_TELSObj)
        {
            string err = messageName;

            if (cmpBranch_TELSObj == null)
            {
                err = "error_nullobject";
                return err;
            }

            else if (cmpBranch_TELSObj.cmpBranchID.ToString().Trim() == "0")
            {
                err = "error_emptydata_cmpBranchID";
                return err;
            }
            else if (cmpBranch_TELSObj.cmpBranchTel.Trim() == "" || cmpBranch_TELSObj.cmpBranchTel == null)
            {
                err = "error_emptydata_cmpBranchTel";
                return err;
            }


            return err;

        }
    }
}
