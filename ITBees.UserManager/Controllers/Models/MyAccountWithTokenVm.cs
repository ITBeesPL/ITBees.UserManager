using System.Linq;
using ITBees.FAS.ApiInterfaces.Companies;
using ITBees.FAS.ApiInterfaces.MyAccounts;
using ITBees.UserManager.Interfaces.Models;

namespace ITBees.UserManager.Controllers.Models;

public class MyAccountWithTokenVm : MyAccountVm
{
    public MyAccountWithTokenVm() { }
    public MyAccountWithTokenVm(MyAccountVm myAccount, TokenVm newToken)
    {
        this.TokenVm = newToken;
        this.Language = myAccount.Language;
        this.Guid = myAccount.Guid;
        this.Companies = myAccount.Companies.Select(x => new CompanyWithUserRoleVm(x)).ToList();
    }

    public TokenVm TokenVm { get; set; }
}