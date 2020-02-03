using DomainLayer.Managers.DataLayer.DataManagers;
using DomainLayer.Managers.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DomainLayer.Managers.DataLayer
{
    internal sealed class DataFacade
    {
        private MovieDataManager _movieDataManager;

        private MovieDataManager MovieDataManager { get { return _movieDataManager ?? (_movieDataManager = new MovieDataManager()); } }

        public Task<IEnumerable<Movie>> GetAllMovies()
        {
            return MovieDataManager.GetAllMovies();
        }
    }
}
