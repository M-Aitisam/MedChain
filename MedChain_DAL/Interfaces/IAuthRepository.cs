using MedChain_Models.Entities;
using MedChain_Models.Enums;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;

namespace MedChain_DAL.Interfaces
{
    public interface IAuthRepository
    {
        Task<ApplicationUser?> GetUserById(string userId);
        Task<ApplicationUser?> GetUserByEmail(string email);
        Task<bool> RegisterUser(ApplicationUser user, string password, UserRoles role);
        Task<bool> CheckPassword(ApplicationUser user, string password);
        Task<IList<string>> GetUserRoles(ApplicationUser user);
        Task<bool> AddUserToRole(ApplicationUser user, UserRoles role);
        Task<IDbContextTransaction> BeginTransactionAsync();
        Task<IDbContextTransaction> BeginTransactionAsync(IsolationLevel isolationLevel);
    }
}
