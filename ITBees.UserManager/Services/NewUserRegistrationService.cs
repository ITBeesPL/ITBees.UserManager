using System;
using System.Threading.Tasks;
using InheritedMapper;
using ITBees.Interfaces.Repository;
using ITBees.Models.Companies;
using ITBees.Models.Languages;
using ITBees.Models.Users;
using ITBees.Translations;
using ITBees.UserManager.Interfaces.Models;
using ITBees.UserManager.Interfaces.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace ITBees.UserManager.Services
{
    public class NewUserRegistrationService<T> : INewUserRegistrationService where T : IdentityUser, new()
    {
        private readonly IUserManager _userManager;
        private readonly IWriteOnlyRepository<UserAccount> _userAccountWriteOnlyRepository;
        private readonly IReadOnlyRepository<UserAccount> _userAccountReadOnlyRepository;
        private readonly IWriteOnlyRepository<Company> _companyWoRepository;
        private readonly IReadOnlyRepository<Company> _companyRoRepository;
        private readonly IWriteOnlyRepository<UsersInCompany> _usersInCompanyWoRepo;
        private readonly ILogger<NewUserRegistrationService<T>> _logger;

        public NewUserRegistrationService(IUserManager userManager,
            IWriteOnlyRepository<UserAccount> userAccountWriteOnlyRepository,
            IReadOnlyRepository<UserAccount> userAccountReadOnlyRepository,
            IWriteOnlyRepository<Company> companyWoRepository,
            IReadOnlyRepository<Company> companyRoRepository,
            IWriteOnlyRepository<UsersInCompany> usersInCompanyWoRepo,
            ILogger<NewUserRegistrationService<T>> logger)
        {
            _userManager = userManager;
            _userAccountWriteOnlyRepository = userAccountWriteOnlyRepository;
            _userAccountReadOnlyRepository = userAccountReadOnlyRepository;
            _companyWoRepository = companyWoRepository;
            _companyRoRepository = companyRoRepository;
            _usersInCompanyWoRepo = usersInCompanyWoRepo;
            _logger = logger;
        }

        public async Task<Guid> RegisterNewUser(NewUserRegistrationIm newUserRegistrationIm)
        {
            var newUser = new T()
            {
                UserName = newUserRegistrationIm.Email,
                Email = newUserRegistrationIm.Email
            };

            var result = await _userManager.CreateAsync(newUser, newUserRegistrationIm.Password);
            if (!result.Succeeded) throw new Exception("User Creation Failed");

            UserAccount userSavedData;
            var currentUserGuid = await _userManager.FindByEmailAsync(newUser.Email);
            
            if(string.IsNullOrEmpty(newUserRegistrationIm.Language))
                newUserRegistrationIm.Language = "en";

            var userLanguage = new DerivedAsTFromStringClassResolver<Language>().GetInstance(newUserRegistrationIm.Language);
            try
            {
                userSavedData = _userAccountWriteOnlyRepository.InsertData(
                    new UserAccount()
                    {
                        Email = newUserRegistrationIm.Email,
                        Guid = new Guid(newUser.Id),
                        Phone = newUserRegistrationIm.Phone,
                        FirstName = newUserRegistrationIm.FirstName,
                        LastName = newUserRegistrationIm.LastName,
                        LanguageId = userLanguage.Id
                    });

                if (newUserRegistrationIm.CompanyGuid == null)
                {
                    CreateCompanyAndAddCurrentUser(newUserRegistrationIm, newUser, Guid.Parse(currentUserGuid.Id), userSavedData, userLanguage);
                }
                else
                {
                    CheckIfCurrentUserIsOwnerOfCompanyAndAddNewUserToThisCompany(newUserRegistrationIm,
                        Guid.Parse(currentUserGuid.Id), userSavedData, userLanguage);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                var user = new UserAccount()
                {
                    Email = newUserRegistrationIm.Email,
                    Guid = new Guid(newUser.Id)
                };

                userSavedData = _userAccountWriteOnlyRepository.InsertData(user);
            }

            return userSavedData.Guid;
        }

        private void CheckIfCurrentUserIsOwnerOfCompanyAndAddNewUserToThisCompany(
            NewUserRegistrationIm newUserRegistrationIm, Guid? currentUserGuid, UserAccount userSavedData, Language userLanguage)
        {
            if (currentUserGuid.HasValue)
            {
                var company =
                    _companyRoRepository.GetFirst(x => x.Guid == newUserRegistrationIm.CompanyGuid);
                if (company.OwnerGuid == currentUserGuid)
                {
                    _usersInCompanyWoRepo.InsertData(new UsersInCompany()
                    {
                        CompanyGuid = company.Guid,
                        AddedByGuid = currentUserGuid.Value,
                        AddedDate = DateTime.Now,
                        UserAccountGuid = userSavedData.Guid
                    });
                }
                else
                {
                    throw new Exception(Translate.Get(
                        () => Translations.FAS.UserManager.NewUserRegistration.ToAddNewUserYouMustBeCompanyOwner,
                        userLanguage));
                }
            }
            else
            {
                throw new Exception(Translate.Get(() => Translations.FAS.UserManager.NewUserRegistration.IfYouWantToAddNewUserToCompany, userLanguage));
            }
        }

        private void CreateCompanyAndAddCurrentUser(NewUserRegistrationIm newUserRegistrationIm, T newUser,
            Guid? currentUserGuid, UserAccount userSavedData, Language userLanguage)
        {
            if (string.IsNullOrEmpty(newUserRegistrationIm.CompanyName))
            {
                newUserRegistrationIm.CompanyName = Translate.Get(()=> Translations.FAS.UserManager.NewUserRegistration.DefaultPrivateCompanyName, userLanguage);
            }

            var company = _companyWoRepository.InsertData(new Company()
            {
                CompanyName = newUserRegistrationIm.CompanyName,
                Created = DateTime.Now,
                CreatedByGuid = new Guid(newUser.Id),
                IsActive = true,
                OwnerGuid = new Guid(newUser.Id)
            });

            _usersInCompanyWoRepo.InsertData(new UsersInCompany()
            {
                CompanyGuid = company.Guid,
                AddedByGuid = currentUserGuid.Value,
                AddedDate = DateTime.Now,
                UserAccountGuid = userSavedData.Guid
            });
        }
    }
}