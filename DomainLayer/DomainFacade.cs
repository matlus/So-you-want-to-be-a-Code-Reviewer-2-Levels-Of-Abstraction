using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using DomainLayer.Managers;
using DomainLayer.Managers.Enums;
using DomainLayer.Managers.Models;
using DomainLayer.Managers.ServiceLocators;

[assembly: InternalsVisibleTo("AcceptanceTests")]
[assembly: InternalsVisibleTo("Testing.Shared")]
[assembly: InternalsVisibleTo("ClassTests")]

namespace DomainLayer
{
    public sealed class DomainFacade : IDisposable
    {
        private bool _disposed;
        private readonly ServiceLocatorBase _serviceLocator;

        private MovieManager _movieManager;

        private MovieManager MovieManager { get { return _movieManager ?? (_movieManager = _serviceLocator.CreateMovieManager()); } }

        public DomainFacade()
          : this(new ServiceLocator())
        {
        }

        internal DomainFacade(ServiceLocatorBase serviceLocator)
        {
            _serviceLocator = serviceLocator;
        }

        public Task<IEnumerable<Movie>> GetAllMovies()
        {
            return MovieManager.GetAllMovies();
        }

        public Task<IEnumerable<Movie>> GetMoviesByGenre(Genre genre)
        {
            return MovieManager.GetMoviesByGenre(genre);
        }

        public async Task CreateMovie(Movie movie)
        {
            await MovieManager.CreateMovie(movie).ConfigureAwait(false);
        }

        [ExcludeFromCodeCoverage]
        private void Dispose(bool disposing)
        {
            if (disposing && !_disposed)
            {
                _movieManager?.Dispose();
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
    }
}
