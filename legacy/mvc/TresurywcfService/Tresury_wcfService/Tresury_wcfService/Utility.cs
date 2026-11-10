using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Tresury_wcfService
{
    public static class Utility
    {
        //makes expression for specific prop
        public static Expression<Func<TSource, T>> GetExpression<TSource, T>(string propertyName)
        {
            var param = Expression.Parameter(typeof(TSource), "x");
            //Expression conversion = Expression.Convert(, typeof(object));   //important to use the Expression.Convert
            return (Expression<Func<TSource, T>>)Expression.Lambda(Expression.Property
            (param, propertyName), param);
        }

        //makes deleget for specific prop
        public static Func<TSource, object> GetFunc<TSource>(string propertyName)
        {
            return GetExpression<TSource, object>(propertyName).Compile();  //only need compiled expression
        }

        //OrderBy overload
        public static IOrderedEnumerable<TSource>
        OrderBy<TSource>(this IEnumerable<TSource> source, string propertyName)
        {
            return source.OrderBy(GetFunc<TSource>(propertyName));
        }

        //OrderBy overload
        public static IOrderedQueryable<TSource>
        OrderBy<TSource,T>(this IQueryable<TSource> source, string propertyName)
        {
            return source.OrderBy(GetExpression<TSource,T>(propertyName));
        }



        //OrderBy overload
        public static IOrderedEnumerable<TSource>
        OrderByDescending<TSource>(this IEnumerable<TSource> source, string propertyName)
        {
            return source.OrderByDescending(GetFunc<TSource>(propertyName));
        }

        //OrderBy overload
        public static IOrderedQueryable<TSource>
        OrderByDescending<TSource, T>(this IQueryable<TSource> source, string propertyName)
        {
            return source.OrderByDescending(GetExpression<TSource, T>(propertyName));
        }
    }
}
