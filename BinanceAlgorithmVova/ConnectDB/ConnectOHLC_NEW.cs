using BinanceAlgorithmVova.Objects;
using Dapper;
using Dapper.Contrib.Extensions;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

namespace BinanceAlgorithmVova.ConnectDB
{
    public static class ConnectOHLC_NEW
    {
        public static string connectionString = @"Data Source=.\SQLEXPRESS;Initial Catalog=OHLC_NEWs;Integrated Security=True;Connect Timeout=30;Encrypt=False;TrustServerCertificate=False;ApplicationIntent=ReadWrite;MultiSubnetFailover=False";
        public static long Insert(OHLC_NEW ohlc)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                return connection.Insert(ohlc);
            }
        }
        public static List<OHLC_NEW> Get()
        {
            using (IDbConnection connection = new SqlConnection(connectionString))
            {
                return connection.Query<OHLC_NEW>($"SELECT * FROM OHLC_NEWs").ToList();
            }
        }
        public static void DeleteAll()
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.DeleteAll<OHLC_NEW>();
            }
        }
        public static bool Update(OHLC_NEW ohlc)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                return connection.Update(ohlc);
            }
        }
        public static void Delete(int id)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Delete(new OHLC_NEW() { Id = id });
            }
        }
    }
}
