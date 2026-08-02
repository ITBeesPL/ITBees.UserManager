using System;
using System.Collections.Generic;
using ITBees.Interfaces.Repository;
using ITBees.Models.Users;
using ITBees.RestfulApiControllers.Exceptions;
using ITBees.UserManager.Controllers.PlatformOperator;
using ITBees.UserManager.Interfaces;

namespace ITBees.UserManager.Services.PlatformOperator;

public class PlatformUsersService : IPlatformUsersService
{
    private const string DefaultSortColumn = "UserAccount.SetupTime";

    /// <summary>
    /// Maps property names of <see cref="PlatformUserAccountVm"/> (as sent by the client) to property paths
    /// of <see cref="UsersInCompany"/> understood by the repository sorting.
    /// </summary>
    private static readonly Dictionary<string, string> SortColumnsMap = new(StringComparer.OrdinalIgnoreCase)
    {
        { "guid", "UserAccountGuid" },
        { "email", "UserAccount.Email" },
        { "displayName", "UserAccount.FirstName" },
        { "firstName", "UserAccount.FirstName" },
        { "lastName", "UserAccount.LastName" },
        { "phone", "UserAccount.Phone" },
        { "created", DefaultSortColumn },
        { "lastLoginDate", "UserAccount.LastLoginDateTime" },
        { "loginCount", "UserAccount.LoginsCount" },
        { "companyName", "Company.CompanyName" },
        { "city", "Company.City" },
        { "companyRole", "IdentityRole.Name" },
        { "subscriptionPlan", "Company.CompanyPlatformSubscription.SubscriptionPlanName" },
        { "subscriptionPlanActiveTo", "Company.CompanyPlatformSubscription.SubscriptionActiveTo" }
    };

    private readonly IReadOnlyRepository<UsersInCompany> _usersInCompanyRoRepo;
    private readonly IAspCurrentUserService _aspCurrentUserService;

    public PlatformUsersService(IReadOnlyRepository<UsersInCompany> usersInCompanyRoRepo,
        IAspCurrentUserService aspCurrentUserService)
    {
        _usersInCompanyRoRepo = usersInCompanyRoRepo;
        _aspCurrentUserService = aspCurrentUserService;
    }

    public PaginatedResult<PlatformUserAccountVm> GetUsers(string search, int? page, int? pageSize, string sortColumn,
        SortOrder? sortOrder)
    {
        if (_aspCurrentUserService.CurrentUserIsPlatformOperator() == false)
            throw new FasApiErrorException("Current user is not platform operator", 403);

        var sortOptions = new SortOptions(page, pageSize, TranslateSortColumn(sortColumn), sortOrder);

        if (string.IsNullOrEmpty(search))
        {
            return _usersInCompanyRoRepo
                .GetDataPaginated(x => true, sortOptions,
                    x => x.UserAccount, x => x.Company, x => x.Company.CompanyPlatformSubscription, x => x.IdentityRole)
                .MapTo(x => new PlatformUserAccountVm(x));
        }
        else
        {
            search = search.ToLower();
            return _usersInCompanyRoRepo
                .GetDataPaginated(x => x.UserAccount.FirstName.ToLower().Contains(search) ||
                                       x.UserAccount.LastName.ToLower().Contains(search) ||
                                       x.UserAccount.Email.ToLower().Contains(search) ||
                                       x.UserAccount.Phone.ToLower().Contains(search)
                    , sortOptions,
                    x => x.UserAccount, x => x.Company, x => x.Company.CompanyPlatformSubscription, x => x.IdentityRole)
                .MapTo(x => new PlatformUserAccountVm(x));
        }
    }

    /// <summary>
    /// Translates view model property name into an entity property path. When nothing was requested (or requested
    /// column is unknown) the newest registered users are returned first.
    /// </summary>
    private static string TranslateSortColumn(string sortColumn)
    {
        if (string.IsNullOrWhiteSpace(sortColumn))
            return DefaultSortColumn;

        return SortColumnsMap.TryGetValue(sortColumn.Trim(), out var mappedColumn)
            ? mappedColumn
            : DefaultSortColumn;
    }
}
