using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using InheritedMapper;
using ITBees.Interfaces.Platforms;
using ITBees.Interfaces.Repository;
using ITBees.Mailing.Interfaces;
using ITBees.Models.Companies;
using ITBees.Models.EmailMessages;
using ITBees.Models.Languages;
using ITBees.Models.Users;
using ITBees.RestfulApiControllers.Authorization;
using ITBees.RestfulApiControllers.Exceptions;
using ITBees.RestfulApiControllers.Models;
using ITBees.Translations;
using ITBees.UserManager.Controllers;
using ITBees.UserManager.Controllers.Models;
using ITBees.UserManager.Interfaces;
using ITBees.UserManager.Interfaces.Models;
using ITBees.UserManager.Permissions;
using ITBees.UserManager.Services.Acl;
using ITBees.UserManager.Services.Mailing;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
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
        private readonly IConfirmationUrlParametersStore _confirmationUrlParametersStore;
        private readonly IPermissionService _permissionService;
        private readonly IReadOnlyRepository<FasIdentityRole> _identityRoleRoRepo;

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
            IWriteOnlyRepository<InvoiceData> invoiceDataRwRepo,
            IConfirmationUrlParametersStore confirmationUrlParametersStore,
            IPermissionService permissionService,
            IReadOnlyRepository<FasIdentityRole> identityRoleRoRepo)
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
            _confirmationUrlParametersStore = confirmationUrlParametersStore;
            _permissionService = permissionService;
            _identityRoleRoRepo = identityRoleRoRepo;
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
                    throw new ArgumentException(
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

                _logger.LogError("Errors while creating new user: {errors}", translatedErrors.ToString());
                throw new ArgumentException(translatedErrors.ToString());
            }

            UserAccount userSavedData = null;
            var currentUserGuid = await _userManager.FindByEmailAsync(newUserRegistrationIm.Email);
            var emailConfirmationToken = string.Empty;
            var setPasswordToken = string.Empty;

            if (sendConfirmationEmail)
            {
                var raw = await _userManager.GenerateEmailConfirmationTokenAsync(currentUserGuid);
                emailConfirmationToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(raw));
            }

            if (inviteToSetPassword)
            {
                var raw = await _userManager.GeneratePasswordResetTokenAsync(currentUserGuid);
                setPasswordToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(raw));
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


                company = CreateCompanyAndAddCurrentUser(newUserRegistrationIm.CompanyName, newUser, currentUserGuid.Id,
                    userLanguage);

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

                StoreConfirmationUrlParameters(userSavedData.Guid,
                    newUserRegistrationIm.ConfirmationUrlParameters);

                if (sendConfirmationEmail)
                {
                    EmailMessage emailMessage = null;
                    if (invitationEmailCreator == null)
                    {
                        emailMessage = _registrationEmailComposer.ComposeEmailConfirmation(newUserRegistrationIm,
                            emailConfirmationToken, setPasswordToken, newUserRegistrationIm.CompanyName);
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
                var isInvalidRecipientEmail = IsSmtpRecipientRejection(e);

                if (isInvalidRecipientEmail)
                {
                    _logger.LogWarning(
                        "Registration rejected - invalid recipient email {email}: {message}. Rolling back created records.",
                        newUserRegistrationIm.Email, e.Message);
                }
                else
                {
                    _logger.LogError(e, "CreateNewUser error : {message}", e.Message);
                }

                if (userSavedData != null && (company == null || isInvalidRecipientEmail))
                {
                    if (!isInvalidRecipientEmail)
                    {
                        _logger.LogError(
                            "Registration failed after UserAccount {userGuid} ({email}) was created but before company assignment - rolling back to avoid orphaned user.",
                            newUser.Id, newUserRegistrationIm.Email);
                    }

                    RollBackCreatedRecords(newUser.Id, invoiceDataGuid);

                    try
                    {
                        var identityToDelete = await _userManager.FindByEmailAsync(newUserRegistrationIm.Email);
                        if (identityToDelete != null)
                        {
                            await _userManager.DeleteAccount(false, identityToDelete.Id);
                        }
                    }
                    catch (Exception cleanupEx)
                    {
                        _logger.LogError(cleanupEx,
                            "Rollback of IdentityUser for email {email} failed: {message}",
                            newUserRegistrationIm.Email, cleanupEx.Message);
                    }

                    if (isInvalidRecipientEmail)
                    {
                        var userLang = GetUserLanguage(newUserRegistrationIm);
                        var invalidEmailMsg = Translate.Get(
                            () => Translations.UserManager.NewUserRegistration.Errors.InvalidEmail, userLang);
                        throw new ArgumentException(
                            $"{invalidEmailMsg} : {newUserRegistrationIm.Email}", e);
                    }

                    throw new Exception($"Registration failed and was rolled back: {e.Message}", e);
                }

                if (userSavedData == null)
                {
                    throw new Exception(e.Message);
                }

                return new NewUserRegistrationResult(userSavedData.Guid, e.Message, invoiceDataGuid,
                    company?.Guid ?? Guid.Empty);
            }

            _logger.LogInformation("New user created with email {email} and guid {guid}", newUserRegistrationIm.Email,
                userSavedData.Guid);
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

        /// <summary>
        /// Removes what CreateNewUser wrote for the new account, in the order the foreign keys need:
        /// UserAccount.LastUsedCompanyGuid points at the new company while Company.CreatedByGuid and OwnerGuid
        /// point back at the account, so neither row can go while the other one still points at it. Until this
        /// was cleared first both deletes failed, and every refused e-mail address left an account without a
        /// login and its "Private" company behind - and that address could not register again. The account
        /// is brand new, so every company it created or owns was created by this registration.
        /// </summary>
        private void RollBackCreatedRecords(Guid userGuid, Guid? invoiceDataGuid)
        {
            TryRollBack("UserAccount.LastUsedCompanyGuid", userGuid, () =>
                _userAccountWriteOnlyRepository.UpdateData(x => x.Guid == userGuid,
                    x => { x.LastUsedCompanyGuid = null; }));

            if (invoiceDataGuid.HasValue)
            {
                TryRollBack("InvoiceData", userGuid, () =>
                    _invoiceDataRwRepo.DeleteData(x => x.Guid == invoiceDataGuid.Value));
            }

            TryRollBack("UsersInCompany", userGuid, () =>
                _usersInCompanyWoRepo.DeleteData(x => x.UserAccountGuid == userGuid));
            TryRollBack("Company", userGuid, () =>
                _companyWoRepository.DeleteData(x => x.CreatedByGuid == userGuid || x.OwnerGuid == userGuid));
            TryRollBack("UserConfirmationUrlParameters", userGuid, () =>
                _confirmationUrlParametersStore.Delete(userGuid));
            TryRollBack("UserAccount", userGuid, () =>
                _userAccountWriteOnlyRepository.DeleteData(x => x.Guid == userGuid));
        }

        private void TryRollBack(string what, Guid userGuid, Action rollBack)
        {
            try
            {
                rollBack();
            }
            catch (Exception cleanupEx)
            {
                _logger.LogError(cleanupEx,
                    "Rollback of {what} for user {userGuid} failed: {message}",
                    what, userGuid, cleanupEx.Message);
            }
        }

        private static bool IsSmtpRecipientRejection(Exception e)
        {
            var ex = e;
            while (ex != null)
            {
                var msg = ex.Message ?? string.Empty;
                if (msg.IndexOf("Recipient address rejected", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    msg.IndexOf("Domain not found", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    msg.IndexOf("Invalid recipient", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    msg.IndexOf("No such user", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    msg.StartsWith("5.1.1", StringComparison.OrdinalIgnoreCase) ||
                    msg.StartsWith("5.1.2", StringComparison.OrdinalIgnoreCase) ||
                    msg.StartsWith("5.1.3", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
                ex = ex.InnerException;
            }
            return false;
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
            string accountEmailActivationBaseLink = "",
            IExternalSecurityService externalSecurityService = null)
        {
            if (externalSecurityService == null)
            {
                externalSecurityService =
                    new DefaultSecurityService<T, TCompany>(_aspCurrentUserService, _accessControlService);
            }

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

                var currentUser = externalSecurityService.GetCurrentUser();

                if (currentUser == null)
                {
                    throw new YouMustBeLoggedInToCreateNewUserInvitationException(Translate.Get(
                        () => ITBees.UserManager.Translations.UserManager.NewUserRegistration
                            .YouMustBeLoggedInToAddNewUser,
                        newUserRegistrationIm.Language));
                }

                var accessControlResult = externalSecurityService.CheckUserAccessToMethod(currentUser,
                    typeof(NewUserRegistrationService<T, TCompany>),
                    nameof(this.CreateAndInviteNewUserToCompany), companyGuid.Value);

                if (accessControlResult.CanDoResult == false)
                    throw new Authorization403ForbiddenException(accessControlResult.Message);

                CheckRoleGrant(newUserRegistrationIm.UserRoleGuid, companyGuid.Value);

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

                var targetCompanyName = newUserRegistrationIm.InvitationToCompany ?? company.CompanyName;
                var inviterName = newUserRegistrationIm.InvitationCreatorName ?? currentUser.DisplayName;

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

                        // Invited before but has never set the password (or never confirmed the e-mail): the "accept"
                        // link of a registered user needs logging in, which he cannot do - he gets the "set the
                        // password" e-mail again, with fresh tokens. Setting it accepts his invitations (AcceptAccount).
                        emailMessage = alreadyRegisteredUser.EmailConfirmed
                            ? _registrationEmailComposer.ComposeEmailWithInvitationToOrganization(
                                newUserRegistrationIm, targetCompanyName, inviterName, userLanguage,
                                accountEmailActivationBaseLink)
                            : await ComposeSetPasswordInvitation(alreadyRegisteredUser, newUserRegistrationIm,
                                targetCompanyName, userLanguage, accountEmailActivationBaseLink);

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
                    _userAccountWriteOnlyRepository.InsertData(
                        new UserAccount()
                        {
                            Email = newUserRegistrationIm.Email,
                            Guid = newUser.Id,
                            Phone = newUserRegistrationIm.Phone,
                            FirstName = newUserRegistrationIm.FirstName,
                            LastName = newUserRegistrationIm.LastName,
                            LanguageId = userLanguage.Id,
                            SetupTime = DateTime.Now,
                            LastUsedCompanyGuid = companyGuid
                        });

                    CreateNewUserInvitationDbRecord(companyGuid, newUser, currentUser,
                        newUserRegistrationIm.UserRoleGuid);

                    var newUserCompany =
                        CreateCompanyAndAddCurrentUser(string.Empty, newUser, newUser.Id, userLanguage);

                    emailMessage = await ComposeSetPasswordInvitation(newUser, newUserRegistrationIm,
                        targetCompanyName, userLanguage, accountEmailActivationBaseLink);

                    if (newUserRegistrationIm.SendEmailInvitation)
                        _emailSendingService.SendEmail(platformDefaultEmailAccount, emailMessage);

                    return new NewUserRegistrationResult(newUser.Id, string.Empty, null, company.Guid);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        /// <summary>
        /// The invitation of an account without a password: fresh e-mail confirmation and password-reset tokens in the
        /// link where the invited person sets the password (POST /AcceptAccount, which also accepts his invitations).
        /// </summary>
        private async Task<EmailMessage> ComposeSetPasswordInvitation(T user,
            NewUserRegistrationWithInvitationIm newUserRegistrationIm, string targetCompanyName, Language userLanguage,
            string accountEmailActivationBaseLink)
        {
            var rawEmailToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var emailTokenB64 = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(rawEmailToken));

            _logger.LogInformation(
                "Email token generated for {email}. b64Len={b64Len}, b64Sha={b64Sha}, b64Prefix={b64Prefix}, rawLen={rawLen}, rawSha={rawSha}, rawPrefix={rawPrefix}",
                newUserRegistrationIm.Email,
                emailTokenB64.Length,
                TokenLogHelper.Sha256(emailTokenB64),
                TokenLogHelper.Prefix(emailTokenB64),
                rawEmailToken.Length,
                TokenLogHelper.Sha256(rawEmailToken),
                TokenLogHelper.Prefix(rawEmailToken)
            );

            // Password reset token
            var rawPasswordToken = await _userManager.GeneratePasswordResetTokenAsync(user);
            var passTokenB64 = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(rawPasswordToken));

            _logger.LogInformation(
                "Password reset token generated for {email}. b64Len={b64Len}, b64Sha={b64Sha}, b64Prefix={b64Prefix}, rawLen={rawLen}, rawSha={rawSha}, rawPrefix={rawPrefix}",
                newUserRegistrationIm.Email,
                passTokenB64.Length,
                TokenLogHelper.Sha256(passTokenB64),
                TokenLogHelper.Prefix(passTokenB64),
                rawPasswordToken.Length,
                TokenLogHelper.Sha256(rawPasswordToken),
                TokenLogHelper.Prefix(rawPasswordToken)
            );

            StoreConfirmationUrlParameters(user.Id, newUserRegistrationIm.ConfirmationUrlParameters);

            return _registrationEmailComposer.ComposeEmailWithUserCreationAndInvitationToOrganization(
                newUserRegistrationIm, targetCompanyName, emailTokenB64, userLanguage,
                accountEmailActivationBaseLink, passTokenB64);
        }

        /// <summary>
        /// The rule of docs/PERMISSIONS.md ("Granting roles"): a logged-in user gives only a role holding nothing they do
        /// not hold in the company. Without it anybody allowed to invite could invite anyone - himself too - with a
        /// full-access or the PlatformOperator role. The platform operator passes; a flow without a logged-in user
        /// (server-to-server, with its own IExternalSecurityService) is not checked here.
        /// </summary>
        private void CheckRoleGrant(Guid? roleGuid, Guid companyGuid)
        {
            if (roleGuid == null || _aspCurrentUserService.GetCurrentUserGuid() == null)
                return;

            var role = _identityRoleRoRepo.GetFirst(x => x.Id == roleGuid.Value);
            if (role == null)
                throw new FasApiErrorException("The role of the invitation does not exist.", 400);

            _permissionService.CheckRoleGrant(role, companyGuid);
        }

        private void StoreConfirmationUrlParameters(Guid userAccountGuid,
            List<ConfirmationUrlParameterIm> confirmationUrlParameters)
        {
            if (confirmationUrlParameters == null || confirmationUrlParameters.Count == 0)
            {
                return;
            }

            try
            {
                _confirmationUrlParametersStore.Save(userAccountGuid, confirmationUrlParameters);
            }
            catch (Exception e)
            {
                _logger.LogWarning(e,
                    "Could not store confirmation url parameters for user {userAccountGuid}, a resent activation link will not contain them.",
                    userAccountGuid);
            }
        }

        private List<ConfirmationUrlParameterIm> ReadConfirmationUrlParameters(Guid userAccountGuid)
        {
            try
            {
                return _confirmationUrlParametersStore.Get(userAccountGuid);
            }
            catch (Exception e)
            {
                _logger.LogWarning(e,
                    "Could not read confirmation url parameters for user {userAccountGuid}, resending the activation link without them.",
                    userAccountGuid);
                return null;
            }
        }

        public async Task ResendConfirmationEmail(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            var userLanguage = _userAccountRoRepo.GetData(x => x.Email == email, x => x.Language).First();

            var confirmationUrlParameters = ReadConfirmationUrlParameters(user.Id);

            var emailConfirmationTokenRaw = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var emailConfirmationToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(emailConfirmationTokenRaw));

            var emailMessage =
                _registrationEmailComposer.ComposeEmailConfirmation(
                    new NewUserRegistrationIm()
                    {
                        Email = email,
                        Language = userLanguage.Language.Code,
                        ConfirmationUrlParameters = confirmationUrlParameters
                    },
                    emailConfirmationToken, "", "");
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

            if (!string.IsNullOrEmpty(newUserRegistrationIm.Language))
            {
                try
                {
                    userLanguage = new DerivedAsTFromStringClassResolver<Language>()
                        .GetInstance(newUserRegistrationIm.Language);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "Unable to resolve Language instance for code '{code}' - falling back to En.",
                        newUserRegistrationIm.Language);
                }
            }

            return userLanguage ?? new Pl();
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

        private TCompany CreateCompanyAndAddCurrentUser(string companyName, T newUser,
            Guid? currentUserGuid, Language userLanguage)
        {
            var safeLanguage = userLanguage ?? new Pl();

            if (string.IsNullOrEmpty(companyName))
            {
                try
                {
                    companyName = Translate.Get(
                        () => Translations.UserManager.NewUserRegistration.DefaultPrivateCompanyName, safeLanguage);
                }
                catch (Exception translateEx)
                {
                    _logger.LogWarning(translateEx,
                        "Translate.Get failed for DefaultPrivateCompanyName - using hardcoded fallback.");
                }

                if (string.IsNullOrEmpty(companyName))
                {
                    companyName = "Prywatna";
                }
            }

            var company = _companyWoRepository.InsertData(new TCompany()
            {
                CompanyName = companyName,
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
                UserAccountGuid = currentUserGuid.Value,
            });

            return company;
        }
    }
}