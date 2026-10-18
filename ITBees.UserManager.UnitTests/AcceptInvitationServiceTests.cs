using System;
using System.Linq;
using ITBees.Models.Users;
using ITBees.UserManager.Services;
using ITBees.UserManager.UnitTests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
// ITBees.Models.Languages has a language class called Is (Icelandic).
using Is = NUnit.Framework.Is;

namespace ITBees.UserManager.UnitTests
{
    /// <summary>
    /// GET /AcceptInvitation?email&amp;companyGuid accepted the invitation of any e-mail without logging in - the link
    /// holds nothing secret, so anybody who knew the address and the company could make that person a member with the
    /// role of the invitation. Now the logged-in user accepts only his own invitation.
    /// </summary>
    public class AcceptInvitationServiceTests
    {
        private const string InvitedEmail = "zaproszony@firma.pl";

        private readonly Guid _companyGuid = Guid.NewGuid();
        private readonly Guid _invitedGuid = Guid.NewGuid();
        private readonly Guid _inviterGuid = Guid.NewGuid();
        private readonly Guid _roleGuid = Guid.NewGuid();

        private FakeRepository<UsersInvitationsToCompanies> _invitations;
        private FakeRepository<UsersInCompany> _memberships;

        [SetUp]
        public void SetUp()
        {
            _invitations = new FakeRepository<UsersInvitationsToCompanies>(new UsersInvitationsToCompanies
            {
                UserAccountGuid = _invitedGuid,
                UserAccount = new UserAccount { Guid = _invitedGuid, Email = InvitedEmail },
                CompanyGuid = _companyGuid,
                CreatedByGuid = _inviterGuid,
                IdentityRoleId = _roleGuid
            });
            _memberships = new FakeRepository<UsersInCompany>();
        }

        private AcceptInvitationService CreateService(CurrentUser loggedInUser) =>
            new(_invitations, _memberships, new FakeAspCurrentUserService(loggedInUser),
                NullLogger<AcceptInvitationService>.Instance);

        private CurrentUser LoggedIn(Guid guid, string email) => new() { Guid = guid, Email = email };

        [Test]
        public void The_invited_user_accepts_his_invitation()
        {
            var result = CreateService(LoggedIn(_invitedGuid, InvitedEmail)).Accept(true, InvitedEmail, _companyGuid);

            Assert.That(result.Accepted, Is.True);
            Assert.That(_invitations.Items.Single().Applied, Is.True);
            var membership = _memberships.Items.Single();
            Assert.That(membership.UserAccountGuid, Is.EqualTo(_invitedGuid));
            Assert.That(membership.CompanyGuid, Is.EqualTo(_companyGuid));
            Assert.That(membership.IdentityRoleId, Is.EqualTo(_roleGuid));
            Assert.That(membership.AddedByGuid, Is.EqualTo(_inviterGuid));
        }

        [Test]
        public void The_e_mail_of_the_link_is_compared_without_case()
        {
            var result = CreateService(LoggedIn(_invitedGuid, InvitedEmail)).Accept(true, " Zaproszony@Firma.pl ", _companyGuid);

            Assert.That(result.Accepted, Is.True);
        }

        [Test]
        public void Nobody_accepts_without_logging_in()
        {
            Assert.Throws<UnauthorizedAccessException>(() =>
                CreateService(null).Accept(true, InvitedEmail, _companyGuid));

            AssertNothingAccepted();
        }

        [Test]
        public void Another_user_cannot_accept_the_invitation_of_someone_else()
        {
            var result = CreateService(LoggedIn(Guid.NewGuid(), "ktos@inny.pl")).Accept(true, InvitedEmail, _companyGuid);

            Assert.That(result.Accepted, Is.False);
            Assert.That(result.Message, Does.Contain(InvitedEmail));
            AssertNothingAccepted();
        }

        [Test]
        public void Another_user_cannot_accept_it_by_leaving_the_e_mail_out()
        {
            var result = CreateService(LoggedIn(Guid.NewGuid(), "ktos@inny.pl")).Accept(true, null, _companyGuid);

            Assert.That(result.Accepted, Is.False);
            AssertNothingAccepted();
        }

        [Test]
        public void An_invitation_to_another_company_is_not_accepted()
        {
            var result = CreateService(LoggedIn(_invitedGuid, InvitedEmail)).Accept(true, InvitedEmail, Guid.NewGuid());

            Assert.That(result.Accepted, Is.False);
            AssertNothingAccepted();
        }

        [Test]
        public void An_accepted_invitation_is_not_accepted_twice()
        {
            CreateService(LoggedIn(_invitedGuid, InvitedEmail)).Accept(true, InvitedEmail, _companyGuid);

            var result = CreateService(LoggedIn(_invitedGuid, InvitedEmail)).Accept(true, InvitedEmail, _companyGuid);

            Assert.That(result.Accepted, Is.False);
            Assert.That(_memberships.Items, Has.Count.EqualTo(1));
        }

        private void AssertNothingAccepted()
        {
            Assert.That(_invitations.Items.Single().Applied, Is.False, "UsersInvitationsToCompanies.Applied");
            Assert.That(_memberships.Items, Is.Empty, "UsersInCompany");
        }
    }
}
