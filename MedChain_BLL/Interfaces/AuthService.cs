// MedChain_BLL/Services/AuthService.cs
using MedChain_BLL.DTOs;
using MedChain_BLL.Interfaces;
using MedChain_DAL.Interfaces;
using MedChain_Models.Entities;
using MedChain_Models.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace MedChain_BLL.Services
{
    public class AuthService : IAuthService
    {
        private readonly IAuthRepository _authRepository;
        private readonly IConfiguration _configuration;
        private readonly UserManager<ApplicationUser> _userManager;

        public AuthService(
            IAuthRepository authRepository,
            IConfiguration configuration,
            UserManager<ApplicationUser> userManager)
        {
            _authRepository = authRepository;
            _configuration = configuration;
            _userManager = userManager;
        }

        public async Task<AuthResponseDTO> Login(LoginDTO loginDTO)
        {
            var user = await _authRepository.GetUserByEmail(loginDTO.Email);
            if (user == null)
            {
                return new AuthResponseDTO { IsSuccess = false, Message = "User not found" };
            }

            var isPasswordValid = await _authRepository.CheckPassword(user, loginDTO.Password);
            if (!isPasswordValid)
            {
                return new AuthResponseDTO { IsSuccess = false, Message = "Invalid password" };
            }

            var roles = await _userManager.GetRolesAsync(user);
            var primaryRole = roles.FirstOrDefault(); // Or handle multiple roles as needed

            var token = GenerateJwtToken(user, roles);

            return new AuthResponseDTO
            {
                IsSuccess = true,
                Message = "Login successful",
                Token = token,
                UserId = user.Id,
                Role = primaryRole // Now using the actual role from Identity
            };
        }

        public async Task<AuthResponseDTO> Register(RegisterDTO registerDTO)
        {
            var userExists = await _userManager.FindByEmailAsync(registerDTO.Email);
            if (userExists != null)
            {
                return new AuthResponseDTO { IsSuccess = false, Message = "User already exists" };
            }

            var user = new ApplicationUser
            {
                FullName = registerDTO.FullName,
                UserName = registerDTO.Email,
                Email = registerDTO.Email
            };

            var result = await _userManager.CreateAsync(user, registerDTO.Password);
            if (!result.Succeeded)
            {
                return new AuthResponseDTO
                {
                    IsSuccess = false,
                    Message = string.Join(", ", result.Errors.Select(e => e.Description))
                };
            }

            // Add user to role
            var roleResult = await _userManager.AddToRoleAsync(user, registerDTO.Role.ToString());
            if (!roleResult.Succeeded)
            {
                return new AuthResponseDTO
                {
                    IsSuccess = false,
                    Message = string.Join(", ", roleResult.Errors.Select(e => e.Description))
                };
            }

            var roles = await _userManager.GetRolesAsync(user);
            var token = GenerateJwtToken(user, roles);

            return new AuthResponseDTO
            {
                IsSuccess = true,
                Message = "Registration successful",
                Token = token,
                UserId = user.Id,
                Role = roles.FirstOrDefault()
            };
        }

        private string GenerateJwtToken(ApplicationUser user, IList<string> roles)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim("FullName", user.FullName)
            };

            // Add all roles as claims
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8
                .GetBytes(_configuration["Jwt:Key"]));

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.Now.AddDays(1),
                SigningCredentials = creds,
                Issuer = _configuration["Jwt:Issuer"],
                Audience = _configuration["Jwt:Audience"]
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);

            return tokenHandler.WriteToken(token);
        }

        public async Task<bool> UserExists(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            return user != null;
        }
    }
}