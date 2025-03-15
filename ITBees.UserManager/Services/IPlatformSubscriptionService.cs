using ITBees.Models.Users;

namespace ITBees.UserManager.Services;

public interface IPlatformSubscriptionService
{
    void ActivateSubscriptionPlanIfNeeded(UsersInCompany lastUsedCompany);
}