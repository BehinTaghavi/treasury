using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Tresury_wcfService
{

    public class cls_Receipt_Pay_Type : Icls_Receipt_Pay_Type
    {
        TreasuryEntities1 Treasury = new TreasuryEntities1();

        public bool Insert(Receipt_Pay_Type Receipt_Pay_TypeObj, out string Output_Message)
        {

            Output_Message = "insert_success";

            string validated_messageName = ValidateObject(Output_Message, Receipt_Pay_TypeObj);
            if (Output_Message != validated_messageName)
            {
                Output_Message = validated_messageName;
                return false;
            }

            try
            {
 
                Treasury.Receipt_Pay_Type.Add(Receipt_Pay_TypeObj);
                Treasury.SaveChanges();
                return true;
            }
            catch
            {
                Output_Message = "insert_error";
                return false;
            }



        }

        public bool Update(Receipt_Pay_Type Receipt_Pay_TypeObj, out string Output_Message)
        {

            Output_Message = "update_success";

            string validated_messageName = ValidateObject(Output_Message, Receipt_Pay_TypeObj);
            if (Output_Message != validated_messageName)
            {
                Output_Message = validated_messageName;
                return false;
            }

            try
            {
                Receipt_Pay_Type obj = Treasury.Receipt_Pay_Type.First(p => p.Receipt_Pay_TypeID == Receipt_Pay_TypeObj.Receipt_Pay_TypeID);
                obj.Receipt_Pay_TypeDescription = Receipt_Pay_TypeObj.Receipt_Pay_TypeDescription;
                obj.Receipt_Pay_TypeDescription2 = Receipt_Pay_TypeObj.Receipt_Pay_TypeDescription2;
                obj.UsedInBehalfs = Receipt_Pay_TypeObj.UsedInBehalfs;
                obj.UsedInPayAndReciepts = Receipt_Pay_TypeObj.UsedInPayAndReciepts;
 


                Treasury.SaveChanges();
                return true;
            }
            catch
            {
                Output_Message = "update_error";
                return false;
            }



        }

        public bool Delete(int Receipt_Pay_TypeID, out string Output_Message)
        {

            Output_Message = "delete_success";
            try
            {
                Receipt_Pay_Type obj = Treasury.Receipt_Pay_Type.First(p => p.Receipt_Pay_TypeID == Receipt_Pay_TypeID);
                Treasury.Receipt_Pay_Type.Remove(obj);
                Treasury.SaveChanges();
                return true;
            }
            catch
            {
                Output_Message = "delete_error";
                return false;
            }



        }

        public IQueryable<Receipt_Pay_Type> SelectAll(int pageNum, int pageSize, string orderByFieldName, string orderByFieldType, bool isAscendingOrder, out int totalRows)
        {
            Treasury.Configuration.ProxyCreationEnabled = false;
            Treasury.Configuration.LazyLoadingEnabled = false;
            IQueryable<Receipt_Pay_Type> articles = from obj in Treasury.Receipt_Pay_Type select obj;
            return ClsPaging.PagedResult<Receipt_Pay_Type, IQueryable<Receipt_Pay_Type>>(articles, pageNum, pageSize, orderByFieldName, isAscendingOrder, out totalRows, orderByFieldType);
        }

        public IQueryable<Receipt_Pay_Type> SelectRow(int Receipt_Pay_TypeID)
        {
            Treasury.Configuration.ProxyCreationEnabled = false;
            Treasury.Configuration.LazyLoadingEnabled = false;
            IQueryable<Receipt_Pay_Type> articles = Treasury.Receipt_Pay_Type.Where(p => p.Receipt_Pay_TypeID == Receipt_Pay_TypeID);
            return articles;
        }

        public IQueryable<Receipt_Pay_Type> SelectBy_DynamicWhereClause(string WhereClause, int pageNum, int pageSize, string orderByFieldName, string orderByFieldType, bool isAscendingOrder, out int totalRows)
        {
            Treasury.Configuration.ProxyCreationEnabled = false;
            Treasury.Configuration.LazyLoadingEnabled = false;
            IQueryable<Receipt_Pay_Type> articles = Treasury.Receipt_Pay_Type.Where(WhereClause);
            return ClsPaging.PagedResult<Receipt_Pay_Type, IQueryable<Receipt_Pay_Type>>(articles, pageNum, pageSize, orderByFieldName, isAscendingOrder, out totalRows, orderByFieldType);
        }

        private string ValidateObject(string messageName, Receipt_Pay_Type Receipt_Pay_TypeObj)
        {
            string err = messageName;

            if (Receipt_Pay_TypeObj == null)
            {
                err = "error_nullobject";
                return err;
            }
 
            else if (Receipt_Pay_TypeObj.Receipt_Pay_TypeDescription.Trim() == "" || Receipt_Pay_TypeObj.Receipt_Pay_TypeDescription == null)
            {
                err = "error_emptydata_Receipt_Pay_TypeDescription";
                return err;
            }
 
            else if (Receipt_Pay_TypeObj.UsedInBehalfs== Receipt_Pay_TypeObj.UsedInPayAndReciepts)
            {
                err = "error_UsedInBehalfs_should_not_be_equeal_to_UsedInPayAndReciepts";
                return err;
            }


            return err;

        }
    }
}
