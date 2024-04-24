using System.Linq;
using ITBees.FAS.ApiInterfaces.Companies;
using ITBees.FAS.ApiInterfaces.MyAccounts;
using ITBees.Models.MyAccount;
using ITBees.UserManager.Interfaces.Models;

namespace ITBees.UserManager.Controllers.Models;

public class MyAccountVmWithToken : MyAccountVm
{
    public MyAccountVmWithToken() { }
    public MyAccountVmWithToken(MyAccount myAccount, TokenVm newToken)
    {
        this.TokenVm = newToken;
        this.Language = myAccount.Language.Code;
        this.Guid = myAccount.Guid;
        this.Companies = myAccount.Companies.Select(x => new CompanyVm()
        {
            CompanyName = x.CompanyName,
            Created = x.Created,
            CreatedBy = x.CreatedBy.DisplayName,
            Guid = x.Guid,
            IsActive = x.IsActive,
            CreatedByGuid = x.CreatedByGuid,
            Owner = x.Owner.DisplayName,
            OwnerGuid = x.OwnerGuid
        }).ToList();
    }

    public TokenVm TokenVm { get; set; }
}