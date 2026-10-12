using System;
using ITBees.Models.Users;
using ITBees.UserManager.Interfaces;
using ITBees.UserManager.Interfaces.Models;

namespace ITBees.UserManager.UnitTests.Fakes
{
    public class FakeAspCurrentUserService : IAspCurrentUserService
    {
        private readonly CurrentUser _currentUser;
        private readonly bool _isPlatformOperator;

        public FakeAspCurrentUserService(CurrentUser currentUser, bool isPlatformOperator = false)
        {
            _currentUser = currentUser;
            _isPlatformOperator = isPlatformOperator;
        }

        public Guid? GetCurrentUserGuid() => _currentUser?.Guid;
        public CurrentUser GetCurrentUser() => _currentUser;
        public CurrentSessionUser GetCurrentSessionUser() => throw new NotSupportedException();
        public TypeOfOperation GetMyAcceessToCompany(Guid companyGuid) => throw new NotSupportedException();
        public bool TryCanIDoForCompany(TypeOfOperation typeOfOperation, Guid companyGuid) => throw new NotSupportedException();
        public bool CurrentUserIsPlatformOperator() => _isPlatformOperator;
        public bool CurrentUserIsInRole(string role) => false;
        public bool CurrentUserIsInRole(string role, Guid companyGuid) => false;
    }
}
