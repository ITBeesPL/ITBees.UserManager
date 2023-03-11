using System;

namespace ITBees.UserManager.Interfaces.Models
{
    public class TokenVm
    {
        public string Value { get; set; }
        public DateTime TokenExpirationDate { get; set; }
    }
}