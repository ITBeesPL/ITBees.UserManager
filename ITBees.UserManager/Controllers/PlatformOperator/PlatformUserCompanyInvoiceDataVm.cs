using System;
using ITBees.Models.Companies;

namespace ITBees.UserManager.Controllers.PlatformOperator;

/// <summary>
/// Dane do faktury jednej firmy użytkownika (użytkownik może należeć do wielu firm) -
/// edytowane przez operatora platformy w szczegółach użytkownika w panelu administracyjnym.
/// </summary>
public class PlatformUserCompanyInvoiceDataVm
{
    public PlatformUserCompanyInvoiceDataVm()
    {
    }

    public PlatformUserCompanyInvoiceDataVm(Company company, InvoiceData invoiceData)
    {
        CompanyGuid = company.Guid;
        CompanyDisplayName = company.CompanyName;

        if (invoiceData == null) return;

        InvoiceDataGuid = invoiceData.Guid;
        CompanyName = invoiceData.CompanyName;
        Nip = invoiceData.NIP;
        Street = invoiceData.Street;
        PostCode = invoiceData.PostCode;
        City = invoiceData.City;
        Country = invoiceData.Country;
        InvoiceEmail = invoiceData.InvoiceEmail;
        InvoiceRequested = invoiceData.InvoiceRequested;
        Modified = invoiceData.ModifiedDate ?? invoiceData.Created;
    }

    /// <summary>Firma, której dotyczą dane (nazwa z encji Company - do nagłówka sekcji).</summary>
    public Guid CompanyGuid { get; set; }

    public string CompanyDisplayName { get; set; }

    /// <summary>Edytowany wiersz InvoiceData; null, gdy firma nie ma jeszcze żadnych danych do faktury.</summary>
    public Guid? InvoiceDataGuid { get; set; }

    public string CompanyName { get; set; }
    public string Nip { get; set; }
    public string Street { get; set; }
    public string PostCode { get; set; }
    public string City { get; set; }
    public string Country { get; set; }
    public string InvoiceEmail { get; set; }
    public bool InvoiceRequested { get; set; }
    public DateTime? Modified { get; set; }
}
