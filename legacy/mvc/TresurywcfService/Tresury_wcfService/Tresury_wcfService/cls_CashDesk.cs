using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Tresury_wcfService
{
    public class cls_CashDesk : Icls_CashDesk
    {
        TreasuryEntities1 Treasury = new TreasuryEntities1();

        public bool Insert(CashDesk CashDeskObj, out string Output_Message)
        {

            Output_Message = "insert_success";

            string validated_messageName = ValidateObject(Output_Message, CashDeskObj);
            if (Output_Message != validated_messageName)
            {
                Output_Message = validated_messageName;
                return false;
            }

            try
            {
                CashDeskObj.CreateDateTime = DateTime.Now;
                Treasury.CashDesks.Add(CashDeskObj);
                Treasury.SaveChanges();
                return true;
            }
            catch
            {
                Output_Message = "insert_error";
                return false;
            }



        }

        public bool Update(CashDesk CashDeskObj, out string Output_Message)
        {

            Output_Message = "update_success";

            string validated_messageName = ValidateObject(Output_Message, CashDeskObj);
            if (Output_Message != validated_messageName)
            {
                Output_Message = validated_messageName;
                return false;
            }

            try
            {
                CashDesk obj = Treasury.CashDesks.First(p => p.CashDeskID == CashDeskObj.CashDeskID);
                obj.cmpBranchID = CashDeskObj.cmpBranchID;
                obj.CashDeskDescription = CashDeskObj.CashDeskDescription;
                obj.CashDeskDescription2 = CashDeskObj.CashDeskDescription2;
                obj.Account_No = CashDeskObj.Account_No;
                obj.Unique_Account_No = CashDeskObj.Unique_Account_No;
                obj.Unique_Account_No2 = CashDeskObj.Unique_Account_No2;
                obj.IsActive = CashDeskObj.IsActive;

                Treasury.SaveChanges();
                return true;
            }
            catch
            {
                Output_Message = "update_error";
                return false;
            }



        }

        public bool Delete(int CashDeskID, out string Output_Message)
        {

            Output_Message = "delete_success";
            try
            {
                CashDesk obj = Treasury.CashDesks.First(p => p.CashDeskID == CashDeskID);
                Treasury.CashDesks.Remove(obj);
                Treasury.SaveChanges();
                return true;
            }
            catch
            {
                Output_Message = "delete_error";
                return false;
            }



        }

        public IQueryable<CashDesk> SelectAll(int pageNum, int pageSize, string orderByFieldName, string orderByFieldType, bool isAscendingOrder, out int totalRows)
        {
            Treasury.Configuration.ProxyCreationEnabled = false;
            Treasury.Configuration.LazyLoadingEnabled = false;
            IQueryable<CashDesk> articles = from obj in Treasury.CashDesks select obj;
            return ClsPaging.PagedResult<CashDesk, IQueryable<CashDesk>>(articles, pageNum, pageSize, orderByFieldName, isAscendingOrder, out totalRows, orderByFieldType);
        }

        public IQueryable<CashDesk> SelectRow(int CashDeskID)
        {
            Treasury.Configuration.ProxyCreationEnabled = false;
            Treasury.Configuration.LazyLoadingEnabled = false;
            IQueryable<CashDesk> articles = Treasury.CashDesks.Where(p => p.CashDeskID == CashDeskID);
            return articles;
        }

        public IQueryable<CashDesk> SelectBy_DynamicWhereClause(string WhereClause, int pageNum, int pageSize, string orderByFieldName, string orderByFieldType, bool isAscendingOrder, out int totalRows)
        {
            Treasury.Configuration.ProxyCreationEnabled = false;
            Treasury.Configuration.LazyLoadingEnabled = false;
            IQueryable<CashDesk> articles = Treasury.CashDesks.Where(WhereClause);
            return ClsPaging.PagedResult<CashDesk, IQueryable<CashDesk>>(articles, pageNum, pageSize, orderByFieldName, isAscendingOrder, out totalRows, orderByFieldType);
        }

        private string ValidateObject(string messageName, CashDesk CashDeskObj)
        {
            string err = messageName;

            if (CashDeskObj == null)
            {
                err = "error_nullobject";
                return err;
            }

            else if (CashDeskObj.cmpBranchID.ToString().Trim() == "0")
            {
                err = "error_emptydata_cmpBranchID";
                return err;
            }
            else if (CashDeskObj.CashDeskDescription.Trim() == "" || CashDeskObj.CashDeskDescription == null)
            {
                err = "error_emptydata_CashDeskDescription";
                return err;
            }
            else if (CashDeskObj.Account_No.Trim() == "" || CashDeskObj.Account_No == null)
            {
                err = "error_emptydata_Account_No";
                return err;
            }
 


            return err;

        }
    }
}
