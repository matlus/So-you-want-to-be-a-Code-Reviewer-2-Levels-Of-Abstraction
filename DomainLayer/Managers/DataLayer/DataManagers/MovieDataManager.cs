using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Threading.Tasks;
using DomainLayer.Managers.Models;
using DomainLayer.Managers.Parsers;

namespace DomainLayer.Managers.DataLayer.DataManagers
{
    internal sealed class MovieDataManager
    {
        private readonly SqlClientFactory sqlClientFactory = SqlClientFactory.Instance;

        private DbConnection CreateDbConnection()
        {
            var dbConnection = sqlClientFactory.CreateConnection();
            dbConnection.ConnectionString = @"Data Source=(localdb)\ProjectsV13;Initial Catalog=MovieDb;Integrated Security=True;TrustServerCertificate=True;";
            return dbConnection;
        }

        public async Task<IEnumerable<Movie>> GetAllMovies()
        {
            var dbConnection = CreateDbConnection();
            DbCommand dbCommand = null;
            DbDataReader dbDataReader = null;
            try
            {
                await dbConnection.OpenAsync().ConfigureAwait(false);
                dbCommand = dbConnection.CreateCommand();
                dbCommand.CommandType = CommandType.StoredProcedure;
                dbCommand.CommandText = "dbo.GetAllMovies";
                dbDataReader = await dbCommand.ExecuteReaderAsync().ConfigureAwait(false);
                return MapToMovies(dbDataReader);
            }
            finally
            {
                dbDataReader?.Dispose();
                dbCommand?.Dispose();
                dbConnection.Dispose();
            }
        }

        private static IEnumerable<Movie> MapToMovies(DbDataReader dbDataReader)
        {
            var movies = new List<Movie>();

            while (dbDataReader.Read())
            {
                movies.Add(new Movie(
                    title: (string)dbDataReader[0],
                    genre: GenreParser.Parse((string)dbDataReader[1]),
                    year: (int)dbDataReader[2],
                    imageUrl: (string)dbDataReader[3]));
            }

            return movies;
        }
    }
}
