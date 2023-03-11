namespace ITBees.UserManager.Interfaces.Models
{
    public class CheckEmailStatusVm
    {
        public string Email { get; set; }
        public bool EmailAllowedToRegister { get; set; }
        public string CheckStatus { get; set; }
        public string Message { get; set; }
    }
}