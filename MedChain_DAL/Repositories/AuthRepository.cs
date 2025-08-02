// MedChain_DAL/Repositories/AuthRepository.cs
using MedChain_DAL.Data;
using MedChain_DAL.Interfaces;
using MedChain_Models.Entities;
using MedChain_Models.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Data;

namespace MedChain_DAL.Repositories
{
    public class AuthRepository : IAuthRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AuthRepository(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _roleManager = roleManager ?? throw new ArgumentNullException(nameof(roleManager));
        }

        public async Task<ApplicationUser?> GetUserById(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId)) return null;
            return await _userManager.FindByIdAsync(userId);
        }

        public async Task<ApplicationUser?> GetUserByEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return null;
            return await _userManager.FindByEmailAsync(email);
        }

        public async Task<bool> RegisterUser(ApplicationUser user, string password, UserRoles role)
        {
            if (user == null || string.IsNullOrWhiteSpace(password)) return false;

            var result = await _userManager.CreateAsync(user, password);
            if (!result.Succeeded) return false;

            var roleName = role.ToString();
            if (!await _roleManager.RoleExistsAsync(roleName))
            {
                await _roleManager.CreateAsync(new IdentityRole(roleName));
            }

            var roleResult = await _userManager.AddToRoleAsync(user, roleName);
            return roleResult.Succeeded;
        }

        public async Task<bool> CheckPassword(ApplicationUser user, string password)
        {
            if (user == null || string.IsNullOrWhiteSpace(password)) return false;
            return await _userManager.CheckPasswordAsync(user, password);
        }

        public async Task<IList<string>> GetUserRoles(ApplicationUser user)
        {
            if (user == null) return new List<string>();
            return await _userManager.GetRolesAsync(user);
        }

        public async Task<bool> AddUserToRole(ApplicationUser user, UserRoles role)
        {
            if (user == null) return false;
            var result = await _userManager.AddToRoleAsync(user, role.ToString());
            return result.Succeeded;
        }

        public async Task<IDbContextTransaction> BeginTransactionAsync()
        {
            return await _context.Database.BeginTransactionAsync();
        }

        public async Task<IDbContextTransaction> BeginTransactionAsync(IsolationLevel isolationLevel)
        {
            return await _context.Database.BeginTransactionAsync(isolationLevel);
        }
    }
}