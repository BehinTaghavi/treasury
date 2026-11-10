using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Tresury_wcfService
{

    public class cls_Receipt_Pay_TypeDetail : Icls_Receipt_Pay_TypeDetail
    {
        TreasuryEntities1 Treasury = new TreasuryEntities1();

        public bool Insert(Receipt_Pay_TypeDetail Receipt_Pay_TypeDetailObj, out string Output_Message)
        {

            Output_Message = "insert_success";

            string validated_messageName = ValidateObject(Output_Message, Receipt_Pay_TypeDetailObj);
            if (Output_Message != validated_messageName)
            {
                Output_Message = validated_messageName;
                return false;
            }

            try
            {
 
                Treasury.Receipt_Pay_TypeDetail.Add(Receipt_Pay_TypeDetailObj);
                Treasury.SaveChanges();
                return true;
            }
            catch
            {
                Output_Message = "insert_error";
                return false;
            }



        }

        public bool Update(Receipt_Pay_TypeDetail Receipt_Pay_TypeDetailObj, out string Output_Message)
        {

            Output_Message = "update_success";

            string validated_messageName = ValidateObject(Output_Message, Receipt_Pay_TypeDetailObj);
            if (Output_Message != validated_messageName)
            {
                Output_Message = validated_messageName;
                return false;
            }

            try
            {
                Receipt_Pay_TypeDetail obj = Treasury.Receipt_Pay_TypeDetail.First(p => p.Receipt_Pay_TypeDetailID == Receipt_Pay_TypeDetailObj.Receipt_Pay_TypeDetailID);
                obj.Receipt_Pay_TypeID = Receipt_Pay_TypeDetailObj.Receipt_Pay_TypeID;
                obj.Receipt_Pay_TypeDetailDescription = Receipt_Pay_TypeDetailObj.Receipt_Pay_TypeDetailDescription;
                obj.Receipt_Pay_TypeDetailDescription2 = Receipt_Pay_TypeDetailObj.Receipt_Pay_TypeDetailDescription2;
                obj.Has_Source = Receipt_Pay_TypeDetailObj.Has_Source;
                obj.Source_TypeID = Receipt_Pay_TypeDetailObj.Source_TypeID;
                obj.Has_Destination = Receipt_Pay_TypeDetailObj.Has_Destination;
                obj.DestinationTypeID = Receipt_Pay_TypeDetailObj.DestinationTypeID;
                obj.Has_due_date = Receipt_Pay_TypeDetailObj.Has_due_date;

                Treasury.SaveChanges();
                return true;
            }
            catch
            {
                Output_Message = "update_error";
                return false;
            }



        }

        public bool Delete(int Receipt_Pay_TypeDetailID, out string Output_Message)
        {

            Output_Message = "delete_success";
            try
            {
                Receipt_Pay_TypeDetail obj = Treasury.Receipt_Pay_TypeDetail.First(p => p.Receipt_Pay_TypeDetailID == Receipt_Pay_TypeDetailID);
                Treasury.Receipt_Pay_TypeDetail.Remove(obj);
                Treasury.SaveChanges();
                return true;
            }
            catch
            {
                Output_Message = "delete_error";
                return false;
            }



        }

        public IQueryable<Receipt_Pay_TypeDetail> SelectAll(int pageNum, int pageSize, string orderByFieldName, string orderByFieldType, bool isAscendingOrder, out int totalRows)
        {
            Treasury.Configuration.ProxyCreationEnabled = false;
            Treasury.Configuration.LazyLoadingEnabled = false;
            IQueryable<Receipt_Pay_TypeDetail> articles = from obj in Treasury.Receipt_Pay_TypeDetail select obj;
            return ClsPaging.PagedResult<Receipt_Pay_TypeDetail, IQueryable<Receipt_Pay_TypeDetail>>(articles, pageNum, pageSize, orderByFieldName, isAscendingOrder, out totalRows, orderByFieldType);
        }

        public IQueryable<Receipt_Pay_TypeDetail> SelectRow(int Receipt_Pay_TypeDetailID)
        {
            Treasury.Configuration.ProxyCreationEnabled = false;
            Treasury.Configuration.LazyLoadingEnabled = false;
            IQueryable<Receipt_Pay_TypeDetail> articles = Treasury.Receipt_Pay_TypeDetail.Where(p => p.Receipt_Pay_TypeDetailID == Receipt_Pay_TypeDetailID);
            return articles;
        }

        public IQueryable<Receipt_Pay_TypeDetail> SelectBy_DynamicWhereClause(string WhereClause, int pageNum, int pageSize, string orderByFieldName, string orderByFieldType, bool isAscendingOrder, out int totalRows)
        {
            Treasury.Configuration.ProxyCreationEnabled = false;
            Treasury.Configuration.LazyLoadingEnabled = false;
            IQueryable<Receipt_Pay_TypeDetail> articles = Treasury.Receipt_Pay_TypeDetail.Where(WhereClause);
            return ClsPaging.PagedResult<Receipt_Pay_TypeDetail, IQueryable<Receipt_Pay_TypeDetail>>(articles, pageNum, pageSize, orderByFieldName, isAscendingOrder, out totalRows, orderByFieldType);
        }

        private string ValidateObject(string messageName, Receipt_Pay_TypeDetail Receipt_Pay_TypeDetailObj)
        {
            string err = messageName;

            if (Receipt_Pay_TypeDetailObj == null)
            {
                err = "error_nullobject";
                return err;
            }

            else if (Receipt_Pay_TypeDetailObj.Receipt_Pay_TypeID.ToString().Trim() == "0")
            {
                err = "error_emptydata_Receipt_Pay_TypeID";
                return err;
            }
            else if (Receipt_Pay_TypeDetailObj.Receipt_Pay_TypeDetailDescription.Trim() == "" || Receipt_Pay_TypeDetailObj.Receipt_Pay_TypeDetailDescription == null)
            {
                err = "error_emptydata_Receipt_Pay_TypeDetailDescription";
                return err;
            }
            else if (Receipt_Pay_TypeDetailObj.Has_Source == true && (Receipt_Pay_TypeDetailObj.Source_TypeID == null || Receipt_Pay_TypeDetailObj.Source_TypeID == 0 ) )
            {
                err = "error_emptydata_Source_TypeID";
                return err;
            }
            else if (Receipt_Pay_TypeDetailObj.Has_Source == false && (Receipt_Pay_TypeDetailObj.Source_TypeID != null || Receipt_Pay_TypeDetailObj.Source_TypeID != 0))
            {
                err = "error_ShouldBeClean_Source_TypeID";
                return err;
            }
            else if (Receipt_Pay_TypeDetailObj.Has_Destination == true && (Receipt_Pay_TypeDetailObj.DestinationTypeID == null || Receipt_Pay_TypeDetailObj.DestinationTypeID == 0))
            {
                err = "error_emptydata_DestinationTypeID";
                return err;
            }
            else if (Receipt_Pay_TypeDetailObj.Has_Destination == false && (Receipt_Pay_TypeDetailObj.DestinationTypeID != null || Receipt_Pay_TypeDetailObj.DestinationTypeID != 0))
            {
                err = "error_ShouldBeClean_DestinationTypeID";
                return err;
            }


            return err;

        }
    }
}
