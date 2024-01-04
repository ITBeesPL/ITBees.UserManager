using System.Collections.Generic;
using ITBees.Models.Companies;

namespace ITBees.UserManager.Interfaces.Services
{
    public interface IMyCompaniesService
    {
        IEnumerable<Company> GetMyCompaniesQuerable();
    }
}