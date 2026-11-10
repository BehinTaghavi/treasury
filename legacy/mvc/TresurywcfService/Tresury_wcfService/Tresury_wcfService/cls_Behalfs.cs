using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Tresury_wcfService
{
    public class cls_Behalfs : Icls_Behalfs
    {
        TreasuryEntities1 Treasury = new TreasuryEntities1();

        public bool Insert(Behalf BehalfObj, out string Output_Message)
        {

            Output_Message = "insert_success";

            string validated_messageName = ValidateObject(Output_Message, BehalfObj);
            if (Output_Message != validated_messageName)
            {
                Output_Message = validated_messageName;
                return false;
            }

            try
            {
                BehalfObj.CreateDateTime = DateTime.Now;
                Treasury.Behalfs.Add(BehalfObj);
                Treasury.SaveChanges();
                return true;
            }
            catch
            {
                Output_Message = "insert_error";
                return false;
            }
 
        }

        public bool Update(Behalf BehalfObj, out string Output_Message)
        {

            Output_Message = "update_success";

            string validated_messageName = ValidateObject(Output_Message, BehalfObj);
            if (Output_Message != validated_messageName)
            {
                Output_Message = validated_messageName;
                return false;
            }

            try
            {
                Behalf obj = Treasury.Behalfs.First(p => p.BehalfID == BehalfObj.BehalfID);
                obj.BehalfGroupID = BehalfObj.BehalfGroupID;
                obj.BehalfDescription = BehalfObj.BehalfDescription.Trim();
                obj.BehalfDescription2 = BehalfObj.BehalfDescription2;

                Treasury.SaveChanges();
                return true;
            }
            catch
            {
                Output_Message = "update_error";
                return false;
            }



        }

        public bool Delete(int BehalfID, out string Output_Message)
        {

            Output_Message = "delete_success";
            try
            {
                Behalf obj = Treasury.Behalfs.First(p => p.BehalfID == BehalfID);
                Treasury.Behalfs.Remove(obj);
                Treasury.SaveChanges();
                return true;
            }
            catch
            {
                Output_Message = "delete_error";
                return false;
            }



        }

        public IQueryable<Behalf> SelectAll(int pageNum, int pageSize, string orderByFieldName, string orderByFieldType, bool isAscendingOrder, out int totalRows)
        {
            Treasury.Configuration.ProxyCreationEnabled = false;
            Treasury.Configuration.LazyLoadingEnabled = false;
            IQueryable<Behalf> articles = from obj in Treasury.Behalfs select obj;
            return ClsPaging.PagedResult<Behalf, IQueryable<Behalf>>(articles, pageNum, pageSize, orderByFieldName, isAscendingOrder, out totalRows, orderByFieldType);
        }

        public IQueryable<Behalf> SelectRow(int BehalfID)
        {
            Treasury.Configuration.ProxyCreationEnabled = false;
            Treasury.Configuration.LazyLoadingEnabled = false;
            IQueryable<Behalf> articles = Treasury.Behalfs.Where(p => p.BehalfID == BehalfID);
            return articles;
        }

        public IQueryable<Behalf> SelectBy_DynamicWhereClause(string WhereClause, int pageNum, int pageSize, string orderByFieldName, string orderByFieldType, bool isAscendingOrder, out int totalRows)
        {
            Treasury.Configuration.ProxyCreationEnabled = false;
            Treasury.Configuration.LazyLoadingEnabled = false;
            IQueryable<Behalf> articles = Treasury.Behalfs.Where(WhereClause);
            return ClsPaging.PagedResult<Behalf, IQueryable<Behalf>>(articles, pageNum, pageSize, orderByFieldName, isAscendingOrder, out totalRows, orderByFieldType);
        }

        private string ValidateObject(string messageName, Behalf BehalfObj)
        {
            string err = messageName;

            if (BehalfObj == null)
            {
                err = "error_nullobject";
                return err;
            }

            else if (BehalfObj.BehalfGroupID.ToString().Trim() == "0")
            {
                err = "error_emptydata_BehalfGroupID";
                return err;
            }
            else if (BehalfObj.BehalfDescription.Trim() == "" || BehalfObj.BehalfDescription == null)
            {
                err = "error_emptydata_BehalfDescription";
                return err;
            }
 

            return err;

        }
    }
}
