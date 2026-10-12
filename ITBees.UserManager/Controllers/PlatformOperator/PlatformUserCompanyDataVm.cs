using ITBees.Models.Companies;

namespace ITBees.UserManager.Controllers.PlatformOperator;

/// <summary>
/// Dane firmy zapisane w samej encji Company (to, co użytkownik wpisał w aplikacji, np. w danych
/// firmy kilometrówki) - w odróżnieniu od InvoiceData podawanych przy płatności. Faktura bywa
/// wystawiona, zanim klient poda dane do faktury, więc operator widzi je obok i może przepisać.
/// </summary>
public class PlatformUserCompanyDataVm
{
    public PlatformUserCompanyDataVm()
    {
    }

    public PlatformUserCompanyDataVm(Company company)
    {
        CompanyName = company.CompanyName;
        CompanyShortName = company.CompanyShortName;
        Nip = company.Nip;
        Street = company.Street;
        PostCode = company.PostCode;
        City = company.City;
        Email = company.Email;
    }

    public string CompanyName { get; set; }
    public string CompanyShortName { get; set; }
    public string Nip { get; set; }
    public string Street { get; set; }
    public string PostCode { get; set; }
    public string City { get; set; }
    public string Email { get; set; }
}
