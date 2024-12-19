using ITBees.Interfaces.Repository;
using ITBees.Models.Users;
using ITBees.UserManager.Controllers.Invitations.Models;

namespace ITBees.UserManager.Services.Invitations;

public class UserInvitationService : IUserInvitationService
{
    private readonly IReadOnlyRepository<UsersInvitationsToCompanies> _usersInvitationsToCompaniesRoRepo;
    private readonly IWriteOnlyRepository<UsersInvitationsToCompanies> _usersInvitationsToCompaniesRwRepo;
    private readonly IWriteOnlyRepository<UsersInCompany> _usersInvitationsToCompanyRwRepo;

    public UserInvitationService(
        IReadOnlyRepository<UsersInvitationsToCompanies> usersInvitationsToCompaniesRoRepo,
        IWriteOnlyRepository<UsersInvitationsToCompanies> usersInvitationsToCompaniesRwRepo,
        IWriteOnlyRepository<UsersInCompany> usersInvitationsToCompanyRwRepo
    )
    {
        _usersInvitationsToCompaniesRoRepo = usersInvitationsToCompaniesRoRepo;
        _usersInvitationsToCompaniesRwRepo = usersInvitationsToCompaniesRwRepo;
        _usersInvitationsToCompanyRwRepo = usersInvitationsToCompanyRwRepo;
    }

    public void ApplyNewUser(InvitationExistingUserIm invitationExistingUserIm)
    {
    }

    public ApplyInvitationResultForNewUserVm ApplyNewUser(InvitationNewUserIm invitationNewUserIm)
    {
        return new ApplyInvitationResultForNewUserVm()
        {
            Success = false, Message = "not implemented yet"
        };
    }

    public ApplyInvitationResultForExistingUserVm ApplyExistingUser(InvitationExistingUserIm invitationExistingUserIm)
    {
        return new ApplyInvitationResultForExistingUserVm()
        {
            Success = false, Message = "not implemented yet"
        };
    }
}