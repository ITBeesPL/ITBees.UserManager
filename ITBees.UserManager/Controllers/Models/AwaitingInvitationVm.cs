using System;
using ITBees.Models.Users;

namespace ITBees.UserManager.Controllers.Models
{
    public class AwaitingInvitationVm
    {
        public AwaitingInvitationVm(UsersInvitationsToCompanies x)
        {
            CompanyName = x.Company.CompanyName;
            CreatedDate = x.CreatedDate;
            this.Guid = x.Guid;
            CreatedByDisplayName = x.CreatedBy.DisplayName;
        }

        public string CreatedByDisplayName { get; set; }

        public Guid Guid { get; set; }

        public DateTime CreatedDate { get; set; }

        public string CompanyName { get; set; }
    }
}