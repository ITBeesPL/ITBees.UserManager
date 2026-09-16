using System;
using System.Collections.Generic;
using System.Linq;
using ITBees.Interfaces.Repository;
using ITBees.Models.Companies;
using ITBees.Models.Users;
using ITBees.RestfulApiControllers.Exceptions;
using ITBees.UserManager.Controllers.PlatformOperator;
using ITBees.UserManager.Controllers.PlatformOperator.Models;
using ITBees.UserManager.Interfaces;

namespace ITBees.UserManager.Services.PlatformOperator;

/// <summary>
/// Dane do faktury firm użytkownika edytowane przez operatora platformy (panel administracyjny,
/// szczegóły użytkownika). Encję InvoiceData mapuje i rejestruje ITBees.FAS.Payments
/// (PaymentsManagerSetup.RegisterDbModels) - host musi mieć ją zarejestrowaną w swoim DbContext.
/// </summary>
public class PlatformUserInvoiceDataService : IPlatformUserInvoiceDataService
{
    private readonly IReadOnlyRepository<UsersInCompany> _usersInCompanyRoRepo;
    private readonly IReadOnlyRepository<InvoiceData> _invoiceDataRoRepo;
    private readonly IWriteOnlyRepository<InvoiceData> _invoiceDataRwRepo;
    private readonly IReadOnlyRepository<Company> _companyRoRepo;
    private readonly IAspCurrentUserService _aspCurrentUserService;

    public PlatformUserInvoiceDataService(IReadOnlyRepository<UsersInCompany> usersInCompanyRoRepo,
        IReadOnlyRepository<InvoiceData> invoiceDataRoRepo,
        IWriteOnlyRepository<InvoiceData> invoiceDataRwRepo,
        IReadOnlyRepository<Company> companyRoRepo,
        IAspCurrentUserService aspCurrentUserService)
    {
        _usersInCompanyRoRepo = usersInCompanyRoRepo;
        _invoiceDataRoRepo = invoiceDataRoRepo;
        _invoiceDataRwRepo = invoiceDataRwRepo;
        _companyRoRepo = companyRoRepo;
        _aspCurrentUserService = aspCurrentUserService;
    }

    public List<PlatformUserCompanyInvoiceDataVm> GetForUser(Guid userGuid)
    {
        EnsurePlatformOperator();

        var companies = _usersInCompanyRoRepo
            .GetData(x => x.UserAccountGuid == userGuid, x => x.Company)
            .Where(x => x.Company != null)
            .Select(x => x.Company)
            .GroupBy(x => x.Guid)
            .Select(x => x.First())
            .OrderBy(x => x.CompanyName)
            .ToList();

        return companies
            .Select(company => new PlatformUserCompanyInvoiceDataVm(company, GetCurrentInvoiceData(company.Guid)))
            .ToList();
    }

    public PlatformUserCompanyInvoiceDataVm Update(PlatformUserInvoiceDataUm um)
    {
        EnsurePlatformOperator();

        var company = _companyRoRepo.GetData(x => x.Guid == um.CompanyGuid).FirstOrDefault()
                      ?? throw new FasApiErrorException($"Company {um.CompanyGuid} not found", 404);

        var currentUserGuid = _aspCurrentUserService.GetCurrentUserGuid();
        var existing = GetCurrentInvoiceData(company.Guid);

        if (existing == null)
        {
            var inserted = _invoiceDataRwRepo.InsertData(new InvoiceData()
            {
                CompanyGuid = company.Guid,
                CompanyName = um.CompanyName?.Trim(),
                NIP = um.Nip?.Trim(),
                Street = um.Street?.Trim(),
                PostCode = um.PostCode?.Trim(),
                City = um.City?.Trim(),
                Country = um.Country?.Trim(),
                InvoiceEmail = um.InvoiceEmail?.Trim(),
                InvoiceRequested = um.InvoiceRequested,
                IsActive = true,
                Created = DateTime.Now,
                CreatedByGuid = currentUserGuid
            });

            return new PlatformUserCompanyInvoiceDataVm(company, inserted);
        }

        // Celowo bez zmiany CompanyGuid, IsActive, SubscriptionPlanGuid ani Created - operator
        // poprawia wyłącznie dane nabywcy, a wiersz pozostaje szablonem dla odnowień.
        var updated = _invoiceDataRwRepo.UpdateData(x => x.Guid == existing.Guid, x =>
        {
            x.CompanyName = um.CompanyName?.Trim();
            x.NIP = um.Nip?.Trim();
            x.Street = um.Street?.Trim();
            x.PostCode = um.PostCode?.Trim();
            x.City = um.City?.Trim();
            x.Country = um.Country?.Trim();
            x.InvoiceEmail = um.InvoiceEmail?.Trim();
            x.InvoiceRequested = um.InvoiceRequested;
            x.ModifiedDate = DateTime.Now;
            x.ModifiedByGuid = currentUserGuid;
        }).First();

        return new PlatformUserCompanyInvoiceDataVm(company, updated);
    }

    /// <summary>
    /// Najnowszy po Created wiersz InvoiceData firmy - to samo kryterium, którym
    /// ITBees.FAS.Payments (CreateNewInvoiceBasedOnLastInvoice) wybiera szablon danych
    /// nabywcy przy odnowieniu subskrypcji.
    /// </summary>
    private InvoiceData GetCurrentInvoiceData(Guid companyGuid)
    {
        return _invoiceDataRoRepo
            .GetData(x => x.CompanyGuid == companyGuid)
            .OrderByDescending(x => x.Created)
            .FirstOrDefault();
    }

    private void EnsurePlatformOperator()
    {
        if (_aspCurrentUserService.CurrentUserIsPlatformOperator() == false)
            throw new FasApiErrorException("Current user is not platform operator", 403);
    }
}
