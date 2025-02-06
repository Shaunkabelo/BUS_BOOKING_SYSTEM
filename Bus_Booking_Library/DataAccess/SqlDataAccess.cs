using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bus_Booking_Library.DataAccess
{
    public class SqlDataAccess:ISqlDataAccess
    {
        private readonly IConfiguration _config;

        public SqlDataAccess(IConfiguration config)
        {
            _config = config;
        }

        public async Task<IEnumerable<T>> GetData<T, P>(string spName, P parameters, string connectionId = "conn")
        {
            using IDbConnection connection = new SqlConnection(_config.GetConnectionString(connectionId));
            return await connection.QueryAsync<T>(spName, parameters, commandType: CommandType.StoredProcedure);
        }

        public async Task SaveData<T>(string spName, T parameters, string connectionId = "conn")
        {
            using IDbConnection connection = new SqlConnection(_config.GetConnectionString(connectionId));
            await connection.ExecuteAsync(spName, parameters, commandType: CommandType.StoredProcedure);
        }










        // [LOGIN]  Implementing the QueryFirstOrDefaultAsync method 
        public async Task<T> QueryFirstOrDefaultAsync<T>(string query, object parameters = null, string connectionId = "conn")
        {
            using IDbConnection connection = new SqlConnection(_config.GetConnectionString(connectionId));
            return await connection.QueryFirstOrDefaultAsync<T>(query, parameters);
        }





        // Implementing NEW Methods for CRUD Operations
        public async Task<IEnumerable<T>> LoadData<T, U>(string sql, U parameters, string connectionId = "conn")
        {
            using (var connection = new SqlConnection(_config.GetConnectionString(connectionId)))
            {
                await connection.OpenAsync();
                return await connection.QueryAsync<T>(sql, parameters, commandType: CommandType.Text);
            }
        }

        // Method to execute a SQL query (e.g., INSERT, UPDATE, DELETE)
        public async Task<int> Save<T>(string sql, T parameters, string connectionId = "conn")
        {
            using (var connection = new SqlConnection(_config.GetConnectionString(connectionId)))
            {
                await connection.OpenAsync();
                Console.WriteLine($"Opened connection to {connectionId}");
                Console.WriteLine($"Executing SQL: {sql}");


                return await connection.ExecuteAsync(sql, parameters, commandType: CommandType.Text);
            }
        }


        // Method to execute a SQL query (e.g., INSERT, UPDATE, DELETE)
        public async Task<int> Savedata<T>(string sql, T parameters, string connectionId = "conn")
        {
            using (var connection = new SqlConnection(_config.GetConnectionString(connectionId)))
            {
                await connection.OpenAsync();
                return await connection.ExecuteAsync(sql, parameters, commandType: CommandType.Text);
            }
        }

    }
}
