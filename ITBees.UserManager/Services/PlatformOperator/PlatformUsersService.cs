using System;
using ITBees.Interfaces.Repository;
using ITBees.Models.Users;
using ITBees.RestfulApiControllers.Exceptions;
using ITBees.UserManager.Controllers.PlatformOperator;
using ITBees.UserManager.Interfaces;

namespace ITBees.UserManager.Services.PlatformOperator;

public class PlatformUsersService : IPlatformUsersService
{
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

        if (string.IsNullOrEmpty(search))
        {
            return _usersInCompanyRoRepo
                .GetDataPaginated(x => true, new SortOptions(page, pageSize, sortColumn, sortOrder),
                    x => x.UserAccount, x=>x.Company, x=>x.Company.CompanyPlatformSubscription).MapTo(x => new PlatformUserAccountVm(x));
        }
        else
        {
            search = search.ToLower();
            return _usersInCompanyRoRepo
                .GetDataPaginated(x => x.UserAccount.FirstName.ToLower().Contains(search) ||
                                       x.UserAccount.LastName.ToLower().Contains(search) ||
                                       x.UserAccount.Email.ToLower().Contains(search) ||
                                       x.UserAccount.Phone.ToLower().Contains(search)
                    , new SortOptions(page, pageSize, sortColumn, sortOrder),
                    x => x.UserAccount, x=>x.Company, x=>x.Company.CompanyPlatformSubscription).MapTo(x => new PlatformUserAccountVm(x));
        }
    }
}