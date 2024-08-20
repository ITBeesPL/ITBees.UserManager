using System.Collections.Generic;
using ITBees.Models.Companies;

namespace ITBees.UserManager.Interfaces
{
    public interface IMyCompaniesService
    {
        IEnumerable<Company> GetMyCompaniesQueryable();
    }
}