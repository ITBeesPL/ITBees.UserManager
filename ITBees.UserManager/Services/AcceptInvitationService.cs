using System;
using System.Linq;
using ITBees.Interfaces.Repository;
using ITBees.Models.Languages;
using ITBees.Models.Users;
using ITBees.Translations;
using ITBees.UserManager.Controllers.Models;
using ITBees.UserManager.Interfaces;
using ITBees.UserManager.Translations;

namespace ITBees.UserManager.Services
{
    public class AcceptInvitationService : IAcceptInvitationService
    {
        private readonly IAspCurrentUserService _aspCurrentUserService;
        private readonly IWriteOnlyRepository<UsersInvitationsToCompanies> _usersInvitationsWoRepo;
        private readonly IWriteOnlyRepository<UsersInCompany> _usersInCompanyWoRepo;

        public AcceptInvitationService(IAspCurrentUserService aspCurrentUserService,
            IWriteOnlyRepository<UsersInvitationsToCompanies> usersInvitationsWoRepo,
            IWriteOnlyRepository<UsersInCompany> usersInCompanyWoRepo)
        {
            _aspCurrentUserService = aspCurrentUserService;
            _usersInvitationsWoRepo = usersInvitationsWoRepo;
            _usersInCompanyWoRepo = usersInCompanyWoRepo;
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
                        UserAccountGuid = result.First().UserAccountGuid
                    });

                    return new AcceptInvitationResultVm() { Accepted = true, Message = string.Empty };
                }
            }
            catch (Exception e)
            {
                return new AcceptInvitationResultVm() { Accepted = false, Message = $"{Translate.Get(() => UserInvitation.ErrorWhileAcceptingUserInvitation, new En())}" + e.Message };
            }

            return new AcceptInvitationResultVm() { Accepted = false, };
        }
    }
}