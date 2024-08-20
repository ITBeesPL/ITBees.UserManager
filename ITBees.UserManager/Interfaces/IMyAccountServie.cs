using ITBees.FAS.ApiInterfaces.MyAccounts;
using ITBees.Models.MyAccount;

namespace ITBees.UserManager.Interfaces
{
    public interface IMyAccountServie
    {
        MyAccountVm GetMyAccountData();
    }
}