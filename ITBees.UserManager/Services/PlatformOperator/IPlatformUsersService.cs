using ITBees.Interfaces.Repository;
using ITBees.UserManager.Controllers.PlatformOperator;

namespace ITBees.UserManager.Services.PlatformOperator;

public interface IPlatformUsersService
{
    PaginatedResult<PlatformUserAccountVm> GetUsers(string search, int? page, int? pageSize, string sortColumn,
        SortOrder? sortOrder);
}