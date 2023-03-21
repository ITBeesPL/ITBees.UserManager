using System.Text.Json.Serialization;

namespace ITBees.UserManager.Interfaces.Models
{
    public class CheckEmailStatusVm
    {
        public string Email { get; set; }
        public bool EmailAllowedToRegister { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public CheckStatus CheckStatus { get; set; }
        public string Message { get; set; }
    }

    public enum CheckStatus
    {
        EmailNotRegistered,
        EmailAlreadyRegistered,
        EmailAlreadyRegisteredButNotConfirmed
    }
}