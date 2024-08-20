using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using InheritedMapper;
using ITBees.Interfaces.Platforms;
using ITBees.Interfaces.Repository;
using ITBees.Mailing.Interfaces;
using ITBees.Models.Companies;
using ITBees.Models.EmailMessages;
using ITBees.Models.Languages;
using ITBees.Models.Users;
using ITBees.Translations;
using ITBees.UserManager.Interfaces;
using ITBees.UserManager.Interfaces.Models;
using ITBees.UserManager.Services.Acl;
using ITBees.UserManager.Services.Mailing;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace ITBees.UserManager.Services.Registration
{
    public class NewUserRegistrationService<T, TCompany> : INewUserRegistrationService where T : IdentityUser, new() where TCompany : Company, new()
    {
        private readonly IUserManager _userManager;
        private readonly IWriteOnlyRepository<UserAccount> _userAccountWriteOnlyRepository;
        private readonly IWriteOnlyRepository<TCompany> _companyWoRepository;
        private readonly IReadOnlyRepository<TCompany> _companyRoRepository;
        private readonly IWriteOnlyRepository<UsersInCompany> _usersInCompanyWoRepo;
        private readonly ILogger<NewUserRegistrationService<T, TCompany>> _logger;
        private readonly IAspCurrentUserService _aspCurrentUserService;
        private readonly IRegistrationEmailComposer _registrationEmailComposer;
        private readonly IAccessControlService _accessControlService;
        private readonly IEmailSendingService _emailSendingService;
        private readonly IPlatformSettingsService _platformSettingsService;
        private readonly IReadOnlyRepository<UsersInvitationsToCompanies> _usersInvitationsToCompaniesRoRepo;
        private readonly IWriteOnlyRepository<UsersInvitationsToCompanies> _usersInvitationsToCompaniesRwRepo;

        public NewUserRegistrationService(IUserManager userManager,
            IWriteOnlyRepository<UserAccount> userAccountWriteOnlyRepository,
            IWriteOnlyRepository<TCompany> companyWoRepository,
            IReadOnlyRepository<TCompany> companyRoRepository,
            IWriteOnlyRepository<UsersInCompany> usersInCompanyWoRepo,
            ILogger<NewUserRegistrationService<T, TCompany>> logger,
            IAspCurrentUserService aspCurrentUserService,
            IRegistrationEmailComposer registrationEmailComposer,
            IAccessControlService accessControlService,
            IEmailSendingService emailSendingService,
            IPlatformSettingsService platformSettingsService,
            IReadOnlyRepository<UsersInvitationsToCompanies> usersInvitationsToCompaniesRoRepo,
            IWriteOnlyRepository<UsersInvitationsToCompanies> usersInvitationsToCompaniesRwRepo
            )
        {
            _userManager = userManager;
            _userAccountWriteOnlyRepository = userAccountWriteOnlyRepository;
            _companyWoRepository = companyWoRepository;
            _companyRoRepository = companyRoRepository;
            _usersInCompanyWoRepo = usersInCompanyWoRepo;
            _logger = logger;
            _aspCurrentUserService = aspCurrentUserService;
            _registrationEmailComposer = registrationEmailComposer;
            _accessControlService = accessControlService;
            _emailSendingService = emailSendingService;
            _platformSettingsService = platformSettingsService;
            _usersInvitationsToCompaniesRoRepo = usersInvitationsToCompaniesRoRepo;
            _usersInvitationsToCompaniesRwRepo = usersInvitationsToCompaniesRwRepo;
        }

        public async Task<NewUserRegistrationResult> CreateNewUser(NewUserRegistrationIm newUserRegistrationIm, bool sendConfirmationEmail = true)
        {
            var newUser = new T()
            {
                UserName = newUserRegistrationIm.Email,
                Email = newUserRegistrationIm.Email
            };

            var result = await _userManager.CreateAsync(newUser, newUserRegistrationIm.Password);
            var userLanguage = GetUserLanguage(newUserRegistrationIm);

            if (result.Succeeded == false)
            {
                if (result.Errors.Any(x => x.Code == "DuplicateUserName"))
                    throw new Exception(Translate.Get(() => Translations.UserManager.NewUserRegistration.EmailAlreadyRegistered, userLanguage));
                StringBuilder translatedErrors = new StringBuilder();
                foreach (var identityError in result.Errors)
                {
                    var code = Translate.Get(typeof(Translations.UserManager.NewUserRegistration.Errors), identityError.Code, userLanguage, true);
                    var description = Translate.Get(typeof(Translations.UserManager.NewUserRegistration.Errors), identityError.Description, userLanguage, true);
                    translatedErrors.AppendLine($"{code} - {description}");
                }
                throw new ArgumentException(translatedErrors.ToString());
            }

            UserAccount userSavedData = null;
            var currentUserGuid = await _userManager.FindByEmailAsync(newUserRegistrationIm.Email);
            var emailConfirmationToken = string.Empty;

            if (sendConfirmationEmail)
            {
                emailConfirmationToken = await _userManager.GenerateEmailConfirmationTokenAsync(currentUserGuid);
            }
            else
            {

            }

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
                        LanguageId = userLanguage.Id,
                        SetupTime = DateTime.Now
                    });


                CreateCompanyAndAddCurrentUser(newUserRegistrationIm, newUser, new Guid(currentUserGuid.Id),
                    userSavedData, userLanguage);

                if (sendConfirmationEmail)
                {
                    var emailMessage =
                        _registrationEmailComposer.ComposeEmailConfirmation(newUserRegistrationIm,
                            emailConfirmationToken);
                    var platformEmailAccount = _platformSettingsService.GetPlatformDefaultEmailAccount();

                    _emailSendingService.SendEmail(platformEmailAccount, emailMessage);
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

                if (userSavedData == null)
                {
                    throw new Exception(e.Message);
                }

                return new NewUserRegistrationResult(userSavedData.Guid, e.Message);
            }

            return new NewUserRegistrationResult(userSavedData.Guid, string.Empty);
        }

        public async Task<NewUserRegistrationResult> CreateAndInviteNewUserToCompany(NewUserRegistrationWithInvitationIm newUserRegistrationIm)
        {
            var companyGuid = newUserRegistrationIm.CompanyGuid;
            var userLanguage = GetUserLanguage(newUserRegistrationIm);
            if (companyGuid == null)
            {
                throw new Exception(Translate.Get(() => ITBees.UserManager.Translations.UserManager.NewUserRegistration.ToInviteNewUserYouMustSpecifyTargetCompany, userLanguage));
            }

            var currentUser = _aspCurrentUserService.GetCurrentUser();
            if (currentUser == null)
            {
                throw new YouMustBeLoggedInToCreateNewUserInvitationException(Translate.Get(() => ITBees.UserManager.Translations.UserManager.NewUserRegistration.YouMustBeLoggedInToAddNewUser, newUserRegistrationIm.Language));
            }

            var accessControlResult = _accessControlService.CanDo(currentUser, typeof(NewUserRegistrationService<T,TCompany>),
                nameof(this.CreateAndInviteNewUserToCompany), companyGuid.Value); //Todo security implementation

            if (accessControlResult.CanDoResult == false)
                throw new Exception(accessControlResult.Message);

            var newUser = new T()
            {
                UserName = newUserRegistrationIm.Email,
                Email = newUserRegistrationIm.Email
            };

            var company = _companyRoRepository.GetFirst(x => x.Guid == companyGuid);

            IdentityResult result = await _userManager.CreateAsync(newUser, Guid.NewGuid().ToString()); //create temporary password, which should be changed after email confirmation
            var emailConfirmationToken = string.Empty;

            EmailMessage emailMessage = null;
            var platformDefaultEmailAccount = _platformSettingsService.GetPlatformDefaultEmailAccount();

            if (result.Succeeded == false)
            {
                if (result.Errors.Any(x => x.Code == "DuplicateUserName")) // user has account already created on platform, so we can send only invitation for him
                {
                    var alreadyRegisteredUser = await _userManager.FindByEmailAsync(newUserRegistrationIm.Email);

                    var usersInvitationsToCompaniesList = _usersInvitationsToCompaniesRoRepo.GetData(x =>
                        x.UserAccountGuid == Guid.Parse(alreadyRegisteredUser.Id) && x.CompanyGuid == companyGuid).ToList();
                    if (usersInvitationsToCompaniesList.Any() == false)
                    {
                        CreateNewUserInvitationDbRecord(companyGuid, alreadyRegisteredUser, currentUser);
                    }
                    emailMessage = _registrationEmailComposer.ComposeEmailWithInvitationToOrganization(newUserRegistrationIm, company.CompanyName, currentUser.DisplayName, userLanguage);

                    _emailSendingService.SendEmail(platformDefaultEmailAccount, emailMessage);

                    return new NewUserRegistrationResult(new Guid(alreadyRegisteredUser.Id), string.Empty);
                }
                else
                {
                    throw new Exception(Translate.Get(
                        () => Translations.UserManager.NewUserRegistration.Errors.ErrorWhileRegisteringAUserAccount,
                        userLanguage));
                }
            }
            else
            {
                var user = await _userManager.FindByEmailAsync(newUser.Email);
                emailConfirmationToken = await _userManager.GenerateEmailConfirmationTokenAsync(result);
                CreateNewUserInvitationDbRecord(companyGuid, user, currentUser);
            }

            UserAccount userSavedData = null;

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
                        LanguageId = userLanguage.Id,
                        SetupTime = DateTime.Now
                    });
                var token = await _userManager.GenerateEmailConfirmationTokenAsync(newUserRegistrationIm.Email);
                emailMessage = _registrationEmailComposer.ComposeEmailWithUserCreationAndInvitationToOrganization(newUserRegistrationIm, company.CompanyName, token, userLanguage);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                var user = new UserAccount()
                {
                    Email = newUserRegistrationIm.Email,
                    Guid = new Guid(newUser.Id)
                };

                if (userSavedData == null)
                {
                    throw new Exception(e.Message);
                }

                return new NewUserRegistrationResult(userSavedData.Guid, e.Message);
            }

            return new NewUserRegistrationResult(userSavedData.Guid, string.Empty);
        }

        private void CreateNewUserInvitationDbRecord(Guid? companyGuid, IdentityUser alreadyRegisteredUser,
            CurrentUser currentUser)
        {
            _usersInvitationsToCompaniesRwRepo.InsertData(new UsersInvitationsToCompanies()
            {
                CompanyGuid = companyGuid.Value,
                UserAccountGuid = Guid.Parse(alreadyRegisteredUser.Id),
                CreatedByGuid = currentUser.Guid,
                CreatedDate = DateTime.Now,
            });
        }

        private Language GetUserLanguage(IVmWithLanguageDefined newUserRegistrationIm)
        {
            Language userLanguage = null;
            userLanguage = newUserRegistrationIm.Language != null ? new DerivedAsTFromStringClassResolver<Language>().GetInstance(newUserRegistrationIm.Language) : new En();

            return userLanguage;
        }

        private void AddNewUserToCompany(
            NewUserRegistrationWithInvitationIm newUserRegistrationIm, Guid? currentUserGuid, UserAccount userSavedData, Language userLanguage)
        {
            if (currentUserGuid.HasValue)
            {
                var company =
                    _companyRoRepository.GetData(x => x.Guid == newUserRegistrationIm.CompanyGuid).FirstOrDefault();

                if (company == null)
                    throw new Exception(Translate.Get(
                        () => Translations.UserManager.NewUserRegistration.CouldNotFindCompanyWithSpecifiedGuid,
                        userLanguage));

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
                        () => Translations.UserManager.NewUserRegistration.ToAddNewUserYouMustBeCompanyOwner,
                        userLanguage));
                }
            }
            else
            {
                throw new Exception(Translate.Get(() => Translations.UserManager.NewUserRegistration.IfYouWantToAddNewUserToCompany, userLanguage));
            }
        }

        private void CreateCompanyAndAddCurrentUser(NewUserRegistrationIm newUserRegistrationIm, T newUser,
            Guid? currentUserGuid, UserAccount userSavedData, Language userLanguage)
        {
            if (string.IsNullOrEmpty(newUserRegistrationIm.CompanyName))
            {
                newUserRegistrationIm.CompanyName = Translate.Get(() => Translations.UserManager.NewUserRegistration.DefaultPrivateCompanyName, userLanguage);
            }

            var company = _companyWoRepository.InsertData(new TCompany()
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