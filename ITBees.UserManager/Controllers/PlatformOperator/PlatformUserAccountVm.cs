using System;
using ITBees.Models.Users;

namespace ITBees.UserManager.Controllers.PlatformOperator;

public class PlatformUserAccountVm
{
    public PlatformUserAccountVm(UsersInCompany x)
    {
        Guid = x.UserAccountGuid;
        FirstName = x.UserAccount.FirstName;
        LastName = x.UserAccount.LastName;
        Email = x.UserAccount.Email;
        Phone = x.UserAccount.Phone;
        CompanyName = x.Company.CompanyName;
        Street = x.Company.Street;
        City = x.Company.City;
        PostCode = x.Company.PostCode;
        Phone = x.UserAccount.Phone;
        LastLoginDate = x.UserAccount.LastLoginDateTime.Value;
        // Country = x.Company.Country
        SubscriptionPlan = x.Company.CompanyPlatformSubscription.SubscriptionPlanName;
        SubscriptionPlanActiveTo = x.Company.CompanyPlatformSubscription.SubscriptionActiveTo;
        LoginCount = x.UserAccount.LoginsCount;
    }

    public int LoginCount { get; set; }

    public DateTime? SubscriptionPlanActiveTo { get; set; }

    public Guid Guid { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }
    public string Phone { get; set; }
    public string PostCode { get; set; }
    public string Street { get; set; }
    public string City { get; set; }
    public string Country { get; set; }
    public string SubscriptionPlan { get; set; }
    public string CompanyName { get; set; }
    public DateTime? LastLoginDate { get; set; }
}