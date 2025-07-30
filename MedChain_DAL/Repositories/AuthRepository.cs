using MedChain_DAL.Data;
using MedChain_DAL.Interfaces;
using MedChain_Models.Entities;
using MedChain_Models.Enums;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            return await _userManager.FindByIdAsync(userId);
        }

        public async Task<ApplicationUser?> GetUserByEmail(string email)
        {
            return await _userManager.FindByEmailAsync(email);
        }

        public async Task<bool> RegisterUser(ApplicationUser user, string password, UserRoles role)
        {
            var result = await _userManager.CreateAsync(user, password);
            if (!result.Succeeded) return false;

            // Ensure role exists
            var roleName = role.ToString();
            if (!await _roleManager.RoleExistsAsync(roleName))
            {
                await _roleManager.CreateAsync(new IdentityRole(roleName));
            }

            // Assign role to user
            var roleResult = await _userManager.AddToRoleAsync(user, roleName);
            return roleResult.Succeeded;
        }

        public async Task<bool> CheckPassword(ApplicationUser user, string password)
        {
            return await _userManager.CheckPasswordAsync(user, password);
        }

        public async Task<IList<string>> GetUserRoles(ApplicationUser user)
        {
            return await _userManager.GetRolesAsync(user);
        }

        public async Task<bool> AddUserToRole(ApplicationUser user, UserRoles role)
        {
            var result = await _userManager.AddToRoleAsync(user, role.ToString());
            return result.Succeeded;
        }
    }
}
