using System;
using ITBees.Models.Users;

namespace ITBees.UserManager.DbModels;

public class UserConfirmationUrlParameters
{
    public int Id { get; set; }
    public UserAccount UserAccount { get; set; }
    public Guid UserAccountGuid { get; set; }
    public string ParametersJson { get; set; }
    public DateTime Created { get; set; }
}
