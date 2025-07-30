// MedChain_BLL/Services/RoleService.cs
using MedChain_Models.Enums;
using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;

namespace MedChain_BLL.Services
{
    public class RoleService
    {
        private readonly RoleManager<IdentityRole> _roleManager;

        public RoleService(RoleManager<IdentityRole> roleManager)
        {
            _roleManager = roleManager;
        }

        public async Task EnsureRolesExist()
        {
            foreach (var role in Enum.GetNames(typeof(UserRoles)))
            {
                if (!await _roleManager.RoleExistsAsync(role))
                {
                    await _roleManager.CreateAsync(new IdentityRole(role));
                }
            }
        }
    }
}