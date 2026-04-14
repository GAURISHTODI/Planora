using System.ComponentModel.DataAnnotations;

namespace PlanoraApp.API.Services.UserService.Models
{
    public class User
    {
        [Key]
        public string Id { get; set; } = string.Empty; // Mapped to Entra ID object id
        
        public string Email { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        
        public string CurrencyPreference { get; set; } = "USD";
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime LastLoginAt { get; set; } = DateTime.UtcNow;
    }
}
