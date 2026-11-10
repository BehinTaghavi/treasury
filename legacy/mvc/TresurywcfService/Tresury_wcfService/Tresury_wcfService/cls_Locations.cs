using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Tresury_wcfService
{

    public class cls_Locations : Icls_Locations
    {
        TreasuryEntities1 Treasury = new TreasuryEntities1();

        public bool Insert(Location LocationObj, out string Output_Message)
        {

            Output_Message = "insert_success";

            string validated_messageName = ValidateObject(Output_Message, LocationObj);
            if (Output_Message != validated_messageName)
            {
                Output_Message = validated_messageName;
                return false;
            }

            try
            {                 
                Treasury.Locations.Add(LocationObj);
                Treasury.SaveChanges();
                return true;
            }
            catch
            {
                Output_Message = "insert_error";
                return false;
            }



        }

        public bool Update(Location LocationObj, out string Output_Message)
        {

            Output_Message = "update_success";

            string validated_messageName = ValidateObject(Output_Message, LocationObj);
            if (Output_Message != validated_messageName)
            {
                Output_Message = validated_messageName;
                return false;
            }

            try
            {
                Location obj = Treasury.Locations.First(p => p.LocationID == LocationObj.LocationID);
                obj.cmpBranchID = LocationObj.cmpBranchID;
                obj.LocationName = LocationObj.LocationName.Trim();
                obj.LocationName2 = LocationObj.LocationName2;
 

                Treasury.SaveChanges();
                return true;
            }
            catch
            {
                Output_Message = "update_error";
                return false;
            }



        }

        public bool Delete(int LocationID, out string Output_Message)
        {

            Output_Message = "delete_success";
            try
            {
                Location obj = Treasury.Locations.First(p => p.LocationID == LocationID);
                Treasury.Locations.Remove(obj);
                Treasury.SaveChanges();
                return true;
            }
            catch
            {
                Output_Message = "delete_error";
                return false;
            }



        }

        public IQueryable<Location> SelectAll(int pageNum, int pageSize, string orderByFieldName, string orderByFieldType, bool isAscendingOrder, out int totalRows)
        {
            Treasury.Configuration.ProxyCreationEnabled = false;
            Treasury.Configuration.LazyLoadingEnabled = false;
            IQueryable<Location> articles = from obj in Treasury.Locations select obj;
            return ClsPaging.PagedResult<Location, IQueryable<Location>>(articles, pageNum, pageSize, orderByFieldName, isAscendingOrder, out totalRows, orderByFieldType);
        }

        public IQueryable<Location> SelectRow(int LocationID)
        {
            Treasury.Configuration.ProxyCreationEnabled = false;
            Treasury.Configuration.LazyLoadingEnabled = false;
            IQueryable<Location> articles = Treasury.Locations.Where(p => p.LocationID == LocationID);
            return articles;
        }

        public IQueryable<Location> SelectBy_DynamicWhereClause(string WhereClause, int pageNum, int pageSize, string orderByFieldName, string orderByFieldType, bool isAscendingOrder, out int totalRows)
        {
            Treasury.Configuration.ProxyCreationEnabled = false;
            Treasury.Configuration.LazyLoadingEnabled = false;
            IQueryable<Location> articles = Treasury.Locations.Where(WhereClause);
            return ClsPaging.PagedResult<Location, IQueryable<Location>>(articles, pageNum, pageSize, orderByFieldName, isAscendingOrder, out totalRows, orderByFieldType);
        }

        private string ValidateObject(string messageName, Location LocationObj)
        {
            string err = messageName;

            if (LocationObj == null)
            {
                err = "error_nullobject";
                return err;
            }

            else if (LocationObj.cmpBranchID.ToString().Trim() == "0")
            {
                err = "error_emptydata_cmpBranchID";
                return err;
            }
            else if (LocationObj.LocationName.Trim() == "" || LocationObj.LocationName == null)
            {
                err = "error_emptydata_LocationName";
                return err;
            }
 


            return err;

        }
    }
}
