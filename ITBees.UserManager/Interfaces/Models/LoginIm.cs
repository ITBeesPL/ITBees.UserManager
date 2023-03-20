using System.ComponentModel.DataAnnotations;

namespace ITBees.UserManager.Interfaces.Models
{
    /// <summary>
    /// Login input model
    /// </summary>
    public class LoginIm
    {
        [Required]
        public string Username { get; set; }
        [Required]
        public string Password { get; set; }
        [Required]
        public string Language { get; set; }
    }
}