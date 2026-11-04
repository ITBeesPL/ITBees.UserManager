using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using ITBees.Interfaces.Platforms;
using ITBees.Mailing.Interfaces;
using ITBees.Models.EmailMessages;
using ITBees.Models.Languages;
using ITBees.Translations;
using ITBees.UserManager.Interfaces;
using ITBees.UserManager.Interfaces.Models;
using ITBees.UserManager.Services.Mailing;
using ITBees.UserManager.UnitTests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
// ITBees.Models.Languages has a language class called Is (Icelandic).
using Is = NUnit.Framework.Is;

namespace ITBees.UserManager.UnitTests
{
    /// <summary>
    /// Where the links of the invitation e-mails point when the caller passes no page address. Up to 8.0.413 they
    /// pointed at the API itself (DefaultApiUrl): the link of a registered user hit GET /AcceptInvitation, which
    /// needs the token of the logged-in user since 8.0.412, so a click in the e-mail got a 401 - from every
    /// application inviting through POST /NewInvitedUserRegistration, which passes no address.
    /// </summary>
    public class RegistrationEmailComposerTests
    {
        private const string Email = "zaproszony@firma.pl";
        private const string EmailInLink = "zaproszony%40firma.pl";

        /// <summary>
        /// The templates of the two invitation e-mails, as the i18n files of an application hold them (Translate answers
        /// only from loaded files), reduced to the link.
        /// </summary>
        private const string EmailTemplates = @"{""NewUserRegistrationEmail"": {
            ""ITBees.UserManager.Translations.NewUserRegistrationEmail.ComposeEmailWithInvitationToOrganizationSubject"": ""[[INVITING_NAME]] invites you to [[COMPANY_NAME]]"",
            ""ITBees.UserManager.Translations.NewUserRegistrationEmail.ComposeEmailWithInvitationToOrganizationBody"": ""<a href=\""[[EMAIL_CONFIRMATION_URL]][[CONFIRMATION_PARAMETERS]]\"">link</a>"",
            ""ITBees.UserManager.Translations.NewUserRegistrationEmail.ComposeEmailWithUserCreationAndInvitationToOrganizationSubject"": ""Invitation to [[COMPANY_NAME]]"",
            ""ITBees.UserManager.Translations.NewUserRegistrationEmail.ComposeEmailWithUserCreationAndInvitationToOrganizationBody"": ""<a href=\""[[EMAIL_CONFIRMATION_URL]][[CONFIRMATION_PARAMETERS]]\"">link</a>""
        }}";

        private readonly Guid _companyGuid = Guid.NewGuid();

        private string _translationsPath;
        private string _applicationSiteUrl;
        private Dictionary<string, string> _platformSettings;

        [SetUp]
        public void SetUp()
        {
            // The e-mail templates come from the i18n files of the application - here the four of the invitations.
            _translationsPath = Path.Combine(Path.GetTempPath(), "email-composer-tests-" + Guid.NewGuid());
            Directory.CreateDirectory(_translationsPath);
            File.WriteAllText(Path.Combine(_translationsPath, "en.json"), EmailTemplates);
            Translate.ClearTranslations();
            Translate.LoadFiles(_translationsPath);

            _applicationSiteUrl = "https://app.test/";
            _platformSettings = new Dictionary<string, string> { ["DefaultApiUrl"] = "https://api.test" };
        }

        [TearDown]
        public void TearDown()
        {
            Translate.ClearTranslations();
            Directory.Delete(_translationsPath, true);
        }

        private RegistrationEmailComposer Composer() => new(
            Stub.Of<IEmailSendingService>(),
            NullLogger<RegistrationEmailComposer>.Instance,
            Stub.Of<IUserManagerSettings>(
                ("get_APPLICATION_SITE_URL", _ => _applicationSiteUrl),
                ("get_PLATFORM_NAME", _ => "Platform")),
            Stub.Of<IPlatformSettingsService>(
                ("GetSetting", args => _platformSettings.GetValueOrDefault((string)args[0]))));

        private NewUserRegistrationWithInvitationIm Invitation() => new()
        {
            Email = Email, CompanyGuid = _companyGuid, Language = "en"
        };

        /// <summary>The e-mail of a registered user: the link opens the page accepting the invitation.</summary>
        private string LinkOfRegisteredUserEmail(string baseLink = "") => Href(Composer()
            .ComposeEmailWithInvitationToOrganization(Invitation(), "Firma", "Jan", new En(), baseLink));

        /// <summary>The e-mail of a new or never confirmed account: the link opens the page setting the password.</summary>
        private string LinkOfSetPasswordEmail(string baseLink = "") => Href(Composer()
            .ComposeEmailWithUserCreationAndInvitationToOrganization(Invitation(), "Firma", "emailToken", new En(),
                baseLink, "passwordToken"));

        private static string Href(EmailMessage email) =>
            Regex.Match(email.BodyHtml, "href=\"([^\"]+)\"").Groups[1].Value;

        [Test]
        public void The_address_the_caller_passes_wins()
        {
            Assert.That(LinkOfRegisteredUserEmail("https://my.app/invitations"), Does.StartWith(
                $"https://my.app/invitations/acceptInvitation?emailInvitation=true&email={EmailInLink}&companyGuid={_companyGuid}"));
            Assert.That(LinkOfSetPasswordEmail("https://my.app/setPassword"), Does.StartWith(
                $"https://my.app/setPassword?emailInvitation=true&token=emailToken&email={EmailInLink}&tokenAuth=passwordToken"));
        }

        [Test]
        public void Without_an_address_the_links_open_the_fas_auth_pages_of_the_application()
        {
            Assert.That(LinkOfRegisteredUserEmail(), Does.StartWith(
                $"https://app.test/auth/acceptInvitationExistingUser/acceptInvitation?emailInvitation=true&email={EmailInLink}&companyGuid={_companyGuid}"));
            Assert.That(LinkOfSetPasswordEmail(), Does.StartWith(
                "https://app.test/auth/acceptInvitation?emailInvitation=true&token=emailToken"));
        }

        [Test]
        public void The_site_url_works_without_the_trailing_slash_too()
        {
            // Octopark has "https://operator.octopark.net", Kilometrówka "http://localhost:4200/".
            _applicationSiteUrl = "https://app.test";

            Assert.That(LinkOfSetPasswordEmail(), Does.StartWith("https://app.test/auth/acceptInvitation?"));
            Assert.That(LinkOfRegisteredUserEmail(),
                Does.StartWith("https://app.test/auth/acceptInvitationExistingUser/acceptInvitation?"));
        }

        [Test]
        public void An_application_names_its_own_pages_in_the_platform_settings()
        {
            _platformSettings[RegistrationEmailComposer.AcceptInvitationUrlSetting] = "https://operator.test/join";
            _platformSettings[RegistrationEmailComposer.AcceptInvitationExistingUserUrlSetting] = "https://operator.test/invitation ";

            Assert.That(LinkOfSetPasswordEmail(), Does.StartWith("https://operator.test/join?emailInvitation=true"));
            Assert.That(LinkOfRegisteredUserEmail(), Does.StartWith("https://operator.test/invitation/acceptInvitation?"));
        }

        [Test]
        public void Without_a_site_url_the_api_address_is_used_as_before()
        {
            _applicationSiteUrl = null;

            Assert.That(LinkOfRegisteredUserEmail(), Does.StartWith("https://api.test/acceptInvitation?"));
            Assert.That(LinkOfSetPasswordEmail(), Does.StartWith("https://api.test?emailInvitation=true"));
        }
    }
}
