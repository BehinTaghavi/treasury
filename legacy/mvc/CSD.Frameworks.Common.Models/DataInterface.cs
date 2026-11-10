using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;

namespace CSD.Common.Models
{
    public interface IDataService<T,KeyType>
    {
        void initLoadOptions(bool loadRelatedData=false);
        IQueryable<T> GetAll(Expression<Func<T, bool>> expression = null );

        int GetCount(Expression<Func<T, bool>> expression = null);
        bool Exists(Expression<Func<T, bool>> expression = null);
        T Create(T obj);
        T Get(KeyType id, bool loadRelatedData = false);
        T Edit(T obj);
        void Delete(int id);
    }
}
