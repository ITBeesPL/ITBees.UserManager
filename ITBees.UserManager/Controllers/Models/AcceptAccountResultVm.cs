using ITBees.UserManager.Interfaces.Models;

namespace ITBees.UserManager.Controllers.Models;

public class AcceptAccountResultVm
{
    public AcceptAccountResultVm()
    {
        
    }
    
    public AcceptAccountResultVm(bool success, TokenVm token, string message)
    {
        Success = success;
        Token = token;
        Message = message;
    }

    public bool Success { get; set; }
    public TokenVm Token { get; }
    public string Message { get; }
}