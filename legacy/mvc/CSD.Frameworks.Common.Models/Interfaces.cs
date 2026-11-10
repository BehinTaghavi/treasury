using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CSD.Common.Models
{
  
    /// <summary>
    /// نمونه توابع جهت دسترسی به یک نمونه جدول در بانک
    /// </summary>
    /// <typeparam name="D"> DataContext</typeparam>
    /// <typeparam name="T"> Model</typeparam>
    /// <typeparam name="S"> SearchModel</typeparam>
    public interface IModelDAL<D, T,S>
        where T : class
        where D : class
        where S : class
    {
        #region GET
        T Get(object Id);
        T Get(D dc, object Id);
        #endregion

        #region Find
        IQueryable<T> GetAll();
        IQueryable<T> Find(D dc, S model);
        IQueryable<T> Find(S model);
        #endregion

        #region Add
        T Add(T model);
        T Add(D dc, T model);
        #endregion

        #region Update
        T Update(T model, bool InsertIfNotExist = false);
        T Update(D dc, T model, bool InsertIfNotExist = false);
        #endregion

        #region Delete
        bool Delete(T model);
        bool Delete(D dc, T model);
        #endregion
    }
}
