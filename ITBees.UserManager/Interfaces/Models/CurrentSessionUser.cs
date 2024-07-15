using System;
using ITBees.Models.Users;

namespace ITBees.UserManager.Interfaces.Models;

public class CurrentSessionUser
{
    public CurrentSessionUser(CurrentUser user, bool isAuthorized, Guid? currentUserGuid)
    {
        CurrentUser = user;
        IsAuthorized = isAuthorized;
        CurrentUserGuid = currentUserGuid;
    }

    public CurrentUser CurrentUser { get; private set; }
    public Guid? CurrentUserGuid { get; private set; }
    public bool IsAuthorized { get; private set; }
}