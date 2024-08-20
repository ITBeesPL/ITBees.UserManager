using ITBees.FAS.ApiInterfaces.MyAccounts;

namespace ITBees.UserManager.Interfaces;

public interface IMyAccountUpdateService
{
    void UpdateMyAccount(MyAccountIm myAccountIm);
}