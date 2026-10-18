using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ITBees.UserManager.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace ITBees.UserManager.UnitTests.Fakes
{
    /// <summary>In-memory Identity: the list plays AspNetUsers, an e-mail already in it is a "DuplicateUserName".</summary>
    public class FakeIdentity : IUserManager<IdentityUser<Guid>>
    {
        public List<IdentityUser<Guid>> Users { get; } = new();

        public Task<IdentityResult> CreateAsync(object user, string password)
        {
            var identityUser = (IdentityUser<Guid>)user;
            if (Users.Any(x => x.Email == identityUser.Email))
                return Task.FromResult(IdentityResult.Failed(new IdentityError { Code = "DuplicateUserName" }));
            identityUser.Id = Guid.NewGuid();
            Users.Add(identityUser);
            return Task.FromResult(IdentityResult.Success);
        }

        public Task<IdentityUser<Guid>> FindByEmailAsync(string email) =>
            Task.FromResult(Users.FirstOrDefault(x => x.Email == email));

        public Task<string> GenerateEmailConfirmationTokenAsync(dynamic user) => Task.FromResult("confirm");
        public Task<string> GeneratePasswordResetTokenAsync(object user) => Task.FromResult("reset");

        public Task DeleteAccount(bool leaveAccountGuidForFutureBillingInformation, Guid userGuid)
        {
            Users.RemoveAll(x => x.Id == userGuid);
            return Task.CompletedTask;
        }

        public Task<bool> CheckPasswordAsync(object user, string password) => throw new NotSupportedException();
        public Task<IdentityResult> ConfirmEmailAsync(object user, string token) => throw new NotSupportedException();
        public Task<IdentityResult> AddToRoleAsync(object user, string role) => throw new NotSupportedException();
        public Task<IList<string>> GetRolesAsync(object user) => throw new NotSupportedException();
        public Task<IdentityResult> ResetPasswordAsync(object user, string token, string newPassword) => throw new NotSupportedException();
        public Task<IdentityUser<Guid>> FindByIdAsync(string userIDGuid) => throw new NotSupportedException();
        public Task<IdentityResult> ChangePasswordAsync(object user, string currentPass, string newPass) => throw new NotSupportedException();
        public Task<IdentityUser<Guid>> FindByLoginAsync(string loginProvider, string providerKey) => throw new NotSupportedException();
        public Task<IdentityResult> AddLoginAsync(object user, string loginProvider, string providerKey) => throw new NotSupportedException();
    }
}
