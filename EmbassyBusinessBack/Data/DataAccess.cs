using Dapper;
using Microsoft.Data.SqlClient;
using System.Data;

namespace EmbassyBusinessBack.Data
{
    public class DataAccess
    {
        public static string cString;

        public static string ConnectionString()
        {

            var config = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddJsonFile("appsettings.Development.json")
            .Build();

            return config.GetConnectionString("SQLConexion");

        }


       

        public static DataTable performQuery(string query)
        {
            DataTable dtResultado = new DataTable();

            // Usar el bloque using para garantizar que los recursos se liberen adecuadamente
            try
            {
                using (SqlConnection connection = new SqlConnection(ConnectionString()))
                using (SqlDataAdapter sda = new SqlDataAdapter(query, connection))
                {
                    sda.Fill(dtResultado);
                }
            }
            catch (Exception ex)
            {
                // Loguear o manejar la excepción según sea necesario
                dtResultado = new DataTable();
            }

            return dtResultado;
        }

        // Método para ejecutar una consulta SQL y devolver un solo objeto del tipo especificado
        public static T fromQueryObject<T>(string query)
        {
            try
            {
                // No es necesario inicializar la lista antes de sobrescribirla
                using (var connection = new SqlConnection(ConnectionString()))
                {
                    return connection.QueryFirstOrDefault<T>(query);
                }
            }
            catch (Exception ex)
            {
                // Loguear o manejar la excepción según sea necesario
                return default(T);
            }
        }

        // Método para ejecutar una consulta SQL y devolver una lista de objetos del tipo especificado
        public static List<T> fromQueryListOf<T>(string query)
        {
            // Usar el bloque using para garantizar que los recursos se liberen adecuadamente
            using (var connection = new SqlConnection(ConnectionString()))
            {
                return connection.Query<T>(query).ToList();
            }
        }
    }
}
