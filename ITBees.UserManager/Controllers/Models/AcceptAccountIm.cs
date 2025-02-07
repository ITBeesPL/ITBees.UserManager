namespace ITBees.UserManager.Controllers.Models;

public class AcceptAccountIm
{
    public string Email { get; }
    public string Token { get; }
    public string NewPassword { get; }
    public string Lang { get; set; }

    public AcceptAccountIm()
    {
        
    }

    public AcceptAccountIm(string email, string token, string newPassword)
    {
        Email = email;
        Token = token;
        NewPassword = newPassword;
    }
}