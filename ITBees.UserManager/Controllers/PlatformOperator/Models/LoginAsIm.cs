namespace ITBees.UserManager.Controllers.PlatformOperator.Models;

public class LoginAsIm
{
    public string Email { get; set; }
    /// <summary>
    /// This sould be definied in config.json (different types,
    /// different values ie:
    /// "admserviceurl" : "https://superapp.com/adm",
    /// "userserviceurl" : "https://superapp.com/app"
    /// etc...) 
    /// </summary>
    public string ServiceType { get; set; }
}