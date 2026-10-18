using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ITBees.Interfaces.Platforms;
using ITBees.Mailing.Interfaces;
using ITBees.Models.Companies;
using ITBees.Models.EmailMessages;
using ITBees.Models.Roles;
using ITBees.Models.Users;
using ITBees.RestfulApiControllers.Authorization;
using ITBees.RestfulApiControllers.Exceptions;
using ITBees.UserManager.DbModels;
using ITBees.UserManager.Interfaces;
using ITBees.UserManager.Interfaces.Models;
using ITBees.UserManager.Permissions;
using ITBees.UserManager.Services;
using ITBees.UserManager.Services.Acl;
using ITBees.UserManager.Services.Mailing;
using ITBees.UserManager.Services.Registration;
using ITBees.UserManager.UnitTests.Fakes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
// ITBees.Models.Languages has a language class called Is (Icelandic).
using Is = NUnit.Framework.Is;

namespace ITBees.UserManager.UnitTests
{
    /// <summary>
    /// POST /NewInvitedUserRegistration: only someone from the company invites to it (AccessControlService), and a
    /// logged-in user gives only a role holding nothing he does not hold there (CheckRoleGrant). Before, any logged-in
    /// user could invite anybody - himself too - to any company with any role, PlatformOperator included.
    /// </summary>
    public class InvitationToCompanyTests
    {
        private const string OpenBarrier = "test.barrier.open";
        private const string EditTariff = "test.tariff.edit";
        private const string InvitedEmail = "nowy@firma.pl";

        private readonly Guid _companyGuid = Guid.NewGuid();
        private readonly Guid _employeeGuid = Guid.NewGuid();
        private readonly FasIdentityRole _gateOpener = new("GateOpener") { Id = Guid.NewGuid() };
        private readonly FasIdentityRole _tariffEditor = new("TariffEditor") { Id = Guid.NewGuid() };
        private readonly FasIdentityRole _fullAccess = new("Manager") { Id = Guid.NewGuid() };
        private readonly FasIdentityRole _platformOperator = new(Role.PlatformOperator) { Id = Guid.NewGuid() };

        private FakeIdentity _identity;
        private FakeRepository<UserAccount> _userAccounts;
        private FakeRepository<Company> _companies;
        private FakeRepository<UsersInCompany> _memberships;
        private FakeRepository<UsersInvitationsToCompanies> _invitations;
        private FakeRepository<FasIdentityRole> _roles;
        private FakeRepository<RolePermission> _rolePermissions;
        private List<(string Method, object[] Args)> _composedEmails;
        private int _sentEmails;

        [SetUp]
        public void SetUp()
        {
            _identity = new FakeIdentity();
            _userAccounts = new FakeRepository<UserAccount>(
                new UserAccount { Guid = _employeeGuid, Email = "pracownik@firma.pl" });
            _companies = new FakeRepository<Company>(
                new Company { Guid = _companyGuid, CompanyName = "Firma", OwnerGuid = Guid.NewGuid() });
            // The employee is a member of the company with a role that opens the barrier and nothing else.
            _memberships = new FakeRepository<UsersInCompany>(new UsersInCompany
            {
                UserAccountGuid = _employeeGuid, CompanyGuid = _companyGuid,
                IdentityRole = _gateOpener, IdentityRoleId = _gateOpener.Id
            });
            _invitations = new FakeRepository<UsersInvitationsToCompanies>();
            _roles = new FakeRepository<FasIdentityRole>(_gateOpener, _tariffEditor, _fullAccess, _platformOperator);
            _rolePermissions = new FakeRepository<RolePermission>(
                new RolePermission { RoleId = _gateOpener.Id, PermissionKey = OpenBarrier },
                new RolePermission { RoleId = _tariffEditor.Id, PermissionKey = OpenBarrier },
                new RolePermission { RoleId = _tariffEditor.Id, PermissionKey = EditTariff },
                new RolePermission { RoleId = _fullAccess.Id, PermissionKey = RolePermission.FullAccessKey });
            _composedEmails = new List<(string Method, object[] Args)>();
            _sentEmails = 0;
        }

        [Test]
        public async Task A_member_invites_with_a_role_holding_only_what_he_holds()
        {
            await Invite(Employee(), _gateOpener.Id);

            Assert.That(_invitations.Items.Single().IdentityRoleId, Is.EqualTo(_gateOpener.Id));
        }

        [Test]
        public void A_member_cannot_give_a_role_holding_more_than_he_holds()
        {
            Assert.ThrowsAsync<Authorization403ForbiddenException>(() => Invite(Employee(), _tariffEditor.Id));

            AssertNothingCreated();
        }

        [Test]
        public void A_member_cannot_give_a_full_access_role()
        {
            Assert.ThrowsAsync<Authorization403ForbiddenException>(() => Invite(Employee(), _fullAccess.Id));

            AssertNothingCreated();
        }

        [Test]
        public void A_member_cannot_invite_himself_as_the_platform_operator()
        {
            _identity.Users.Add(new IdentityUser<Guid> { Id = _employeeGuid, Email = "pracownik@firma.pl" });

            Assert.ThrowsAsync<Authorization403ForbiddenException>(() =>
                Invite(Employee(), _platformOperator.Id, email: "pracownik@firma.pl"));

            Assert.That(_invitations.Items, Is.Empty);
        }

        [Test]
        public async Task A_member_invites_without_a_role()
        {
            await Invite(Employee(), roleGuid: null);

            Assert.That(_invitations.Items.Single().IdentityRoleId, Is.Null);
        }

        [Test]
        public void Someone_outside_the_company_cannot_invite_to_it()
        {
            _memberships.Items.Clear();

            Assert.ThrowsAsync<Authorization403ForbiddenException>(() => Invite(Employee(), roleGuid: null));

            AssertNothingCreated();
        }

        [Test]
        public async Task The_platform_operator_gives_any_role_in_any_company()
        {
            var platformOperator = new CurrentUser
            {
                Guid = Guid.NewGuid(), DisplayName = "Operator", UserRoles = new[] { Role.PlatformOperator }
            };

            await Invite(platformOperator, _platformOperator.Id, isPlatformOperator: true);

            Assert.That(_invitations.Items.Single().IdentityRoleId, Is.EqualTo(_platformOperator.Id));
        }

        [Test]
        public async Task A_flow_without_a_logged_in_user_is_not_checked()
        {
            // Server-to-server: the application's own security service vouches for the call, nobody is logged in.
            var service = CreateService(loggedInUser: null);
            var applicationSecurity = Stub.Of<IExternalSecurityService>(
                ("GetCurrentUser", _ => new CurrentUser { Guid = Guid.NewGuid(), DisplayName = "System" }),
                ("CheckUserAccessToMethod", _ => new AccessControlResult(true, string.Empty)));

            await service.CreateAndInviteNewUserToCompany(Invitation(_fullAccess.Id), "", applicationSecurity);

            Assert.That(_invitations.Items.Single().IdentityRoleId, Is.EqualTo(_fullAccess.Id));
        }

        [Test]
        public void A_role_that_does_not_exist_is_refused()
        {
            var error = Assert.ThrowsAsync<FasApiErrorException>(() => Invite(Employee(), Guid.NewGuid()));

            Assert.That(error.FasApiErrorVm.StatusCode, Is.EqualTo(400));
            AssertNothingCreated();
        }

        [Test]
        public async Task The_owner_who_is_not_a_member_gives_a_full_access_role()
        {
            // Kilometrówka creates a company without adding its owner to UsersInCompany.
            _memberships.Items.Clear();
            _companies.Items[0].OwnerGuid = _employeeGuid;

            await Invite(Employee(), _fullAccess.Id);

            Assert.That(_invitations.Items.Single().IdentityRoleId, Is.EqualTo(_fullAccess.Id));
        }

        [Test]
        public async Task The_creator_of_a_company_without_an_owner_gives_a_full_access_role()
        {
            _companies.Items[0].OwnerGuid = null;
            _companies.Items[0].CreatedByGuid = _employeeGuid;

            await Invite(Employee(), _fullAccess.Id);

            Assert.That(_invitations.Items.Single().IdentityRoleId, Is.EqualTo(_fullAccess.Id));
        }

        [Test]
        public void The_creator_of_a_company_owned_by_someone_else_gives_only_what_he_holds()
        {
            _companies.Items[0].CreatedByGuid = _employeeGuid;

            Assert.ThrowsAsync<Authorization403ForbiddenException>(() => Invite(Employee(), _fullAccess.Id));

            AssertNothingCreated();
        }

        [Test]
        public void The_owner_cannot_give_the_platform_operator_role()
        {
            _companies.Items[0].OwnerGuid = _employeeGuid;

            Assert.ThrowsAsync<Authorization403ForbiddenException>(() => Invite(Employee(), _platformOperator.Id));

            AssertNothingCreated();
        }

        [Test]
        public async Task A_registered_user_gets_the_link_accepting_the_invitation()
        {
            _identity.Users.Add(new IdentityUser<Guid> { Id = Guid.NewGuid(), Email = InvitedEmail, EmailConfirmed = true });

            await Invite(Employee(), _gateOpener.Id);

            Assert.That(_composedEmails.Single().Method, Is.EqualTo("ComposeEmailWithInvitationToOrganization"));
            Assert.That(_sentEmails, Is.EqualTo(1));
        }

        [Test]
        public async Task An_account_without_a_password_gets_the_set_password_e_mail_again()
        {
            // Invited before, never set the password: the "accept" link needs logging in, which he cannot do.
            var account = new IdentityUser<Guid> { Id = Guid.NewGuid(), Email = InvitedEmail, EmailConfirmed = false };
            _identity.Users.Add(account);

            var result = await Invite(Employee(), _gateOpener.Id);

            var email = _composedEmails.Single();
            Assert.That(email.Method, Is.EqualTo("ComposeEmailWithUserCreationAndInvitationToOrganization"));
            Assert.That(email.Args[2], Is.EqualTo(Base64Url("confirm")), "e-mail confirmation token");
            Assert.That(email.Args[5], Is.EqualTo(Base64Url("reset")), "password reset token");
            Assert.That(_sentEmails, Is.EqualTo(1));
            Assert.That(result.UserGuid, Is.EqualTo(account.Id));
            Assert.That(_invitations.Items.Single().UserAccountGuid, Is.EqualTo(account.Id));
            Assert.That(_identity.Users, Has.Count.EqualTo(1));
        }

        [Test]
        public async Task Nothing_is_sent_when_the_application_sends_the_e_mail_itself()
        {
            // Kartony24 and Kilometrówka send their own "set the password" e-mail to such an account.
            _identity.Users.Add(new IdentityUser<Guid> { Id = Guid.NewGuid(), Email = InvitedEmail, EmailConfirmed = false });
            var invitation = Invitation(_gateOpener.Id);
            invitation.SendEmailInvitation = false;

            await CreateService(Employee()).CreateAndInviteNewUserToCompany(invitation);

            Assert.That(_sentEmails, Is.EqualTo(0));
            Assert.That(_invitations.Items, Has.Count.EqualTo(1));
        }

        private static string Base64Url(string token) =>
            WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

        private void AssertNothingCreated()
        {
            Assert.That(_identity.Users, Is.Empty, "IdentityUser");
            Assert.That(_invitations.Items, Is.Empty, "UsersInvitationsToCompanies");
            Assert.That(_userAccounts.Items, Has.Count.EqualTo(1), "UserAccount");
        }

        private CurrentUser Employee() =>
            new() { Guid = _employeeGuid, DisplayName = "Pracownik", UserRoles = Array.Empty<string>() };

        private Task<NewUserRegistrationResult> Invite(CurrentUser user, Guid? roleGuid, bool isPlatformOperator = false,
            string email = InvitedEmail)
        {
            return CreateService(user, isPlatformOperator)
                .CreateAndInviteNewUserToCompany(Invitation(roleGuid, email));
        }

        private NewUserRegistrationWithInvitationIm Invitation(Guid? roleGuid, string email = InvitedEmail) => new()
        {
            Email = email,
            CompanyGuid = _companyGuid,
            FirstName = "Jan",
            LastName = "Nowy",
            Language = "pl",
            UserRoleGuid = roleGuid,
            SendEmailInvitation = true
        };

        private NewUserRegistrationService<IdentityUser<Guid>, Company> CreateService(CurrentUser loggedInUser,
            bool isPlatformOperator = false)
        {
            var currentUserService = new FakeAspCurrentUserService(loggedInUser, isPlatformOperator);
            var permissionService = new PermissionService(currentUserService,
                new HttpContextAccessor { HttpContext = new DefaultHttpContext() }, _memberships, _companies,
                _rolePermissions, new PermissionCatalog(new[] { new TestCatalog() }),
                new MemoryCache(new MemoryCacheOptions()), NullLogger<PermissionService>.Instance);

            return new NewUserRegistrationService<IdentityUser<Guid>, Company>(
                _identity,
                _userAccounts,
                _companies,
                _companies,
                _memberships,
                NullLogger<NewUserRegistrationService<IdentityUser<Guid>, Company>>.Instance,
                currentUserService,
                Stub.Of<IRegistrationEmailComposer>(
                    ("ComposeEmailWithInvitationToOrganization", args => Composed("ComposeEmailWithInvitationToOrganization", args)),
                    ("ComposeEmailWithUserCreationAndInvitationToOrganization",
                        args => Composed("ComposeEmailWithUserCreationAndInvitationToOrganization", args))),
                new AccessControlService(_memberships, _companies, NullLogger<AccessControlService>.Instance),
                Stub.Of<IEmailSendingService>(("SendEmail", _ => { _sentEmails++; return null; })),
                Stub.Of<IPlatformSettingsService>(),
                _invitations,
                _invitations,
                _userAccounts,
                new FakeRepository<InvoiceData>(),
                Stub.Of<IConfirmationUrlParametersStore>(),
                permissionService,
                _roles);
        }

        private EmailMessage Composed(string method, object[] args)
        {
            _composedEmails.Add((method, args));
            return new EmailMessage();
        }

        private class TestCatalog : IPermissionCatalogContributor
        {
            public IEnumerable<string> GetPermissionKeys() => new[] { OpenBarrier, EditTariff };
        }
    }
}
