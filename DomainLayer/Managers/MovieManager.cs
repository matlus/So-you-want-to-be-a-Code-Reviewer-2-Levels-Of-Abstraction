using DomainLayer.Managers.ConfigurationProviders;
using DomainLayer.Managers.DataLayer;
using DomainLayer.Managers.Enums;
using DomainLayer.Managers.Exceptions;
using DomainLayer.Managers.Models;
using DomainLayer.Managers.Parsers;
using DomainLayer.Managers.ServiceLocators;
using DomainLayer.Managers.Services.ImdbService;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;

namespace DomainLayer.Managers
{
    internal sealed class MovieManager : IDisposable
    {
        private bool _disposed;
        private readonly ServiceLocatorBase _serviceLocator;
        private readonly SqlClientFactory _sqlClientFactory = SqlClientFactory.Instance;

        private ConfigurationProviderBase _configurationProvider;
        private ConfigurationProviderBase ConfigurationProvider { get { return _configurationProvider ?? (_configurationProvider = _serviceLocator.CreateConfigurationProvider()); } }

        private ImdbServiceGateway _imdbServiceGateway;
        private ImdbServiceGateway ImdbServiceGateway
        {
            get
            {
                return _imdbServiceGateway ?? (_imdbServiceGateway = _serviceLocator.CreateImdbServiceGateway(ConfigurationProvider.GetImdbServiceBaseUrl()));
            }
        }

        private DataFacade _dataFacade;
        private DataFacade DataFacade { get { return _dataFacade ?? (_dataFacade = new DataFacade()); } }

        public MovieManager(ServiceLocatorBase serviceLocator)
        {
            _serviceLocator = serviceLocator;
        }

        private DbConnection CreateDbConnection()
        {
            var dbConnection = _sqlClientFactory.CreateConnection();
            dbConnection.ConnectionString = @"Data Source=(localdb)\ProjectsV13;Initial Catalog=MovieDb;Integrated Security=True;TrustServerCertificate=True;";
            return dbConnection;
        }

        public async Task CreateMovie(Movie movie)
        {
            if (movie == null)
            {
                throw new InvalidMovieException("The movie parameter can not be null.");
            }

            var genreErrorMessage = ValidateGenre(movie.Genre);
            var titleErrorMessage = ValidateProperty("Title", movie.Title);
            var imageUrlErrorMessage = ValidateProperty("ImageUrl", movie.ImageUrl);
            var yearErrorMessage = ValidateYear(movie.Year);

            if (genreErrorMessage != null || titleErrorMessage != null || imageUrlErrorMessage != null || yearErrorMessage != null)
            {
                throw new InvalidMovieException($"{genreErrorMessage} \r\n {titleErrorMessage} \r\n {imageUrlErrorMessage} \r\n {yearErrorMessage}");
            }

            var dbConnection = CreateDbConnection();
            DbCommand dbCommand = null;
            DbTransaction dbTransaction = null;
            try
            {
                await dbConnection.OpenAsync().ConfigureAwait(false);
                dbTransaction = dbConnection.BeginTransaction(IsolationLevel.Serializable);
                dbCommand = dbConnection.CreateCommand();
                dbCommand.Transaction = dbTransaction;

                dbCommand.CommandType = CommandType.StoredProcedure;
                dbCommand.CommandText = "dbo.CreateMovie";

                AddDbParameter(dbCommand, "@Title", movie.Title, DbType.String, 50);
                AddDbParameter(dbCommand, "@Genre", GenreParser.ToString(movie.Genre), DbType.String, 50);
                AddDbParameter(dbCommand, "@Year", movie.Year, DbType.Int32, 0);
                AddDbParameter(dbCommand, "@ImageUrl", movie.ImageUrl, DbType.String, 200);

                await dbCommand.ExecuteNonQueryAsync().ConfigureAwait(false);
                dbTransaction.Commit();
            }
            catch (DbException e)
            {
                dbTransaction?.Rollback();

                if (e.Message.Contains("duplicate key row in object 'dbo.Movie'", StringComparison.OrdinalIgnoreCase))
                {
                    throw new DuplicateMovieException($"A Movie with the Title: {movie.Title} already exists. Please use a different title", e);
                }
                else
                {
                    throw;
                }
            }
            finally
            {
                dbCommand?.Dispose();
                dbTransaction?.Dispose();
                dbConnection?.Dispose();
            }
        }

        public async Task<IEnumerable<Movie>> GetAllMovies()
        {
            var moviesTask = ImdbServiceGateway.GetAllMovies();
            var moviesFromDbTask = DataFacade.GetAllMovies();

            await Task.WhenAll(moviesTask, moviesFromDbTask).ConfigureAwait(false);

            var movies = moviesTask.Result;
            var moviesFromDb = moviesFromDbTask.Result;

            var moviesList = movies.ToList();
            moviesList.AddRange(moviesFromDb);
            return moviesList;
        }

        public async Task<IEnumerable<Movie>> GetMoviesByGenre(Genre genre)
        {
            GenreParser.Validate(genre);
            var allMovies = await GetAllMovies().ConfigureAwait(false);
            return allMovies.Where(m => m.Genre == genre);
        }

        private static string ValidateYear(int year)
        {
            const int minimumYear = 1900;

            if (year >= minimumYear && year <= DateTime.Today.Year)
            {
                return null;
            }

            return $"The Year, must be between {minimumYear} and {DateTime.Today.Year} (inclusive)";
        }

        private static string ValidateGenre(Genre genre)
        {
            return GenreParser.ValidationMessage(genre);
        }

        private static string ValidateProperty(string propertyName, string propertyValue)
        {
            switch (DetermineNullEmptyOrWhiteSpaces(propertyValue))
            {
                case StringState.Null:
                    return $"The Movie {propertyName} must be a valid {propertyName} and can not be null";
                case StringState.Empty:
                    return $"The Movie {propertyName} must be a valid {propertyName} and can not be Empty";
                case StringState.WhiteSpaces:
                    return $"The Movie {propertyName} must be a valid {propertyName} and can not be Whitespaces";
                case StringState.Valid:
                default:
                    return null;
            }
        }

        internal enum StringState { Null, Empty, Valid, WhiteSpaces }
        private static StringState DetermineNullEmptyOrWhiteSpaces(string data)
        {
            if (data == null)
            {
                return StringState.Null;
            }
            else if (data.Length == 0)
            {
                return StringState.Empty;
            }

            foreach (var chr in data)
            {
                if (!char.IsWhiteSpace(chr))
                {
                    return StringState.Valid;
                }
            }

            return StringState.WhiteSpaces;
        }

        private static void AddDbParameter(DbCommand dbCommand, string parameterName, object value, DbType dbType, int size)
        {
            var dbParameter = dbCommand.CreateParameter();
            dbParameter.ParameterName = parameterName;
            dbParameter.Value = value;
            dbParameter.DbType = dbType;
            dbParameter.Size = size;
            dbCommand.Parameters.Add(dbParameter);
        }

        [ExcludeFromCodeCoverage]
        private void Dispose(bool disposing)
        {
            if (disposing && !_disposed)
            {
                _imdbServiceGateway?.Dispose();
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
    }
}
