// MedChain_BLL/DTOs/AuthDTOs.cs
using MedChain_Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace MedChain_BLL.DTOs
{
    public class LoginDTO
    {
        [Required, EmailAddress]
        public string? Email { get; set; }

        [Required]
        public string? Password { get; set; }
    }

    public class RegisterDTO
    {
        [Required]
        public string? FullName { get; set; }

        [Required, EmailAddress]
        public string? Email { get; set; }

        [Required]
        public string? Password { get; set; }

        [Required]
        public UserRoles Role { get; set; }
    }

    public class AuthResponseDTO
    {
        public bool IsSuccess { get; set; }
        public string? Message { get; set; }
        public string? Token { get; set; }
        public string? UserId { get; set; }
        public string? Role { get; set; } // Changed from UserRoles to string
    }
}