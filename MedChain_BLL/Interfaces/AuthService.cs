// MedChain_BLL/Services/AuthService.cs
using MedChain_BLL.DTOs;
using MedChain_BLL.Interfaces;
using MedChain_DAL.Interfaces;
using MedChain_Models.Entities;
using MedChain_Models.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.Data;
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
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            IAuthRepository authRepository,
            IConfiguration configuration,
            UserManager<ApplicationUser> userManager,
            ILogger<AuthService> logger)
        {
            _authRepository = authRepository ?? throw new ArgumentNullException(nameof(authRepository));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<AuthResponseDTO> Login(LoginDTO loginDTO)
        {
            if (loginDTO == null || string.IsNullOrWhiteSpace(loginDTO.Email) || string.IsNullOrWhiteSpace(loginDTO.Password))
            {
                _logger.LogWarning("Login attempt with invalid data");
                return new AuthResponseDTO { IsSuccess = false, Message = "Invalid credentials" };
            }

            try
            {
                var user = await _authRepository.GetUserByEmail(loginDTO.Email);
                if (user == null)
                {
                    _logger.LogWarning("Login attempt failed - User not found: {Email}", loginDTO.Email);
                    return new AuthResponseDTO { IsSuccess = false, Message = "Invalid credentials" };
                }

                var isPasswordValid = await _authRepository.CheckPassword(user, loginDTO.Password);
                if (!isPasswordValid)
                {
                    _logger.LogWarning("Login attempt failed - Invalid password for user: {Email}", loginDTO.Email);
                    return new AuthResponseDTO { IsSuccess = false, Message = "Invalid credentials" };
                }

                var roles = await _userManager.GetRolesAsync(user);
                var primaryRole = roles.FirstOrDefault();

                var token = GenerateJwtToken(user, roles);

                _logger.LogInformation("User logged in successfully: {Email}", loginDTO.Email);
                return new AuthResponseDTO
                {
                    IsSuccess = true,
                    Message = "Login successful",
                    Token = token,
                    UserId = user.Id,
                    Role = primaryRole ?? string.Empty
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for email: {Email}", loginDTO.Email);
                return new AuthResponseDTO { IsSuccess = false, Message = "An error occurred during login" };
            }
        }

        public async Task<AuthResponseDTO> Register(RegisterDTO registerDTO)
        {
            if (registerDTO == null ||
                string.IsNullOrWhiteSpace(registerDTO.Email) ||
                string.IsNullOrWhiteSpace(registerDTO.Password) ||
                string.IsNullOrWhiteSpace(registerDTO.FullName))
            {
                _logger.LogWarning("Registration attempt with invalid data");
                return new AuthResponseDTO { IsSuccess = false, Message = "Invalid registration data" };
            }

            try
            {
                var userExists = await _userManager.FindByEmailAsync(registerDTO.Email);
                if (userExists != null)
                {
                    _logger.LogWarning("Registration attempt failed - User already exists: {Email}", registerDTO.Email);
                    return new AuthResponseDTO { IsSuccess = false, Message = "User already exists" };
                }

                var user = new ApplicationUser
                {
                    FullName = registerDTO.FullName,
                    UserName = registerDTO.Email,
                    Email = registerDTO.Email,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await using var transaction = await _authRepository.BeginTransactionAsync();

                try
                {
                    var createResult = await _userManager.CreateAsync(user, registerDTO.Password);
                    if (!createResult.Succeeded)
                    {
                        await transaction.RollbackAsync();
                        var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
                        _logger.LogWarning("User creation failed: {Errors}", errors);
                        return new AuthResponseDTO { IsSuccess = false, Message = errors };
                    }

                    var roleResult = await _userManager.AddToRoleAsync(user, registerDTO.Role.ToString());
                    if (!roleResult.Succeeded)
                    {
                        await transaction.RollbackAsync();
                        var errors = string.Join(", ", roleResult.Errors.Select(e => e.Description));
                        _logger.LogWarning("Role assignment failed: {Errors}", errors);
                        return new AuthResponseDTO { IsSuccess = false, Message = errors };
                    }

                    await transaction.CommitAsync();

                    var roles = await _userManager.GetRolesAsync(user);
                    var token = GenerateJwtToken(user, roles);

                    _logger.LogInformation("User registered successfully: {Email}", registerDTO.Email);
                    return new AuthResponseDTO
                    {
                        IsSuccess = true,
                        Message = "Registration successful",
                        Token = token,
                        UserId = user.Id,
                        Role = roles.FirstOrDefault() ?? string.Empty
                    };
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Error during registration transaction for email: {Email}", registerDTO.Email);
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during registration");
                return new AuthResponseDTO
                {
                    IsSuccess = false,
                    Message = "An error occurred during registration"
                };
            }
        }

        private string GenerateJwtToken(ApplicationUser user, IList<string> roles)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            if (roles == null) throw new ArgumentNullException(nameof(roles));

            var jwtKey = _configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key not configured");
            var jwtIssuer = _configuration["Jwt:Issuer"] ?? throw new InvalidOperationException("JWT Issuer not configured");
            var jwtAudience = _configuration["Jwt:Audience"] ?? throw new InvalidOperationException("JWT Audience not configured");

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, user.UserName ?? string.Empty),
                new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
                new Claim("FullName", user.FullName ?? string.Empty),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role ?? string.Empty));
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddDays(1),
                SigningCredentials = creds,
                Issuer = jwtIssuer,
                Audience = jwtAudience
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);

            return tokenHandler.WriteToken(token);
        }

        public async Task<bool> UserExists(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return false;
            }

            try
            {
                var user = await _userManager.FindByEmailAsync(email);
                return user != null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if user exists: {Email}", email);
                throw;
            }
        }
    }
}