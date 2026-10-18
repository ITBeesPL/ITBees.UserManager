using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Mail;
using System.Reflection;
using System.Threading.Tasks;
using ITBees.Interfaces.Platforms;
using ITBees.Interfaces.Repository;
using ITBees.Mailing.Interfaces;
using ITBees.Models.Companies;
using ITBees.Models.EmailMessages;
using ITBees.Models.Users;
using ITBees.Translations;
using ITBees.UserManager.DbModels;
using ITBees.UserManager.Interfaces;
using ITBees.UserManager.Interfaces.Models;
using ITBees.UserManager.Permissions;
using ITBees.UserManager.Services;
using ITBees.UserManager.Services.Acl;
using ITBees.UserManager.Services.Mailing;
using ITBees.UserManager.Services.Registration;
using ITBees.UserManager.UnitTests.Fakes;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
// ITBees.Models.Languages has a language class called Is (Icelandic).
using Is = NUnit.Framework.Is;

namespace ITBees.UserManager.UnitTests
{
    /// <summary>
    /// A registration whose invitation e-mail the mail server refuses is rolled back. UserAccount.LastUsedCompanyGuid
    /// points at the new company and Company.CreatedByGuid / OwnerGuid point back at the account, so the rollback
    /// has to break that cycle first - otherwise MySQL refuses both deletes and the account stays behind without a
    /// login, blocking its e-mail address for good. The tables below refuse deletes the same way the database does.
    /// </summary>
    public class NewUserRegistrationRollbackTests
    {
        private const string Email = "k@skb.pl";

        private GuardedRepository<UserAccount> _userAccounts;
        private GuardedRepository<Company> _companies;
        private GuardedRepository<UsersInCompany> _usersInCompany;
        private GuardedRepository<InvoiceData> _invoiceData;
        private GuardedRepository<UserConfirmationUrlParameters> _confirmationUrlParameters;
        private FakeIdentity _identity;
        private Exception _sendFailure;
        private string _translationsPath;

        [SetUp]
        public void SetUp()
        {
            _translationsPath = Path.Combine(Path.GetTempPath(), "registration-rollback-tests-" + Guid.NewGuid());
            Directory.CreateDirectory(_translationsPath);
            File.WriteAllText(Path.Combine(_translationsPath, "en.json"), "{}");
            // As in PrawkoBe: the Polish text is what the student saw for k@skb.pl.
            File.WriteAllText(Path.Combine(_translationsPath, "pl.json"),
                "{\"UserManager\": {\"NewUserRegistration\": {\"Errors\": {" +
                "\"ITBees.UserManager.Translations.UserManager.NewUserRegistration.Errors.InvalidEmail\": \"Nieprawidłowy e-mail\"}}}}");
            Translate.ClearTranslations();
            Translate.LoadFiles(_translationsPath);

            _userAccounts = new GuardedRepository<UserAccount>(account =>
                _companies.Items.Any(c => c.CreatedByGuid == account.Guid || c.OwnerGuid == account.Guid) ? "Company" :
                _usersInCompany.Items.Any(u => u.UserAccountGuid == account.Guid || u.AddedByGuid == account.Guid) ? "UsersInCompany" :
                _invoiceData.Items.Any(i => i.CreatedByGuid == account.Guid) ? "InvoiceData" :
                _confirmationUrlParameters.Items.Any(p => p.UserAccountGuid == account.Guid) ? "UserConfirmationUrlParameters" :
                null);
            _companies = new GuardedRepository<Company>(company =>
                _userAccounts.Items.Any(a => a.LastUsedCompanyGuid == company.Guid) ? "UserAccount" :
                _usersInCompany.Items.Any(u => u.CompanyGuid == company.Guid) ? "UsersInCompany" :
                _invoiceData.Items.Any(i => i.CompanyGuid == company.Guid) ? "InvoiceData" :
                null);
            _usersInCompany = new GuardedRepository<UsersInCompany>(_ => null);
            _invoiceData = new GuardedRepository<InvoiceData>(_ => null);
            _confirmationUrlParameters = new GuardedRepository<UserConfirmationUrlParameters>(_ => null);
            _identity = new FakeIdentity();
            _sendFailure = new SmtpFailedRecipientException(SmtpStatusCode.MailboxUnavailable, Email,
                "5.1.1 <k@skb.pl>: Recipient address rejected: User unknown in virtual mailbox table");
        }

        [TearDown]
        public void TearDown()
        {
            Translate.ClearTranslations();
            Directory.Delete(_translationsPath, true);
        }

        [Test]
        public void The_tables_refuse_a_delete_like_the_database_does()
        {
            var account = new UserAccount { Guid = Guid.NewGuid(), Email = Email };
            var company = new Company { Guid = Guid.NewGuid(), CreatedByGuid = account.Guid, OwnerGuid = account.Guid };
            account.LastUsedCompanyGuid = company.Guid;
            _userAccounts.InsertData(account);
            _companies.InsertData(company);

            Assert.Throws<InvalidOperationException>(() => _companies.DeleteData(x => x.Guid == company.Guid));
            Assert.Throws<InvalidOperationException>(() => _userAccounts.DeleteData(x => x.Guid == account.Guid));
        }

        [Test]
        public void A_refused_mailbox_leaves_nothing_behind()
        {
            var error = Assert.ThrowsAsync<ArgumentException>(() => Register());

            Assert.That(error.Message, Is.EqualTo("Nieprawidłowy e-mail : " + Email));
            Assert.That(error.InnerException, Is.SameAs(_sendFailure));
            AssertNothingLeft();
        }

        [Test]
        public void A_refused_mailbox_also_removes_the_invoice_data_and_the_confirmation_url_parameters()
        {
            Assert.ThrowsAsync<ArgumentException>(() => Register(
                new AdditionalInvoiceDataIm { CompanyName = "Nowak", City = "Wrocław", InvoiceEmail = Email },
                new List<ConfirmationUrlParameterIm> { new("returnUrl", "/app/osk") }));

            AssertNothingLeft();
        }

        [Test]
        public async Task The_address_can_register_again_after_a_refused_mailbox()
        {
            Assert.ThrowsAsync<ArgumentException>(() => Register());
            _sendFailure = null;

            var result = await Register();

            Assert.That(_userAccounts.Items.Single().Guid, Is.EqualTo(result.UserGuid));
            Assert.That(_companies.Items, Has.Count.EqualTo(1));
        }

        private void AssertNothingLeft()
        {
            Assert.That(_userAccounts.Items, Is.Empty, "UserAccount");
            Assert.That(_companies.Items, Is.Empty, "Company");
            Assert.That(_usersInCompany.Items, Is.Empty, "UsersInCompany");
            Assert.That(_invoiceData.Items, Is.Empty, "InvoiceData");
            Assert.That(_confirmationUrlParameters.Items, Is.Empty, "UserConfirmationUrlParameters");
            Assert.That(_identity.Users, Is.Empty, "IdentityUser");
        }

        private Task<NewUserRegistrationResult> Register(
            AdditionalInvoiceDataIm invoiceData = null,
            List<ConfirmationUrlParameterIm> confirmationUrlParameters = null)
        {
            var service = new NewUserRegistrationService<IdentityUser<Guid>, Company>(
                _identity,
                _userAccounts,
                _companies,
                _companies,
                _usersInCompany,
                NullLogger<NewUserRegistrationService<IdentityUser<Guid>, Company>>.Instance,
                new FakeAspCurrentUserService(null),
                Stub.Of<IRegistrationEmailComposer>(("ComposeEmailConfirmation", _ => new EmailMessage())),
                Stub.Of<IAccessControlService>(),
                Stub.Of<IEmailSendingService>(("SendEmail", _ => _sendFailure == null ? (object)null : throw _sendFailure)),
                Stub.Of<IPlatformSettingsService>(),
                new FakeRepository<UsersInvitationsToCompanies>(),
                new FakeRepository<UsersInvitationsToCompanies>(),
                _userAccounts,
                _invoiceData,
                new ConfirmationUrlParametersStore(_confirmationUrlParameters, _confirmationUrlParameters,
                    NullLogger<ConfirmationUrlParametersStore>.Instance),
                Stub.Of<IPermissionService>(),
                new FakeRepository<FasIdentityRole>());

            return service.CreateNewUser(new NewUserRegistrationIm
            {
                Email = Email,
                FirstName = "Czesław",
                LastName = "Nowak",
                CompanyName = "Private",
                Language = "pl",
                Phone = "796233281",
                ConfirmationUrlParameters = confirmationUrlParameters
            }, true, invoiceData);
        }

        /// <summary>
        /// The fake repository with the foreign keys of the real tables: a row that another row still points at
        /// cannot be deleted, like MySQL's "Cannot delete or update a parent row".
        /// </summary>
        private class GuardedRepository<T> : FakeRepository<T>, IWriteOnlyRepository<T> where T : class
        {
            private readonly Func<T, string> _referencedBy;

            public GuardedRepository(Func<T, string> referencedBy)
            {
                _referencedBy = referencedBy;
            }

            public new int DeleteData(Expression<Func<T, bool>> predicate)
            {
                var matching = Items.Where(predicate.Compile()).ToList();
                foreach (var row in matching)
                {
                    var referencedBy = _referencedBy(row);
                    if (referencedBy != null)
                        throw new InvalidOperationException(
                            $"Cannot delete or update a parent row: a foreign key constraint fails ({referencedBy} -> {typeof(T).Name})");
                }

                Items.RemoveAll(matching.Contains);
                return matching.Count;
            }

            int IWriteOnlyRepository<T>.DeleteData(Expression<Func<T, bool>> predicate) => DeleteData(predicate);

            void IWriteOnlyRepository<T>.DeleteData(Expression<Func<T, bool>> predicate,
                params Expression<Func<T, object>>[] includeProperties) => DeleteData(predicate);
        }
    }
}
