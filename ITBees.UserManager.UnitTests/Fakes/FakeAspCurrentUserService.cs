using System;
using ITBees.Models.Users;
using ITBees.UserManager.Interfaces;
using ITBees.UserManager.Interfaces.Models;

namespace ITBees.UserManager.UnitTests.Fakes
{
    public class FakeAspCurrentUserService : IAspCurrentUserService
    {
        private readonly CurrentUser _currentUser;

        public FakeAspCurrentUserService(CurrentUser currentUser)
        {
            _currentUser = currentUser;
        }

        public Guid? GetCurrentUserGuid() => _currentUser?.Guid;
        public CurrentUser GetCurrentUser() => _currentUser;
        public CurrentSessionUser GetCurrentSessionUser() => throw new NotSupportedException();
        public TypeOfOperation GetMyAcceessToCompany(Guid companyGuid) => throw new NotSupportedException();
        public bool TryCanIDoForCompany(TypeOfOperation typeOfOperation, Guid companyGuid) => throw new NotSupportedException();
        public bool CurrentUserIsPlatformOperator() => false;
        public bool CurrentUserIsInRole(string role) => false;
        public bool CurrentUserIsInRole(string role, Guid companyGuid) => false;
    }
}
