using System;
using ITBees.Interfaces.Repository;
using ITBees.Models.Users;
using ITBees.UserManager.Controllers.Invitations.Models;
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
    public InvtiationResendResultVm Resend(InvitationResendIm invitationIm)
    {
        try
        {
            _aspCurrentUserService.TryCanIDoForCompany(TypeOfOperation.Rw, invitationIm.CompanyGuid);
            _newUserRegistrationService.ResendInvitationToCompany(invitationIm);
            return new InvtiationResendResultVm() { Success = true };
        }
        catch (Exception e)
        {
            return new InvtiationResendResultVm() { Success = false , Message = $"Failed to resend invitation {e.Message}"};
        }
    }
}