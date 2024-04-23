using ITBees.FAS.ApiInterfaces.MyAccounts;

namespace ITBees.UserManager.Services;

public interface IMyAccountUpdateService
{
    void UpdateMyAccount(MyAccountIm myAccountIm);
}