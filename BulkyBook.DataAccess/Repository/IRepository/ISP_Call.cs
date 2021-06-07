using Dapper;
using System;
using System.Collections.Generic;
using System.Text;

namespace BulkyBook.DataAccess.Repository.IRepository
{
    public interface ISP_Call : IDisposable
    {
        T Single<T>(string procedureName, DynamicParameters param = null); // return single value like count or single value, we will use scaler that returns a single or boolean value

        void Execute(string procedureName, DynamicParameters param = null); // to execute something to the database like adding or deleting but no retrieving 

        T OneRecord<T>(string procedureName, DynamicParameters param = null); //to retrieve one comlete row 

        IEnumerable<T> List<T>(string procedureName, DynamicParameters param = null);// to retrieve all rows

        Tuple<IEnumerable<T1>, IEnumerable<T2>> List<T1, T2>(string procedureName, DynamicParameters param = null);//to return 2 tables
    }
}
