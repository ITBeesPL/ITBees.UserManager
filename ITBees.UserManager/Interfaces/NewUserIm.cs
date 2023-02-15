namespace ITBees.UserManager.Interfaces
{
    /// <summary>
    /// Model responsible for creation of new user in platform
    /// </summary>
    public class NewUserIm
    {
        public string Email { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }

        public string? Phone { get; set; }

        public string Password { get; set; }
    }
}