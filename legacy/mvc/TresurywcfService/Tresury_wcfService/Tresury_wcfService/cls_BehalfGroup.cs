using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Tresury_wcfService
{
    public class cls_BehalfGroup : Icls_BehalfGroup
    {
        TreasuryEntities1 Treasury = new TreasuryEntities1();

        public bool Insert(BehalfGroup BehalfGroupObj, out string Output_Message)
        {

            Output_Message = "insert_success";

            string validated_messageName = ValidateObject(Output_Message, BehalfGroupObj);
            if (Output_Message != validated_messageName)
            {
                Output_Message = validated_messageName;
                return false;
            }

            try
            {
                BehalfGroupObj.CreateDateTime = DateTime.Now;
                Treasury.BehalfGroups.Add(BehalfGroupObj);
                Treasury.SaveChanges();
                return true;
            }
            catch
            {
                Output_Message = "insert_error";
                return false;
            }



        }

        public bool Update(BehalfGroup BehalfGroupObj, out string Output_Message)
        {

            Output_Message = "update_success";

            string validated_messageName = ValidateObject(Output_Message, BehalfGroupObj);
            if (Output_Message != validated_messageName)
            {
                Output_Message = validated_messageName;
                return false;
            }

            try
            {
                BehalfGroup obj = Treasury.BehalfGroups.First(p => p.BehalfGroupID == BehalfGroupObj.BehalfGroupID);
                obj.Receipt_Pay_TypeID = BehalfGroupObj.Receipt_Pay_TypeID;
                obj.BehalfGroupDescription = BehalfGroupObj.BehalfGroupDescription;
                obj.BehalfGroupDescription2 = BehalfGroupObj.BehalfGroupDescription2;
                obj.BehalfGroup_Account = BehalfGroupObj.BehalfGroup_Account;


                Treasury.SaveChanges();
                return true;
            }
            catch
            {
                Output_Message = "update_error";
                return false;
            }



        }

        public bool Delete(int BehalfGroupID, out string Output_Message)
        {

            Output_Message = "delete_success";
            try
            {
                BehalfGroup obj = Treasury.BehalfGroups.First(p => p.BehalfGroupID == BehalfGroupID);
                Treasury.BehalfGroups.Remove(obj);
                Treasury.SaveChanges();
                return true;
            }
            catch
            {
                Output_Message = "delete_error";
                return false;
            }



        }

        public IQueryable<BehalfGroup> SelectAll(int pageNum, int pageSize, string orderByFieldName, string orderByFieldType, bool isAscendingOrder, out int totalRows)
        {
            Treasury.Configuration.ProxyCreationEnabled = false;
            Treasury.Configuration.LazyLoadingEnabled = false;
            IQueryable<BehalfGroup> articles = from obj in Treasury.BehalfGroups select obj;
            return ClsPaging.PagedResult<BehalfGroup, IQueryable<BehalfGroup>>(articles, pageNum, pageSize, orderByFieldName, isAscendingOrder, out totalRows, orderByFieldType);
        }

        public IQueryable<BehalfGroup> SelectRow(int BehalfGroupID)
        {
            Treasury.Configuration.ProxyCreationEnabled = false;
            Treasury.Configuration.LazyLoadingEnabled = false;
            IQueryable<BehalfGroup> articles = Treasury.BehalfGroups.Where(p => p.BehalfGroupID == BehalfGroupID);
            return articles;
        }

        public IQueryable<BehalfGroup> SelectBy_DynamicWhereClause(string WhereClause, int pageNum, int pageSize, string orderByFieldName, string orderByFieldType, bool isAscendingOrder, out int totalRows)
        {
            Treasury.Configuration.ProxyCreationEnabled = false;
            Treasury.Configuration.LazyLoadingEnabled = false;
            IQueryable<BehalfGroup> articles = Treasury.BehalfGroups.Where(WhereClause);
            return ClsPaging.PagedResult<BehalfGroup, IQueryable<BehalfGroup>>(articles, pageNum, pageSize, orderByFieldName, isAscendingOrder, out totalRows, orderByFieldType);
        }

        private string ValidateObject(string messageName, BehalfGroup BehalfGroupObj)
        {
            string err = messageName;

            if (BehalfGroupObj == null)
            {
                err = "error_nullobject";
                return err;
            }

            else if (BehalfGroupObj.Receipt_Pay_TypeID.ToString().Trim() == "0")
            {
                err = "error_emptydata_Receipt_Pay_TypeID";
                return err;
            }
            else if (BehalfGroupObj.BehalfGroupDescription.Trim() == "" || BehalfGroupObj.BehalfGroupDescription == null)
            {
                err = "error_emptydata_BehalfGroupDescription";
                return err;
            }


            return err;

        }
    }
}
