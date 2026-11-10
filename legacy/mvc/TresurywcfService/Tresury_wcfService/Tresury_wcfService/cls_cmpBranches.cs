using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
 
namespace Tresury_wcfService
{
    public class cls_cmpBranches : Icls_cmpBranches
    {
        TreasuryEntities1 Treasury = new TreasuryEntities1();

        public bool  Insert(cmpBranch BranchObj , out string Output_Message )
        {

            Output_Message = "insert_success";

            string validated_messageName = ValidateObject(Output_Message, BranchObj);
            if (Output_Message != validated_messageName)
            {
                Output_Message = validated_messageName;
                return false; 
            }

            try
            {
                Treasury.cmpBranches.Add(BranchObj);
                Treasury.SaveChanges();
                return true; 
            }
            catch
            {
                Output_Message = "insert_error";
                return false;
            }


             
        }

        public bool  Update(cmpBranch BranchObj, out string Output_Message)
        {

            Output_Message = "update_success";

            string validated_messageName = ValidateObject(Output_Message, BranchObj);
            if (Output_Message != validated_messageName)
            {
                Output_Message = validated_messageName;
                return false;
            }

            try
            {
                cmpBranch obj = Treasury.cmpBranches.First(p => p.cmpBranchID == BranchObj.cmpBranchID);
                obj.cmpBranchAddress = BranchObj.cmpBranchAddress;
                obj.cmpBranchAddress2 = BranchObj.cmpBranchAddress2;
                obj.cmpBranchName  = BranchObj.cmpBranchName;
                obj.cmpBranchName2 = BranchObj.cmpBranchName2;

                Treasury.SaveChanges();
                return true;
            }
            catch
            {
                Output_Message = "update_error";
                return false;
            }


            
        }

        public bool  Delete(int Branchid, out string Output_Message)
        {

            Output_Message = "delete_success";
            try
            {
                cmpBranch obj = Treasury.cmpBranches.First(p => p.cmpBranchID == Branchid);
                Treasury.cmpBranches.Remove(obj);
                Treasury.SaveChanges();
                return true;
            }
            catch
            {
                Output_Message = "delete_error";
                return false;
            }



        }


        // noteye mohem : be khatere class e uttility bood ke tavanestim orderby ra overload konim
        public IQueryable<cmpBranch> SelectAll(int pageNum, int pageSize, string orderByFieldName,  string orderByFieldType, bool isAscendingOrder , out int totalRows)
        {
            Treasury.Configuration.ProxyCreationEnabled = false;
            Treasury.Configuration.LazyLoadingEnabled = false;
            IQueryable  < Tresury_wcfService.cmpBranch > articles = from obj in Treasury.cmpBranches select obj;
            //return ClsPaging.PagedResult(articles, pageNum, pageSize, p => p.cmpBranchID, isAscendingOrder, out totalRows) ;             
            return ClsPaging.PagedResult< cmpBranch , IQueryable<cmpBranch>>(articles, pageNum, pageSize, orderByFieldName, isAscendingOrder, out totalRows, orderByFieldType);
 

        }

        public IQueryable<cmpBranch>  SelectRow  (int BranchID)
        {
            Treasury.Configuration.ProxyCreationEnabled = false;
            Treasury.Configuration.LazyLoadingEnabled = false;
            IQueryable<cmpBranch> articles = Treasury.cmpBranches.Where(p => p.cmpBranchID == BranchID) ;   //  from obj in Treasury.cmpBranches where obj.cmpBranchID == BranchID select obj;
            return articles ;
          //  return ClsPaging.PagedResult (articles ,  pageNum ,  pageSize, p => p.cmpBranchID, isAscendingOrder, out totalRows) ;
        }

        // noteye mohem : be khatere class e DynamicQueryable bood ke tavanestim  Where Clause ra overload konim
        public IQueryable<cmpBranch> SelectBy_DynamicWhereClause(string WhereClause, int pageNum, int pageSize, string orderByFieldName, string orderByFieldType, bool isAscendingOrder, out int totalRows)
        {
            Treasury.Configuration.ProxyCreationEnabled = false;
            Treasury.Configuration.LazyLoadingEnabled = false;
            var articles = Treasury.cmpBranches.Where(WhereClause);
            return ClsPaging.PagedResult<cmpBranch, IQueryable<cmpBranch>>(articles, pageNum, pageSize, orderByFieldName, isAscendingOrder, out totalRows , orderByFieldType);
        }

        private string  ValidateObject(string messageName , cmpBranch BranchObj)
        {
            string err = messageName;

            if(BranchObj==null)
            {
                err = "error_nullobject";
                return err;
            }

            if (BranchObj.cmpBranchAddress.Trim() == "" || BranchObj.cmpBranchAddress == null)
            {
                err = "error_emptydata_BranchAddress";
                return err;
            }
            if (BranchObj.cmpBranchName.Trim() == "" || BranchObj.cmpBranchName == null)
            {
                err = "error_emptydata_BranchName";
                return err;
            }
             return err;

        }
    }
}
