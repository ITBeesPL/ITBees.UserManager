using System;
using ITBees.Interfaces.Repository;
using ITBees.Models.Companies;

namespace ITBees.UserManager.Services.Acl
{
    /// <summary>
    /// The owner of a company for inviting people to it: Company.OwnerGuid or - for a company without it - its creator,
    /// member of the company or not (Kilometrówka creates companies without adding the owner to UsersInCompany).
    /// AccessControlService lets him invite and NewUserRegistrationService lets him give any role but PlatformOperator,
    /// so both checks of an invitation recognise the same owner.
    /// </summary>
    public static class CompanyOwnership
    {
        public static bool IsOwner<TCompany>(IReadOnlyRepository<TCompany> companyRoRepo, Guid companyGuid, Guid userGuid)
            where TCompany : Company
        {
            return companyRoRepo.HasData(x => x.Guid == companyGuid &&
                                              (x.OwnerGuid == userGuid ||
                                               (x.OwnerGuid == null && x.CreatedByGuid == userGuid)));
        }
    }
}
