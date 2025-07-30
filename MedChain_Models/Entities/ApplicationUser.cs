// MedChain_Models/Entities/ApplicationUser.cs
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace MedChain_Models.Entities
{
    public class ApplicationUser : IdentityUser
    {
        [Required]
        public string FullName { get; set; } = string.Empty;

        public DateTime DateOfBirth { get; set; }
        public string? Address { get; set; }
        public string? ProfilePictureUrl { get; set; }
        public string? Bio { get; set; }
        public string? Specialization { get; set; }
        public string? LicenseNumber { get; set; }
        public string? InsuranceProvider { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public string? WalletAddress { get; set; }

        // Remove this property - roles are managed by Identity
        // public string? Role { get; set; }
    }
}