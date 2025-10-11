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
        private readonly ILogger<AcceptInvitationService> _logger;

        public AcceptInvitationService(IWriteOnlyRepository<UsersInvitationsToCompanies> usersInvitationsWoRepo,
            IWriteOnlyRepository<UsersInCompany> usersInCompanyWoRepo,
            ILogger<AcceptInvitationService> logger)
        {
            _usersInvitationsWoRepo = usersInvitationsWoRepo;
            _usersInCompanyWoRepo = usersInCompanyWoRepo;
            _logger = logger;
        }

        public AcceptInvitationResultVm Accept(bool emailInvitation, string email, Guid companyGuid)
        {
            try
            {
                var result = _usersInvitationsWoRepo
                    .UpdateData(x => x.Applied == false &&
                                     x.UserAccount.Email == email &&
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
                _logger.LogError($"Error while accepting user invitation for user {email} in company guid {companyGuid}", e);
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