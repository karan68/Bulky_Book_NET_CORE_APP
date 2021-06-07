using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace BulkyBook.DataAccess.Repository.IRepository
{
    public interface IRepository<T> where T : class //it will be a generic repo, and since it is generic
                                                    //we don't know the type of object so we use T and tell
                                                    //where T is  A class
    {
        T Get(int id); // to retrive a category from database
        /*
       The code Expression<Func<TEntity, bool>> filter means the caller will provide a lambda expression based on the TEntity type,
      and this expression will return a Boolean value. For example, if the repository is instantiated for the book entity type,
      the code in the calling method might specify book => book.Name == "xyz" for the filter parameter.

      The code Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy also means the caller will provide a lambda expression. 
      But in this case, the input to the expression is an IQueryable object for the TEntity type. 
      The expression will return an ordered version of that IQueryable object. For example, if the repository is instantiated for the Student entity type,
      the code in the calling method might specify q => q.OrderBy(s => s.Name) for the orderBy parameter.

       */
        IEnumerable<T> GetAll(
            Expression<Func<T, bool>> filter = null,
            Func<IQueryable<T>, IOrderedQueryable<T>> orderBy = null,// this line is for retrieviing a list of values and oderin is set to null
            string includeProperties = null //it is used when we deal with foreign keys
            );

        T GetFirstOrDefault(
            Expression<Func<T, bool>> filter = null, //in case of this it will only show one value so no use of line no 16 done here
            string includeProperties = null
            );

        void Add(T entity);
        void Remove(int id);
        void Remove(T entity);
        void RemoveRange(IEnumerable<T> entity);


    }
}
