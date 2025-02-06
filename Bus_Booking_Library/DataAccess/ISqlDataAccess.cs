using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bus_Booking_Library.DataAccess
{
    public interface ISqlDataAccess
    {
        Task<IEnumerable<T>> GetData<T, P>(string spName, P parameters, string connectionId = "conn");
        Task SaveData<T>(string spName, T parameters, string connectionId = "conn");


        //LOGIN QUERY
        Task<T> QueryFirstOrDefaultAsync<T>(string query, object parameters = null, string connectionId = "conn");


        //NEW METHODS FOR LOADING SQL QUERY DIRECTLY INTO VS
        Task<IEnumerable<T>> LoadData<T, U>(string sql, U parameters, string connectionId = "conn");
        Task<int> Save<T>(string sql, T parameters, string connectionId = "conn");


    }
}
