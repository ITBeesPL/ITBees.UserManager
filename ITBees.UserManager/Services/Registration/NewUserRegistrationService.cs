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
using ITBees.RestfulApiControllers.Exceptions;
using ITBees.RestfulApiControllers.Models;
using ITBees.Translations;
using ITBees.UserManager.Controllers.Models;
using ITBees.UserManager.Interfaces;
using ITBees.UserManager.Interfaces.Models;
using ITBees.UserManager.Services.Acl;
using ITBees.UserManager.Services.Mailing;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace ITBees.UserManager.Services.Registration
{
    public class NewUserRegistrationService<T, TCompany> : INewUserRegistrationService,
        INewUserRegistrationService<TCompany>
        where T : IdentityUser<Guid>, new() where TCompany : Company, new()
    {
        private readonly IUserManager<T> _userManager;
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
        private readonly IReadOnlyRepository<UserAccount> _userAccountRoRepo;
        private readonly IWriteOnlyRepository<InvoiceData> _invoiceDataRwRepo;
        private readonly IWriteOnlyRepository<TCompany> _companyRwRepo;

        public NewUserRegistrationService(IUserManager<T> userManager,
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
            IWriteOnlyRepository<UsersInvitationsToCompanies> usersInvitationsToCompaniesRwRepo,
            IReadOnlyRepository<UserAccount> userAccountRoRepo,
            IWriteOnlyRepository<InvoiceData> invoiceDataRwRepo)
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
            _userAccountRoRepo = userAccountRoRepo;
            _invoiceDataRwRepo = invoiceDataRwRepo;
        }

        public async Task<NewUserRegistrationResult> CreateNewUser(NewUserRegistrationIm newUserRegistrationIm,
            bool sendConfirmationEmail = true,
            AdditionalInvoiceDataIm additionalInvoiceDataIm = null,
            IInvitationEmailBodyCreator invitationEmailCreator = null,
            bool inviteToSetPassword = false,
            bool useTCompanyRepository = false)
        {
            var newUser = new T()
            {
                UserName = newUserRegistrationIm.Email,
                Email = newUserRegistrationIm.Email
            };

            var password = string.IsNullOrEmpty(newUserRegistrationIm.Password)
                ? GenerateRandomPassword()
                : newUserRegistrationIm.Password;

            var result = await _userManager.CreateAsync(newUser, password);
            var userLanguage = GetUserLanguage(newUserRegistrationIm);
            Guid? invoiceDataGuid = null;

            if (result.Succeeded == false)
            {
                if (result.Errors.Any(x => x.Code == "DuplicateUserName"))
                    throw new Exception(
                        Translate.Get(() => Translations.UserManager.NewUserRegistration.EmailAlreadyRegistered,
                            userLanguage) + $" : {newUserRegistrationIm.Email}");
                StringBuilder translatedErrors = new StringBuilder();
                foreach (var identityError in result.Errors)
                {
                    var code = Translate.Get(typeof(Translations.UserManager.NewUserRegistration.Errors),
                        identityError.Code, userLanguage, true);
                    var description = Translate.Get(typeof(Translations.UserManager.NewUserRegistration.Errors),
                        identityError.Description, userLanguage, true);
                    translatedErrors.AppendLine($"{code} - {description}");
                }

                throw new ArgumentException(translatedErrors.ToString());
            }

            UserAccount userSavedData = null;
            var currentUserGuid = await _userManager.FindByEmailAsync(newUserRegistrationIm.Email);
            var emailConfirmationToken = string.Empty;
            var setPasswordToken = string.Empty;

            if (sendConfirmationEmail)
            {
                emailConfirmationToken = await _userManager.GenerateEmailConfirmationTokenAsync(currentUserGuid);
            }

            if (inviteToSetPassword)
            {
                setPasswordToken = await _userManager.GeneratePasswordResetTokenAsync(currentUserGuid);
            }

            TCompany company = null;
            try
            {
                userSavedData = _userAccountWriteOnlyRepository.InsertData(
                    new UserAccount()
                    {
                        Email = newUserRegistrationIm.Email,
                        Guid = newUser.Id,
                        Phone = newUserRegistrationIm.Phone,
                        FirstName = newUserRegistrationIm.FirstName,
                        LastName = newUserRegistrationIm.LastName,
                        LanguageId = userLanguage.Id,
                        SetupTime = DateTime.Now,
                    });


                company = CreateCompanyAndAddCurrentUser(newUserRegistrationIm, newUser, currentUserGuid.Id,
                    userSavedData, userLanguage);

                if (additionalInvoiceDataIm != null)
                {
                    invoiceDataGuid = _invoiceDataRwRepo.InsertData(new InvoiceData()
                    {
                        CompanyGuid = company.Guid,
                        CompanyName = additionalInvoiceDataIm.CompanyName,
                        Created = DateTime.Now,
                        City = additionalInvoiceDataIm.City,
                        IsActive = true,
                        CreatedByGuid = currentUserGuid.Id,
                        Country = additionalInvoiceDataIm.Country,
                        Street = additionalInvoiceDataIm.Street,
                        InvoiceEmail = additionalInvoiceDataIm.InvoiceEmail,
                        InvoiceRequested = additionalInvoiceDataIm.InvoiceRequested,
                        PostCode = additionalInvoiceDataIm.PostCode,
                        NIP = additionalInvoiceDataIm.NIP
                    }).Guid;
                }

                if (sendConfirmationEmail)
                {
                    EmailMessage emailMessage = null;
                    if (invitationEmailCreator == null)
                    {
                        emailMessage = _registrationEmailComposer.ComposeEmailConfirmation(newUserRegistrationIm,
                            emailConfirmationToken);
                    }
                    else
                    {
                        emailMessage = invitationEmailCreator.Create(newUserRegistrationIm,
                            emailConfirmationToken, setPasswordToken);
                    }

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
                    Guid = newUser.Id
                };

                if (userSavedData == null)
                {
                    throw new Exception(e.Message);
                }

                return new NewUserRegistrationResult(userSavedData.Guid, e.Message, invoiceDataGuid, company.Guid);
            }

            return new NewUserRegistrationResult(userSavedData.Guid, string.Empty, invoiceDataGuid, company.Guid);
        }

        public async Task<NewUserRegistrationResult> CreateNewPartnerUser<TCompany>(
            NewUserRegistrationIm newUserRegistrationInputDto,
            bool sendConfirmationEmail,
            IInvitationEmailBodyCreator invitationEmailCreator,
            AdditionalInvoiceDataIm additionalInvoiceDataIm,
            bool inviteToSetPassword) where TCompany : Company
        {
            return await CreateNewUser(newUserRegistrationInputDto, sendConfirmationEmail, additionalInvoiceDataIm,
                invitationEmailCreator, inviteToSetPassword);
        }

        private static string GenerateRandomPassword()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*()-_=+";
            Random random = new Random();
            char[] password = new char[20];

            for (int i = 0; i < password.Length; i++)
            {
                password[i] = chars[random.Next(chars.Length)];
            }

            return new string(password);
        }

        public async Task<NewUserRegistrationResult> CreateAndInviteNewUserToCompany(
            NewUserRegistrationWithInvitationIm newUserRegistrationIm, 
            string accountEmailActivationBaseLink = "")
        {
            try
            {
                var companyGuid = newUserRegistrationIm.CompanyGuid;
                var userLanguage = GetUserLanguage(newUserRegistrationIm);
                if (companyGuid == null)
                {
                    throw new Exception(Translate.Get(
                        () => ITBees.UserManager.Translations.UserManager.NewUserRegistration
                            .ToInviteNewUserYouMustSpecifyTargetCompany, userLanguage));
                }

                var currentUser = _aspCurrentUserService.GetCurrentUser();
                if (currentUser == null)
                {
                    throw new YouMustBeLoggedInToCreateNewUserInvitationException(Translate.Get(
                        () => ITBees.UserManager.Translations.UserManager.NewUserRegistration
                            .YouMustBeLoggedInToAddNewUser,
                        newUserRegistrationIm.Language));
                }

                var accessControlResult = _accessControlService.CanDo(currentUser,
                    typeof(NewUserRegistrationService<T, TCompany>),
                    nameof(this.CreateAndInviteNewUserToCompany), companyGuid.Value); //Todo security implementation

                if (accessControlResult.CanDoResult == false)
                    throw new Exception(accessControlResult.Message);

                var newUser = new T()
                {
                    UserName = newUserRegistrationIm.Email,
                    Email = newUserRegistrationIm.Email
                };

                var company = _companyRoRepository.GetFirst(x => x.Guid == companyGuid);

                IdentityResult
                    result = await _userManager.CreateAsync(newUser,
                        Guid.NewGuid()
                            .ToString()); //create temporary password, which should be changed after email confirmation
                var emailConfirmationToken = string.Empty;

                EmailMessage emailMessage = null;
                var platformDefaultEmailAccount = _platformSettingsService.GetPlatformDefaultEmailAccount();

                if (result.Succeeded == false)
                {
                    if (result.Errors.Any(x =>
                            x.Code ==
                            "DuplicateUserName")) // user has account already created on platform, so we can send only invitation for him
                    {
                        var alreadyRegisteredUser = await _userManager.FindByEmailAsync(newUserRegistrationIm.Email);

                        var usersInvitationsToCompaniesList = _usersInvitationsToCompaniesRoRepo.GetData(x =>
                                x.UserAccountGuid == alreadyRegisteredUser.Id && x.CompanyGuid == companyGuid)
                            .ToList();
                        if (usersInvitationsToCompaniesList.Any() == false)
                        {
                            CreateNewUserInvitationDbRecord(companyGuid, alreadyRegisteredUser, currentUser,
                                newUserRegistrationIm.UserRoleGuid);
                        }

                        emailMessage = _registrationEmailComposer.ComposeEmailWithInvitationToOrganization(
                            newUserRegistrationIm, company.CompanyName, currentUser.DisplayName, userLanguage, 
                            accountEmailActivationBaseLink);

                        if (newUserRegistrationIm.SendEmailInvitation)
                            _emailSendingService.SendEmail(platformDefaultEmailAccount, emailMessage);

                        return new NewUserRegistrationResult(alreadyRegisteredUser.Id, string.Empty, null,
                            company.Guid);
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
                    _userAccountWriteOnlyRepository.InsertData(
                        new UserAccount()
                        {
                            Email = newUserRegistrationIm.Email,
                            Guid = newUser.Id,
                            Phone = newUserRegistrationIm.Phone,
                            FirstName = newUserRegistrationIm.FirstName,
                            LastName = newUserRegistrationIm.LastName,
                            LanguageId = userLanguage.Id,
                            SetupTime = DateTime.Now
                        });
                    emailConfirmationToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    CreateNewUserInvitationDbRecord(companyGuid, user, currentUser, newUserRegistrationIm.UserRoleGuid);
                    var token = await _userManager.GenerateEmailConfirmationTokenAsync(newUser);
                    emailMessage = _registrationEmailComposer.ComposeEmailWithUserCreationAndInvitationToOrganization(
                        newUserRegistrationIm, company.CompanyName, token, userLanguage, accountEmailActivationBaseLink);

                    if (newUserRegistrationIm.SendEmailInvitation)
                        _emailSendingService.SendEmail(platformDefaultEmailAccount, emailMessage);

                    return new NewUserRegistrationResult(user.Id, string.Empty, null, company.Guid);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        public async Task ResendConfirmationEmail(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            var userLanguage = _userAccountRoRepo.GetData(x => x.Email == email, x => x.Language).First();

            var emailConfirmationToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var emailMessage =
                _registrationEmailComposer.ComposeEmailConfirmation(
                    new NewUserRegistrationIm() { Email = email, Language = userLanguage.Language.Code },
                    emailConfirmationToken);
            var platformEmailAccount = _platformSettingsService.GetPlatformDefaultEmailAccount();

            _emailSendingService.SendEmail(platformEmailAccount, emailMessage);
        }

        public async Task ResendInvitationToCompany(InvitationResendIm invitationIm)
        {
            var user = await _userManager.FindByEmailAsync(invitationIm.UserEmail);
            if (user == null)
            {
                throw new FasApiErrorException(new FasApiErrorVm(
                    Translate.Get(() => Translations.UserInvitation.UserForSpecifiedEmailNotFound,
                        _aspCurrentUserService.GetCurrentUser().Language), 400, ""));
            }

            var usersInvitationToCompany = _usersInvitationsToCompaniesRoRepo
                .GetData(x => x.CompanyGuid == invitationIm.CompanyGuid && x.UserAccountGuid == user.Id)
                .FirstOrDefault();
            if (user.EmailConfirmed)
            {
                if (usersInvitationToCompany == null)
                {
                    throw new FasApiErrorException(new FasApiErrorVm(
                        Translate.Get(() => Translations.UserInvitation.InvitationNotExists,
                            _aspCurrentUserService.GetCurrentUser().Language), 400, ""));
                }

                if (usersInvitationToCompany.Applied)
                {
                    throw new FasApiErrorException(new FasApiErrorVm(
                        Translate.Get(() => Translations.UserInvitation.InvitationAlreadyAccepted,
                            _aspCurrentUserService.GetCurrentUser().Language), 400, ""));
                }
            }
            else
            {
            }
        }

        private void CreateNewUserInvitationDbRecord(Guid? companyGuid, dynamic alreadyRegisteredUser,
            CurrentUser currentUser, Guid? fasIdentityRoleGuid)
        {
            _usersInvitationsToCompaniesRwRepo.InsertData(new UsersInvitationsToCompanies()
            {
                CompanyGuid = companyGuid.Value,
                UserAccountGuid = alreadyRegisteredUser.Id,
                CreatedByGuid = currentUser.Guid,
                CreatedDate = DateTime.Now,
                IdentityRoleId = fasIdentityRoleGuid
            });
        }

        private Language GetUserLanguage(IVmWithLanguageDefined newUserRegistrationIm)
        {
            Language userLanguage = null;
            userLanguage = newUserRegistrationIm.Language != null
                ? new DerivedAsTFromStringClassResolver<Language>().GetInstance(newUserRegistrationIm.Language)
                : new En();

            return userLanguage;
        }

        private void AddNewUserToCompany(
            NewUserRegistrationWithInvitationIm newUserRegistrationIm, Guid? currentUserGuid, UserAccount userSavedData,
            Language userLanguage)
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
                throw new Exception(Translate.Get(
                    () => Translations.UserManager.NewUserRegistration.IfYouWantToAddNewUserToCompany, userLanguage));
            }
        }

        private TCompany CreateCompanyAndAddCurrentUser(NewUserRegistrationIm newUserRegistrationIm, T newUser,
            Guid? currentUserGuid, UserAccount userSavedData, Language userLanguage)
        {
            if (string.IsNullOrEmpty(newUserRegistrationIm.CompanyName))
            {
                newUserRegistrationIm.CompanyName = Translate.Get(
                    () => Translations.UserManager.NewUserRegistration.DefaultPrivateCompanyName, userLanguage);
            }

            var company = _companyWoRepository.InsertData(new TCompany()
            {
                CompanyName = newUserRegistrationIm.CompanyName,
                Created = DateTime.Now,
                CreatedByGuid = newUser.Id,
                IsActive = true,
                OwnerGuid = newUser.Id
            });

            _userAccountWriteOnlyRepository.UpdateData(x => x.Guid == newUser.Id,
                x => { x.LastUsedCompanyGuid = company.Guid; });

            _usersInCompanyWoRepo.InsertData(new UsersInCompany()
            {
                CompanyGuid = company.Guid,
                AddedByGuid = currentUserGuid.Value,
                AddedDate = DateTime.Now,
                UserAccountGuid = userSavedData.Guid
            });

            return company;
        }
    }
}