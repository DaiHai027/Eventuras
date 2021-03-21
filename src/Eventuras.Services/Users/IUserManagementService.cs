using System;
using System.Threading;
using System.Threading.Tasks;
using Eventuras.Domain;

namespace Eventuras.Services.Users
{
    /// <summary>
    /// The scope of this service is create/update/delete users only.
    /// For retrieving users see <see cref="IUserRetrievalService"/>.
    /// </summary>
    public interface IUserManagementService
    {
        /// <exception cref="Exceptions.NotAccessibleException">Not permitted to create users.</exception>
        /// <exception cref="Exceptions.DuplicateException">Active user with the given email is already created.</exception>
        Task<ApplicationUser> CreateNewUserAsync(
            string name,
            string email,
            string phoneNumber = null,
            CancellationToken cancellationToken = default);
    }
}
