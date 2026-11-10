using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Tresury_wcfService
{
    [ServiceContract]
    public interface Icls_Locations
    {
        [OperationContract]
        bool Insert(Location LocationsObj, out string Output_Message);

        [OperationContract]
        bool Update(Location LocationsObj, out string Output_Message);

        [OperationContract]
        bool Delete(int LocationID, out string Output_Message);

        [OperationContract]
        IQueryable<Location> SelectAll(int pageNum, int pageSize, string orderByFieldName, string orderByFieldType, bool isAscendingOrder, out int totalRows);

        [OperationContract]
        IQueryable<Location> SelectRow(int LocationID);

        [OperationContract]
        IQueryable<Location> SelectBy_DynamicWhereClause(string WhereClause, int pageNum, int pageSize, string orderByFieldName, string orderByFieldType, bool isAscendingOrder, out int totalRows);

    }
}
