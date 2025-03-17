using System;
using ITBees.Interfaces.Repository;
using ITBees.Models.Companies;
using ITBees.Models.Users;
using Microsoft.Extensions.Logging;

namespace ITBees.UserManager.Services;

public class PlatformSubscriptionService : IPlatformSubscriptionService
{
    private readonly IWriteOnlyRepository<Company> _companyRwRepository;
    private readonly ILogger<PlatformSubscriptionService> _logger;

    public PlatformSubscriptionService(IWriteOnlyRepository<Company> companyRwRepository,
        ILogger<PlatformSubscriptionService> logger)
    {
        _companyRwRepository = companyRwRepository;
        _logger = logger;
    }

    public void ActivateSubscriptionPlanIfNeeded(UsersInCompany lastUsedCompany)
    {
        if (lastUsedCompany.Company.CompanyPlatformSubscription?.SubscriptionPlanGuid != null &&
            lastUsedCompany.Company.CompanyPlatformSubscription.SubscriptionActiveTo == null)
        {
            DateTime endDate = DateTime.Now;
            if (lastUsedCompany.Company.CompanyPlatformSubscription.SubscriptionPlan.IntervalDays == 0)
            {
                endDate = endDate.AddMonths(lastUsedCompany.Company.CompanyPlatformSubscription.SubscriptionPlan
                    .Interval);
            }
            else
            {
                endDate = endDate.AddDays(lastUsedCompany.Company.CompanyPlatformSubscription.SubscriptionPlan
                    .IntervalDays);
            }

            var formattedDate = endDate.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss");
            
            lastUsedCompany.Company.CompanyPlatformSubscription.SubscriptionActiveTo = endDate;

            //it is done by pure sql to prevent problems with inheritance in derived Company classes / differten generics of Company model. 
            _companyRwRepository.Sql(
                $"UPDATE Company SET CompanyPlatformSubscription_SubscriptionActiveTo = '{formattedDate}' WHERE Guid = '{lastUsedCompany.CompanyGuid}'");


            _logger.LogInformation($"Activated subscription plan for company {lastUsedCompany.Company.CompanyName} - after first login, plan : {lastUsedCompany.Company.CompanyPlatformSubscription.SubscriptionPlan.PlanName} valid to : {formattedDate}");
        }
    }
}