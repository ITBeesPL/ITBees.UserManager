using ITBees.Interfaces.Repository;
using ITBees.Models.Users;
using ITBees.UserManager.Controllers.Models;
using ITBees.UserManager.Interfaces;

namespace ITBees.UserManager.Services.Invitations;

public class InvitationResendService : IInvitationResendService
{
    private readonly IAspCurrentUserService _aspCurrentUserService;
    private readonly IReadOnlyRepository<UsersInvitationsToCompanies> _usersInvitationsToCompaniesRoRepo;
    private readonly INewUserRegistrationService _newUserRegistrationService;

    public InvitationResendService(IAspCurrentUserService aspCurrentUserService, 
        IReadOnlyRepository<UsersInvitationsToCompanies>usersInvitationsToCompaniesRoRepo,
        INewUserRegistrationService newUserRegistrationService)
    {
        _aspCurrentUserService = aspCurrentUserService;
        _usersInvitationsToCompaniesRoRepo = usersInvitationsToCompaniesRoRepo;
        _newUserRegistrationService = newUserRegistrationService;
    }
    public void Resend(InvitationResendIm invitationIm)
    {
        _aspCurrentUserService.TryCanIDoForCompany(TypeOfOperation.Rw, invitationIm.CompanyGuid);
        _newUserRegistrationService.ResendInvitationToCompany(invitationIm);
    }
}