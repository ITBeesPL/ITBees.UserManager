using System;
using System.Linq;
using ITBees.Interfaces.Repository;
using ITBees.Models.Languages;
using ITBees.Models.Users;
using ITBees.Translations;
using ITBees.UserManager.Controllers.Models;
using ITBees.UserManager.Interfaces;
using ITBees.UserManager.Translations;
using Microsoft.Extensions.Logging;

namespace ITBees.UserManager.Services
{
    public class AcceptInvitationService : IAcceptInvitationService
    {
        private readonly IWriteOnlyRepository<UsersInvitationsToCompanies> _usersInvitationsWoRepo;
        private readonly IWriteOnlyRepository<UsersInCompany> _usersInCompanyWoRepo;
        private readonly IAspCurrentUserService _aspCurrentUserService;
        private readonly ILogger<AcceptInvitationService> _logger;

        public AcceptInvitationService(IWriteOnlyRepository<UsersInvitationsToCompanies> usersInvitationsWoRepo,
            IWriteOnlyRepository<UsersInCompany> usersInCompanyWoRepo,
            IAspCurrentUserService aspCurrentUserService,
            ILogger<AcceptInvitationService> logger)
        {
            _usersInvitationsWoRepo = usersInvitationsWoRepo;
            _usersInCompanyWoRepo = usersInCompanyWoRepo;
            _aspCurrentUserService = aspCurrentUserService;
            _logger = logger;
        }

        /// <summary>
        /// Accepts the invitation of the logged-in user to the company - only his own. The link in the e-mail carries
        /// nothing secret (the e-mail address and the company), so accepting by them let anyone who knew both make
        /// somebody a member of the company with the role of the invitation. The e-mail of the link, when given, has
        /// to be the one of the logged-in user.
        /// </summary>
        public AcceptInvitationResultVm Accept(bool emailInvitation, string email, Guid companyGuid)
        {
            var currentUser = _aspCurrentUserService.GetCurrentUser();
            if (currentUser == null)
                throw new UnauthorizedAccessException("Log in to accept the invitation.");

            if (string.IsNullOrWhiteSpace(email) == false &&
                string.Equals(email.Trim(), currentUser.Email, StringComparison.OrdinalIgnoreCase) == false)
            {
                _logger.LogWarning("User {UserGuid} tried to accept the invitation of {Email} to company {CompanyGuid}",
                    currentUser.Guid, email, companyGuid);
                return new AcceptInvitationResultVm()
                {
                    Accepted = false, Message = $"The invitation is for {email.Trim()} - log in as {email.Trim()} to accept it."
                };
            }

            try
            {
                var result = _usersInvitationsWoRepo
                    .UpdateData(x => x.Applied == false &&
                                     x.UserAccountGuid == currentUser.Guid &&
                                     x.CompanyGuid == companyGuid, x =>
                    {
                        x.Applied = true;
                    });
                if (result.Any())
                {
                    var newUser = _usersInCompanyWoRepo.InsertData(new UsersInCompany()
                    {
                        AddedByGuid = result.First().CreatedByGuid,
                        AddedDate = DateTime.Now,
                        CompanyGuid = result.First().CompanyGuid,
                        UserAccountGuid = result.First().UserAccountGuid,
                        IdentityRoleId = result.First().IdentityRoleId
                    });

                    return new AcceptInvitationResultVm() { Accepted = true, Message = string.Empty };
                }
            }
            catch (Exception e)
            {
                _logger.LogError($"Error while accepting user invitation for user {currentUser.Email} in company guid {companyGuid}", e);
                return new AcceptInvitationResultVm() { Accepted = false, Message = $"{Translate.Get(() => UserInvitation.ErrorWhileAcceptingUserInvitation, new En())}" + e.Message };
            }

            return new AcceptInvitationResultVm() { Accepted = false, };
        }

        public AcceptInvitationResultVm AcceptAllAwaitingInvitations(string email)
        {
            try
            {
                var result = _usersInvitationsWoRepo
                    .UpdateData(x => x.Applied == false &&
                                     x.UserAccount.Email == email, x =>
                    {
                        x.Applied = true;
                    });
                
                if (result.Any())
                {
                    foreach (var invitaiton in result)
                    {
                        var newUser = _usersInCompanyWoRepo.InsertData(new UsersInCompany()
                        {
                            AddedByGuid = invitaiton.CreatedByGuid,
                            AddedDate = DateTime.Now,
                            CompanyGuid = invitaiton.CompanyGuid,
                            UserAccountGuid = invitaiton.UserAccountGuid,
                            IdentityRoleId = invitaiton.IdentityRoleId
                        });
                    }
                    
                    return new AcceptInvitationResultVm() { Accepted = true, Message = string.Empty };
                }
            }
            catch (Exception e)
            {
                _logger.LogError("Error while accepting all user invitations for user {email}", e);
                return new AcceptInvitationResultVm() { Accepted = false, Message = $"{Translate.Get(() => UserInvitation.ErrorWhileAcceptingUserInvitation, new En())}" + e.Message };
            }

            return new AcceptInvitationResultVm() { Accepted = false, };
        }
    }
}