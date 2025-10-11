using ITBees.Interfaces.CodeGeneration;

namespace ITBees.UserManager.Controllers.Models;

public class AcceptAccountIm
{
    public string Email { get; set; }
    public string Token { get; set; }
    [NullableStringProperty] public string? TokenAuth { get; set; }
    public string NewPassword { get; set; }
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