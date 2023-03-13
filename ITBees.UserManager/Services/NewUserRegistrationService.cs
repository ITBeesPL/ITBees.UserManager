using System;
using System.Threading.Tasks;
using InheritedMapper;
using ITBees.BaseServices.Platforms.Interfaces;
using ITBees.Interfaces.Repository;
using ITBees.Models.Companies;
using ITBees.Models.Languages;
using ITBees.Models.Users;
using ITBees.UserManager.Interfaces.Models;
using ITBees.UserManager.Interfaces.Services;
using Microsoft.AspNetCore.Identity;

namespace ITBees.UserManager.Services
{
    public class NewUserRegistrationService<T> : INewUserRegistrationService where T : IdentityUser, new()
    {
        private readonly IUserManager _userManager;
        private readonly IWriteOnlyRepository<UserAccount> _userAccountWriteOnlyRepository;
        private readonly IReadOnlyRepository<UserAccount> _userAccountReadOnlyRepository;
        private readonly IWriteOnlyRepository<Company> _companyWoRepository;
        private readonly IReadOnlyRepository<Company> _companyRoRepository;
        private readonly IAspCurrentUserService _aspCurrentUserService;
        private readonly IWriteOnlyRepository<UsersInCompany> _usersInCompanyWoRepo;
        private readonly ITranslator _translator;

        public NewUserRegistrationService(IUserManager userManager,
            IWriteOnlyRepository<UserAccount> userAccountWriteOnlyRepository,
            IReadOnlyRepository<UserAccount> userAccountReadOnlyRepository,
            IWriteOnlyRepository<Company> companyWoRepository,
            IReadOnlyRepository<Company> companyRoRepository,
            IAspCurrentUserService aspCurrentUserService,
            IWriteOnlyRepository<UsersInCompany> usersInCompanyWoRepo,
            ITranslator translator)
        {
            _userManager = userManager;
            _userAccountWriteOnlyRepository = userAccountWriteOnlyRepository;
            _userAccountReadOnlyRepository = userAccountReadOnlyRepository;
            _companyWoRepository = companyWoRepository;
            _companyRoRepository = companyRoRepository;
            _aspCurrentUserService = aspCurrentUserService;
            _usersInCompanyWoRepo = usersInCompanyWoRepo;
            _translator = translator;
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

            var languageId = new DerivedAsTFromStringClassResolver<Language>().GetInstance(newUserRegistrationIm.Language);
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
                        LanguageId = languageId.Id
                    });

                if (newUserRegistrationIm.CompanyGuid == null)
                {
                    CreateCompanyAndAddCurrentUser(newUserRegistrationIm, newUser, Guid.Parse(currentUserGuid.Id), userSavedData);
                }
                else
                {
                    CheckIfCurrentUserIsOwnerOfCompanyAndAddNewUserToThisCompany(newUserRegistrationIm, Guid.Parse(currentUserGuid.Id), userSavedData);
                }
            }
            catch (Exception e)
            {
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
            NewUserRegistrationIm newUserRegistrationIm, Guid? currentUserGuid, UserAccount userSavedData)
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
                    throw new Exception("To add new user You must be company owner!");
                }
            }
            else
            {
                throw new Exception("If You want to add new user to company, you have to be authenticated!");
            }
        }

        private void CreateCompanyAndAddCurrentUser(NewUserRegistrationIm newUserRegistrationIm, T newUser,
            Guid? currentUserGuid, UserAccount userSavedData)
        {
            if (string.IsNullOrEmpty(newUserRegistrationIm.CompanyName))
            {
                newUserRegistrationIm.CompanyName = "Private";
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